using System.Text;
using BloodLink.Data;
using BloodLink.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// MVC pages (Controllers + Views) and the JSON API controllers (Controllers/Api).
builder.Services.AddControllersWithViews();

// Database (SQL Server). Connection string is in appsettings.json → ConnectionStrings:DefaultConnection
builder.Services.AddDbContext<BloodLinkDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- JWT authentication ----------
// The browser gets a token from POST /api/auth/login and sends it back in the
// "Authorization: Bearer <token>" header. Settings are in appsettings.json → Jwt.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
builder.Services.AddAuthorization();

// HttpClient for calling Ollama (the chatbot, Controllers/Api/ChatbotController.cs).
builder.Services.AddHttpClient();

// Sends emails with SMTP (Helpers/EmailSender.cs, settings in appsettings.json → Email).
builder.Services.AddSingleton<EmailSender>();

// Turns donors back on 3 months after their donation (Helpers/DonationRules.cs).
builder.Services.AddHostedService<DonorReactivationService>();

var app = builder.Build();

// Create / update the database and add the demo accounts on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BloodLinkDbContext>();
    db.Database.Migrate();
    DbInitializer.Seed(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();   // reads the JWT token (must come before UseAuthorization)
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
