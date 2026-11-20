using System.ComponentModel.DataAnnotations;

namespace BloodLink.Models.Entities
{
    // Venue that can be booked for campaigns / conferences (Hall page).
    public class Hall
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(60)]
        public string Governorate { get; set; } = string.Empty;

        [MaxLength(60)]
        public string? EntityType { get; set; }        // University, Community center…

        [MaxLength(200)]
        public string? Location { get; set; }

        public int Capacity { get; set; }

        public decimal Price { get; set; }             // JOD

        public decimal Rating { get; set; }

        [MaxLength(60)]
        public string? Tag { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(300)]
        public string? SupportedEvents { get; set; }   // comma separated: Conference,Workshop,Training

        [MaxLength(300)]
        public string? Features { get; set; }          // comma separated

        public bool IsActive { get; set; } = true;

        public ICollection<HallBooking> Bookings { get; set; } = new List<HallBooking>();
    }

    // Booking request for a hall; reviewed by the admin, then paid.
    public class HallBooking
    {
        public int Id { get; set; }

        public int HallId { get; set; }
        public Hall Hall { get; set; } = null!;

        // Account that sent the request (if signed in).
        public int? RequestedById { get; set; }
        public User? RequestedBy { get; set; }

        [Required, MaxLength(150)]
        public string OrganizationName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string ContactName { get; set; } = string.Empty;

        [Required, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Phone { get; set; } = string.Empty;

        [Required, MaxLength(60)]
        public string EventType { get; set; } = string.Empty;

        public DateOnly EventDate { get; set; }

        [MaxLength(40)]
        public string? EventTime { get; set; }         // Morning / Evening / "09:00 – 15:00"

        public int Attendees { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public decimal Price { get; set; }             // price at the time of booking

        public HallBookingStatus Status { get; set; } = HallBookingStatus.PendingApproval;

        [MaxLength(500)]
        public string? AdminNote { get; set; }

        [MaxLength(40)]
        public string? PaymentMethod { get; set; }

        public DateTime? PaidAt { get; set; }

        // The Stripe payment (pi_...) that paid for this booking.
        [MaxLength(100)]
        public string? PaymentIntentId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<HallBookingDocument> Documents { get; set; } = new List<HallBookingDocument>();
    }

    public class HallBookingDocument
    {
        public int Id { get; set; }

        public int HallBookingId { get; set; }
        public HallBooking HallBooking { get; set; } = null!;

        [Required, MaxLength(120)]
        public string Label { get; set; } = string.Empty;

        [Required, MaxLength(260)]
        public string FilePath { get; set; } = string.Empty;
    }
}
