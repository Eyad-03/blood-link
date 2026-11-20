/* =====================================================================
   BloodLink — Visa payments with Stripe (shared by every payment form)

     const card = await StripePay.mount(document.getElementById("adCard"), errorElement);
     const result = await card.pay("/api/payments/ad/12");   // throws an Error with a message if it fails
     card.clear();

   - The card field is Stripe's own (an iframe): the card number never touches our server.
   - Only Visa is accepted: checked here while typing AND on the server before charging.
   - If the bank asks for 3D Secure, Stripe shows it, then we call /api/payments/confirm.
   ===================================================================== */
(function () {
  let stripe = null;
  let config = null;  // { publishableKey, currency, exchangeRate } from /api/payments/config
  let ready = null;   // the setup runs only once

  function loadScript(src) {
    return new Promise((resolve, reject) => {
      const s = document.createElement("script");
      s.src = src; s.onload = resolve; s.onerror = reject;
      document.head.appendChild(s);
    });
  }

  // Gets the publishable key from our server and loads Stripe.js.
  function setup() {
    if (!ready) {
      ready = (async () => {
        config = await Api.get("/api/payments/config");
        if (!config.publishableKey) throw new Error("Online payments are not set up yet.");
        try { await loadScript("https://js.stripe.com/v3/"); }
        catch { throw new Error("Could not load the payment form. Check your internet connection."); }
        stripe = Stripe(config.publishableKey);
      })();
      ready.catch(() => { ready = null; });   // allow a retry later
    }
    return ready;
  }

  // Puts a Stripe card field inside "container". Messages are shown in "errorBox".
  async function mount(container, errorBox) {
    await setup();

    const card = stripe.elements().create("card", {
      hidePostalCode: true,
      style: {
        base: { fontSize: "15px", color: "#1b1c28", fontFamily: '"DM Sans", system-ui, sans-serif', "::placeholder": { color: "#a3a4b4" } },
        invalid: { color: "#c91835" }
      }
    });
    card.mount(container);

    // Prices are in JOD; if Stripe charges in another currency, say so under the card field.
    const note = container.parentElement.querySelector(".visa-note");
    if (note && config.currency !== "jod" && !note.dataset.rate) {
      note.dataset.rate = "1";
      note.textContent += ` You will be charged in ${config.currency.toUpperCase()} (1 JOD = ${config.exchangeRate} ${config.currency.toUpperCase()}).`;
    }

    let brand = "unknown";
    const show = (text) => { if (errorBox) { errorBox.textContent = text; errorBox.hidden = !text; } };

    // While typing: Stripe tells us the card brand → refuse anything that is not Visa.
    card.on("change", (e) => {
      brand = e.brand;
      if (e.error) show(e.error.message);
      else if (brand && brand !== "unknown" && brand !== "visa") show("Only Visa cards are accepted.");
      else show("");
    });

    return {
      clear: () => { card.clear(); show(""); },

      // Pays with the typed card. "url" is our API for this payment, e.g. /api/payments/hall-booking/7
      async pay(url) {
        if (brand && brand !== "unknown" && brand !== "visa") throw new Error("Only Visa cards are accepted.");

        // 1. Stripe turns the card into a PaymentMethod id ("pm_...").
        const { paymentMethod, error } = await stripe.createPaymentMethod({ type: "card", card });
        if (error) throw new Error(error.message);
        if (paymentMethod.card.brand !== "visa") throw new Error("Only Visa cards are accepted.");

        // 2. Our server checks the card again and takes the money.
        let result = await Api.post(url, { paymentMethodId: paymentMethod.id });

        // 3. The bank wants a 3D Secure check → Stripe shows it, then the server finishes.
        if (result.status === "requires_action") {
          const { error: authError } = await stripe.handleNextAction({ clientSecret: result.clientSecret });
          if (authError) throw new Error(authError.message);
          result = await Api.post("/api/payments/confirm", { paymentIntentId: result.paymentIntentId });
        }
        return { ...result, last4: paymentMethod.card.last4 };
      }
    };
  }

  window.StripePay = { mount };
})();
