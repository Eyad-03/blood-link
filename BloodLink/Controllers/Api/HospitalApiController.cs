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
        public HospitalApiController(BloodLinkDbContext db) : base(db) { }

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

            // If the donor has an account with the same name, link it (it counts on his profile).
            string donorName = dto.Donor.Trim();
            var donorUser = db.Users.FirstOrDefault(u => u.Role == UserRole.Donor && u.FullName == donorName);

            var record = new DonationRecord
            {
                OrganizationId = MyOrgId,
                DonorId = donorUser?.Id,
                DonorName = donorName,
                RecipientName = dto.Recipient.Trim(),
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
                recipient = r.RecipientName,
                type = Format.Blood(r.BloodType),
                units = r.Units,
                date = Format.Date(r.DonationDate),
                status = r.Status == DonationRecordStatus.Verified ? "Verified" : "Pending verification",
                notes = r.Notes ?? ""
            };
        }

        // ---------------- Donor directory ----------------

        [HttpGet("donors")]
        public IActionResult Donors()
        {
            var donors = db.Users.Where(u => u.Role == UserRole.Donor && u.Status == AccountStatus.Active)
                .OrderBy(u => u.FullName).ToList();

            return Ok(donors.Select(d => new
            {
                name = d.FullName,
                email = d.Email,
                type = Format.Blood(d.BloodType),
                location = string.Join(", ", new[] { d.Area, d.City }.Where(x => !string.IsNullOrEmpty(x))),
                last = d.LastDonationDate == null ? "—" : d.LastDonationDate.Value.ToString("dd MMM yyyy"),
                availability = d.IsAvailable ? "Available" : "Not active",
                availableAgainOn = Format.Date(d.AvailableAgainOn)
            }));
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
