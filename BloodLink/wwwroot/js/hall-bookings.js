/* =====================================================================
   Hall bookings — hospital dashboard section
   Shows the hall bookings this hospital account made and lets it pay:
     GET  /api/halls/bookings/mine, POST /api/payments/hall-booking/{id} (Visa via Stripe)
   Status values stay compatible: "Pending approval" | "Approved" | "Paid" | "Rejected"
   ===================================================================== */
(function () {
  let myRequests = [];
  const byId = id => document.getElementById(id);
  if (!byId("hall-bookings")) return;

  let activeFilter = "All";
  let payingId = null;
  let detailsId = null;

  const esc = v => String(v ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
  const money = v => `${Number(v || 0).toLocaleString("en-US", { maximumFractionDigits: 2 })} JOD`;
  const dateLabel = v => {
    if (!v) return "Not provided";
    const d = new Date(`${v}T12:00:00`);
    return Number.isNaN(d.getTime()) ? String(v) : d.toLocaleDateString("en-GB", { day: "2-digit", month: "short", year: "numeric" });
  };
  const notify = msg => (typeof toast === "function" ? toast(msg) : alert(msg));
  const celebrate = () => { if (typeof bloodBurst === "function") bloodBurst("#13895f"); };

  /* ---------- Status vocabulary ---------- */
  const STATUS = {
    "Pending approval": { cls: "pending", label: "Pending review", next: "Waiting for the venue provider to review the request." },
    "Approved": { cls: "payable", label: "Payment available", next: "Approved — pay now to confirm the booking." },
    "Paid": { cls: "verified", label: "Booking confirmed", next: "Paid and confirmed. Keep the receipt for reference." },
    "Rejected": { cls: "rejected", label: "Request rejected", next: "Contact the venue provider for clarification." }
  };
  const st = s => STATUS[s] || STATUS["Pending approval"];

  /* ---------- Data from the server ---------- */
  function readRequests() { return myRequests; }
  async function loadRequests() {
    try { myRequests = await Api.get("/api/halls/bookings/mine"); } catch { myRequests = []; }
    render();
  }

  /* ---------- Rendering ---------- */
  function render() {
    const all = readRequests();
    const count = s => all.filter(r => r.status === s).length;
    const approved = all.filter(r => r.status === "Approved");
    const due = approved.reduce((s, r) => s + (Number(r.price) || 0), 0);

    byId("hbTotal").textContent = all.length;
    byId("hbPending").textContent = count("Pending approval");
    byId("hbApproved").textContent = approved.length;
    byId("hbPaid").textContent = count("Paid");
    byId("hbDueTotal").textContent = `${money(due)} due`;
    const nav = byId("navHallBookings");
    if (nav) { nav.textContent = approved.length; nav.classList.toggle("hb-nav-due", approved.length > 0); }
    document.querySelectorAll("[data-hb-count]").forEach(el => {
      const f = el.dataset.hbCount; el.textContent = f === "All" ? all.length : count(f);
    });
    document.querySelectorAll("[data-hb-filter]").forEach(t => t.classList.toggle("active", t.dataset.hbFilter === activeFilter));

    // Banner: payment is now possible
    const banner = byId("hbPayBanner");
    banner.hidden = approved.length === 0;
    if (approved.length) {
      byId("hbPayBannerTitle").textContent = approved.length === 1 ? "Payment is now available" : `${approved.length} bookings are ready for payment`;
      byId("hbPayBannerText").textContent = approved.length === 1
        ? `${approved[0].hallName} approved your request for ${dateLabel(approved[0].eventDate)}. Pay ${money(approved[0].price)} to confirm it.`
        : `The venues approved your requests. ${money(due)} in total is due to confirm them.`;
      byId("hbPayBannerButton").dataset.hbPay = approved[0].id;
      byId("hbPayBannerButton").firstChild.textContent = approved.length === 1 ? "Pay now " : "Pay the first one ";
    }

    // Table
    const q = byId("hbSearch").value.trim().toLowerCase();
    const rows = all.filter(r =>
      (activeFilter === "All" || r.status === activeFilter) &&
      (!q || [r.hallName, r.id, r.organization, r.eventType, r.governorate].some(v => String(v || "").toLowerCase().includes(q))));
    const sort = byId("hbSort").value;
    rows.sort((a, b) => {
      if (sort === "oldest") return new Date(a.createdAt || 0) - new Date(b.createdAt || 0);
      if (sort === "eventDate") return String(a.eventDate || "").localeCompare(String(b.eventDate || ""));
      // Newest first, but keep payable requests on top so they're not missed
      if ((a.status === "Approved") !== (b.status === "Approved")) return a.status === "Approved" ? -1 : 1;
      return new Date(b.createdAt || 0) - new Date(a.createdAt || 0);
    });
    byId("hbVisibleCount").textContent = rows.length;

    byId("hbTableBody").innerHTML = rows.length ? rows.map(r => {
      const s = st(r.status);
      const payable = r.status === "Approved";
      return `<tr class="${payable ? "hb-row-payable" : ""}">
        <td><span class="request-id">${esc(r.id || "—")}</span><small class="cell-subtext">Sent ${esc(r.createdAt ? new Date(r.createdAt).toLocaleDateString("en-GB", { day: "2-digit", month: "short" }) : "—")}</small></td>
        <td><b>${esc(r.hallName || "Hall booking")}</b><small class="cell-subtext">${esc(r.eventType || "Event")} · ${esc(r.governorate || "—")} · ${esc(r.attendees ?? "—")} guests</small></td>
        <td class="hb-nowrap">${esc(dateLabel(r.eventDate))}</td>
        <td><b>${esc(money(r.price))}</b></td>
        <td><span class="status-pill ${s.cls}">${esc(s.label)}</span><small class="cell-subtext hb-next">${esc(s.next)}</small></td>
        <td><div class="record-actions">
          ${payable ? `<button class="record-action hb-pay-action" type="button" data-hb-pay="${esc(r.id)}">Pay now</button>` : ""}
          <button class="record-action" type="button" data-hb-details="${esc(r.id)}">Details</button>
        </div></td>
      </tr>`;
    }).join("") : `<tr><td colspan="6" class="empty-records">${all.length ? "No requests match this filter or search." : "No hall booking requests yet. Use “Book a hall” to send your first one."}</td></tr>`;
  }

  /* ---------- Details pop-up ---------- */
  const detailsDlg = byId("hbDetailsDialog");
  const payDlg = byId("hbPayDialog");
  const open = d => (typeof d.showModal === "function" ? d.showModal() : d.setAttribute("open", ""));
  const close = d => (d.open && typeof d.close === "function" ? d.close() : d.removeAttribute("open"));

  function openDetails(id) {
    const r = readRequests().find(x => String(x.id) === String(id)); if (!r) return;
    detailsId = r.id;
    const s = st(r.status);
    byId("hbDetailsTitle").textContent = r.hallName || "Hall booking request";
    const fields = [
      ["Request ID", r.id], ["Event date", dateLabel(r.eventDate)], ["Event time", r.eventTime],
      ["Event type", r.eventType], ["Organization", r.organization], ["Contact person", r.contactName],
      ["Email", r.email], ["Phone", r.phone], ["Expected attendees", r.attendees],
      ["Governorate", r.governorate], ["Booking fee", money(r.price)]
    ];
    if (r.status === "Paid") fields.push(["Receipt", r.receipt || "—"], ["Paid on", r.paidAt ? new Date(r.paidAt).toLocaleDateString("en-GB", { day: "2-digit", month: "short", year: "numeric" }) : "—"]);
    const docs = Array.isArray(r.documents) && r.documents.length
      ? r.documents.map(d => `${esc(d.label)} — ${esc(d.fileName)}`).join("<br>") : "No document details saved.";
    byId("hbDetailsBody").innerHTML = `
      <div class="hb-status-strip ${s.cls}"><span class="status-pill ${s.cls}">${esc(s.label)}</span><p>${esc(s.next)}</p></div>
      <div class="hb-detail-grid">${fields.map(([l, v]) => `<div><small>${esc(l)}</small><b>${esc(v ?? "—")}</b></div>`).join("")}</div>
      <div class="hb-detail-note"><b>Event description</b><p>${esc(r.description || "No description provided.")}</p><b>Documents submitted</b><p>${docs}</p></div>`;
    byId("hbDetailsPay").hidden = r.status !== "Approved";
    open(detailsDlg);
  }

  /* ---------- Payment pop-up ---------- */
  const showStep = step => payDlg.querySelectorAll("[data-hb-step]").forEach(s => (s.hidden = s.dataset.hbStep !== step));
  let hbCard = null;   // the Stripe card field (stripe-pay.js), created the first time the dialog opens
  const isProcessing = () => !payDlg.querySelector('[data-hb-step="processing"]').hidden;

  function openPayment(id) {
    const r = readRequests().find(x => String(x.id) === String(id));
    if (!r) return;
    if (r.status !== "Approved") { notify(r.status === "Paid" ? "This booking is already paid." : "Payment opens once the venue approves this request."); render(); return; }
    payingId = r.id;
    if (detailsDlg.open) close(detailsDlg);
    byId("hbCoTitle").textContent = r.hallName || "Hall booking";
    byId("hbCoMeta").textContent = `${r.id} · ${dateLabel(r.eventDate)}${r.eventTime ? " · " + r.eventTime : ""}`;
    byId("hbCoTotal").textContent = money(r.price);
    byId("hbPayAmount").textContent = money(r.price);
    byId("hbPayError").hidden = true;
    byId("hbTerms").checked = false;
    showStep("pay");
    open(payDlg);
    if (hbCard) hbCard.clear();
    else StripePay.mount(byId("hbCard"), byId("hbPayError"))
      .then(c => { hbCard = c; })
      .catch(err => { byId("hbPayError").hidden = false; byId("hbPayError").textContent = err.message; byId("hbPayButton").disabled = true; });
  }

  // Visa payment with Stripe — POST /api/payments/hall-booking/{id} (the server takes the price from the database).
  async function pay() {
    const box = byId("hbPayError");
    const fail = msg => { showStep("pay"); box.hidden = false; box.textContent = msg; };
    if (!hbCard) return fail("The payment form is not ready yet.");
    if (!byId("hbTerms").checked) return fail("Please confirm the booking details.");
    box.hidden = true;
    showStep("processing");
    let result;
    try { result = await hbCard.pay(`/api/payments/hall-booking/${encodeURIComponent(payingId)}`); }
    catch (err) { fail(err.message); loadRequests(); return; }
    await loadRequests();
    hbCard.clear();
    const r = readRequests().find(x => String(x.id) === String(payingId));
    byId("hbSuccessText").textContent = `${r.hallName} is booked for ${dateLabel(r.eventDate)}. The status is now “Booking confirmed”.`;
    byId("hbRcptId").textContent = r.receipt;
    byId("hbRcptAmount").textContent = money(r.amountPaid);
    byId("hbRcptMethod").textContent = `Visa •••• ${result.last4}`;
    showStep("success");
    render();
    celebrate();
    notify(`Payment received — ${r.id} is confirmed.`);
  }

  /* ---------- Events ---------- */
  document.querySelectorAll("[data-hb-filter]").forEach(t => t.addEventListener("click", () => { activeFilter = t.dataset.hbFilter; render(); }));
  byId("hbSearch").addEventListener("input", render);
  byId("hbSort").addEventListener("change", render);
  byId("hbTableBody").addEventListener("click", e => {
    const p = e.target.closest("[data-hb-pay]"); if (p) return openPayment(p.dataset.hbPay);
    const d = e.target.closest("[data-hb-details]"); if (d) openDetails(d.dataset.hbDetails);
  });
  byId("hbPayBannerButton").addEventListener("click", e => openPayment(e.currentTarget.dataset.hbPay));
  byId("hbDetailsPay").addEventListener("click", () => openPayment(detailsId));

  detailsDlg.addEventListener("click", e => { if (e.target.closest("[data-hb-close]") || e.target === detailsDlg) close(detailsDlg); });
  payDlg.addEventListener("click", e => { if (isProcessing()) return; if (e.target.closest("[data-hb-close]") || e.target === payDlg) close(payDlg); });
  payDlg.addEventListener("cancel", e => { if (isProcessing()) e.preventDefault(); });
  byId("hbPayButton").addEventListener("click", pay);

  // Refresh when the user comes back to the tab (the admin may have approved a booking).
  window.addEventListener("focus", loadRequests);

  window.renderHallBookings = render;
  window.hallPaymentsDue = () => readRequests().filter(r => r.status === "Approved").length;
  render();
  loadRequests();
})();