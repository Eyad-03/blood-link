using BloodLink.Data;
using BloodLink.Helpers;
using BloodLink.Models.Dtos;
using BloodLink.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BloodLink.Controllers.Api
{
    // POST /api/reports  → "Report an issue" page. Anyone can send a report.
    [Route("api/reports")]
    public class ReportsController : ApiBaseController
    {
        public ReportsController(BloodLinkDbContext db) : base(db) { }

        [HttpPost]
        public IActionResult Create(CreateReportDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Email) || dto.Body.Trim().Length < 20)
                return BadRequest(new { message = "Check the highlighted fields and try again." });

            var report = new Report
            {
                ReporterId = CurrentUserId == 0 ? null : CurrentUserId,
                FromName = dto.From.Trim(),
                ReporterRole = dto.Role,
                Email = dto.Email.Trim(),
                Category = dto.Category,
                Related = dto.Related,
                Title = dto.Title.Trim(),
                Body = dto.Body.Trim(),
                Priority = dto.Priority == "high" ? ReportPriority.High : dto.Priority == "low" ? ReportPriority.Low : ReportPriority.Medium
            };
            // Only the file names are saved (no file storage in this version).
            foreach (var name in dto.Attachments.Take(5))
                report.Attachments.Add(new ReportAttachment { FilePath = name });

            db.Reports.Add(report);
            db.SaveChanges();
            return Ok(ToJson(report));
        }

        // "InReview" -> "In review"
        public static string StatusText(ReportStatus status)
        {
            return status == ReportStatus.InReview ? "In review" : status.ToString();
        }

        // JSON shape used by admin.js (Attachments must be loaded with .Include).
        public static object ToJson(Report r)
        {
            return new
            {
                id = r.Id.ToString(),
                title = r.Title,
                category = r.Category,
                priority = r.Priority.ToString().ToLower(),
                from = r.FromName,
                role = r.ReporterRole,
                email = r.Email,
                createdAt = Format.Iso(r.CreatedAt),
                status = StatusText(r.Status),
                read = r.IsRead,
                related = r.Related,
                attachments = r.Attachments.Select(a => a.FilePath),
                body = r.Body,
                response = r.Response ?? ""
            };
        }
    }
}
