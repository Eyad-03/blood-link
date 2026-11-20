using System.Security.Claims;
using BloodLink.Data;
using BloodLink.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BloodLink.Controllers.Api
{
    // Every API controller inherits from this class.
    // It gives them the database and a quick way to know who is signed in.
    [ApiController]
    public abstract class ApiBaseController : ControllerBase
    {
        protected readonly BloodLinkDbContext db;

        protected ApiBaseController(BloodLinkDbContext db)
        {
            this.db = db;
        }

        // The user id saved inside the JWT token (0 when nobody is signed in).
        protected int CurrentUserId
        {
            get
            {
                var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
                return int.TryParse(value, out int id) ? id : 0;
            }
        }

        // Loads the signed-in user from the database (null when nobody is signed in).
        protected User? CurrentUser()
        {
            if (CurrentUserId == 0) return null;
            return db.Users.Find(CurrentUserId);
        }

        // Saves a line in the admin "Recent activity" list.
        protected void LogAdminAction(string text, string icon, string color)
        {
            db.AdminActivityLogs.Add(new AdminActivityLog
            {
                AdminId = CurrentUserId == 0 ? null : CurrentUserId,
                Text = text,
                Icon = icon,
                Color = color
            });
        }
    }
}
