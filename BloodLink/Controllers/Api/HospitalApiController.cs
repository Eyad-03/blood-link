using BloodLink.Data;
using BloodLink.Helpers;
using BloodLink.Models.Dtos;
using BloodLink.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodLink.Controllers.Api
{
    // Everything on the hospital dashboard (/Hospital). Only hospital staff can call it.
    // Each hospital user belongs to one Organization (User.OrganizationId).
    [Authorize(Roles = "Hospital")]
    [ActiveHospital]   // only after the admin approved the institution (Helpers/ActiveHospitalAttribute.cs)
    [Route("api/hospital")]
    public class HospitalApiController : ApiBaseController
    {
        private readonly EmailSender emailSender;

        public HospitalApiController(BloodLinkDbContext db, EmailSender emailSender) : base(db)
        {
            this.emailSender = emailSender;
        }

        // The organization of the signed-in staff member.
        private int MyOrgId => CurrentUser()?.OrganizationId ?? 0;

        // ---------------- Profile ----------------

        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            var org = db.Organizations.Find(MyOrgId);
            if (org == null) return NotFound();
            return Ok(new { name = org.Name, email = org.Email ?? "", phone = org.Phone ?? "", city = org.City, address = org.Address ?? "" });
        }

        [HttpPut("profile")]
        public IActionResult SaveProfile(HospitalProfileDto dto)
        {
            var org = db.Organizations.Find(MyOrgId);
            if (org == null) return NotFound();
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest(new { message = "Hospital name is required." });

            org.Name = dto.Name.Trim();
            org.Email = dto.Email;
            org.Phone = dto.Phone;
            org.City = dto.City ?? org.City;
            org.Address = dto.Address;
            db.SaveChanges();
            return Ok();
        }

        // ---------------- Blood inventory ----------------

        // Returns { "O+": 52, "O-": 12, ... }
        [HttpGet("inventory")]
        public IActionResult Inventory()
        {
            var rows = db.BloodInventory.Where(i => i.OrganizationId == MyOrgId).ToList();
            var result = new Dictionary<string, int>();
            foreach (var row in rows) result[Format.Blood(row.BloodType)] = row.Units;
            return Ok(result);
        }

        // ---------------- Donation records ----------------

        [HttpGet("records")]
        public IActionResult Records()
        {
            var rows = db.DonationRecords
                .Where(r => r.OrganizationId == MyOrgId)
                .OrderByDescending(r => r.DonationDate).ThenByDescending(r => r.Id)
                .ToList();
            return Ok(rows.Select(RecordJson));
        }

        [HttpPost("records")]
        public IActionResult AddRecord(CreateDonationRecordDto dto)
        {
            var type = Format.ParseBlood(dto.Type);
            var date = Format.ParseDate(dto.Date);
            if (string.IsNullOrWhiteSpace(dto.Donor) || string.IsNullOrWhiteSpace(dto.Recipient) || type == null || date == null || dto.Units < 1)
                return BadRequest(new { message = "Please fill in all the donation details." });

            string donorEmail = dto.DonorEmail.Trim().ToLower();
            string recipientEmail = dto.RecipientEmail.Trim().ToLower();
            if (!IsEmail(donorEmail)) return BadRequest(new { message = "Please enter a valid donor email." });
            if (!IsEmail(recipientEmail)) return BadRequest(new { message = "Please enter a valid recipient email." });

            // If the donor has a BloodLink account with this email, link it
            // (the donation counts on their profile and starts the 3-month rest).
            string donorName = dto.Donor.Trim();
            var donorUser = db.Users.FirstOrDefault(u => u.Role == UserRole.Donor && u.Email == donorEmail);

            var record = new DonationRecord
            {
                OrganizationId = MyOrgId,
                DonorId = donorUser?.Id,
                DonorName = donorName,
                DonorEmail = donorEmail,
                RecipientName = dto.Recipient.Trim(),
                RecipientEmail = recipientEmail,
                BloodType = type.Value,
                Units = dto.Units,
                DonationDate = date.Value,
                Status = DonationRecordStatus.Verified,
                Notes = dto.Notes
            };
            db.DonationRecords.Add(record);
            if (donorUser != null) DonationRules.MarkDonated(donorUser, date.Value);   // donor rests for 3 months
            db.SaveChanges();
            return Ok(RecordJson(record));
        }

        [HttpPut("records/{id}/verify")]
        public IActionResult VerifyRecord(int id)
        {
            var record = db.DonationRecords.FirstOrDefault(r => r.Id == id && r.OrganizationId == MyOrgId);
            if (record == null) return NotFound();
            record.Status = DonationRecordStatus.Verified;
            db.SaveChanges();
            return Ok(RecordJson(record));
        }

        private static object RecordJson(DonationRecord r)
        {
            return new
            {
                id = r.Id.ToString(),
                donor = r.DonorName,
                donorEmail = r.DonorEmail ?? "",
                recipient = r.RecipientName,
                recipientEmail = r.RecipientEmail ?? "",
                type = Format.Blood(r.BloodType),
                units = r.Units,
                date = Format.Date(r.DonationDate),
                status = r.Status == DonationRecordStatus.Verified ? "Verified" : "Pending verification",
                notes = r.Notes ?? ""
            };
        }

        // ---------------- Donor directory ----------------

        // Only the donors who gave blood at THIS hospital (from its verified donation records).
        [HttpGet("donors")]
        public IActionResult Donors()
        {
            return Ok(MyDonors().Select(d => new
            {
                name = d.Name,
                email = d.Email,
                type = d.BloodType,
                location = d.Location,
                last = d.LastDonation.ToString("dd MMM yyyy"),
                donations = d.Donations,
                availability = d.Available ? "Available" : "Not active",
                availableAgainOn = Format.Date(d.AvailableAgainOn)
            }));
        }

        // "Send email" on the Donor directory: one email per donor (nobody sees the others' addresses).
        // Donors who are not available (e.g. resting after a donation) are skipped.
        [HttpPost("donors/email")]
        public IActionResult EmailDonors(EmailDonorsDto dto)
        {
            if (!emailSender.IsSetUp)
                return BadRequest(new { message = "Email sending is not set up yet (appsettings.json → Email)." });

            var org = db.Organizations.Find(MyOrgId)!;
            var wanted = dto.Emails.Select(e => e.Trim().ToLower()).ToHashSet();
            // Only donors from this hospital's directory can be emailed.
            var donors = MyDonors().Where(d => wanted.Contains(d.Email)).ToList();

            int sent = 0, skipped = 0, failed = 0;
            foreach (var donor in donors)
            {
                if (!donor.Available) { skipped++; continue; }
                try
                {
                    emailSender.Send(donor.Email, $"{org.Name} needs blood donors", DonationRequestEmail(org, donor.Name, dto.BloodType));
                    sent++;
                }
                catch (Exception)
                {
                    failed++;   // e.g. wrong SMTP password or the mail server is down
                }
            }

            if (sent == 0 && failed > 0)
                return StatusCode(502, new { message = "The emails could not be sent. Check the Email settings." });
            return Ok(new { sent, skipped, failed });
        }

        // One donor in the directory (built from the donation records).
        private class DirectoryDonor
        {
            public string Name = "", Email = "", BloodType = "", Location = "";
            public DateOnly LastDonation;
            public int Donations;
            public bool Available;
            public DateOnly? AvailableAgainOn;
        }

        private List<DirectoryDonor> MyDonors()
        {
            var records = db.DonationRecords
                .Where(r => r.OrganizationId == MyOrgId && r.Status == DonationRecordStatus.Verified && r.DonorEmail != null)
                .ToList();

            var result = new List<DirectoryDonor>();
            foreach (var group in records.GroupBy(r => r.DonorEmail!.ToLower()))
            {
                var last = group.OrderByDescending(r => r.DonationDate).First();
                var user = db.Users.FirstOrDefault(u => u.Email == group.Key);
                var donor = new DirectoryDonor
                {
                    Name = user?.FullName ?? last.DonorName,
                    Email = group.Key,
                    BloodType = Format.Blood(last.BloodType),
                    Location = user == null ? "" : string.Join(", ", new[] { user.Area, user.City }.Where(x => !string.IsNullOrEmpty(x))),
                    LastDonation = last.DonationDate,
                    Donations = group.Count()
                };

                if (user != null)
                {
                    // Registered donor: use the availability from their account (3-month rest included).
                    donor.Available = user.IsAvailable && user.Status == AccountStatus.Active;
                    donor.AvailableAgainOn = user.AvailableAgainOn;
                }
                else
                {
                    // No account: available again 3 months after the last donation here.
                    var again = last.DonationDate.AddMonths(DonationRules.RestMonths);
                    donor.Available = again <= DateOnly.FromDateTime(DateTime.Now);
                    donor.AvailableAgainOn = donor.Available ? null : again;
                }
                result.Add(donor);
            }
            return result.OrderBy(d => d.Name).ToList();
        }

        private static bool IsEmail(string text)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        // The email the donors receive.
        private static string DonationRequestEmail(Organization org, string donorName, string? bloodType)
        {
            string e(string? v) => System.Net.WebUtility.HtmlEncode(v ?? "");
            string need = string.IsNullOrWhiteSpace(bloodType) ? "blood" : $"<b>{e(bloodType)}</b> blood";
            return $@"
<div style=""font-family:Arial,sans-serif;max-width:560px;margin:auto;color:#1b1c28"">
  <h2 style=""color:#c91835;margin-bottom:4px"">♥ BloodLink</h2>
  <p>Hello {e(donorName)},</p>
  <p><b>{e(org.Name)}</b> urgently needs {need} donors. You donated with us before —
     if you feel well and are able to donate again, we would be very grateful.</p>
  <p>📍 {e(org.Address ?? org.City)}<br>📞 {e(org.Phone ?? "")}</p>
  <p>You can book a donation appointment on BloodLink, or contact the hospital directly.</p>
  <p style=""color:#7a7b8d;font-size:12px"">Please only donate if you meet the medical requirements and at least 3 months have passed since your last donation.</p>
  <p>Thank you for saving lives.<br>{e(org.Name)} — BloodLink</p>
</div>";
        }

        // ---------------- Donation appointments ----------------

        [HttpGet("appointments")]
        public IActionResult Appointments()
        {
            var list = db.DonationAppointments
                .Include(a => a.Organization)
                .Where(a => a.OrganizationId == MyOrgId)
                .OrderByDescending(a => a.CreatedAt)
                .ToList();
            return Ok(list.Select(AppointmentsController.ToJson));
        }

        // One endpoint for all the hospital decisions: approve / decline / cancel / complete.
        [HttpPut("appointments/{id}")]
        public IActionResult Decide(int id, AppointmentDecisionDto dto)
        {
            var a = db.DonationAppointments.Include(x => x.Organization)
                .FirstOrDefault(x => x.Id == id && x.OrganizationId == MyOrgId);
            if (a == null) return NotFound();

            if (dto.Action == "approve")
            {
                var date = Format.ParseDate(dto.Date);
                var time = Format.ParseTime(dto.Time);
                if (date == null || time == null) return BadRequest(new { message = "Please choose a date and time." });
                if (date < DateOnly.FromDateTime(DateTime.Now)) return BadRequest(new { message = "The date can't be in the past." });
                if (date.Value.DayOfWeek == DayOfWeek.Friday) return BadRequest(new { message = "Fridays are closed. Please choose another day." });

                a.Status = AppointmentStatus.Approved;
                a.ConfirmedDate = date;
                a.ConfirmedTime = time;
                a.HospitalNote = dto.Note;
                a.DeclineReason = null;
            }
            else if (dto.Action == "decline" || dto.Action == "cancel")
            {
                if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length < 3)
                    return BadRequest(new { message = "Please write a short reason for the donor." });

                a.Status = dto.Action == "decline" ? AppointmentStatus.Declined : AppointmentStatus.Cancelled;
                a.DeclineReason = dto.Reason.Trim();
                if (dto.Action == "cancel") a.CancelledBy = "hospital";
            }
            else if (dto.Action == "complete")
            {
                if (a.Status != AppointmentStatus.Approved) return BadRequest(new { message = "Only confirmed appointments can be completed." });
                a.Status = AppointmentStatus.Completed;
                var donationDate = a.ConfirmedDate ?? DateOnly.FromDateTime(DateTime.Now);

                // Always count the donation for the donor (profile page).
                var donor = db.Users.Find(a.DonorId);
                if (donor != null) DonationRules.MarkDonated(donor, donationDate);   // donor rests for 3 months

                db.DonationRecords.Add(new DonationRecord
                {
                    OrganizationId = MyOrgId,
                    DonorId = a.DonorId,
                    DonorName = a.DonorName,
                    DonorEmail = (a.DonorEmail ?? donor?.Email)?.ToLower(),   // → this hospital's Donor directory
                    RecipientName = "Blood bank stock",
                    BloodType = a.BloodType,
                    Units = 1,
                    DonationDate = donationDate,
                    Status = DonationRecordStatus.Verified,
                    AppointmentId = a.Id,
                    Notes = "Appointment " + a.Id
                });

                // Optional: add one unit to the blood stock.
                if (dto.AddStock)
                {
                    var stock = db.BloodInventory.FirstOrDefault(i => i.OrganizationId == MyOrgId && i.BloodType == a.BloodType);
                    if (stock == null)
                    {
                        stock = new BloodInventory { OrganizationId = MyOrgId, BloodType = a.BloodType, Units = 0 };
                        db.BloodInventory.Add(stock);
                    }
                    stock.Units += 1;
                    stock.UpdatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                return BadRequest(new { message = "Unknown action." });
            }

            a.DonorNotified = false;   // the donor will see a message next time he opens his page
            a.UpdatedAt = DateTime.UtcNow;
            db.SaveChanges();
            return Ok(AppointmentsController.ToJson(a));
        }
    }
}
