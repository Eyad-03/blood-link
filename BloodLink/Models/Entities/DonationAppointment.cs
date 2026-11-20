using System.ComponentModel.DataAnnotations;

namespace BloodLink.Models.Entities
{
    // Donor books a visit at a hospital/center; hospital approves with a confirmed time
    // (Donor → Appointments and Hospital → Appointments).
    public class DonationAppointment
    {
        public int Id { get; set; }

        public int DonorId { get; set; }
        public User Donor { get; set; } = null!;

        public int OrganizationId { get; set; }
        public Organization Organization { get; set; } = null!;

        // Contact details as entered on the booking form.
        [Required, MaxLength(80)]
        public string DonorName { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string DonorPhone { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? DonorEmail { get; set; }

        public BloodType BloodType { get; set; }

        public DateOnly RequestedDate { get; set; }
        public TimeOnly RequestedTime { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // ---- Eligibility check ----
        public int Age { get; set; }
        public decimal WeightKg { get; set; }
        public DateOnly? LastDonationDate { get; set; }
        public bool FeelingWell { get; set; }
        public bool TakesMedication { get; set; }
        [MaxLength(200)]
        public string? MedicationDetail { get; set; }
        public bool TravelledAbroad { get; set; }
        [MaxLength(200)]
        public string? TravelDetail { get; set; }
        public bool RecentProcedure { get; set; }      // tattoo, piercing, surgery or dental work (6 months)
        public EligibilityState EligibilityState { get; set; } = EligibilityState.Idle;
        [MaxLength(1000)]
        public string? EligibilityFlags { get; set; }   // notes for the hospital, separated by new lines

        // ---- Hospital decision ----
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
        public DateOnly? ConfirmedDate { get; set; }
        public TimeOnly? ConfirmedTime { get; set; }
        [MaxLength(500)]
        public string? HospitalNote { get; set; }
        [MaxLength(500)]
        public string? DeclineReason { get; set; }
        public bool DonorNotified { get; set; }

        // Who cancelled the appointment: "donor" or "hospital".
        [MaxLength(20)]
        public string? CancelledBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
