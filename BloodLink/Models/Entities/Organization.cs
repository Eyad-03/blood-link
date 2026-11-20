using System.ComponentModel.DataAnnotations;

namespace BloodLink.Models.Entities
{
    // Hospital / blood donation center / medical center.
    // Verified by the admin (Admin → Accounts) before it becomes Active.
    public class Organization
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public OrganizationType Type { get; set; } = OrganizationType.Hospital;

        [Required, MaxLength(60)]
        public string City { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? LicenseNumber { get; set; }

        [MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? ContactPerson { get; set; }

        // Opening hours for donation appointments (Sun–Thu).
        public TimeOnly OpenTime { get; set; } = new(8, 0);
        public TimeOnly CloseTime { get; set; } = new(16, 0);

        // How many donors can be booked in the same time slot.
        public int SlotCapacity { get; set; } = 3;

        public OrganizationStatus Status { get; set; } = OrganizationStatus.Pending;

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? DecidedAt { get; set; }

        // When the hospital pressed "Send for review" after uploading its documents (null = not sent yet).
        public DateTime? SubmittedAt { get; set; }

        public ICollection<User> Staff { get; set; } = new List<User>();
        public ICollection<OrganizationDocument> Documents { get; set; } = new List<OrganizationDocument>();
        public ICollection<BloodInventory> Inventory { get; set; } = new List<BloodInventory>();
        public ICollection<DonationAppointment> Appointments { get; set; } = new List<DonationAppointment>();
        public ICollection<DonationRecord> DonationRecords { get; set; } = new List<DonationRecord>();
        public ICollection<Advertisement> Advertisements { get; set; } = new List<Advertisement>();
    }

    // License, registration certificate, representative ID… uploaded at sign-up.
    public class OrganizationDocument
    {
        public int Id { get; set; }

        public int OrganizationId { get; set; }
        public Organization Organization { get; set; } = null!;

        [Required, MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        // Where the file is saved on the server (inside App_Data/uploads, not public).
        [Required, MaxLength(260)]
        public string FilePath { get; set; } = string.Empty;

        // The name of the file on the hospital's computer, e.g. "license.pdf".
        [MaxLength(200)]
        public string? FileName { get; set; }

        // null = not uploaded yet
        public long? FileSizeBytes { get; set; }

        // Ticked by the admin while reviewing the account.
        public bool IsChecked { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
