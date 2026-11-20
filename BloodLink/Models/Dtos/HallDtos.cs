namespace BloodLink.Models.Dtos
{
    // Booking form on the Hall page (hall.js).
    public class CreateHallBookingDto
    {
        public int HallId { get; set; }
        public string Organization { get; set; } = "";
        public string ContactName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string EventType { get; set; } = "";
        public string EventDate { get; set; } = "";
        public int Attendees { get; set; }
        public string? EventTime { get; set; }
        public string? Description { get; set; }
        public List<HallDocumentDto> Documents { get; set; } = new();
    }

    public class HallDocumentDto
    {
        public string Label { get; set; } = "";
        public string FileName { get; set; } = "";
    }

    // Payment of an approved booking.
    public class PayDto
    {
        public string Method { get; set; } = "card";
    }

    // Admin decision on a booking / report / account / user.
    public class AdminDecisionDto
    {
        public string Action { get; set; } = "";
        public string? Note { get; set; }
    }
}
