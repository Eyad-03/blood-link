namespace BloodLink.Models.Dtos
{
    // The conversation sent by chatbot.js (the newest message is last).
    public class ChatbotDto
    {
        public List<ChatbotMessage> Messages { get; set; } = new();
    }

    public class ChatbotMessage
    {
        public string Role { get; set; } = "user";   // user | assistant
        public string Content { get; set; } = "";
    }
}
