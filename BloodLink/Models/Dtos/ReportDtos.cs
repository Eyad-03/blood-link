namespace BloodLink.Models.Dtos
{
    // "Report an issue" form (report.js).
    public class CreateReportDto
    {
        public string From { get; set; } = "";
        public string Role { get; set; } = "";
        public string Email { get; set; } = "";
        public string Category { get; set; } = "";
        public string Related { get; set; } = "";
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public string Priority { get; set; } = "medium";   // low | medium | high
        public List<string> Attachments { get; set; } = new();
    }
}
