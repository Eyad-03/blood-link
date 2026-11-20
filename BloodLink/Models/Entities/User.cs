using System.ComponentModel.DataAnnotations;

namespace BloodLink.Models.Entities
{
    // Every person who signs up: donor, recipient (requester), hospital staff or admin.
    // Register page: role, fullName, email, password, requiredBloodType / requestCity, institutionName / institutionCity.
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(80)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(256), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Phone { get; set; }

        public UserRole Role { get; set; } = UserRole.Donor;

        public AccountStatus Status { get; set; } = AccountStatus.Active;

        // Donor's own blood type, or the blood type a recipient needs.
        public BloodType? BloodType { get; set; }

        [MaxLength(60)]
        public string? City { get; set; }

        [MaxLength(120)]
        public string? Area { get; set; }

        // Used by the donor search map (only when the donor agrees to share location).
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool ShareLocation { get; set; }

        // Donor availability toggle (Donor → Requests page).
        public bool IsAvailable { get; set; }

        [MaxLength(20)]
        public string? PreferredContactMethod { get; set; }   // phone | sms | email

        public DateOnly? LastDonationDate { get; set; }

        // After a donation the donor rests for 3 months (IsAvailable = false).
        // On this date the background service turns IsAvailable back on (see Helpers/DonationRules.cs).
        public DateOnly? AvailableAgainOn { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastActiveAt { get; set; }

        // Filled by the admin when the account is suspended (Admin → Users).
        [MaxLength(500)]
        public string? SuspendReason { get; set; }
        public DateTime? SuspendedAt { get; set; }

        // Set for hospital staff accounts.
        public int? OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        public ICollection<BloodRequest> SentRequests { get; set; } = new List<BloodRequest>();
        public ICollection<BloodRequest> ReceivedRequests { get; set; } = new List<BloodRequest>();
        public ICollection<DonationAppointment> Appointments { get; set; } = new List<DonationAppointment>();
        public ICollection<DonationRecord> Donations { get; set; } = new List<DonationRecord>();
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    }
}
