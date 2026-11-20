using System.Security.Claims;
using BloodLink.Data;
using BloodLink.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BloodLink.Helpers
{
    // Put [ActiveHospital] on an action (or a whole controller) that only APPROVED hospitals may use.
    // It runs before the action: if the user's institution is not "Active" yet, the action is
    // skipped and the browser gets 403 with a message.
    public class ActiveHospitalAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<BloodLinkDbContext>();

            int.TryParse(context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId);
            int? orgId = db.Users.Where(u => u.Id == userId).Select(u => u.OrganizationId).FirstOrDefault();

            bool isActive = db.Organizations.Any(o => o.Id == orgId && o.Status == OrganizationStatus.Active);
            if (!isActive)
            {
                context.Result = new ObjectResult(new { message = "Your institution is waiting for BloodLink approval." })
                {
                    StatusCode = 403
                };
            }
        }
    }
}
