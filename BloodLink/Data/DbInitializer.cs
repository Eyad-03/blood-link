using BloodLink.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace BloodLink.Data
{
    // Adds demo accounts the first time the app runs (only when the Users table is empty),
    // so you can sign in and try every page straight away.
    //
    //   Admin      admin@bloodlink.jo      / Admin@123
    //   Hospital   hospital@alhayat.jo     / Hospital@123   (Al Hayat Hospital)
    //   Recipient  ahmad@example.com       / Recipient@123
    //   Donors     lina@example.com, omar@example.com, ...  / Donor@123
    public static class DbInitializer
    {
        public static void Seed(BloodLinkDbContext db)
        {
            if (db.Users.Any()) return;   // already seeded

            var hasher = new PasswordHasher<User>();
            User NewUser(string name, string email, string password, UserRole role)
            {
                var user = new User { FullName = name, Email = email, Role = role, Status = AccountStatus.Active };
                user.PasswordHash = hasher.HashPassword(user, password);
                return user;
            }

            // ---- Admin, hospital staff and a recipient ----
            db.Users.Add(NewUser("BloodLink Admin", "admin@bloodlink.jo", "Admin@123", UserRole.Admin));

            var staff = NewUser("Hospital Admin", "hospital@alhayat.jo", "Hospital@123", UserRole.Hospital);
            staff.OrganizationId = 1;   // Al Hayat Hospital (added by SeedData)
            staff.City = "Irbid";
            db.Users.Add(staff);

            var recipient = NewUser("Ahmad Saleh", "ahmad@example.com", "Recipient@123", UserRole.Recipient);
            recipient.City = "Irbid";
            recipient.Phone = "+962 7 9000 2001";
            recipient.BloodType = BloodType.ONegative;
            db.Users.Add(recipient);

            // ---- Donors (same people as the old demo map) ----
            var donors = new (string Name, string Email, string City, string Area, BloodType Type, bool Available, double Lat, double Lng)[]
            {
                ("Lina Khalil", "lina@example.com", "Irbid", "University Street", BloodType.OPositive, true, 32.5355, 35.8515),
                ("Omar Haddad", "omar@example.com", "Irbid", "Al-Hashmi", BloodType.APositive, true, 32.5482, 35.8578),
                ("Sara Nasser", "sara@example.com", "Amman", "Khalda", BloodType.BPositive, false, 31.9952, 35.8427),
                ("Yazan Saleh", "yazan@example.com", "Irbid", "Al-Barha", BloodType.OPositive, true, 32.5512, 35.8394),
                ("Maya Ibrahim", "maya@example.com", "Amman", "Shmeisani", BloodType.ABPositive, true, 31.9648, 35.8967),
                ("Adam Faris", "adam@example.com", "Zarqa", "New Zarqa", BloodType.ANegative, false, 32.0728, 36.0942),
                ("Noor Samir", "noor@example.com", "Irbid", "Al-Husun", BloodType.ONegative, true, 32.4851, 35.8765),
                ("Kareem Adel", "kareem@example.com", "Amman", "Jubaiha", BloodType.BNegative, true, 32.0184, 35.8721),
                ("Hala Mahmoud", "hala@example.com", "Irbid", "Downtown", BloodType.ABNegative, false, 32.5556, 35.8501),
                ("Tareq Amin", "tareq@example.com", "Amman", "Abdoun", BloodType.OPositive, true, 31.9498, 35.8831),
                ("Dana Rami", "dana@example.com", "Zarqa", "Prince Hasan", BloodType.APositive, true, 32.0833, 36.1056),
                ("Zaid Fawzi", "zaid@example.com", "Irbid", "Al-Naseem", BloodType.BPositive, true, 32.5422, 35.8664)
            };
            foreach (var d in donors)
            {
                var donor = NewUser(d.Name, d.Email, "Donor@123", UserRole.Donor);
                donor.City = d.City;
                donor.Area = d.Area;
                donor.BloodType = d.Type;
                donor.IsAvailable = d.Available;
                donor.Latitude = d.Lat;
                donor.Longitude = d.Lng;
                donor.ShareLocation = true;
                db.Users.Add(donor);
            }

            // ---- Two ads from Al Hayat Hospital so the Ads page is not empty ----
            var today = DateOnly.FromDateTime(DateTime.Now);
            db.Advertisements.Add(new Advertisement
            {
                OrganizationId = 1, Title = "Community blood donation drive", Department = "Blood bank",
                Category = "Blood donation drive", Badge = "Donate",
                Description = "Help us restock O-, A- and B- blood. Every donor receives a free blood-type and hemoglobin check. Please eat beforehand and bring a valid ID.",
                EventDate = today.AddDays(3), StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(17, 0),
                Location = "Blood bank, ground floor", Phone = "+962 2 700 0000",
                Plan = AdPlan.Featured, StartDate = today.AddDays(-2), EndDate = today.AddDays(11),
                Fee = 50, Vat = 8, TotalPaid = 58, Views = 120
            });
            db.Advertisements.Add(new Advertisement
            {
                OrganizationId = 1, Title = "Free orthopedic consultation day", Department = "Orthopedics",
                Category = "Free consultation day", Badge = "Free",
                Description = "Meet our orthopedic consultants for a free assessment of knee, back, shoulder and joint pain. Bring any previous X-rays or reports.",
                EventDate = today.AddDays(6), StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(14, 0),
                Location = "Outpatient clinics, 2nd floor", Phone = "+962 2 700 0000", BookingRequired = true,
                Plan = AdPlan.Premium, StartDate = today.AddDays(-1), EndDate = today.AddDays(28),
                Fee = 90, Vat = 14.40m, TotalPaid = 104.40m, Views = 260
            });

            db.SaveChanges();
        }
    }
}
