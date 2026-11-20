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
    js/             page scripts; api.js calls the API, nav.js builds the navbar for each role
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

### Sign in with Google

The Login and Register pages show a **"Sign in with Google"** button once a Google Client ID is set
(without it the button is hidden and everything else works).

1. Open <https://console.cloud.google.com/> → create a project (or pick one).
2. **APIs & Services → OAuth consent screen**: choose *External*, fill the app name and your email, save.
3. **APIs & Services → Credentials → Create credentials → OAuth client ID**:
   - Application type: **Web application**
   - Authorized JavaScript origins: `https://localhost:7080` and `http://localhost:5080`
     (add your real domain later)
4. Copy the **Client ID** (ends with `.apps.googleusercontent.com`) into `appsettings.json`:
   ```json
   "Google": { "ClientId": "1234567890-abc.apps.googleusercontent.com" }
   ```

How it works: `google-auth.js` shows Google's button → Google gives the browser a signed
`credential` → `POST /api/auth/google` checks it with `GoogleJsonWebSignature.ValidateAsync`
(package `Google.Apis.Auth`) → we answer with **our own JWT**, like a normal login.
On the Register page the role (and hospital / city details) are taken from the form.
On the Login page an email without an account gets "Create an account first".

### Rest period after a donation

When a donation is recorded (request completed, appointment completed, hospital record, or added on the
profile page) the donor becomes **not available** and `Users.AvailableAgainOn` = donation date + 3 months.
While resting the donor can't switch back on, accept requests, receive new requests or book an appointment.
`DonorReactivationService` (in `Helpers/DonationRules.cs`) runs when the app starts and every hour,
and turns donors back on when their date arrives.

### Navbar per role

`wwwroot/js/nav.js` builds the top links and the profile menu from the signed-in role
(Guest, Donor, Recipient, Hospital, Admin). To change a link, edit the `NAV` object at the top of that file.
If a user opens a page of another role, `Api.requireLogin([...])` sends them to their own home page.

### New hospital accounts

1. The hospital registers and signs in straight away.
2. `/Hospital` shows a verification window (`hospital-onboarding.js`) that can't be closed:
   upload the 3 documents (PDF / JPG / PNG, max 10 MB) → **Send for review**.
3. The admin opens the files in Admin → Accounts, ticks them and activates the account
   (or rejects it with a reason — the hospital then uploads again).
4. The window closes by itself and the dashboard works normally.

Files are saved in `App_Data/uploads` (not public; only the admin API sends them back).
Dashboard APIs use `[ActiveHospital]` (`Helpers/ActiveHospitalAttribute.cs`) so they only work after approval.

### Not included yet

- Uploaded files for hall bookings and report attachments — only the file names are saved
  (hospital verification documents ARE uploaded).
- Real emails for the certificate — `POST /api/send-certificate` only records who it was sent to.
- Payments are demo payments — only the payment method is saved.
