/* =====================================================================
   BloodLink — "Continue with Google" (Login and Register pages)

   1. Ask our server for the Google Client ID (GET /api/auth/google-config).
      Empty → the Google button stays hidden.
   2. Load Google's script and draw the official Google button.
   3. When the user picks a Google account, Google gives us a "credential".
   4. Send it to POST /api/auth/google. The server checks it with Google and
      answers like a normal login (our JWT token + the page to open).

   The page decides the mode with <div id="googleArea" data-mode="login|register">.
   ===================================================================== */
(function () {
  const area = document.getElementById("googleArea");
  if (!area) return;
  const mode = area.dataset.mode;   // "login" or "register"
  const message = document.getElementById("formMessage");

  function showError(text) {
    if (!message) { alert(text); return; }
    message.className = "form-message error";
    message.textContent = text;
  }
  function showInfo(text) {
    if (!message) return;
    message.className = "form-message";
    message.textContent = text;
  }

  // Register page: Google gives us the name and email, but we still need the role
  // (and the hospital / city details) from the form.
  function registerDetails() {
    const value = (id) => (document.getElementById(id)?.value || "").trim();
    const role = document.querySelector('input[name="role"]:checked')?.value || "donor";
    const details = { role };

    if (role === "requester") {
      details.requiredBloodType = document.querySelector('input[name="requiredBloodType"]:checked')?.value || "";
      details.requestCity = value("requestCity");
      if (!details.requestCity) return { error: "Please choose your city first, then continue with Google." };
    }
    if (role === "hospital") {
      details.institutionName = value("institutionName");
      details.institutionCity = value("institutionCity");
      if (details.institutionName.length < 3 || !details.institutionCity)
        return { error: "Please fill in the institution name and city first, then continue with Google." };
    }
    if (!document.getElementById("consent")?.checked)
      return { error: "Please agree to the Terms of Service and Privacy Policy first." };
    return details;
  }

  // Google calls this after the user chooses an account.
  async function onGoogleCredential(response) {
    let body = { credential: response.credential };
    if (mode === "register") {
      const details = registerDetails();
      if (details.error) { showError(details.error); return; }
      body = { ...body, ...details };
    }

    showInfo("Signing you in with Google…");
    try {
      const result = await Api.post("/api/auth/google", body);
      Api.saveLogin(result.token, result.user);
      showInfo("Welcome! Taking you in…");

      // Login page: go back to the page that asked for a login (?returnUrl=...) if any.
      const returnUrl = new URLSearchParams(location.search).get("returnUrl");
      const target = mode === "login" && returnUrl && returnUrl.startsWith("/") && !returnUrl.startsWith("//") ? returnUrl : result.redirect;
      setTimeout(() => { window.location.href = target || "/"; }, 600);
    } catch (err) {
      showError(err.message);
    }
  }

  // Load Google's script (only when Google sign-in is set up).
  function loadGoogleScript() {
    return new Promise((resolve, reject) => {
      const script = document.createElement("script");
      script.src = "https://accounts.google.com/gsi/client";
      script.async = true;
      script.onload = resolve;
      script.onerror = reject;
      document.head.appendChild(script);
    });
  }

  async function start() {
    let clientId = "";
    try { clientId = (await Api.get("/api/auth/google-config")).clientId; } catch { return; }
    if (!clientId) return;   // not set up → keep the button hidden

    try { await loadGoogleScript(); } catch { return; }   // no internet → no Google button

    google.accounts.id.initialize({ client_id: clientId, callback: onGoogleCredential });
    area.hidden = false;
    const holder = document.getElementById("googleButton");
    google.accounts.id.renderButton(holder, {
      theme: "outline",
      size: "large",
      shape: "pill",
      text: mode === "register" ? "signup_with" : "signin_with",
      width: Math.min(400, Math.max(240, holder.offsetWidth || 360))
    });
  }

  start();
})();
