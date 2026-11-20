using BloodLink.Data;
using BloodLink.Helpers;
using BloodLink.Models.Dtos;
using BloodLink.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodLink.Controllers.Api
{
    // Blood requests between a recipient and a donor.
    //   POST /api/requests                 recipient sends a request   (Donor → Search)
    //   GET  /api/requests/sent            recipient's requests        (Recipient → Requests)
    //   GET  /api/requests/received        donor's incoming requests   (Donor → Requests)
    //   PUT  /api/requests/{id}/status     accept / decline / complete
    [Authorize]
    [Route("api/requests")]
    public class RequestsController : ApiBaseController
    {
        public RequestsController(BloodLinkDbContext db) : base(db) { }

        [Authorize(Roles = "Recipient")]
        [HttpPost]
        public IActionResult Create(CreateBloodRequestDto dto)
        {
            var me = CurrentUser()!;
            var donor = db.Users.FirstOrDefault(u => u.Id == dto.DonorId && u.Role == UserRole.Donor);
            if (donor == null) return NotFound(new { message = "Donor not found." });
            if (!donor.IsAvailable) return BadRequest(new { message = "This donor is not available right now. Please choose another donor." });

            var bloodType = Format.ParseBlood(dto.BloodType) ?? me.BloodType ?? donor.BloodType;
            if (bloodType == null) return BadRequest(new { message = "Please choose the blood type you need." });

            if (!Enum.TryParse(dto.Priority, out RequestPriority priority)) priority = RequestPriority.Normal;

            var request = new BloodRequest
            {
                RecipientId = me.Id,
                DonorId = donor.Id,
                BloodType = bloodType.Value,
                Priority = priority,
                Units = 1,
                Location = me.City,
                Message = string.IsNullOrWhiteSpace(dto.Message) ? null : dto.Message.Trim()
            };
            db.BloodRequests.Add(request);
            db.SaveChanges();

            return Ok(new { id = request.Id.ToString(), priority = request.Priority.ToString() });
        }

        // Requests the signed-in recipient has sent.
        [Authorize(Roles = "Recipient")]
        [HttpGet("sent")]
        public IActionResult Sent()
        {
            var rows = db.BloodRequests
                .Include(r => r.Donor)
                .Include(r => r.Recipient)   // needed for the distance
                .Where(r => r.RecipientId == CurrentUserId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            var result = rows.Select(r =>
            {
                // Contact details are shown only after the donor accepts.
                bool showContact = r.Status == BloodRequestStatus.Accepted || r.Status == BloodRequestStatus.Completed;
                return new
                {
                    id = r.Id.ToString(),
                    donorName = r.Donor.FullName,
                    donorInitials = Format.Initials(r.Donor.FullName),
                    bloodType = Format.Blood(r.BloodType),
                    location = string.Join(", ", new[] { r.Donor.Area, r.Donor.City }.Where(x => !string.IsNullOrEmpty(x))),
                    distance = Format.DistanceText(r.Donor, r.Recipient),
                    sentAt = Format.Friendly(r.CreatedAt),
                    dateValue = Format.Iso(r.CreatedAt),
                    status = r.Status.ToString(),
                    priority = r.Priority.ToString(),
                    units = r.Units,
                    message = r.Message ?? "",
                    donorPhone = showContact ? r.Donor.Phone ?? "" : "",
                    donorEmail = showContact ? r.Donor.Email : ""
                };
            });
            return Ok(result);
        }

        // Requests the signed-in donor has received.
        [Authorize(Roles = "Donor")]
        [HttpGet("received")]
        public IActionResult Received()
        {
            var rows = db.BloodRequests
                .Include(r => r.Recipient)
                .Include(r => r.Donor)       // needed for the distance
                .Include(r => r.Hospital)
                .Where(r => r.DonorId == CurrentUserId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            var result = rows.Select(r =>
            {
                bool showContact = r.Status == BloodRequestStatus.Accepted || r.Status == BloodRequestStatus.Completed;
                return new
                {
                    id = r.Id.ToString(),
                    recipientName = r.Recipient.FullName,
                    initials = Format.Initials(r.Recipient.FullName),
                    bloodType = Format.Blood(r.BloodType),
                    location = r.Location ?? r.Recipient.City ?? "",
                    distance = Format.DistanceText(r.Donor, r.Recipient),
                    sentAt = Format.Friendly(r.CreatedAt),
                    dateValue = Format.Iso(r.CreatedAt),
                    status = r.Status.ToString(),
                    priority = r.Priority.ToString(),
                    units = r.Units,
                    hospital = r.Hospital?.Name ?? "",
                    message = r.Message ?? "",
                    recipientPhone = showContact ? r.Recipient.Phone ?? "" : "",
                    recipientEmail = showContact ? r.Recipient.Email : ""
                };
            });
            return Ok(result);
        }

        [HttpPut("{id}/status")]
        public IActionResult ChangeStatus(int id, ChangeStatusDto dto)
        {
            var request = db.BloodRequests.Include(r => r.Donor).Include(r => r.Recipient).FirstOrDefault(r => r.Id == id);
            if (request == null) return NotFound(new { message = "Request not found." });

            bool iAmDonor = request.DonorId == CurrentUserId;
            bool iAmRecipient = request.RecipientId == CurrentUserId;
            if (!iAmDonor && !iAmRecipient) return Forbid();

            if (dto.Status == "Accepted" || dto.Status == "Declined")
            {
                // Only the donor can accept or decline, and only while the request is pending.
                if (!iAmDonor) return Forbid();
                if (request.Status != BloodRequestStatus.Pending)
                    return BadRequest(new { message = "This request was already answered." });
                if (dto.Status == "Accepted" && DonationRules.IsResting(request.Donor))
                    return BadRequest(new { message = $"You donated recently. You can accept requests again from {request.Donor.AvailableAgainOn!.Value:dd MMM yyyy}." });
                if (dto.Status == "Accepted" && !request.Donor.IsAvailable)
                    return BadRequest(new { message = "Turn on your availability before accepting a request." });

                request.Status = dto.Status == "Accepted" ? BloodRequestStatus.Accepted : BloodRequestStatus.Declined;
                request.RespondedAt = DateTime.UtcNow;

                if (request.Status == BloodRequestStatus.Accepted)
                    AddSystemMessage(request.Id, "Request accepted by the donor. You can now chat to coordinate the donation safely.");
            }
            else if (dto.Status == "Completed")
            {
                // Donor or recipient can mark an accepted request as completed.
                if (request.Status != BloodRequestStatus.Accepted)
                    return BadRequest(new { message = "Only accepted requests can be completed." });

                request.Status = BloodRequestStatus.Completed;
                request.CompletedAt = DateTime.UtcNow;
                AddSystemMessage(request.Id, "Donation marked as completed. This conversation is now read-only.");

                // Count it as a donation on the donor's profile.
                db.DonationRecords.Add(new DonationRecord
                {
                    OrganizationId = request.HospitalId,
                    DonorId = request.DonorId,
                    DonorName = request.Donor.FullName,
                    RecipientName = request.Recipient.FullName,
                    BloodType = request.BloodType,
                    Units = request.Units,
                    DonationDate = DateOnly.FromDateTime(DateTime.Now),
                    Status = DonationRecordStatus.Verified,
                    BloodRequestId = request.Id,
                    Notes = "Blood request #" + request.Id
                });
                // The donor rests for 3 months now (not available until then).
                DonationRules.MarkDonated(request.Donor, DateOnly.FromDateTime(DateTime.Now));
            }
            else
            {
                return BadRequest(new { message = "Unknown status." });
            }

            db.SaveChanges();
            return Ok(new { id = request.Id.ToString(), status = request.Status.ToString() });
        }

        private void AddSystemMessage(int requestId, string text)
        {
            db.ChatMessages.Add(new ChatMessage { BloodRequestId = requestId, SenderType = ChatSender.System, Text = text });
        }
    }
}
