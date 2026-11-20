using BloodLink.Data;
using BloodLink.Models.Entities;

namespace BloodLink.Helpers
{
    // The rest rule after a donation, in one place:
    //   donate → not available → 3 months later → available again (automatically).
    public static class DonationRules
    {
        public const int RestMonths = 3;

        private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

        // Call this every time a donation is recorded for a donor.
        public static void MarkDonated(User donor, DateOnly donationDate)
        {
            if (donor.LastDonationDate == null || donationDate > donor.LastDonationDate)
                donor.LastDonationDate = donationDate;

            var availableAgain = donor.LastDonationDate.Value.AddMonths(RestMonths);
            if (availableAgain > Today)
            {
                donor.IsAvailable = false;
                donor.AvailableAgainOn = availableAgain;
            }
        }

        // Is the donor still in the 3-month rest period?
        public static bool IsResting(User donor)
        {
            return donor.AvailableAgainOn != null && donor.AvailableAgainOn > Today;
        }

        // Used after "undo donation": calculate the rest period again from the donations that are left.
        public static void Recalculate(User donor, BloodLinkDbContext db)
        {
            var last = db.DonationRecords
                .Where(r => r.DonorId == donor.Id)
                .OrderByDescending(r => r.DonationDate)
                .Select(r => (DateOnly?)r.DonationDate)
                .FirstOrDefault();

            donor.LastDonationDate = last;
            if (last != null && last.Value.AddMonths(RestMonths) > Today)
            {
                donor.IsAvailable = false;
                donor.AvailableAgainOn = last.Value.AddMonths(RestMonths);
            }
            else if (donor.AvailableAgainOn != null)
            {
                donor.IsAvailable = true;
                donor.AvailableAgainOn = null;
            }
        }

        // Turns back on every donor whose rest period is over. Returns how many were turned on.
        public static int ReactivateDonors(BloodLinkDbContext db)
        {
            var today = Today;
            var donors = db.Users
                .Where(u => u.AvailableAgainOn != null && u.AvailableAgainOn <= today)
                .ToList();

            foreach (var donor in donors)
            {
                donor.IsAvailable = true;
                donor.AvailableAgainOn = null;
            }
            db.SaveChanges();
            return donors.Count;
        }
    }

    // Runs in the background while the website is running:
    // when the app starts and then every hour, it turns back on the donors whose 3 months are over.
    public class DonorReactivationService : BackgroundService
    {
        private readonly IServiceProvider services;
        private readonly ILogger<DonorReactivationService> logger;

        public DonorReactivationService(IServiceProvider services, ILogger<DonorReactivationService> logger)
        {
            this.services = services;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // A background service has no request, so we create a "scope" to get a DbContext.
                using (var scope = services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<BloodLinkDbContext>();
                    int count = DonationRules.ReactivateDonors(db);
                    if (count > 0) logger.LogInformation("{Count} donor(s) are available again.", count);
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
