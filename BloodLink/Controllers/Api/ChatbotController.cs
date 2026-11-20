using System.Text;
using System.Text.Json;
using BloodLink.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace BloodLink.Controllers.Api
{
    // POST /api/chatbot → the BloodLink assistant (the round button at the bottom right of every page).
    //
    // It sends the conversation to Ollama (an AI model running on your own computer) and sends the
    // answer back to the browser WHILE it is being written (streaming), so the user sees it quickly.
    //
    // Settings (optional, these are the defaults):
    //   "Ollama": { "Url": "http://localhost:11434", "Model": "llama3.2:1b" }
    [ApiController]
    [Route("api/chatbot")]
    public class ChatbotController : ControllerBase
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfiguration config;

        public ChatbotController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            this.httpClientFactory = httpClientFactory;
            this.config = config;
        }

        // The rules for the assistant. It only talks about blood and BloodLink.
        private const string SystemPrompt = @"You are the BloodLink Assistant, a friendly helper on BloodLink, a blood donation website in Jordan.
Only answer questions about: blood, blood types and compatibility, blood donation (who can donate, preparation, after care, how often),
and how to use BloodLink. If the question is about anything else, politely say you can only help with blood and blood donation.
Always reply in the same language as the user (Arabic or English). Keep answers short and clear: at most 6 short sentences or a short list.
You are not a doctor: for symptoms or medical decisions, tell the user to ask a doctor. In an emergency in Jordan, call 911.
Use ONLY these blood facts (they are correct; do not invent other facts):
- Universal red-cell donor: O- (O negative). Universal recipient: AB+ (AB positive). Universal plasma donor: AB.
- Who can receive from whom (red cells): O- → everyone. O+ → O+, A+, B+, AB+. A- → A-, A+, AB-, AB+. A+ → A+, AB+.
  B- → B-, B+, AB-, AB+. B+ → B+, AB+. AB- → AB-, AB+. AB+ → AB+ only.
- A patient can receive from: O- ← O- only. O+ ← O+, O-. A- ← A-, O-. A+ ← A+, A-, O+, O-. B- ← B-, O-. B+ ← B+, B-, O+, O-.
  AB- ← AB-, A-, B-, O-. AB+ ← all types.
- Most common donor rules: age 18–65, weight at least 50 kg, feeling healthy on the day, and enough hemoglobin (checked at the center).
  Wait 3 months between whole-blood donations. Recent tattoo/piercing, surgery, some medicines or travel may delay donation — the center decides.
- Before donating: sleep well, eat a meal, drink plenty of water, bring an ID. After: rest 10–15 minutes, drink more fluids,
  avoid heavy exercise and lifting for 24 hours, keep the bandage on for a few hours.
- One donation is about 450 ml and can help up to 3 patients. The body replaces the fluid in a day and the red cells in a few weeks.
Facts about BloodLink:
- Donors must wait 3 months between donations; BloodLink turns their availability back on automatically.
- People who need blood use 'Find Donors' to send a request (with priority and hospital) to an available donor; after the donor accepts they can chat.
- Donors can book a donation appointment at a hospital on 'Book Donation' and answer a short eligibility check.
- Hospitals register, upload 3 documents and wait for admin approval. Payments on BloodLink accept Visa cards only.";

        [HttpPost]
        public async Task Ask(ChatbotDto dto)
        {
            string url = (config["Ollama:Url"] ?? "http://localhost:11434").TrimEnd('/');
            string model = config["Ollama:Model"] ?? "llama3.2:1b";

            // 1. The messages for the model: the rules + only the last 6 messages (shorter = faster).
            var messages = new List<object> { new { role = "system", content = SystemPrompt } };
            foreach (var m in dto.Messages.TakeLast(6))
            {
                if ((m.Role == "user" || m.Role == "assistant") && !string.IsNullOrWhiteSpace(m.Content))
                    messages.Add(new { role = m.Role, content = m.Content.Length > 1000 ? m.Content.Substring(0, 1000) : m.Content });
            }

            var body = new
            {
                model,
                messages,
                stream = true,          // send the answer piece by piece
                keep_alive = "30m",     // keep the model in memory → next answers start fast
                options = new { temperature = 0.3, num_predict = 350 }
            };

            // 2. Ask Ollama.
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(3);   // the very first answer loads the model, it can take a moment
            HttpResponseMessage ollama;
            try
            {
                var json = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, url + "/api/chat") { Content = json };
                ollama = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, HttpContext.RequestAborted);
            }
            catch (HttpRequestException)
            {
                await Problem503("The assistant is offline. Start Ollama on the server (run: ollama serve) and try again.");
                return;
            }

            if (!ollama.IsSuccessStatusCode)
            {
                await Problem503($"The AI model \"{model}\" is not ready. On the server run: ollama pull {model}");
                return;
            }

            // 3. Ollama sends one JSON line per piece: {"message":{"content":"Hel"}} ...
            //    We forward only the text to the browser, piece by piece.
            Response.ContentType = "text/plain; charset=utf-8";
            using var stream = await ollama.Content.ReadAsStreamAsync(HttpContext.RequestAborted);
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = await reader.ReadLineAsync(HttpContext.RequestAborted)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                using var json = JsonDocument.Parse(line);
                if (json.RootElement.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var content))
                {
                    await Response.WriteAsync(content.GetString() ?? "", HttpContext.RequestAborted);
                    await Response.Body.FlushAsync(HttpContext.RequestAborted);   // send it now, don't wait
                }
            }
        }

        private async Task Problem503(string text)
        {
            Response.StatusCode = 503;
            Response.ContentType = "text/plain; charset=utf-8";
            await Response.WriteAsync(text);
        }
    }
}
