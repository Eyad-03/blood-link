using System.ComponentModel.DataAnnotations;

namespace BloodLink.Models.Entities
{
    // One conversation per blood request (chat.js).
    public class ChatMessage
    {
        public int Id { get; set; }

        public int BloodRequestId { get; set; }
        public BloodRequest BloodRequest { get; set; } = null!;

        // Null for system notes ("Request accepted…").
        public int? SenderId { get; set; }
        public User? Sender { get; set; }

        public ChatSender SenderType { get; set; }

        [Required, MaxLength(500)]
        public string Text { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }

    // When each user last opened a chat — used for the unread badge.
    public class ChatReadState
    {
        public int Id { get; set; }

        public int BloodRequestId { get; set; }
        public BloodRequest BloodRequest { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime LastReadAt { get; set; } = DateTime.UtcNow;
    }
}
