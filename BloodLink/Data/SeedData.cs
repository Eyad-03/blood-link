using BloodLink.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BloodLink.Data
{
    // Starting data taken from the demo data in wwwroot/js (verified facilities, halls, hospital stock).
    // Seed values must be fixed (no DateTime.Now) so the migration does not change every time.
    public static class SeedData
    {
        private static readonly DateTime Seeded = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static void Apply(ModelBuilder b)
        {
            b.Entity<Organization>().HasData(
                new Organization { Id = 1, Name = "Al Hayat Hospital", Type = OrganizationType.Hospital, City = "Irbid", Address = "University Street, Irbid", LicenseNumber = "MOH-IRB-40017", Email = "bloodbank@alhayat.example", Phone = "+962 2 700 0000", ContactPerson = "Hospital Admin", OpenTime = new TimeOnly(8, 0), CloseTime = new TimeOnly(16, 0), SlotCapacity = 3, Status = OrganizationStatus.Active, RegisteredAt = Seeded, DecidedAt = Seeded },
                new Organization { Id = 2, Name = "Jerash Family Hospital", Type = OrganizationType.Hospital, City = "Jerash", Address = "Amman–Jerash Road, Jerash", LicenseNumber = "MOH-JRS-20981", Email = "info@jerashfamily.jo", Phone = "+962 2 635 5500", ContactPerson = "Dr. Hiba Khatib", OpenTime = new TimeOnly(8, 0), CloseTime = new TimeOnly(15, 0), SlotCapacity = 3, Status = OrganizationStatus.Active, RegisteredAt = Seeded, DecidedAt = Seeded },
                new Organization { Id = 3, Name = "Aqaba Red Crescent Center", Type = OrganizationType.BloodDonationCenter, City = "Aqaba", Address = "Aqaba city center", LicenseNumber = "MOH-AQB-77310", Email = "aqaba@redcrescent.jo", Phone = "+962 3 201 7788", ContactPerson = "Fadi Amro", OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(15, 0), SlotCapacity = 3, Status = OrganizationStatus.Active, RegisteredAt = Seeded, DecidedAt = Seeded }
            );

            var stock = new (BloodType Type, int Units)[]
            {
                (BloodType.OPositive, 52), (BloodType.ONegative, 12), (BloodType.APositive, 44), (BloodType.ANegative, 8),
                (BloodType.BPositive, 36), (BloodType.BNegative, 6), (BloodType.ABPositive, 61), (BloodType.ABNegative, 29)
            };
            b.Entity<BloodInventory>().HasData(stock.Select((s, i) => new BloodInventory
            {
                Id = i + 1, OrganizationId = 1, BloodType = s.Type, Units = s.Units, UpdatedAt = Seeded
            }));

            b.Entity<Hall>().HasData(
                new Hall { Id = 1, Name = "University of Jordan Conference Hall", Governorate = "Amman", EntityType = "University", Capacity = 250, Price = 180, Rating = 4.9m, Tag = "Popular choice", Location = "Jubeiha, Amman", ImageUrl = "https://images.unsplash.com/photo-1517457373958-b7bdd4587205?auto=format&fit=crop&w=900&q=80", Description = "A modern venue equipped for conferences, seminars, and academic events.", SupportedEvents = "Conference,Workshop,Training", Features = "Presentation screen,Sound system,Parking" },
                new Hall { Id = 2, Name = "Zaha Cultural Center", Governorate = "Amman", EntityType = "Community center", Capacity = 120, Price = 95, Rating = 4.8m, Tag = "Great for initiatives", Location = "Khalda, Amman", ImageUrl = "https://images.unsplash.com/photo-1497366754035-f200968a6e72?auto=format&fit=crop&w=900&q=80", Description = "A flexible space for community activities, training, and workshops.", SupportedEvents = "Initiative,Workshop,Training", Features = "Wi-Fi,Equipment,Parking" },
                new Hall { Id = 3, Name = "Aqaba Grand Hall", Governorate = "Aqaba", EntityType = "Private institution", Capacity = 400, Price = 260, Rating = 4.7m, Tag = "Large capacity", Location = "Aqaba city center", ImageUrl = "https://images.unsplash.com/photo-1505373877841-8d25f7d46678?auto=format&fit=crop&w=900&q=80", Description = "A spacious hall with lighting and facilities for large events.", SupportedEvents = "Conference,Initiative", Features = "Stage,Professional audio,Parking" },
                new Hall { Id = 4, Name = "Irbid Innovation Center", Governorate = "Irbid", EntityType = "Government institution", Capacity = 90, Price = 70, Rating = 4.8m, Tag = "Budget friendly", Location = "University Street, Irbid", ImageUrl = "https://images.unsplash.com/photo-1517502884422-41eaead166d4?auto=format&fit=crop&w=900&q=80", Description = "A practical venue for youth initiatives and small community gatherings.", SupportedEvents = "Initiative,Workshop,Training", Features = "Wi-Fi,Whiteboard,Parking" },
                new Hall { Id = 5, Name = "Greater Irbid Municipality Hall", Governorate = "Irbid", EntityType = "Government institution", Capacity = 180, Price = 130, Rating = 4.6m, Tag = "Available to book", Location = "Downtown, Irbid", ImageUrl = "https://images.unsplash.com/photo-1497366811353-6870744d04b2?auto=format&fit=crop&w=900&q=80", Description = "A multi-purpose hall for public meetings and local events.", SupportedEvents = "Conference,Initiative", Features = "Presentation screen,Sound system,Seating" },
                new Hall { Id = 6, Name = "King Abdullah II Community Center", Governorate = "Zarqa", EntityType = "Community center", Capacity = 70, Price = 55, Rating = 4.5m, Tag = "Affordable", Location = "New Zarqa, Zarqa", ImageUrl = "https://images.unsplash.com/photo-1517245386807-bb43f82c33c4?auto=format&fit=crop&w=900&q=80", Description = "A comfortable venue for short courses, workshops, and volunteer activities.", SupportedEvents = "Workshop,Training,Initiative", Features = "Wi-Fi,Presentation setup,Seating" }
            );
        }
    }
}
