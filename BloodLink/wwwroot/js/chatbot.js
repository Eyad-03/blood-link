/* =====================================================================
   BloodLink Assistant — the round button at the bottom right of every page.
   Add to a page:  <link rel="stylesheet" href="/css/chatbot.css">
                   <script src="/js/chatbot.js" defer></script>

   The answer comes from POST /api/chatbot (Controllers/Api/ChatbotController.cs),
   which asks Ollama. The answer arrives piece by piece (streaming), so we show
   each piece as soon as it arrives → the chat feels fast.
   ===================================================================== */
(function () {
  const STORE_KEY = "bloodlinkBotChat";   // keeps the conversation while moving between pages
  const SUGGESTIONS = ["Who can donate blood?", "Which blood types can give to A+?", "شو لازم أعمل قبل التبرع؟", "How do I find a donor on BloodLink?"];
  const WELCOME = "Hi! I'm the BloodLink assistant 🩸 Ask me about blood types, donation and how to use BloodLink. أهلاً! اسألني عن الدم والتبرع.";

  let messages = [];   // [{ role: "user" | "assistant", content: "..." }]
  let busy = false;
  try { messages = JSON.parse(sessionStorage.getItem(STORE_KEY) || "[]"); } catch { messages = []; }
  const save = () => { try { sessionStorage.setItem(STORE_KEY, JSON.stringify(messages.slice(-20))); } catch { } };

  /* ---------- build the button and the window ---------- */
  const root = document.createElement("div");
  root.className = "bl-bot";
  root.innerHTML = `
    <section class="bl-bot-panel" role="dialog" aria-label="BloodLink assistant" hidden>
      <header class="bl-bot-head">
        <span class="bl-bot-avatar" aria-hidden="true">🩸</span>
        <div><b>BloodLink Assistant</b><small>Blood &amp; donation questions</small></div>
        <button class="bl-bot-icon-btn" type="button" data-bot-clear title="New chat" aria-label="New chat">↺</button>
        <button class="bl-bot-icon-btn" type="button" data-bot-close aria-label="Close">×</button>
      </header>
      <div class="bl-bot-body" aria-live="polite"></div>
      <div class="bl-bot-suggest"></div>
      <form class="bl-bot-form">
        <input type="text" maxlength="500" placeholder="Ask about blood… / اسأل عن الدم…" aria-label="Your question" dir="auto" autocomplete="off">
        <button type="submit" aria-label="Send">➤</button>
      </form>
      <p class="bl-bot-note">General information only — not medical advice.</p>
    </section>
    <button class="bl-bot-fab" type="button" aria-label="Open BloodLink assistant">
      <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M21 12a8 8 0 01-11.6 7.1L4 20.5l1.4-4.6A8 8 0 1121 12z"/><path d="M12 8.2c-1 1.5-2.4 2.7-2.4 4a2.4 2.4 0 004.8 0c0-1.3-1.4-2.5-2.4-4z" fill="currentColor" stroke="none"/></svg>
    </button>`;
  document.body.appendChild(root);

  const panel = root.querySelector(".bl-bot-panel");
  const body = root.querySelector(".bl-bot-body");
  const suggest = root.querySelector(".bl-bot-suggest");
  const form = root.querySelector(".bl-bot-form");
  const input = form.querySelector("input");
  const sendBtn = form.querySelector("button");

  /* ---------- show messages ---------- */
  const esc = (s) => String(s).replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
  // Very small formatting: **bold** and new lines.
  const format = (text) => esc(text).replace(/\*\*(.+?)\*\*/g, "<b>$1</b>").replace(/\n/g, "<br>");

  function addBubble(role, text) {
    const div = document.createElement("div");
    div.className = "bl-bot-msg " + (role === "user" ? "me" : "bot");
    div.dir = "auto";   // Arabic → right to left, English → left to right
    div.innerHTML = format(text);
    body.appendChild(div);
    body.scrollTop = body.scrollHeight;
    return div;
  }

  function renderAll() {
    body.innerHTML = "";
    addBubble("assistant", WELCOME);
    messages.forEach((m) => addBubble(m.role, m.content));
    suggest.innerHTML = messages.length ? "" : SUGGESTIONS.map((q) => `<button type="button" dir="auto">${esc(q)}</button>`).join("");
  }

  /* ---------- ask the server ---------- */
  async function ask(question) {
    if (busy || !question.trim()) return;
    busy = true; sendBtn.disabled = true; suggest.innerHTML = "";
    messages.push({ role: "user", content: question.trim() });
    addBubble("user", question.trim());

    const bubble = addBubble("assistant", "");
    bubble.innerHTML = '<span class="bl-bot-typing"><i></i><i></i><i></i></span>';
    let answer = "";

    try {
      const res = await fetch("/api/chatbot", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ messages })
      });
      if (!res.ok) throw new Error((await res.text()) || "The assistant is not available right now.");

      // Read the answer piece by piece and show it straight away.
      const reader = res.body.getReader();
      const decoder = new TextDecoder();
      while (true) {
        const { value, done } = await reader.read();
        if (done) break;
        answer += decoder.decode(value, { stream: true });
        bubble.innerHTML = format(answer);
        body.scrollTop = body.scrollHeight;
      }
      if (!answer.trim()) throw new Error("No answer. Please try again.");
      messages.push({ role: "assistant", content: answer });
      save();
    } catch (err) {
      bubble.classList.add("error");
      bubble.textContent = err.message;
      messages.pop();   // forget the question that failed
    } finally {
      busy = false; sendBtn.disabled = false; input.focus();
    }
  }

  /* ---------- events ---------- */
  root.querySelector(".bl-bot-fab").addEventListener("click", () => {
    panel.hidden = !panel.hidden;
    root.classList.toggle("open", !panel.hidden);
    if (!panel.hidden) { renderAll(); setTimeout(() => input.focus(), 50); }
  });
  root.querySelector("[data-bot-close]").addEventListener("click", () => { panel.hidden = true; root.classList.remove("open"); });
  root.querySelector("[data-bot-clear]").addEventListener("click", () => { messages = []; save(); renderAll(); });
  form.addEventListener("submit", (e) => { e.preventDefault(); const q = input.value; input.value = ""; ask(q); });
  suggest.addEventListener("click", (e) => { const b = e.target.closest("button"); if (b) ask(b.textContent); });
  document.addEventListener("keydown", (e) => { if (e.key === "Escape" && !panel.hidden) { panel.hidden = true; root.classList.remove("open"); } });
})();
