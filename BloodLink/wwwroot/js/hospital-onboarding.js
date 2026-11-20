/* =====================================================================
   BloodLink — hospital verification window (/Hospital)
   A new hospital can sign in, but until BloodLink approves it:
     1. it uploads the 3 required documents
     2. presses "Send for review"
     3. waits for the admin (the window can't be closed)
   When the admin activates the account, the window closes by itself
   and the dashboard loads normally.

   API (Controllers/Api/OnboardingController.cs):
     GET  /api/hospital/onboarding
     POST /api/hospital/onboarding/documents/{id}   (form-data: file)
     POST /api/hospital/onboarding/submit
   ===================================================================== */
(function () {
  const $ = (id) => document.getElementById(id);
  const esc = (v) => String(v ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
  const MAX_BYTES = 10 * 1024 * 1024;
  const CHECK_EVERY_MS = 30000;   // while waiting, ask the server every 30 seconds

  let state = null;         // last answer from GET /api/hospital/onboarding
  let onActive = null;      // function that loads the dashboard
  let timer = null;

  const ICON_DOC = '<svg viewBox="0 0 24 24"><path d="M14 3H7a2 2 0 00-2 2v14a2 2 0 002 2h10a2 2 0 002-2V8z"/><path d="M14 3v5h5M9 13h6M9 17h4"/></svg>';
  const ICON_OK = '<svg viewBox="0 0 24 24"><path d="M5 12.5l4.5 4.5L19 7.5"/></svg>';

  /* ---------- show the right step ---------- */
  function render() {
    const s = state;
    const waiting = s.status === "Pending" && s.submitted;
    const rejected = s.status === "Rejected";

    $("onbTitle").textContent = waiting ? "Your request is under review" : rejected ? "Please update your documents" : "Verify your institution";
    $("onbSubtitle").textContent = waiting
      ? `Thank you, ${s.name}. BloodLink is checking your documents.`
      : `Hello ${s.name}. Upload the documents below. BloodLink will review them before your portal is activated.`;

    // step indicator: 2 = upload, 3 = review
    document.querySelectorAll("#onbSteps [data-step]").forEach((li) => {
      const step = li.dataset.step;
      li.classList.toggle("done", step === "upload" && waiting);
      li.classList.toggle("current", (step === "upload" && !waiting) || (step === "review" && waiting));
      li.querySelector("b").textContent = step === "upload" && waiting ? "✓" : step === "upload" ? "2" : "3";
    });

    $("onbRejected").hidden = !rejected;
    $("onbReason").textContent = s.reason || "No reason was given.";
    $("onbUpload").hidden = waiting;
    $("onbWaiting").hidden = !waiting;

    if (waiting) renderWaiting(); else renderUpload();
  }

  function renderUpload() {
    const docs = state.documents;
    $("onbDocs").innerHTML = docs.map((d) => `
      <div class="onb-doc ${d.uploaded ? "uploaded" : ""}" data-doc-row="${d.id}">
        <span class="onb-doc-ic">${d.uploaded ? ICON_OK : ICON_DOC}</span>
        <div class="onb-doc-text">
          <b>${esc(d.name)}</b>
          <small>${d.uploaded ? `${esc(d.fileName)} · ${esc(d.size)}` : "Not uploaded yet"}</small>
        </div>
        <label class="onb-pick">
          <span>${d.uploaded ? "Replace" : "Choose file"}</span>
          <input type="file" accept=".pdf,.jpg,.jpeg,.png" data-doc="${d.id}" hidden>
        </label>
      </div>`).join("");

    const done = docs.filter((d) => d.uploaded).length;
    $("onbBar").style.width = (docs.length ? (done / docs.length) * 100 : 0) + "%";
    $("onbCount").textContent = `${done} of ${docs.length} documents uploaded · PDF, JPG or PNG · up to 10 MB each`;
    $("onbSubmit").disabled = done < docs.length;
  }

  function renderWaiting() {
    $("onbSent").innerHTML = state.documents.map((d) => `
      <li><span>${ICON_OK}</span><div><b>${esc(d.name)}</b><small>${esc(d.fileName)} · ${esc(d.size)}</small></div></li>`).join("");
  }

  function showError(text) {
    $("onbError").textContent = text;
    $("onbError").hidden = !text;
  }

  /* ---------- ask the server ---------- */
  async function load() {
    state = await Api.get("/api/hospital/onboarding");

    if (state.status === "Active") {
      // Approved → close the window and start the dashboard.
      clearInterval(timer);
      $("onboarding").hidden = true;
      document.body.classList.remove("onb-open");
      if (onActive) { const start = onActive; onActive = null; start(); }
      return;
    }

    $("onboarding").hidden = false;
    document.body.classList.add("onb-open");
    render();

    // While waiting, check again from time to time.
    clearInterval(timer);
    if (state.status === "Pending" && state.submitted) timer = setInterval(() => load().catch(() => { }), CHECK_EVERY_MS);
  }

  /* ---------- upload one document ---------- */
  $("onbDocs").addEventListener("change", async (e) => {
    const input = e.target.closest("input[type=file][data-doc]");
    if (!input || !input.files[0]) return;
    const file = input.files[0];
    showError("");

    // Quick checks in the browser (the server checks again).
    if (file.size > MAX_BYTES) { showError(`"${file.name}" is larger than 10 MB.`); return; }
    if (!/\.(pdf|jpe?g|png)$/i.test(file.name)) { showError("Only PDF, JPG or PNG files are allowed."); return; }

    const row = input.closest(".onb-doc");
    row.classList.add("uploading");
    row.querySelector("small").textContent = "Uploading " + file.name + "…";

    const form = new FormData();
    form.append("file", file);
    try {
      state = await Api.upload(`/api/hospital/onboarding/documents/${input.dataset.doc}`, form);
      render();
    } catch (err) {
      showError(err.message);
      render();
    }
  });

  /* ---------- send for review ---------- */
  $("onbSubmit").addEventListener("click", async () => {
    showError("");
    $("onbSubmit").disabled = true;
    try {
      state = await Api.post("/api/hospital/onboarding/submit");
      await load();
    } catch (err) {
      showError(err.message);
      $("onbSubmit").disabled = false;
    }
  });

  $("onbRefresh").addEventListener("click", async () => {
    $("onbRefresh").disabled = true;
    try { await load(); } catch { }
    $("onbRefresh").disabled = false;
  });

  /* ---------- used by hospital.js ---------- */
  window.HospitalOnboarding = {
    // Calls startDashboard() now if the institution is approved,
    // otherwise only after the admin approves it.
    start(startDashboard) {
      onActive = startDashboard;
      load().catch((err) => alert(err.message));
    }
  };
})();
