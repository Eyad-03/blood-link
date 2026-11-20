/* =====================================================================
   BloodLink — tiny helper for calling the backend API.
   Load it BEFORE the page script:  <script src="/js/api.js"></script>

     await Api.get("/api/requests/sent")
     await Api.post("/api/requests", { donorId: 5, priority: "Urgent" })
     await Api.put("/api/requests/5/status", { status: "Accepted" })
     Api.requireLogin(["Donor"])   // send the visitor to the login page if needed

   The JWT token from /api/auth/login is kept in localStorage and sent
   with every request in the "Authorization: Bearer <token>" header.
   ===================================================================== */
(function () {
  const TOKEN_KEY = "bloodlinkToken";
  const USER_KEY = "bloodlinkUser";

  function getToken() { try { return localStorage.getItem(TOKEN_KEY) || ""; } catch { return ""; } }
  function getUser() { try { return JSON.parse(localStorage.getItem(USER_KEY) || "null"); } catch { return null; } }

  function saveLogin(token, user) {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  }

  function logout() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  }

  function goToLogin() {
    window.location.href = "/Account/Login?returnUrl=" + encodeURIComponent(location.pathname);
  }

  // Sends a request and returns the JSON answer. Throws an Error with the server message if it fails.
  async function request(method, url, body) {
    const headers = { Accept: "application/json" };
    if (body !== undefined) headers["Content-Type"] = "application/json";
    const token = getToken();
    if (token) headers.Authorization = "Bearer " + token;

    const res = await fetch(url, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });

    // Token missing or expired → sign in again.
    if (res.status === 401 && !url.startsWith("/api/auth/")) { logout(); goToLogin(); throw new Error("Please sign in again."); }

    let data = null;
    const text = await res.text();
    if (text) { try { data = JSON.parse(text); } catch { data = null; } }

    if (!res.ok) {
      const error = new Error((data && data.message) || (res.status === 403 ? "You don't have access to this." : "Something went wrong. Please try again."));
      error.status = res.status;
      error.data = data;
      throw error;
    }
    return data;
  }

  // Use at the top of a page that needs a signed-in user.
  // roles = list of allowed roles, e.g. ["Donor"]. Returns false when the visitor is redirected.
  function requireLogin(roles) {
    const user = getUser();
    if (!getToken() || !user) { goToLogin(); return false; }
    if (roles && roles.length && !roles.includes(user.role)) {
      alert("This page is for " + roles.join(" / ") + " accounts. You are signed in as " + user.role + ".");
      window.location.href = "/";
      return false;
    }
    return true;
  }

  // Any link or button with data-logout signs the user out.
  // (true = "capture" phase, so it still works inside menus that stop click events)
  document.addEventListener("click", (e) => {
    const el = e.target.closest("[data-logout]");
    if (!el) return;
    e.preventDefault();
    logout();
    window.location.href = "/";
  }, true);

  window.Api = {
    get: (url) => request("GET", url),
    post: (url, body) => request("POST", url, body ?? {}),
    put: (url, body) => request("PUT", url, body ?? {}),
    del: (url) => request("DELETE", url),
    getToken, getUser, saveLogin, logout, requireLogin,
    isLoggedIn: () => !!getToken()
  };
})();
