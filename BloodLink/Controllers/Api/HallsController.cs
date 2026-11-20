using BloodLink.Data;
using BloodLink.Helpers;
using BloodLink.Models.Dtos;
using BloodLink.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodLink.Controllers.Api
{
    // Halls that can be booked for campaigns and conferences.
    //   GET  /api/halls                       list of halls (public)
    //   POST /api/halls/bookings              send a booking request
    //   GET  /api/halls/bookings/mine         my booking requests
    //   (paying an approved booking: POST /api/payments/hall-booking/{id} — PaymentsController)
    [Route("api/halls")]
    public class HallsController : ApiBaseController
    {
        public HallsController(BloodLinkDbContext db) : base(db) { }

        [HttpGet]
        public IActionResult All()
        {
            var halls = db.Halls.Where(h => h.IsActive).OrderBy(h => h.Id).ToList();
            return Ok(halls.Select(h => new
            {
                id = h.Id,
                name = h.Name,
                governorate = h.Governorate,
                entity = h.EntityType ?? "",
                events = (h.SupportedEvents ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries),
                capacity = h.Capacity,
                price = h.Price,
                rating = h.Rating,
                tag = h.Tag ?? "",
                tagClass = h.Tag == "Popular choice" ? "" : h.Tag == "Affordable" ? "amber" : "green",
                location = h.Location ?? "",
                image = h.ImageUrl ?? "",
                description = h.Description ?? "",
                features = (h.Features ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            }));
        }

        [Authorize]
        [HttpPost("bookings")]
        public IActionResult Book(CreateHallBookingDto dto)
        {
            var hall = db.Halls.Find(dto.HallId);
            if (hall == null) return NotFound(new { message = "Hall not found." });

            var eventDate = Format.ParseDate(dto.EventDate);
            if (eventDate == null || eventDate <= DateOnly.FromDateTime(DateTime.Now))
                return BadRequest(new { message = "Please choose an event date from tomorrow onwards." });
            if (dto.Documents.Count == 0)
                return BadRequest(new { message = "Please attach all required documents." });

            var booking = new HallBooking
            {
                HallId = hall.Id,
                RequestedById = CurrentUserId,
                OrganizationName = dto.Organization,
                ContactName = dto.ContactName,
                Email = dto.Email,
                Phone = dto.Phone,
                EventType = dto.EventType,
                EventDate = eventDate.Value,
                EventTime = dto.EventTime,
                Attendees = dto.Attendees,
                Description = dto.Description,
                Price = hall.Price,
                Status = HallBookingStatus.PendingApproval
            };
            // Only the file names are saved (no file storage in this version).
            foreach (var doc in dto.Documents)
                booking.Documents.Add(new HallBookingDocument { Label = doc.Label, FilePath = doc.FileName });

            db.HallBookings.Add(booking);
            db.SaveChanges();

            booking.Hall = hall;
            return Ok(ToJson(booking));
        }

        [Authorize]
        [HttpGet("bookings/mine")]
        public IActionResult Mine()
        {
            var list = db.HallBookings
                .Include(b => b.Hall).Include(b => b.Documents)
                .Where(b => b.RequestedById == CurrentUserId)
                .OrderByDescending(b => b.CreatedAt)
                .ToList();
            return Ok(list.Select(ToJson));
        }

        // "PendingApproval" -> "Pending approval" (the text used by the pages)
        public static string StatusText(HallBookingStatus status)
        {
            return status == HallBookingStatus.PendingApproval ? "Pending approval" : status.ToString();
        }

        // JSON shape used by hall.js, hallRequests.js, hall-bookings.js and admin.js.
        // (Hall and Documents must be loaded with .Include.)
        public static object ToJson(HallBooking b)
        {
            return new
            {
                id = b.Id.ToString(),
                hallId = b.HallId,
                hallName = b.Hall.Name,
                governorate = b.Hall.Governorate,
                price = b.Price,
                organization = b.OrganizationName,
                contactName = b.ContactName,
                email = b.Email,
                phone = b.Phone,
                eventType = b.EventType,
                eventDate = Format.Date(b.EventDate),
                eventTime = b.EventTime ?? "",
                attendees = b.Attendees,
                description = b.Description ?? "",
                documents = b.Documents.Select(d => new { label = d.Label, fileName = d.FilePath }),
                status = StatusText(b.Status),
                adminNote = b.AdminNote ?? "",
                paymentMethod = b.PaymentMethod ?? "",
                payMethod = b.PaymentMethod ?? "",
                paidAt = Format.Iso(b.PaidAt),
                amountPaid = b.Status == HallBookingStatus.Paid || b.Status == HallBookingStatus.Completed ? b.Price : 0,
                receipt = "HB-" + b.Id,
                createdAt = Format.Iso(b.CreatedAt)
            };
        }
    }
}
