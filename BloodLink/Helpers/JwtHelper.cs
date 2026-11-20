using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BloodLink.Models.Entities;
using Microsoft.IdentityModel.Tokens;

namespace BloodLink.Helpers
{
    // Creates the JWT token that the browser sends back with every API call
    // (header: "Authorization: Bearer <token>").
    public static class JwtHelper
    {
        public static string CreateToken(User user, IConfiguration config)
        {
            // 1. What we want to remember about the user inside the token.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())   // Donor / Recipient / Hospital / Admin
            };

            // 2. The secret key that signs the token (from appsettings.json → Jwt:Key).
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 3. Build the token.
            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(int.Parse(config["Jwt:ExpireDays"] ?? "7")),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
