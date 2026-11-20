using BloodLink.Data;
using BloodLink.Helpers;
using BloodLink.Models.Dtos;
using BloodLink.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BloodLink.Controllers.Api
{
    // Chat between donor and recipient — one conversation per blood request (chat.js).
    //   GET  /api/chat/unread        unread count for each of my requests
    //   GET  /api/chat/{requestId}   messages (also marks them as read)
    //   POST /api/chat/{requestId}   send a message
    [Authorize]
    [Route("api/chat")]
    public class ChatController : ApiBaseController
    {
        public ChatController(BloodLinkDbContext db) : base(db) { }

        [HttpGet("unread")]
        public IActionResult Unread()
        {
            int me = CurrentUserId;
            var myRequestIds = db.BloodRequests
                .Where(r => r.DonorId == me || r.RecipientId == me)
                .Select(r => r.Id)
                .ToList();

            var result = new Dictionary<string, int>();
            foreach (int requestId in myRequestIds)
            {
                var lastRead = db.ChatReadStates
                    .Where(s => s.BloodRequestId == requestId && s.UserId == me)
                    .Select(s => (DateTime?)s.LastReadAt)
                    .FirstOrDefault() ?? DateTime.MinValue;

                int count = db.ChatMessages.Count(m => m.BloodRequestId == requestId
                    && m.SenderId != null && m.SenderId != me && m.SentAt > lastRead);

                if (count > 0) result[requestId.ToString()] = count;
            }
            return Ok(result);
        }

        [HttpGet("{requestId}")]
        public IActionResult Messages(int requestId)
        {
            var request = db.BloodRequests.Find(requestId);
            if (request == null) return NotFound();
            int me = CurrentUserId;
            if (request.DonorId != me && request.RecipientId != me) return Forbid();

            var messages = db.ChatMessages
                .Where(m => m.BloodRequestId == requestId)
                .OrderBy(m => m.SentAt)
                .ToList()
                .Select(m => new
                {
                    id = m.Id.ToString(),
                    from = m.SenderType.ToString().ToLower(),   // system | donor | recipient
                    text = m.Text,
                    at = Format.Iso(m.SentAt)
                });

            // When did the OTHER person last open the chat? (used for the "seen" ticks)
            int peerId = request.DonorId == me ? request.RecipientId : request.DonorId;
            var peerRead = db.ChatReadStates.FirstOrDefault(s => s.BloodRequestId == requestId && s.UserId == peerId);

            MarkAsRead(requestId, me);
            db.SaveChanges();

            return Ok(new { messages, peerReadAt = Format.Iso(peerRead?.LastReadAt) });
        }

        [HttpPost("{requestId}")]
        public IActionResult Send(int requestId, ChatMessageDto dto)
        {
            var request = db.BloodRequests.Find(requestId);
            if (request == null) return NotFound();
            int me = CurrentUserId;
            if (request.DonorId != me && request.RecipientId != me) return Forbid();

            if (request.Status != BloodRequestStatus.Accepted)
                return BadRequest(new { message = "You can chat only while the request is accepted." });

            string text = (dto.Text ?? "").Trim();
            if (text.Length == 0) return BadRequest(new { message = "Write a message first." });
            if (text.Length > 500) text = text.Substring(0, 500);

            db.ChatMessages.Add(new ChatMessage
            {
                BloodRequestId = requestId,
                SenderId = me,
                SenderType = request.DonorId == me ? ChatSender.Donor : ChatSender.Recipient,
                Text = text
            });
            MarkAsRead(requestId, me);
            db.SaveChanges();
            return Ok();
        }

        private void MarkAsRead(int requestId, int userId)
        {
            var state = db.ChatReadStates.FirstOrDefault(s => s.BloodRequestId == requestId && s.UserId == userId);
            if (state == null)
            {
                state = new ChatReadState { BloodRequestId = requestId, UserId = userId };
                db.ChatReadStates.Add(state);
            }
            state.LastReadAt = DateTime.UtcNow;
        }
    }
}
