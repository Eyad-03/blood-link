namespace BloodLink.Models.Dtos
{
    // Hospital → Profile form.
    public class HospitalProfileDto
    {
        public string Name { get; set; } = "";
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
    }

    // Hospital → "New donation record" form.
    public class CreateDonationRecordDto
    {
        public string Donor { get; set; } = "";
        public string Recipient { get; set; } = "";
        public string Type { get; set; } = "";
        public int Units { get; set; } = 1;
        public string Date { get; set; } = "";
        public string? Notes { get; set; }
    }

    // Hospital → new advertisement (after the checkout dialog).
    public class CreateAdDto
    {
        public string Title { get; set; } = "";
        public string Department { get; set; } = "";
        public string Category { get; set; } = "";
        public string? Badge { get; set; }
        public string? Description { get; set; }
        public string EventDate { get; set; } = "";
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? Location { get; set; }
        public string? Phone { get; set; }
        public bool Booking { get; set; }
        public string Plan { get; set; } = "basic";        // basic | featured | premium
        public string StartDate { get; set; } = "";
        public string? PayMethod { get; set; }
    }
}
