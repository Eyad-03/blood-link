using BloodLink.Data;
using BloodLink.Helpers;
using BloodLink.Models.Dtos;
using BloodLink.Models.Entities;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BloodLink.Controllers.Api
{
    // POST /api/auth/register   POST /api/auth/login   GET /api/auth/me
    // GET  /api/auth/google-config   POST /api/auth/google   ("Continue with Google")
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

            var user = NewUser(dto.Role, dto.FullName.Trim(), email, dto.RequiredBloodType, dto.RequestCity, dto.InstitutionName, dto.InstitutionCity);
            user.PasswordHash = hasher.HashPassword(user, dto.Password);

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

        // ---------------- Google ----------------

        // The pages ask for the Google Client ID (empty = the Google button is hidden).
        [HttpGet("google-config")]
        public IActionResult GoogleConfig()
        {
            return Ok(new { clientId = config["Google:ClientId"] ?? "" });
        }

        // The browser sends the credential Google gave it. We check it with Google's library,
        // then sign the user in with OUR JWT (same answer as /login).
        [HttpPost("google")]
        public async Task<IActionResult> Google(GoogleAuthDto dto)
        {
            string clientId = config["Google:ClientId"] ?? "";
            if (clientId == "") return BadRequest(new { message = "Google sign-in is not set up yet." });

            // 1. Is the credential really from Google, for OUR app, and not expired?
            GoogleJsonWebSignature.Payload google;
            try
            {
                google = await GoogleJsonWebSignature.ValidateAsync(dto.Credential,
                    new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } });
            }
            catch (InvalidJwtException)
            {
                return Unauthorized(new { message = "Google sign-in failed. Please try again." });
            }
            if (!google.EmailVerified) return Unauthorized(new { message = "Your Google email is not verified." });

            // 2. Find the account with this email, or create it (Register page only).
            string email = google.Email.Trim().ToLower();
            var user = db.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                if (string.IsNullOrEmpty(dto.Role))
                    return NotFound(new { message = "There is no BloodLink account for " + email + " yet. Create an account first." });

                string name = string.IsNullOrWhiteSpace(google.Name) ? email.Split('@')[0] : google.Name;
                user = NewUser(dto.Role, name, email, dto.RequiredBloodType, dto.RequestCity, dto.InstitutionName, dto.InstitutionCity);
                // Google users have no BloodLink password: we store the hash of a random one.
                user.PasswordHash = hasher.HashPassword(user, Guid.NewGuid().ToString());
                db.Users.Add(user);
            }

            // 3. Same rules as the normal login.
            if (user.Status == AccountStatus.Suspended)
                return StatusCode(403, new { message = "Your account is suspended. Please contact BloodLink support." });

            user.LastActiveAt = DateTime.UtcNow;
            db.SaveChanges();
            return Ok(LoginResult(user));
        }

        // Builds a new user (and, for a hospital, its organization waiting for the admin).
        // Used by Register and by Google.
        private static User NewUser(string? role, string fullName, string email,
            string? requiredBloodType, string? requestCity, string? institutionName, string? institutionCity)
        {
            var user = new User { FullName = fullName, Email = email, Status = AccountStatus.Active };

            if (role == "requester")
            {
                user.Role = UserRole.Recipient;
                user.BloodType = Format.ParseBlood(requiredBloodType);
                user.City = requestCity;
            }
            else if (role == "hospital")
            {
                // A hospital account = a new organization (waiting for the admin) + a staff user.
                var org = new Organization
                {
                    Name = string.IsNullOrWhiteSpace(institutionName) ? fullName : institutionName.Trim(),
                    City = institutionCity ?? "",
                    Email = email,
                    ContactPerson = fullName,
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
            return user;
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
