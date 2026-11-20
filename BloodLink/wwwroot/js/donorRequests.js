const $ = id => document.getElementById(id);
Api.requireLogin(["Donor"]);

/* Requests come from the server: GET /api/requests/received (Controllers/Api/RequestsController.cs) */
let requests = [];
let activeFilter = "All";
let selectedRequestId = null;
BloodChat.init({ me: "donor" });

function esc(value) { return String(value ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c])); }
function readRequests() { return requests; }
async function loadRequests() {
    try { requests = await Api.get("/api/requests/received"); }
    catch (err) { toast(err.message); }
    renderRequests();
}
function statusClass(status) { return ({ Pending: "pending", Accepted: "accepted", Declined: "declined", Completed: "completed" })[status] || "pending"; }
function statusLabel(status) { return ({ Pending: "Awaiting your response", Accepted: "Request accepted", Declined: "Request declined", Completed: "Donation completed" })[status] || status; }
function renderSummary(rows) {
    $("totalCount").textContent = rows.length;
    $("pendingCount").textContent = rows.filter(r => r.status === "Pending").length;
    $("acceptedCount").textContent = rows.filter(r => r.status === "Accepted").length;
    $("completedCount").textContent = rows.filter(r => r.status === "Completed").length;
    $("allTabCount").textContent = rows.length;
    document.querySelectorAll(".tab[data-filter]").forEach(tab => {
        const filter = tab.dataset.filter;
        const count = filter === "All" ? rows.length : rows.filter(r => r.status === filter).length;
        let badge = tab.querySelector(".tab-count");
        if (filter !== "All" && !badge) { badge = document.createElement("span"); badge.className = "tab-count"; tab.appendChild(badge); }
        if (filter !== "All" && badge) badge.textContent = count;
    });
}
function renderRequests() {
    const all = readRequests(); renderSummary(all);
    const q = $("searchInput").value.trim().toLowerCase();
    let rows = all.filter(r => (activeFilter === "All" || r.status === activeFilter) && (!q || [r.recipientName, r.id, r.location, r.bloodType, r.hospital].some(v => String(v || "").toLowerCase().includes(q))));
    const sort = $("sortSelect").value;
    rows.sort((a, b) => sort === "urgent" ? ({ Urgent: 0, High: 1, Normal: 2 }[a.priority] ?? 3) - ({ Urgent: 0, High: 1, Normal: 2 }[b.priority] ?? 3) : sort === "newest" ? new Date(b.dateValue) - new Date(a.dateValue) : new Date(a.dateValue) - new Date(b.dateValue));
    $("visibleCount").textContent = rows.length;
    $("requestList").innerHTML = rows.map(r =>`
    <article class="request-card" style="--item-index:${rows.indexOf(r)}">
      <div class="request-main">
        <div class="request-avatar">${esc(r.initials || r.recipientName.split(" ").map(n => n[0]).join("").slice(0, 2))}</div>
        <div class="request-info">
          <div class="request-title-row"><h3>${esc(r.recipientName)}</h3><span class="blood-tag">${esc(r.bloodType)}</span><span class="priority ${String(r.priority || "Normal").toLowerCase()}">${esc(r.priority || "Normal")} priority</span></div>
          <p>Request ID: ${esc(r.id)} · Received ${esc(r.sentAt)}</p>
          <div class="request-meta"><span>⌖ ${esc(r.location)}</span><span>↗ <b>${esc(r.distance || "Distance unavailable")}</b></span><span>▣ ${esc(r.units)} unit${Number(r.units) === 1 ? "" : "s"}</span></div>
        </div>
      </div>
      <div class="request-side">
        <span class="status ${statusClass(r.status)}">${statusLabel(r.status)}</span>
        <div class="request-actions"><button class="small-btn" data-details="${esc(r.id)}">View details</button>${r.status === "Pending" ? `<button class="small-btn decline" data-decline="${esc(r.id)}">Decline</button><button class="small-btn accept" data-accept="${esc(r.id)}">Accept</button>` : r.status === "Accepted" ? `${BloodChat.buttonHTML(r.id)}<button class="small-btn" data-complete="${esc(r.id)}">Mark completed</button>` : r.status === "Completed" ? BloodChat.buttonHTML(r.id) : ""}</div>
      </div>
    </article>`).join("");
    $("emptyState").classList.toggle("hidden", rows.length > 0);
    $("requestList").classList.toggle("hidden", rows.length === 0);
    document.querySelectorAll(".tab").forEach(tab => tab.classList.toggle("active", tab.dataset.filter === activeFilter));
    BloodChat.refreshBadges();
}
function toast(message) { $("toast").textContent = message; $("toast").classList.add("show"); clearTimeout(toast.timer); toast.timer = setTimeout(() => $("toast").classList.remove("show"), 2800); }
function openDetails(id) {
    const r = readRequests().find(x => x.id === id); if (!r) return; selectedRequestId = id;
    $("detailsTitle").textContent = `Request ${r.id}`;
    const fields = [["Recipient", r.recipientName], ["Blood type requested", r.bloodType], ["Priority", r.priority || "Normal"], ["Units requested", r.units], ["Location", r.location], ["Distance", r.distance || "Not available"], ["Hospital", r.hospital || "Not provided"], ["Date received", r.sentAt]];
    $("detailsContent").innerHTML = `<div class="detail-grid">${fields.map(([label, value]) => `<div class="detail-item"><small>${esc(label)}</small><b>${esc(value)}</b></div>`).join("")}</div><div class="detail-note">${r.status === "Pending" ? "Please review the details and accept only if you are comfortable and eligible to donate." : r.status === "Accepted" ? "You accepted this request. Coordinate with the recipient and hospital before arranging any donation." : r.status === "Declined" ? "You declined this request. No further action is needed." : "This request is marked as completed."}</div>`;
    $("acceptDetailBtn").classList.toggle("hidden", r.status !== "Pending");
    $("declineDetailBtn").classList.toggle("hidden", r.status !== "Pending");
    $("completeBtn").classList.toggle("hidden", r.status !== "Accepted");
    $("chatDetailBtn").classList.toggle("hidden", !["Accepted", "Completed"].includes(r.status));
    $("detailsModal").classList.remove("hidden"); document.body.style.overflow = "hidden";
}
function openChat(id) {
    const r = readRequests().find(x => x.id === id); if (!r || !["Accepted", "Completed"].includes(r.status)) return;
    closeModal("detailsModal");
    BloodChat.open(r.id, { name: r.recipientName, initials: (r.initials || r.recipientName.split(" ").map(n => n[0]).join("").slice(0, 2)), sub: `Blood recipient · ${r.location}`, meta: [["Request", r.id], ["Blood", r.bloodType], ["Units", r.units], ["Priority", r.priority || "Normal"], ["Hospital", r.hospital]], readOnly: r.status === "Completed" });
}
function closeModal(id) { $(id).classList.add("hidden"); document.body.style.overflow = ""; }
// PUT /api/requests/{id}/status — the server also adds the "accepted" / "completed" note in the chat.
async function updateStatus(id, status) {
    const r = readRequests().find(x => x.id === id); if (!r) return;
    if (status === "Accepted" && availableAgainOn) { toast(`You donated recently. You can accept requests again from ${niceDate(availableAgainOn)}.`); return; }
    if (status === "Accepted" && !$("availabilityToggle").checked) { toast("Turn on your availability before accepting a request."); return; }
    try { await Api.put(`/api/requests/${encodeURIComponent(id)}/status`, { status }); }
    catch (err) { toast(err.message); return; }
    closeModal("detailsModal"); await loadRequests();
    // Completing a donation starts the 3-month rest → read my availability again.
    if (status === "Completed") Api.get("/api/profile").then(p => { availableAgainOn = p.availableAgainOn || ""; $("availabilityToggle").checked = !!p.available; renderAvailability(); }).catch(() => { });
    toast(status === "Accepted" ? "Request accepted. Coordinate through the hospital." : status === "Declined" ? "Request declined." : "Donation marked as completed.");
}
document.querySelectorAll(".tab").forEach(tab => tab.addEventListener("click", () => { activeFilter = tab.dataset.filter; renderRequests(); }));
$("searchInput").addEventListener("input", renderRequests);
$("sortSelect").addEventListener("change", renderRequests);
$("clearFilters").addEventListener("click", () => { $("searchInput").value = ""; activeFilter = "All"; renderRequests(); });
$("requestList").addEventListener("click", e => {
    const details = e.target.closest("[data-details]"), accept = e.target.closest("[data-accept]"), decline = e.target.closest("[data-decline]"), complete = e.target.closest("[data-complete]"), chat = e.target.closest("[data-chat]");
    if (details) openDetails(details.dataset.details);
    if (chat) openChat(chat.dataset.chat);
    if (accept) updateStatus(accept.dataset.accept, "Accepted");
    if (decline) updateStatus(decline.dataset.decline, "Declined");
    if (complete) updateStatus(complete.dataset.complete, "Completed");
});
$("acceptDetailBtn").addEventListener("click", () => selectedRequestId && updateStatus(selectedRequestId, "Accepted"));
$("declineDetailBtn").addEventListener("click", () => selectedRequestId && updateStatus(selectedRequestId, "Declined"));
$("completeBtn").addEventListener("click", () => selectedRequestId && updateStatus(selectedRequestId, "Completed"));
$("chatDetailBtn").addEventListener("click", () => selectedRequestId && openChat(selectedRequestId));
document.querySelectorAll("[data-close]").forEach(btn => btn.addEventListener("click", () => closeModal(btn.dataset.close)));
$("detailsModal").addEventListener("click", e => { if (e.target === $("detailsModal")) closeModal("detailsModal"); });
document.addEventListener("keydown", e => { if (e.key === "Escape") document.querySelectorAll(".modal-backdrop:not(.hidden)").forEach(m => closeModal(m.id)); });
/* Availability is saved on the server (PUT /api/profile/availability).
   After a donation the donor rests for 3 months: the switch is locked until "availableAgainOn". */
let availableAgainOn = "";
function niceDate(ymd) { return new Date(ymd + "T12:00:00").toLocaleDateString("en-GB", { day: "numeric", month: "short", year: "numeric" }); }
function renderAvailability() {
    const available = $("availabilityToggle").checked;
    $("availabilityToggle").disabled = !!availableAgainOn;
    $("availabilityText").textContent = availableAgainOn ? `Resting after your donation · available again on ${niceDate(availableAgainOn)}`
        : available ? "Available to donate" : "Not available right now";
    $("availabilityToggle").closest(".availability").classList.toggle("unavailable", !available);
}
$("availabilityToggle").addEventListener("change", async () => {
    const available = $("availabilityToggle").checked;
    renderAvailability();
    try {
        await Api.put("/api/profile/availability", { available });
        toast(available ? "Your status is now available." : "Your status is now unavailable.");
    } catch (err) {
        $("availabilityToggle").checked = !available; renderAvailability(); toast(err.message);
    }
});

// Start: read my availability and my requests from the server.
Api.get("/api/profile").then(p => { availableAgainOn = p.availableAgainOn || ""; $("availabilityToggle").checked = !!p.available; renderAvailability(); }).catch(() => { });
renderAvailability();
renderRequests();
loadRequests();