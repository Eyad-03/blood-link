namespace BloodLink.Models.Dtos
{
    // The card the user typed in Stripe's card field (stripe-pay.js).
    // We only get an id like "pm_..." — the card number never reaches our server.
    public class PayDto
    {
        public string PaymentMethodId { get; set; } = "";
    }

    // After the bank's 3D Secure check, the browser asks us to finish the payment.
    public class ConfirmPaymentDto
    {
        public string PaymentIntentId { get; set; } = "";
    }
}
