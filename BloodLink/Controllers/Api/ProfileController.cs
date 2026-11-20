using BloodLink.Data;
using BloodLink.Helpers;
using BloodLink.Models.Dtos;
using BloodLink.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BloodLink.Controllers.Api
{
    // The signed-in user's own profile (/Account/Profile and the availability switch on /Donor/Requests).
    //   GET    /api/profile                      my details + donations + certificate
    //   PUT    /api/profile                      edit my details
    //   PUT    /api/profile/availability         available to donate? true / false
    //   PUT    /api/profile/donor-offer          Donate page: blood type, phone, location
    //   POST   /api/profile/donations            add a donation I made myself
    //   DELETE /api/profile/donations/last       undo the last donation I added
    //   POST   /api/profile/certificate          create my certificate (after 5 donations)
    //   POST   /api/send-certificate             mark the certificate as emailed
    [Authorize]
    [Route("api/profile")]
    public class ProfileController : ApiBaseController
    {
        private const int CertificateGoal = 5;

        public ProfileController(BloodLinkDbContext db) : base(db) { }

        [HttpGet]
        public IActionResult Get()
        {
            var me = CurrentUser();
            if (me == null) return Unauthorized();
            return Ok(ProfileJson(me));
        }

        [HttpPut]
        public IActionResult Update(ProfileDto dto)
        {
            var me = CurrentUser()!;
            string email = dto.Email.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Name and email are required." });
            if (db.Users.Any(u => u.Email == email && u.Id != me.Id))
                return Conflict(new { message = "Another account already uses this email." });

            me.FullName = dto.Name.Trim();
            me.Email = email;
            me.Phone = dto.Phone;
            me.City = dto.Location;
            db.SaveChanges();
            return Ok(ProfileJson(me));
        }

        [HttpPut("availability")]
        public IActionResult SetAvailability(AvailabilityDto dto)
        {
            var me = CurrentUser()!;
            me.IsAvailable = dto.Available;
            db.SaveChanges();
            return Ok(new { available = me.IsAvailable });
        }

        // The Donate page form. After this the donor appears on the map of /Donor/Search.
        [Authorize(Roles = "Donor")]
        [HttpPut("donor-offer")]
        public IActionResult DonorOffer(DonorOfferDto dto)
        {
            var me = CurrentUser()!;
            var type = Format.ParseBlood(dto.BloodType);
            if (type == null) return BadRequest(new { message = "Please select your blood type." });
            if (string.IsNullOrWhiteSpace(dto.Phone)) return BadRequest(new { message = "Please enter your phone number." });

            // "Irbid, University Street" → City = "Irbid", Area = "University Street"
            var parts = dto.City.Split(',', 2, StringSplitOptions.TrimEntries);
            me.City = parts[0];
            me.Area = parts.Length > 1 && parts[1] != "Jordan" ? parts[1] : null;

            if (!string.IsNullOrWhiteSpace(dto.FullName)) me.FullName = dto.FullName.Trim();
            me.BloodType = type;
            me.Phone = dto.Phone.Trim();
            me.PreferredContactMethod = dto.ContactMethod;
            me.IsAvailable = true;
            me.ShareLocation = dto.ShareExactLocation && dto.Latitude != null && dto.Longitude != null;
            me.Latitude = me.ShareLocation ? dto.Latitude : null;
            me.Longitude = me.ShareLocation ? dto.Longitude : null;

            db.SaveChanges();
            return Ok(ProfileJson(me));
        }

        [HttpPost("donations")]
        public IActionResult AddDonation()
        {
            var me = CurrentUser()!;
            if (!me.IsAvailable) return BadRequest(new { message = "Switch your status to “Available” first." });

            var today = DateOnly.FromDateTime(DateTime.Now);
            db.DonationRecords.Add(new DonationRecord
            {
                OrganizationId = null,              // added by the donor, not by a hospital
                DonorId = me.Id,
                DonorName = me.FullName,
                RecipientName = "Self-reported",
                BloodType = me.BloodType ?? BloodType.OPositive,
                Units = 1,
                DonationDate = today,
                Status = DonationRecordStatus.PendingVerification
            });
            me.LastDonationDate = today;
            db.SaveChanges();
            return Ok(ProfileJson(me));
        }

        [HttpDelete("donations/last")]
        public IActionResult UndoDonation()
        {
            var me = CurrentUser()!;
            // Only donations the user added himself can be removed.
            var last = db.DonationRecords
                .Where(r => r.DonorId == me.Id && r.OrganizationId == null && r.BloodRequestId == null && r.AppointmentId == null)
                .OrderByDescending(r => r.Id)
                .FirstOrDefault();
            if (last == null) return BadRequest(new { message = "There is no donation you can undo." });

            db.DonationRecords.Remove(last);
            db.SaveChanges();
            return Ok(ProfileJson(me));
        }

        [HttpPost("certificate")]
        public IActionResult IssueCertificate()
        {
            var me = CurrentUser()!;
            if (db.Certificates.Any(c => c.UserId == me.Id)) return Ok(ProfileJson(me));

            int count = db.DonationRecords.Count(r => r.DonorId == me.Id);
            if (count < CertificateGoal)
                return BadRequest(new { message = $"You need {CertificateGoal} donations to get a certificate." });

            db.Certificates.Add(new Certificate
            {
                UserId = me.Id,
                Code = "BL-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                DonationCount = count
            });
            db.SaveChanges();
            return Ok(ProfileJson(me));
        }

        // The certificate page calls this to "email" the certificate.
        // NOTE: no real email is sent yet — we only save who it was sent to.
        // (To send real emails, add an SMTP service here later.)
        [HttpPost("/api/send-certificate")]
        public IActionResult SendCertificate()
        {
            var me = CurrentUser()!;
            var cert = db.Certificates.FirstOrDefault(c => c.UserId == me.Id);
            if (cert == null) return BadRequest(new { message = "Create your certificate first." });

            cert.SentToEmail = me.Email;
            cert.SentAt = DateTime.UtcNow;
            db.SaveChanges();
            return Ok(new { sentTo = cert.SentToEmail, sentAt = Format.Iso(cert.SentAt) });
        }

        // The JSON shape used by profile.js.
        private object ProfileJson(User me)
        {
            var donations = db.DonationRecords
                .Where(r => r.DonorId == me.Id)
                .OrderBy(r => r.DonationDate).ThenBy(r => r.Id)
                .Select(r => r.DonationDate)
                .ToList()
                .Select(d => d.ToString("yyyy-MM-dd") + "T12:00:00");

            var cert = db.Certificates.FirstOrDefault(c => c.UserId == me.Id);

            return new
            {
                name = me.FullName,
                email = me.Email,
                phone = me.Phone ?? "",
                location = me.City ?? "",
                role = me.Role.ToString(),
                bloodType = Format.Blood(me.BloodType),
                available = me.IsAvailable,
                donations,
                certificate = cert == null ? null : new
                {
                    id = cert.Code,
                    issuedAt = Format.Iso(cert.IssuedAt),
                    donations = cert.DonationCount,
                    name = me.FullName,
                    sentAt = cert.SentAt == null ? null : Format.Iso(cert.SentAt),
                    sentTo = cert.SentToEmail
                }
            };
        }
    }
}
