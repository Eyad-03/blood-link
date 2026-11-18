using System.ComponentModel.DataAnnotations;

namespace BloodLink.Models.Entities
{
    // "Report an issue" page → Admin → Reports inbox.
    public class Report
    {
        public int Id { get; set; }

        // Linked account if the reporter is signed in.
        public int? ReporterId { get; set; }
        public User? Reporter { get; set; }

        [Required, MaxLength(100)]
        public string FromName { get; set; } = string.Empty;

        [Required, MaxLength(40)]
        public string ReporterRole { get; set; } = string.Empty;   // Donor, Hospital, Organization…

        [Required, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(60)]
        public string Category { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Related { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(4000)]
        public string Body { get; set; } = string.Empty;

        public ReportPriority Priority { get; set; } = ReportPriority.Medium;

        public ReportStatus Status { get; set; } = ReportStatus.New;

        public bool IsRead { get; set; }

        [MaxLength(2000)]
        public string? Response { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        public ICollection<ReportAttachment> Attachments { get; set; } = new List<ReportAttachment>();
    }

    public class ReportAttachment
    {
        public int Id { get; set; }

        public int ReportId { get; set; }
        public Report Report { get; set; } = null!;

        [Required, MaxLength(260)]
        public string FilePath { get; set; } = string.Empty;
    }
}
