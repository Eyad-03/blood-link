using BloodLink.Data;
using BloodLink.Helpers;
using BloodLink.Models.Dtos;
using BloodLink.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodLink.Controllers.Api
{
    // Donation appointments — donor side (/Donor/Appointments).
    //   GET  /api/facilities                  hospitals & centers that accept bookings (public)
    //   GET  /api/appointments/mine           my appointments
    //   POST /api/appointments                book a new appointment
    //   PUT  /api/appointments/{id}/cancel    cancel my appointment
    //   POST /api/appointments/notified       "I have seen the latest changes"
    [Route("api")]
    public class AppointmentsController : ApiBaseController
    {
        public AppointmentsController(BloodLinkDbContext db) : base(db) { }

        [HttpGet("facilities")]
        public IActionResult Facilities()
        {
            var orgs = db.Organizations
                .Where(o => o.Status == OrganizationStatus.Active)
                .OrderBy(o => o.Name)
                .ToList();

            return Ok(orgs.Select(o => new
            {
                id = o.Id.ToString(),
                name = o.Name,
                type = Format.OrgType(o.Type),
                city = o.City,
                address = o.Address ?? o.City,
                open = Format.Time(o.OpenTime),
                close = Format.Time(o.CloseTime)
            }));
        }

        [Authorize(Roles = "Donor")]
        [HttpGet("appointments/mine")]
        public IActionResult Mine()
        {
            var list = db.DonationAppointments
                .Include(a => a.Organization)
                .Where(a => a.DonorId == CurrentUserId)
                .OrderByDescending(a => a.CreatedAt)
                .ToList();

            return Ok(list.Select(ToJson));
        }

        [Authorize(Roles = "Donor")]
        [HttpPost("appointments")]
        public IActionResult Create(CreateAppointmentDto dto)
        {
            int.TryParse(dto.FacilityId, out int orgId);
            var org = db.Organizations.FirstOrDefault(o => o.Id == orgId && o.Status == OrganizationStatus.Active);
            if (org == null) return BadRequest(new { message = "Please choose a hospital or donation center." });

            var date = Format.ParseDate(dto.RequestedDate);
            var time = Format.ParseTime(dto.RequestedTime);
            var bloodType = Format.ParseBlood(dto.Donor.Type);
            if (date == null || time == null) return BadRequest(new { message = "Please choose a date and time." });
            if (bloodType == null) return BadRequest(new { message = "Please select your blood type." });
            if (date <= DateOnly.FromDateTime(DateTime.Now)) return BadRequest(new { message = "Choose a date from tomorrow onwards." });
            if (date.Value.DayOfWeek == DayOfWeek.Friday) return BadRequest(new { message = "Centers are closed on Fridays." });
            if (dto.Eligibility.State == "blocked") return BadRequest(new { message = "You are not eligible to donate right now." });

            var e = dto.Eligibility;
            var appointment = new DonationAppointment
            {
                DonorId = CurrentUserId,
                OrganizationId = org.Id,
                DonorName = dto.Donor.Name.Trim(),
                DonorPhone = dto.Donor.Phone.Trim(),
                DonorEmail = dto.Donor.Email,
                BloodType = bloodType.Value,
                RequestedDate = date.Value,
                RequestedTime = time.Value,
                Notes = dto.Notes,
                Age = e.Age,
                WeightKg = e.Weight,
                LastDonationDate = Format.ParseDate(e.LastDonation),
                FeelingWell = e.Well != "no",
                TakesMedication = e.Meds == "yes",
                MedicationDetail = e.MedsDetail,
                TravelledAbroad = e.Travel == "yes",
                TravelDetail = e.TravelDetail,
                RecentProcedure = e.Procedure == "yes",
                EligibilityState = e.State == "review" ? EligibilityState.Review : EligibilityState.Ok,
                EligibilityFlags = string.Join("\n", e.Flags),
                Status = AppointmentStatus.Pending,
                DonorNotified = true
            };
            db.DonationAppointments.Add(appointment);

            // Keep the donor's profile up to date with what he typed.
            var me = CurrentUser()!;
            me.BloodType ??= bloodType;
            if (string.IsNullOrEmpty(me.Phone)) me.Phone = appointment.DonorPhone;

            db.SaveChanges();
            appointment.Organization = org;
            return Ok(ToJson(appointment));
        }

        [Authorize(Roles = "Donor")]
        [HttpPut("appointments/{id}/cancel")]
        public IActionResult Cancel(int id)
        {
            var a = db.DonationAppointments.FirstOrDefault(x => x.Id == id && x.DonorId == CurrentUserId);
            if (a == null) return NotFound();
            if (a.Status != AppointmentStatus.Pending && a.Status != AppointmentStatus.Approved)
                return BadRequest(new { message = "This appointment can't be cancelled." });

            a.Status = AppointmentStatus.Cancelled;
            a.CancelledBy = "donor";
            a.UpdatedAt = DateTime.UtcNow;
            db.SaveChanges();
            return Ok();
        }

        [Authorize(Roles = "Donor")]
        [HttpPost("appointments/notified")]
        public IActionResult MarkNotified()
        {
            var list = db.DonationAppointments.Where(a => a.DonorId == CurrentUserId && !a.DonorNotified).ToList();
            foreach (var a in list) a.DonorNotified = true;
            db.SaveChanges();
            return Ok();
        }

        // Turns an appointment into the JSON shape used by donorAppointments.js and hospital.js.
        // (The Organization must be loaded with .Include.)
        public static object ToJson(DonationAppointment a)
        {
            string yesNo(bool value) => value ? "yes" : "no";
            return new
            {
                id = a.Id.ToString(),
                ownerId = a.DonorId.ToString(),
                facilityId = a.OrganizationId.ToString(),
                facilityName = a.Organization.Name,
                facilityType = Format.OrgType(a.Organization.Type),
                facilityCity = a.Organization.City,
                donor = new { name = a.DonorName, phone = a.DonorPhone, email = a.DonorEmail ?? "", type = Format.Blood(a.BloodType) },
                requestedDate = Format.Date(a.RequestedDate),
                requestedTime = Format.Time(a.RequestedTime),
                notes = a.Notes ?? "",
                eligibility = new
                {
                    age = a.Age,
                    weight = a.WeightKg,
                    lastDonation = Format.Date(a.LastDonationDate),
                    well = yesNo(a.FeelingWell),
                    meds = yesNo(a.TakesMedication),
                    medsDetail = a.MedicationDetail ?? "",
                    travel = yesNo(a.TravelledAbroad),
                    travelDetail = a.TravelDetail ?? "",
                    procedure = yesNo(a.RecentProcedure),
                    state = a.EligibilityState.ToString().ToLower(),
                    flags = string.IsNullOrEmpty(a.EligibilityFlags) ? new string[0] : a.EligibilityFlags.Split('\n')
                },
                status = a.Status.ToString(),
                confirmedDate = Format.Date(a.ConfirmedDate),
                confirmedTime = Format.Time(a.ConfirmedTime),
                hospitalNote = a.HospitalNote ?? "",
                declineReason = a.DeclineReason ?? "",
                cancelledBy = a.CancelledBy ?? "",
                donorNotified = a.DonorNotified,
                createdAt = Format.Iso(a.CreatedAt),
                updatedAt = Format.Iso(a.UpdatedAt)
            };
        }
    }
}
