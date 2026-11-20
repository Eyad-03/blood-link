using BloodLink.Data;
using BloodLink.Helpers;
using BloodLink.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BloodLink.Controllers.Api
{
    // GET /api/donors  → list of donors for the map on /Donor/Search (public page).
    [Route("api/donors")]
    public class DonorsController : ApiBaseController
    {
        public DonorsController(BloodLinkDbContext db) : base(db) { }

        [HttpGet]
        public IActionResult GetDonors()
        {
            // Distances are measured from the signed-in user's city (or Irbid by default).
            var me = CurrentUser();
            var from = Format.Cities["Irbid"];
            if (me?.City != null && Format.Cities.ContainsKey(me.City)) from = Format.Cities[me.City];

            var donors = db.Users
                .Where(u => u.Role == UserRole.Donor && u.Status == AccountStatus.Active)
                .ToList();

            var result = new List<object>();
            foreach (var d in donors)
            {
                // No exact location shared → use the centre of the donor's city.
                double lat = from.Lat, lng = from.Lng;
                if (d.ShareLocation && d.Latitude != null && d.Longitude != null)
                {
                    lat = d.Latitude.Value; lng = d.Longitude.Value;
                }
                else if (d.City != null && Format.Cities.ContainsKey(d.City))
                {
                    lat = Format.Cities[d.City].Lat; lng = Format.Cities[d.City].Lng;
                }

                result.Add(new
                {
                    id = d.Id,
                    name = d.FullName,
                    city = d.City ?? "",
                    area = d.Area ?? "",
                    type = Format.Blood(d.BloodType),
                    distance = Format.DistanceKm(from.Lat, from.Lng, lat, lng),
                    available = d.IsAvailable,
                    availableAgainOn = Format.Date(d.AvailableAgainOn),
                    initials = Format.Initials(d.FullName),
                    lat,
                    lng
                });
            }
            return Ok(result);
        }
    }
}
