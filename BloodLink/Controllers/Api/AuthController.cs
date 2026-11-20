using BloodLink.Data;
using BloodLink.Helpers;
using BloodLink.Models.Dtos;
using BloodLink.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BloodLink.Controllers.Api
{
    // POST /api/auth/register   POST /api/auth/login   GET /api/auth/me
    [Route("api/auth")]
    public class AuthController : ApiBaseController
    {
        private readonly IConfiguration config;
        private readonly PasswordHasher<User> hasher = new();

        public AuthController(BloodLinkDbContext db, IConfiguration config) : base(db)
        {
            this.config = config;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterDto dto)
        {
            string email = dto.Email.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Please fill in your name and email." });

            if (dto.Password.Length < 8)
                return BadRequest(new { message = "Password must be at least 8 characters.", field = "password" });

            if (db.Users.Any(u => u.Email == email))
                return Conflict(new { message = "An account with this email already exists.", field = "email" });

            var user = new User
            {
                FullName = dto.FullName.Trim(),
                Email = email,
                Status = AccountStatus.Active
            };
            user.PasswordHash = hasher.HashPassword(user, dto.Password);

            if (dto.Role == "requester")
            {
                user.Role = UserRole.Recipient;
                user.BloodType = Format.ParseBlood(dto.RequiredBloodType);
                user.City = dto.RequestCity;
            }
            else if (dto.Role == "hospital")
            {
                // A hospital account = a new organization (waiting for the admin) + a staff user.
                var org = new Organization
                {
                    Name = dto.InstitutionName?.Trim() ?? dto.FullName,
                    City = dto.InstitutionCity ?? "",
                    Email = email,
                    ContactPerson = dto.FullName.Trim(),
                    Status = OrganizationStatus.Pending
                };
                // The three documents the admin must check before activating the account.
                org.Documents.Add(new OrganizationDocument { Name = "Ministry of Health operating license", FilePath = "not-uploaded-yet" });
                org.Documents.Add(new OrganizationDocument { Name = "Registration certificate", FilePath = "not-uploaded-yet" });
                org.Documents.Add(new OrganizationDocument { Name = "Authorized representative ID", FilePath = "not-uploaded-yet" });

                user.Role = UserRole.Hospital;
                user.City = org.City;
                user.Organization = org;
            }
            else
            {
                user.Role = UserRole.Donor;
                user.IsAvailable = true;
            }

            db.Users.Add(user);
            db.SaveChanges();

            // Sign the new user in straight away (same answer as /login).
            return Ok(LoginResult(user));
        }

        [HttpPost("login")]
        public IActionResult Login(LoginDto dto)
        {
            string email = dto.Email.Trim().ToLower();
            var user = db.Users.FirstOrDefault(u => u.Email == email);

            // Same message for "wrong email" and "wrong password" (safer).
            if (user == null || hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
                return Unauthorized(new { message = "Incorrect email or password." });

            if (user.Status == AccountStatus.Suspended)
                return StatusCode(403, new { message = "Your account is suspended. Please contact BloodLink support." });

            // Hospital staff can sign in even before approval: /Hospital then shows the
            // verification window (upload documents → wait for the admin). See OnboardingController.

            user.LastActiveAt = DateTime.UtcNow;
            db.SaveChanges();

            return Ok(LoginResult(user));
        }

        // What the browser gets after login or register: the JWT token, the user and the page to open.
        private object LoginResult(User user)
        {
            return new
            {
                token = JwtHelper.CreateToken(user, config),
                redirect = HomePageFor(user.Role),
                user = new { id = user.Id, name = user.FullName, email = user.Email, role = user.Role.ToString() }
            };
        }

        // Who am I? (used by the pages to check the token is still valid)
        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            var user = CurrentUser();
            if (user == null) return Unauthorized();
            return Ok(new { id = user.Id, name = user.FullName, email = user.Email, role = user.Role.ToString() });
        }

        // The page each role lands on after signing in.
        private static string HomePageFor(UserRole role)
        {
            if (role == UserRole.Donor) return "/Donor/Requests";
            if (role == UserRole.Recipient) return "/Donor/Search";
            if (role == UserRole.Hospital) return "/Hospital";
            return "/Admin";
        }
    }
}
