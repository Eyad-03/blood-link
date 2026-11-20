using System.ComponentModel.DataAnnotations;

namespace BloodLink.Models.Entities
{
    // A donation documented by the hospital (Hospital → Donation records).
    // Also feeds the donor's donation count / certificate on the profile page.
    // OrganizationId is empty when the donor added the donation himself on the profile page.
    public class DonationRecord
    {
        public int Id { get; set; }

        public int? OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        // Linked donor account, if the donor is registered.
        public int? DonorId { get; set; }
        public User? Donor { get; set; }

        [Required, MaxLength(80)]
        public string DonorName { get; set; } = string.Empty;

        [Required, MaxLength(80)]
        public string RecipientName { get; set; } = string.Empty;   // e.g. "Patient #2841"

        // Emails typed on the verification form. The donor email puts the donor
        // in this hospital's Donor directory (so the hospital can email them later).
        [MaxLength(256)]
        public string? DonorEmail { get; set; }

        [MaxLength(256)]
        public string? RecipientEmail { get; set; }

        public BloodType BloodType { get; set; }

        public int Units { get; set; } = 1;

        public DateOnly DonationDate { get; set; }

        public DonationRecordStatus Status { get; set; } = DonationRecordStatus.Verified;

        [MaxLength(300)]
        public string? Notes { get; set; }

        // Optional links to what led to this donation.
        public int? AppointmentId { get; set; }
        public DonationAppointment? Appointment { get; set; }

        public int? BloodRequestId { get; set; }
        public BloodRequest? BloodRequest { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
