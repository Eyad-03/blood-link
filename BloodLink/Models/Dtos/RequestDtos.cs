namespace BloodLink.Models.Dtos
{
    // Recipient sends a blood request to a donor (Donor → Search page).
    public class CreateBloodRequestDto
    {
        public int DonorId { get; set; }
        public string? BloodType { get; set; }        // the blood type the recipient needs
        public string Priority { get; set; } = "Normal";
        public string? Message { get; set; }
    }

    // Donor answers a request: Accepted / Declined / Completed.
    public class ChangeStatusDto
    {
        public string Status { get; set; } = "";
    }

    // New chat message.
    public class ChatMessageDto
    {
        public string Text { get; set; } = "";
    }

    // Donor availability switch.
    public class AvailabilityDto
    {
        public bool Available { get; set; }
    }
}
