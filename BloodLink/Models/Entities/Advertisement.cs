using System.ComponentModel.DataAnnotations;

namespace BloodLink.Models.Entities
{
    // Paid ad posted by a hospital and shown on the public Ads page (ads-data.js).
    // Status (Scheduled / Active / Expired / Stopped) is calculated from the dates and IsCancelled.
    public class Advertisement
    {
        public int Id { get; set; }

        public int OrganizationId { get; set; }
        public Organization Organization { get; set; } = null!;

        [Required, MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(60)]
        public string Department { get; set; } = string.Empty;

        [Required, MaxLength(60)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Badge { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public DateOnly EventDate { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        public bool BookingRequired { get; set; }

        // ---- Plan & payment ----
        public AdPlan Plan { get; set; } = AdPlan.Basic;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal Fee { get; set; }
        public decimal Vat { get; set; }
        public decimal TotalPaid { get; set; }

        public int Views { get; set; }
        public bool IsCancelled { get; set; }

        // true while the ad is created but not paid yet (it is hidden until the Visa payment succeeds).
        public bool AwaitingPayment { get; set; }

        // The Stripe payment (pi_...) that paid for this ad.
        [MaxLength(100)]
        public string? PaymentIntentId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
