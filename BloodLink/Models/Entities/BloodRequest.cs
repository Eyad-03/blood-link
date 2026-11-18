using System.ComponentModel.DataAnnotations;

namespace BloodLink.Models.Entities
{
    // A recipient asks a donor for blood (Recipient → Requests / Donor → Requests).
    public class BloodRequest
    {
        public int Id { get; set; }

        public int RecipientId { get; set; }
        public User Recipient { get; set; } = null!;

        public int DonorId { get; set; }
        public User Donor { get; set; } = null!;

        // Hospital where the donation should happen (optional).
        public int? HospitalId { get; set; }
        public Organization? Hospital { get; set; }

        public BloodType BloodType { get; set; }

        public int Units { get; set; } = 1;

        public RequestPriority Priority { get; set; } = RequestPriority.Normal;

        [MaxLength(200)]
        public string? Location { get; set; }

        [MaxLength(500)]
        public string? Message { get; set; }

        public BloodRequestStatus Status { get; set; } = BloodRequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public ICollection<ChatReadState> ReadStates { get; set; } = new List<ChatReadState>();
    }
}
