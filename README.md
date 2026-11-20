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

### BloodLink Assistant (chatbot with Ollama)

A round button at the bottom right of every page opens a chat about blood, blood donation and
how to use BloodLink (Arabic or English). Files: `wwwroot/js/chatbot.js`, `wwwroot/css/chatbot.css`,
`Controllers/Api/ChatbotController.cs`.

The answer is generated by **Ollama** running on your computer and streamed to the browser piece by
piece, so it starts appearing right away.

Setup:
1. Install Ollama from <https://ollama.com/download> (it runs in the background after install).
2. Download the small, fast model once: `ollama pull llama3.2:1b`
3. Run the site — the first question loads the model (a few seconds), the next ones are fast.

Optional settings in `appsettings.json` (defaults shown):
`"Ollama": { "Url": "http://localhost:11434", "Model": "llama3.2:1b" }` —
`qwen2.5:1.5b` is also fast and better in Arabic; bigger models answer better but slower.

### Donor directory + "we need blood" emails

- **Donation verification** asks for the donor's and the recipient's email. A donor with a BloodLink
  account (same email) is linked: the donation counts on their profile and starts the 3-month rest.
- **Donor directory** shows only donors who donated at *this* hospital (from its verified records),
  with the number of donations, last donation and availability.
- **Send to All Email** / **Email** sends a real email from the server (`Helpers/EmailSender.cs`) —
  one email per donor, donors who are not available are skipped. If a blood type filter is chosen,
  the email says which blood is needed.

Email setup (Gmail example):
1. Turn on 2-Step Verification on the Gmail account, then create an **App password**
   (<https://myaccount.google.com/apppasswords>).
2. `appsettings.json` → `Email:UserName` = the Gmail address (Host `smtp.gmail.com`, Port `587`, EnableSsl `true`).
3. Put the app password in **User Secrets** (not in Git): `{ "Email": { "Password": "xxxx xxxx xxxx xxxx" } }`

### Payments — Visa only, with Stripe

Every payment on BloodLink (hospital ads, hall bookings on the dashboard and on the Hall page)
uses **Stripe** and accepts **Visa cards only**.

How it works (`stripe-pay.js` + `Controllers/Api/PaymentsController.cs`):
1. The card field is Stripe's own form — the card number never reaches our server.
2. Stripe gives the browser a PaymentMethod id (`pm_...`); our server asks Stripe which card it is
   and refuses anything that is not Visa, **before** taking money.
3. The server takes the amount from the database (never from the browser) and confirms a PaymentIntent.
4. If the bank asks for 3D Secure, Stripe shows it, then `POST /api/payments/confirm` finishes.
5. A new ad is saved as "awaiting payment" and only appears after it is paid.

Setup:
1. Create an account on <https://dashboard.stripe.com/> and stay in **Test mode**.
2. **Developers → API keys**: copy the *Publishable key* (`pk_test_...`) and the *Secret key* (`sk_test_...`).
3. Put the publishable key in `appsettings.json` → `Stripe:PublishableKey`.
4. Put the **secret key** in User Secrets (never in Git): in Visual Studio right-click the project →
   **Manage User Secrets** and add:
   ```json
   { "Stripe": { "SecretKey": "sk_test_..." } }
   ```
5. `Stripe:Currency`: prices on the site are in JOD. Most Stripe accounts can't charge JOD, so use `usd` —
   the server converts at 1 JOD = 1.41 USD (fixed peg). For any other currency also set
   `Stripe:ExchangeRate` (how many of that currency = 1 JOD). Use `jod` only if your account supports it.

Test cards (any future expiry, any CVC): `4242 4242 4242 4242` Visa ✓ ·
`4000 0025 0000 3155` Visa with 3D Secure · `5555 5555 5555 4444` Mastercard ✗ (refused) ·
`4000 0000 0000 0002` declined.

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
