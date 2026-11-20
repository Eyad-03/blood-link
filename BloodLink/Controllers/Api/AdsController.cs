using BloodLink.Data;
using BloodLink.Helpers;
using BloodLink.Models.Dtos;
using BloodLink.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodLink.Controllers.Api
{
    // Paid hospital advertisements.
    //   GET  /api/ads                     all ads (public Ads page + home page)
    //   POST /api/ads/{id}/view           count one view
    //   GET  /api/hospital/ads            my hospital's ads
    //   POST /api/hospital/ads            create an ad (hidden until it is paid: POST /api/payments/ad/{id})
    //   PUT  /api/hospital/ads/{id}/stop  stop an ad
    [Route("api")]
    public class AdsController : ApiBaseController
    {
        // Same plans as ads-data.js: days on the page and price in JOD.
        private static readonly Dictionary<string, (int Days, decimal Price)> Plans = new()
        {
            ["basic"] = (7, 25),
            ["featured"] = (14, 50),
            ["premium"] = (30, 90)
        };
        private const decimal VatRate = 0.16m;

        public AdsController(BloodLinkDbContext db) : base(db) { }

        [HttpGet("ads")]
        public IActionResult All()
        {
            var ads = db.Advertisements.Include(a => a.Organization)
                .Where(a => !a.AwaitingPayment)   // unpaid ads are not shown
                .OrderByDescending(a => a.CreatedAt).ToList();
            return Ok(ads.Select(ToJson));
        }

        [HttpPost("ads/{id}/view")]
        public IActionResult AddView(int id)
        {
            var ad = db.Advertisements.Find(id);
            if (ad == null) return NotFound();
            ad.Views++;
            db.SaveChanges();
            return Ok();
        }

        [Authorize(Roles = "Hospital")]
        [ActiveHospital]
        [HttpGet("hospital/ads")]
        public IActionResult Mine()
        {
            int orgId = CurrentUser()?.OrganizationId ?? 0;
            var ads = db.Advertisements.Include(a => a.Organization)
                .Where(a => a.OrganizationId == orgId && !a.AwaitingPayment)
                .OrderByDescending(a => a.CreatedAt).ToList();
            return Ok(ads.Select(ToJson));
        }

        [Authorize(Roles = "Hospital")]
        [ActiveHospital]
        [HttpPost("hospital/ads")]
        public IActionResult Create(CreateAdDto dto)
        {
            int orgId = CurrentUser()?.OrganizationId ?? 0;
            var org = db.Organizations.Find(orgId);
            if (org == null) return NotFound();

            if (!Plans.ContainsKey(dto.Plan)) dto.Plan = "basic";
            var startDate = Format.ParseDate(dto.StartDate) ?? DateOnly.FromDateTime(DateTime.Now);
            var eventDate = Format.ParseDate(dto.EventDate);
            if (string.IsNullOrWhiteSpace(dto.Title) || eventDate == null)
                return BadRequest(new { message = "Please fill in all required fields." });
            if (eventDate < startDate)
                return BadRequest(new { message = "The ad must go live on or before the event date." });

            var plan = Plans[dto.Plan];
            decimal vat = Math.Round(plan.Price * VatRate, 2);

            var ad = new Advertisement
            {
                OrganizationId = orgId,
                Title = dto.Title.Trim(),
                Department = dto.Department,
                Category = dto.Category,
                Badge = dto.Badge,
                Description = dto.Description,
                EventDate = eventDate.Value,
                StartTime = Format.ParseTime(dto.StartTime),
                EndTime = Format.ParseTime(dto.EndTime),
                Location = dto.Location,
                Phone = dto.Phone,
                BookingRequired = dto.Booking,
                Plan = Enum.Parse<AdPlan>(dto.Plan, ignoreCase: true),
                StartDate = startDate,
                EndDate = startDate.AddDays(plan.Days - 1),
                Fee = plan.Price,
                Vat = vat,
                TotalPaid = plan.Price + vat,
                AwaitingPayment = true   // becomes visible after the Visa payment
            };
            db.Advertisements.Add(ad);
            db.SaveChanges();

            ad.Organization = org;
            return Ok(ToJson(ad));
        }

        [Authorize(Roles = "Hospital")]
        [ActiveHospital]
        [HttpPut("hospital/ads/{id}/stop")]
        public IActionResult Stop(int id)
        {
            int orgId = CurrentUser()?.OrganizationId ?? 0;
            var ad = db.Advertisements.FirstOrDefault(a => a.Id == id && a.OrganizationId == orgId);
            if (ad == null) return NotFound();
            ad.IsCancelled = true;
            db.SaveChanges();
            return Ok();
        }

        // The JSON shape used by ads-data.js (cardHTML, status, ...).
        private object ToJson(Advertisement a)
        {
            int myOrgId = CurrentUser()?.OrganizationId ?? 0;
            return new
            {
                id = a.Id.ToString(),
                owner = a.OrganizationId == myOrgId ? "self" : "other",
                hospital = a.Organization.Name,
                title = a.Title,
                department = a.Department,
                category = a.Category,
                badge = a.Badge ?? "",
                description = a.Description ?? "",
                eventDate = Format.Date(a.EventDate),
                startTime = Format.Time(a.StartTime),
                endTime = Format.Time(a.EndTime),
                location = a.Location ?? "",
                phone = a.Phone ?? "",
                booking = a.BookingRequired,
                plan = a.Plan.ToString().ToLower(),
                startDate = Format.Date(a.StartDate),
                endDate = Format.Date(a.EndDate),
                fee = a.Fee,
                vat = a.Vat,
                paid = a.TotalPaid,
                receipt = "RC-" + a.Id,
                views = a.Views,
                cancelled = a.IsCancelled,
                createdAt = Format.Iso(a.CreatedAt)
            };
        }
    }
}
