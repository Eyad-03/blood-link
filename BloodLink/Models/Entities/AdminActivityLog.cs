using System.ComponentModel.DataAnnotations;

namespace BloodLink.Models.Entities
{
    // "Recent activity" list in the admin console.
    public class AdminActivityLog
    {
        public int Id { get; set; }

        public int? AdminId { get; set; }
        public User? Admin { get; set; }

        [Required, MaxLength(300)]
        public string Text { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Icon { get; set; }

        [MaxLength(20)]
        public string? Color { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
