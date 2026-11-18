using BloodLink.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BloodLink.Data
{
    public class BloodLinkDbContext : DbContext
    {
        public BloodLinkDbContext(DbContextOptions<BloodLinkDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<OrganizationDocument> OrganizationDocuments => Set<OrganizationDocument>();
        public DbSet<BloodInventory> BloodInventory => Set<BloodInventory>();
        public DbSet<BloodRequest> BloodRequests => Set<BloodRequest>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<ChatReadState> ChatReadStates => Set<ChatReadState>();
        public DbSet<DonationAppointment> DonationAppointments => Set<DonationAppointment>();
        public DbSet<DonationRecord> DonationRecords => Set<DonationRecord>();
        public DbSet<Certificate> Certificates => Set<Certificate>();
        public DbSet<Hall> Halls => Set<Hall>();
        public DbSet<HallBooking> HallBookings => Set<HallBooking>();
        public DbSet<HallBookingDocument> HallBookingDocuments => Set<HallBookingDocument>();
        public DbSet<Advertisement> Advertisements => Set<Advertisement>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<ReportAttachment> ReportAttachments => Set<ReportAttachment>();
        public DbSet<AdminActivityLog> AdminActivityLogs => Set<AdminActivityLog>();

        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
            // Store every enum as readable text instead of a number.
            builder.Properties<UserRole>().HaveConversion<string>().HaveMaxLength(20);
            builder.Properties<AccountStatus>().HaveConversion<string>().HaveMaxLength(20);
            builder.Properties<OrganizationType>().HaveConversion<string>().HaveMaxLength(30);
            builder.Properties<OrganizationStatus>().HaveConversion<string>().HaveMaxLength(20);
            builder.Properties<RequestPriority>().HaveConversion<string>().HaveMaxLength(20);
            builder.Properties<BloodRequestStatus>().HaveConversion<string>().HaveMaxLength(20);
            builder.Properties<ChatSender>().HaveConversion<string>().HaveMaxLength(20);
            builder.Properties<AppointmentStatus>().HaveConversion<string>().HaveMaxLength(20);
            builder.Properties<EligibilityState>().HaveConversion<string>().HaveMaxLength(20);
            builder.Properties<DonationRecordStatus>().HaveConversion<string>().HaveMaxLength(30);
            builder.Properties<HallBookingStatus>().HaveConversion<string>().HaveMaxLength(20);
            builder.Properties<AdPlan>().HaveConversion<string>().HaveMaxLength(20);
            builder.Properties<ReportPriority>().HaveConversion<string>().HaveMaxLength(20);
            builder.Properties<ReportStatus>().HaveConversion<string>().HaveMaxLength(20);

            // Blood types are stored the way people write them: "A+", "O-", ...
            builder.Properties<BloodType>().HaveConversion<BloodTypeConverter>().HaveMaxLength(3);

            builder.Properties<decimal>().HavePrecision(10, 2);
        }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // SQL Server does not allow several cascade paths to the same table,
            // so by default deleting a parent is blocked; cascades are enabled only where safe.
            foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
                fk.DeleteBehavior = DeleteBehavior.Restrict;

            // ---------- Users ----------
            b.Entity<User>(e =>
            {
                e.HasIndex(u => u.Email).IsUnique();
                e.HasIndex(u => new { u.Role, u.BloodType, u.City });
                e.HasOne(u => u.Organization).WithMany(o => o.Staff)
                    .HasForeignKey(u => u.OrganizationId).OnDelete(DeleteBehavior.SetNull);
            });

            // ---------- Organizations ----------
            b.Entity<Organization>(e =>
            {
                e.HasIndex(o => o.Name);
                e.HasIndex(o => o.Status);
            });
            b.Entity<OrganizationDocument>()
                .HasOne(d => d.Organization).WithMany(o => o.Documents)
                .HasForeignKey(d => d.OrganizationId).OnDelete(DeleteBehavior.Cascade);

            b.Entity<BloodInventory>(e =>
            {
                e.HasIndex(i => new { i.OrganizationId, i.BloodType }).IsUnique();
                e.HasOne(i => i.Organization).WithMany(o => o.Inventory)
                    .HasForeignKey(i => i.OrganizationId).OnDelete(DeleteBehavior.Cascade);
                e.ToTable(t => t.HasCheckConstraint("CK_BloodInventory_Units", "[Units] >= 0"));
            });

            // ---------- Blood requests + chat ----------
            b.Entity<BloodRequest>(e =>
            {
                e.HasOne(r => r.Recipient).WithMany(u => u.SentRequests).HasForeignKey(r => r.RecipientId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(r => r.Donor).WithMany(u => u.ReceivedRequests).HasForeignKey(r => r.DonorId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(r => r.Hospital).WithMany().HasForeignKey(r => r.HospitalId);
                e.HasIndex(r => new { r.DonorId, r.Status });
                e.HasIndex(r => new { r.RecipientId, r.Status });
                e.ToTable(t => t.HasCheckConstraint("CK_BloodRequests_Units", "[Units] > 0"));
            });
            b.Entity<ChatMessage>(e =>
            {
                e.HasOne(m => m.BloodRequest).WithMany(r => r.Messages)
                    .HasForeignKey(m => m.BloodRequestId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(m => new { m.BloodRequestId, m.SentAt });
            });
            b.Entity<ChatReadState>(e =>
            {
                e.HasOne(s => s.BloodRequest).WithMany(r => r.ReadStates)
                    .HasForeignKey(s => s.BloodRequestId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(s => new { s.BloodRequestId, s.UserId }).IsUnique();
            });

            // ---------- Appointments & donations ----------
            b.Entity<DonationAppointment>(e =>
            {
                e.HasOne(a => a.Donor).WithMany(u => u.Appointments).HasForeignKey(a => a.DonorId);
                e.HasOne(a => a.Organization).WithMany(o => o.Appointments).HasForeignKey(a => a.OrganizationId);
                e.Property(a => a.WeightKg).HasPrecision(5, 1);
                e.HasIndex(a => new { a.OrganizationId, a.Status, a.RequestedDate });
            });
            b.Entity<DonationRecord>(e =>
            {
                e.HasOne(r => r.Organization).WithMany(o => o.DonationRecords).HasForeignKey(r => r.OrganizationId);
                e.HasOne(r => r.Donor).WithMany(u => u.Donations).HasForeignKey(r => r.DonorId);
                e.HasIndex(r => new { r.OrganizationId, r.DonationDate });
                e.ToTable(t => t.HasCheckConstraint("CK_DonationRecords_Units", "[Units] > 0"));
            });
            b.Entity<Certificate>(e =>
            {
                e.HasIndex(c => c.Code).IsUnique();
                e.HasOne(c => c.User).WithMany(u => u.Certificates)
                    .HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- Halls ----------
            b.Entity<HallBooking>(e =>
            {
                e.HasOne(h => h.Hall).WithMany(h => h.Bookings).HasForeignKey(h => h.HallId);
                e.HasOne(h => h.RequestedBy).WithMany().HasForeignKey(h => h.RequestedById).OnDelete(DeleteBehavior.SetNull);
                e.HasIndex(h => h.Status);
            });
            b.Entity<HallBookingDocument>()
                .HasOne(d => d.HallBooking).WithMany(h => h.Documents)
                .HasForeignKey(d => d.HallBookingId).OnDelete(DeleteBehavior.Cascade);

            // ---------- Ads ----------
            b.Entity<Advertisement>(e =>
            {
                e.HasOne(a => a.Organization).WithMany(o => o.Advertisements)
                    .HasForeignKey(a => a.OrganizationId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(a => new { a.StartDate, a.EndDate });
            });

            // ---------- Reports / admin ----------
            b.Entity<Report>(e =>
            {
                e.HasOne(r => r.Reporter).WithMany().HasForeignKey(r => r.ReporterId).OnDelete(DeleteBehavior.SetNull);
                e.HasIndex(r => new { r.Status, r.CreatedAt });
            });
            b.Entity<ReportAttachment>()
                .HasOne(a => a.Report).WithMany(r => r.Attachments)
                .HasForeignKey(a => a.ReportId).OnDelete(DeleteBehavior.Cascade);
            b.Entity<AdminActivityLog>()
                .HasOne(l => l.Admin).WithMany().HasForeignKey(l => l.AdminId).OnDelete(DeleteBehavior.SetNull);

            SeedData.Apply(b);
        }
    }

    // BloodType.APositive <-> "A+"
    public class BloodTypeConverter : ValueConverter<BloodType, string>
    {
        public BloodTypeConverter() : base(v => ToText(v), v => FromText(v)) { }

        public static string ToText(BloodType t) => t switch
        {
            BloodType.APositive => "A+",
            BloodType.ANegative => "A-",
            BloodType.BPositive => "B+",
            BloodType.BNegative => "B-",
            BloodType.ABPositive => "AB+",
            BloodType.ABNegative => "AB-",
            BloodType.OPositive => "O+",
            _ => "O-"
        };

        public static BloodType FromText(string s) => s switch
        {
            "A+" => BloodType.APositive,
            "A-" => BloodType.ANegative,
            "B+" => BloodType.BPositive,
            "B-" => BloodType.BNegative,
            "AB+" => BloodType.ABPositive,
            "AB-" => BloodType.ABNegative,
            "O+" => BloodType.OPositive,
            _ => BloodType.ONegative
        };
    }
}
