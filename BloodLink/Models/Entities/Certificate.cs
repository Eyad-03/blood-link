using System.ComponentModel.DataAnnotations;

namespace BloodLink.Models.Entities
{
    // Certificate of appreciation emailed to a donor after 5 donations (profile.js / send-certificate.js).
    public class Certificate
    {
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string Code { get; set; } = string.Empty;   // BL-XXXX…

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int DonationCount { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        // Filled when the certificate is emailed to the donor.
        [MaxLength(256)]
        public string? SentToEmail { get; set; }

        public DateTime? SentAt { get; set; }
    }
}
