/* =====================================================================
   BloodLink — navbar for each role
   Load it right after the page header:  <script src="/js/nav.js"></script>

   The signed-in user (saved by login.js / api.js in localStorage) decides
   which links appear. To add or change a link, edit the NAV object below.
   ===================================================================== */
(function () {
  // ---------- 1. The links of every role ----------
  // main     = links in the top bar
  // dropdown = links in the profile menu (Sign out is added automatically)
  const NAV = {
    Guest: {
      label: "Guest",
      main: [
        { text: "Home", href: "/" },
        { text: "Find Donors", href: "/Donor/Search" },
        { text: "Halls", href: "/Hall" },
        { text: "Health events", href: "/Home/Ads" }
      ],
      dropdown: [
        { icon: "→", text: "Log in", small: "Sign in to your account", href: "/Account/Login" },
        { icon: "+", text: "Create account", small: "Join BloodLink", href: "/Account/Register" }
      ]
    },
    Donor: {
      label: "Blood donor",
      home: "/Donor/Requests",
      main: [
        { text: "Home", href: "/" },
        { text: "Requests for me", href: "/Donor/Requests" },
        { text: "Book Donation", href: "/Donor/Appointments" },
        { text: "Donate", href: "/Donor/Donate" }
      ],
      dropdown: [
        { icon: "♙", text: "My profile", small: "Donations and certificate", href: "/Account/Profile" },
        { icon: "!", text: "Report an issue", small: "Tell us about a problem", href: "/Home/Report" }
      ]
    },
    Recipient: {
      label: "Needs blood",
      home: "/Donor/Search",
      main: [
        { text: "Home", href: "/" },
        { text: "Find Donors", href: "/Donor/Search" },
        { text: "My Requests", href: "/Recipient/Requests" },
        { text: "Health events", href: "/Home/Ads" }
      ],
      dropdown: [
        { icon: "♙", text: "My profile", small: "View your personal details", href: "/Account/Profile" },
        { icon: "!", text: "Report an issue", small: "Tell us about a problem", href: "/Home/Report" }
      ]
    },
    Hospital: {
      label: "Hospital staff",
      home: "/Hospital",
      main: [
        { text: "Home", href: "/" },
        { text: "Dashboard", href: "/Hospital" },
        { text: "Halls", href: "/Hall" },
        { text: "Health events", href: "/Home/Ads" }
      ],
      dropdown: [
        { icon: "▦", text: "Hospital dashboard", small: "Stock, appointments, ads", href: "/Hospital" },
        { icon: "▤", text: "My hall requests", small: "Track booking status", href: "/Hall/Requests" },
        { icon: "♙", text: "My profile", small: "View your personal details", href: "/Account/Profile" }
      ]
    },
    Admin: {
      label: "Administrator",
      home: "/Admin",
      main: [
        { text: "Home", href: "/" },
        { text: "Admin console", href: "/Admin" },
        { text: "Halls", href: "/Hall" },
        { text: "Health events", href: "/Home/Ads" }
      ],
      dropdown: [
        { icon: "▦", text: "Admin console", small: "Accounts, bookings, reports", href: "/Admin" },
        { icon: "♙", text: "My profile", small: "View your personal details", href: "/Account/Profile" }
      ]
    }
  };

  // ---------- 2. Who is signed in? ----------
  let user = null;
  try { user = JSON.parse(localStorage.getItem("bloodlinkUser") || "null"); } catch { user = null; }
  if (!localStorage.getItem("bloodlinkToken")) user = null;
  const role = user && NAV[user.role] ? user.role : "Guest";
  const nav = NAV[role];

  const esc = (v) => String(v ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
  const initials = (name) => name.split(/\s+/).filter(Boolean).slice(0, 2).map((w) => w[0]).join("").toUpperCase() || "?";
  const isCurrent = (href) => href === "/" ? location.pathname === "/" : location.pathname.toLowerCase().startsWith(href.toLowerCase());

  // ---------- 3. Top bar links (pages with <nav class="main-nav">) ----------
  document.querySelectorAll(".main-nav").forEach((el) => {
    el.innerHTML = nav.main.map((link) => {
      const active = isCurrent(link.href) && link.href !== "/";
      return `<a href="${link.href}" class="main-nav-link${active ? " active" : ""}"${active ? ' aria-current="page"' : ""}>${esc(link.text)}</a>`;
    }).join("");
  });

  // ---------- 4. Profile menu (pages with #profileMenu) ----------
  const menu = document.getElementById("profileMenu");
  if (menu) {
    const name = user ? user.name : "Guest";
    const sub = user ? nav.label : "Welcome to BloodLink";

    const chip = menu.querySelector(".profile-chip");
    if (chip) {
      chip.querySelector(".profile-avatar").textContent = initials(name);
      chip.querySelector(".profile-label").innerHTML = `<strong>${esc(name)}</strong><small>${esc(sub)}</small>`;
    }

    const dropdown = menu.querySelector(".profile-dropdown");
    if (dropdown) {
      const item = (l) => `<a href="${l.href}" class="dropdown-item"><span class="dropdown-icon">${l.icon}</span><span><strong>${esc(l.text)}</strong><small>${esc(l.small)}</small></span><span class="dropdown-arrow">›</span></a>`;
      const signOut = user ? `<a href="/" class="dropdown-item" data-logout><span class="dropdown-icon">⎋</span><span><strong>Sign out</strong><small>${esc(user.email)}</small></span><span class="dropdown-arrow">›</span></a>` : "";
      dropdown.innerHTML = `
        <div class="dropdown-user">
          <span class="profile-avatar">${esc(initials(name))}</span>
          <span><strong>${esc(user ? name : "Guest account")}</strong><small>${esc(user ? user.email : "Sign in to manage your profile")}</small></span>
        </div>
        <div class="dropdown-divider"></div>
        ${nav.dropdown.map(item).join("")}${signOut}`;
    }
  }

  // ---------- 5. Dashboards (hospital / admin): show the real name ----------
  const userInfo = document.querySelector(".topbar .user-info b");
  if (userInfo && user) {
    userInfo.textContent = user.name;
    const avatar = document.querySelector(".topbar .user-avatar");
    if (avatar) avatar.textContent = initials(user.name);
  }

  // ---------- 6. Home page buttons ----------
  if (user) {
    const dashboard = `<a href="${nav.home}" class="nav-cta" data-magnetic>My dashboard <span>→</span></a>`;
    const actions = document.querySelector(".navbar .nav-actions");
    if (actions) actions.innerHTML = dashboard;
    const mobile = document.querySelector(".navbar .nav-mobile-actions");
    if (mobile) mobile.innerHTML = `<a href="/" class="login-link" data-logout>Sign out</a>` + dashboard;
  }

  // ---------- 7. Small public headers (Ads, Hall): add Home + the user's dashboard ----------
  document.querySelectorAll("[data-extra-nav]").forEach((el) => {
    const cls = el.dataset.extraNav;   // the CSS class the links of that page use
    let html = `<a class="${cls}" href="/">Home</a>`;
    html += user ? `<a class="${cls}" href="${nav.home}">My dashboard</a>` : `<a class="${cls}" href="/Account/Login">Log in</a>`;
    el.insertAdjacentHTML("afterbegin", html);
  });
})();
