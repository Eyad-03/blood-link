using BloodLink.Data;
using BloodLink.Helpers;
using BloodLink.Models.Dtos;
using BloodLink.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace BloodLink.Controllers.Api
{
    // All payments on BloodLink go through Stripe, and ONLY Visa cards are accepted.
    //   GET  /api/payments/config                  Stripe publishable key (for stripe-pay.js)
    //   POST /api/payments/hall-booking/{id}       pay an approved hall booking
    //   POST /api/payments/ad/{id}                 pay for a new advertisement
    //   POST /api/payments/confirm                 finish a payment after the bank's 3D Secure check
    //
    // How a payment works:
    //   1. The browser sends the card to Stripe (not to us) and gets a PaymentMethod id "pm_...".
    //   2. We ask Stripe about that card: if it is not Visa → refuse.
    //   3. We calculate the amount from OUR database (never trust a price sent by the browser).
    //   4. We create and confirm a PaymentIntent at Stripe → "succeeded" means the money was taken.
    [Authorize]
    [Route("api/payments")]
    public class PaymentsController : ApiBaseController
    {
        private readonly IConfiguration config;

        public PaymentsController(BloodLinkDbContext db, IConfiguration config) : base(db)
        {
            this.config = config;
            // The secret key (sk_...) — keep it in User Secrets, never in JavaScript.
            StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
        }

        private string Currency => (config["Stripe:Currency"] ?? "jod").ToLower();
        private bool IsSetUp => !string.IsNullOrEmpty(config["Stripe:SecretKey"]) && !string.IsNullOrEmpty(config["Stripe:PublishableKey"]);

        [AllowAnonymous]
        [HttpGet("config")]
        public IActionResult Config()
        {
            // The publishable key (pk_...) is safe to send to the browser.
            return Ok(new { publishableKey = IsSetUp ? config["Stripe:PublishableKey"] : "", currency = Currency });
        }

        // ---------------- Hall booking ----------------

        [HttpPost("hall-booking/{id}")]
        public async Task<IActionResult> PayHallBooking(int id, PayDto dto)
        {
            var booking = db.HallBookings.Include(b => b.Hall)
                .FirstOrDefault(b => b.Id == id && b.RequestedById == CurrentUserId);
            if (booking == null) return NotFound(new { message = "Booking not found." });
            if (booking.Status != HallBookingStatus.Approved)
                return BadRequest(new { message = "This request is not waiting for payment." });

            return await Charge(dto.PaymentMethodId, booking.Price,
                $"Hall booking #{booking.Id} – {booking.Hall.Name}", "hall-booking", booking.Id);
        }

        // ---------------- Advertisement ----------------

        [ActiveHospital]
        [HttpPost("ad/{id}")]
        public async Task<IActionResult> PayAd(int id, PayDto dto)
        {
            int orgId = CurrentUser()?.OrganizationId ?? 0;
            var ad = db.Advertisements.FirstOrDefault(a => a.Id == id && a.OrganizationId == orgId);
            if (ad == null) return NotFound(new { message = "Ad not found." });
            if (!ad.AwaitingPayment) return BadRequest(new { message = "This ad is already paid." });

            return await Charge(dto.PaymentMethodId, ad.TotalPaid, $"Advertisement #{ad.Id} – {ad.Title}", "ad", ad.Id);
        }

        // ---------------- After 3D Secure ----------------

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm(ConfirmPaymentDto dto)
        {
            if (!IsSetUp) return BadRequest(new { message = "Online payments are not set up yet." });

            PaymentIntent intent;
            try
            {
                intent = await new PaymentIntentService().GetAsync(dto.PaymentIntentId,
                    new PaymentIntentGetOptions { Expand = new List<string> { "payment_method" } });
            }
            catch (StripeException ex)
            {
                return BadRequest(new { message = ex.StripeError?.Message ?? "Payment not found." });
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, new { message = "Could not reach the payment service. Please try again." });
            }

            // Only the person who started the payment can finish it.
            if (!intent.Metadata.TryGetValue("userId", out var userId) || userId != CurrentUserId.ToString())
                return Forbid();
            if (intent.Status != "succeeded")
                return BadRequest(new { message = "The payment was not completed. Please try again." });

            MarkAsPaid(intent.Metadata["kind"], int.Parse(intent.Metadata["id"]), intent.Id, intent.PaymentMethod?.Card?.Last4);
            return Ok(new { status = "succeeded" });
        }

        // ---------------- The payment itself ----------------

        private async Task<IActionResult> Charge(string paymentMethodId, decimal amount, string description, string kind, int id)
        {
            if (!IsSetUp) return BadRequest(new { message = "Online payments are not set up yet." });
            if (string.IsNullOrWhiteSpace(paymentMethodId)) return BadRequest(new { message = "Please enter your card details." });

            try
            {
                // 1. Visa only: ask Stripe which card this is BEFORE taking any money.
                var card = await new PaymentMethodService().GetAsync(paymentMethodId);
                if (card.Card?.Brand != "visa")
                    return BadRequest(new { message = "Only Visa cards are accepted." });

                // 2. Take the money.
                var intent = await new PaymentIntentService().CreateAsync(new PaymentIntentCreateOptions
                {
                    Amount = ToStripeAmount(amount),
                    Currency = Currency,
                    PaymentMethod = paymentMethodId,
                    AllowedPaymentMethodTypes = new List<string> { "card" },   // cards only
                    Confirm = true,
                    Description = description,
                    // Saved with the payment so /confirm knows what it was for and who paid.
                    Metadata = new Dictionary<string, string>
                    {
                        ["kind"] = kind,
                        ["id"] = id.ToString(),
                        ["userId"] = CurrentUserId.ToString()
                    }
                });

                // 3a. The bank wants a 3D Secure check → the browser shows it, then calls /confirm.
                if (intent.Status == "requires_action")
                    return Ok(new { status = "requires_action", clientSecret = intent.ClientSecret, paymentIntentId = intent.Id });

                // 3b. Done.
                if (intent.Status == "succeeded")
                {
                    MarkAsPaid(kind, id, intent.Id, card.Card.Last4);
                    return Ok(new { status = "succeeded" });
                }

                return BadRequest(new { message = "The payment was not completed. Please try another Visa card." });
            }
            catch (StripeException ex)
            {
                // e.g. "Your card was declined." / "Your card has insufficient funds."
                return BadRequest(new { message = ex.StripeError?.Message ?? "The payment failed. Please try again." });
            }
            catch (HttpRequestException)
            {
                // No internet / Stripe is down: nothing was charged.
                return StatusCode(503, new { message = "Could not reach the payment service. Please try again." });
            }
        }

        // Saves the payment in our database.
        private void MarkAsPaid(string kind, int id, string paymentIntentId, string? last4)
        {
            if (kind == "hall-booking")
            {
                var booking = db.HallBookings.Find(id);
                if (booking != null && booking.Status == HallBookingStatus.Approved)
                {
                    booking.Status = HallBookingStatus.Paid;
                    booking.PaidAt = DateTime.UtcNow;
                    booking.PaymentMethod = "Visa •••• " + last4;
                    booking.PaymentIntentId = paymentIntentId;
                }
            }
            else if (kind == "ad")
            {
                var ad = db.Advertisements.Find(id);
                if (ad != null && ad.AwaitingPayment)
                {
                    ad.AwaitingPayment = false;   // the ad is now visible
                    ad.PaymentIntentId = paymentIntentId;
                }
            }
            db.SaveChanges();
        }

        // Stripe wants the amount in the smallest unit of the currency.
        // JOD has 3 decimals (1 JOD = 1000 fils) and Stripe needs it rounded to 10 fils;
        // USD / EUR have 2 decimals (1 USD = 100 cents).
        private long ToStripeAmount(decimal amount)
        {
            string[] threeDecimals = { "jod", "kwd", "bhd", "omr", "tnd" };
            if (threeDecimals.Contains(Currency))
                return (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero) * 10;
            return (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);
        }
    }
}
