# BloodLink — ASP.NET Core MVC

BloodLink as an ASP.NET Core MVC project (.NET 10), ready to open in Visual Studio.
The pages are MVC views, and the data comes from a small JSON API (`/api/...`) backed by SQL Server.
Sign-in uses JWT tokens.

## Run it

1. Make sure SQL Server Express is installed (the connection string is in `appsettings.json`).
2. Open `BloodLink.sln` in Visual Studio 2026.
3. Press **F5** (or Ctrl+F5).

On start the app creates/updates the database (`Database.Migrate()`) and adds demo accounts
the first time (`Data/DbInitializer.cs`):

| Role      | Email                 | Password        |
|-----------|-----------------------|-----------------|
| Admin     | admin@bloodlink.jo    | Admin@123       |
| Hospital  | hospital@alhayat.jo   | Hospital@123    |
| Recipient | ahmad@example.com     | Recipient@123   |
| Donor     | lina@example.com (and omar@, zaid@, noor@ … @example.com) | Donor@123 |

Or from a terminal:

```bash
dotnet run --project BloodLink
```

## Layout

```
BloodLink.sln
BloodLink/
  Controllers/      one controller per area (Home, Account, Donor, Recipient, Hall, Hospital, Admin) — returns the pages
  Controllers/Api/  the JSON API used by the JavaScript (one controller per feature)
  Views/            one .cshtml per page (full page, Layout = null, same HTML as before)
  Models/Entities/  database tables
  Models/Dtos/      the data the pages send to the API
  Data/             DbContext, migrations, demo data
  Helpers/          JWT token + small formatting helpers
  wwwroot/
    css/            the original stylesheets, unchanged
    js/             page scripts; api.js is the shared helper that calls the API
    images/
  Program.cs
```

## Old page → new URL

| Old file                 | New URL                | Controller / View                     |
|--------------------------|------------------------|---------------------------------------|
| home.html                | `/`                    | Home / Index                          |
| ads.html                 | `/Home/Ads`            | Home / Ads                            |
| report.html              | `/Home/Report`         | Home / Report                         |
| login.html               | `/Account/Login`       | Account / Login                       |
| register.html            | `/Account/Register`    | Account / Register                    |
| profile.html             | `/Account/Profile`     | Account / Profile                     |
| donorSearch.html         | `/Donor/Search`        | Donor / Search                        |
| donate.html              | `/Donor/Donate`        | Donor / Donate                        |
| donorRequests.html       | `/Donor/Requests`      | Donor / Requests                      |
| donorAppointments.html   | `/Donor/Appointments`  | Donor / Appointments                  |
| bloodRequests.html       | `/Recipient/Requests`  | Recipient / Requests                  |
| hall.html                | `/Hall`                | Hall / Index                          |
| hallRequests.html        | `/Hall/Requests`       | Hall / Requests                       |
| hospital.html            | `/Hospital`            | Hospital / Index                      |
| admin.html               | `/Admin`               | Admin / Index                         |

## Note on Razor

Inside `.cshtml` files the `@` sign is Razor syntax, so a literal `@` is written `@@`
(for example `you@@example.com`). It still shows as a single `@` in the browser.

## How the backend works

1. **Login** — `login.js` posts to `POST /api/auth/login`. The server checks the password
   and returns a **JWT token**. `api.js` keeps it in `localStorage`.
2. **Every API call** — `api.js` adds the header `Authorization: Bearer <token>`.
   ASP.NET reads it (`AddJwtBearer` in `Program.cs`), so `[Authorize(Roles = "Donor")]` works.
3. **Pages** — each page script calls `Api.requireLogin(["Donor"])` and then loads its data,
   e.g. `await Api.get("/api/requests/received")`.

The JWT settings are in `appsettings.json` → `Jwt`. Change `Jwt:Key` to your own long secret.

### Main flow

| Who | Page | API |
|-----|------|-----|
| Anyone | Register / Login | `POST /api/auth/register`, `POST /api/auth/login` |
| Donor | Donate (become visible on the map) | `PUT /api/profile/donor-offer` |
| Recipient | Find donors → send a request | `GET /api/donors`, `POST /api/requests` |
| Donor | My requests → accept / decline / complete, chat | `GET /api/requests/received`, `PUT /api/requests/{id}/status`, `/api/chat/{id}` |
| Recipient | My requests → contact & chat with the donor | `GET /api/requests/sent` |
| Donor | Book a donation appointment | `GET /api/facilities`, `POST /api/appointments` |
| Hospital | Approve / complete appointments, records, stock, ads | `/api/hospital/...`, `/api/hospital/ads` |
| Anyone signed in | Book a hall → admin approves → pay | `/api/halls/...`, `/api/admin/bookings/{id}` |
| Anyone | Report an issue | `POST /api/reports` |
| Admin | Verify hospitals, suspend users, bookings, reports | `/api/admin/...` |

A new hospital account can sign in only after the admin ticks its 3 documents and activates it.

### Not included yet

- Uploaded files (hall documents, report attachments) — only the file names are saved.
- Real emails for the certificate — `POST /api/send-certificate` only records who it was sent to.
- Payments are demo payments — only the payment method is saved.
