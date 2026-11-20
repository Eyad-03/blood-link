/* =====================================================================
   BloodLink chat — shared by /Donor/Requests and /Recipient/Requests
   One conversation per request ID, saved on the server:
     GET  /api/chat/unread        unread badges
     GET  /api/chat/{requestId}   messages (also marks them as read)
     POST /api/chat/{requestId}   send a message
   (Controllers/Api/ChatController.cs). An open chat refreshes every few seconds.

   Usage on each page:
     BloodChat.init({me:"donor"})            // or "recipient"
     BloodChat.buttonHTML(requestId)         // "Chat" button with unread badge
     BloodChat.open(requestId,{name,initials,sub,meta:[[label,value],…],readOnly})
     (system notes like "Request accepted" are added by the server)
     BloodChat.refreshBadges()               // call after re-rendering a list
   ===================================================================== */
(function(){
"use strict";
const MAX_LEN=500,GROUP_MS=5*60*1000,INPUT_MAX_H=110,POLL_MS=4000;
const esc=s=>String(s??"").replace(/[&<>"']/g,c=>({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"}[c]));
const svg=(d,s=13,w=2)=>`<svg viewBox="0 0 24 24" width="${s}" height="${s}" fill="none" stroke="currentColor" stroke-width="${w}" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${d}</svg>`;
const ICON_CHAT=svg('<path d="M21 12a8 8 0 01-11.6 7.1L4 20.5l1.4-4.6A8 8 0 1121 12z"/>');
const ICON_SEND=svg('<path d="M22 2L11 13M22 2l-7 20-4-9-9-4z"/>',18);
const ICON_SHIELD=svg('<path d="M12 3l8 3.2v5.3c0 4.6-3.2 8-8 9.5-4.8-1.5-8-4.9-8-9.5V6.2z"/><path d="M9 12l2.2 2.2L15.5 10"/>',18);
const ICON_CHAT_BIG=svg('<path d="M21 12a8 8 0 01-11.6 7.1L4 20.5l1.4-4.6A8 8 0 1121 12z"/><path d="M9 11h6M9 14h3.5"/>',26,1.7);

/* Quick replies fill the input (the person can edit before sending). */
const QUICK={
  donor:["I'm available today","Which hospital works for you?","I'm on my way","Can we do tomorrow?"],
  recipient:["Thank you so much!","What time suits you?","I'm at the hospital now","Please confirm the hospital"]
};

let me="recipient",current=null,opts={},ui=null,opener=null;

/* ---------- data from the server ---------- */
let unreadMap={};                          // { requestId: number of unread messages }
let thread={messages:[],peerReadAt:""};    // the conversation that is open now
let pollTimer=null;

const unread=id=>unreadMap[id]||0;

function paintBadges(){
  document.querySelectorAll("[data-chat-badge]").forEach(el=>{const n=unread(el.dataset.chatBadge);el.textContent=n>9?"9+":n;el.hidden=!n});
}
// Called by the pages after they re-render their list.
async function refreshBadges(){
  paintBadges();
  try{unreadMap=await Api.get("/api/chat/unread")}catch{return}
  paintBadges();
}
async function loadThread(){
  if(!current)return;
  const id=current;
  try{
    const data=await Api.get("/api/chat/"+encodeURIComponent(id));
    if(id!==current)return;               // the user opened another chat meanwhile
    const changed=data.messages.length!==thread.messages.length||data.peerReadAt!==thread.peerReadAt;
    thread=data;unreadMap[id]=0;
    if(changed)render();
    paintBadges();
  }catch{}
}
const isOpen=()=>!!ui&&!ui.backdrop.hidden;

/* ---------- UI ---------- */
function ensureUI(){
  if(ui)return ui;
  const wrap=document.createElement("div");
  wrap.className="chat-backdrop";wrap.hidden=true;
  wrap.innerHTML=`<section class="chat-modal" role="dialog" aria-modal="true" aria-labelledby="chatTitle">
    <header class="chat-head">
      <span class="chat-avatar" id="chatAvatar"></span>
      <div class="chat-who"><b id="chatTitle"></b><small id="chatSub"></small></div>
      <button class="chat-close" type="button" aria-label="Close chat">×</button>
    </header>
    <div class="chat-context" id="chatContext" hidden></div>
    <div class="chat-body" id="chatBody" aria-live="polite"></div>
    <div class="chat-quick" id="chatQuick"></div>
    <form class="chat-form" id="chatForm" autocomplete="off">
      <textarea id="chatInput" rows="1" maxlength="${MAX_LEN}" placeholder="Write a message…" aria-label="Message"></textarea>
      <button class="chat-send" type="submit" aria-label="Send message" disabled>${ICON_SEND}</button>
    </form>
    <p class="chat-readonly" id="chatReadonly" hidden>${svg('<rect x="5" y="11" width="14" height="9" rx="2.5"/><path d="M8 11V8a4 4 0 018 0v3"/>',14)}This request is completed, so the conversation is now read-only.</p>
  </section>`;
  document.body.appendChild(wrap);
  const q=s=>wrap.querySelector(s);
  ui={backdrop:wrap,avatar:q("#chatAvatar"),title:q("#chatTitle"),sub:q("#chatSub"),context:q("#chatContext"),body:q("#chatBody"),quick:q("#chatQuick"),form:q("#chatForm"),input:q("#chatInput"),send:q(".chat-send"),readonly:q("#chatReadonly")};

  wrap.addEventListener("click",e=>{if(e.target===wrap||e.target.closest(".chat-close"))close()});
  ui.form.addEventListener("submit",e=>{e.preventDefault();submit()});
  ui.input.addEventListener("input",()=>{autosize();ui.send.disabled=!ui.input.value.trim()});
  ui.input.addEventListener("keydown",e=>{if(e.key==="Enter"&&!e.shiftKey&&!e.isComposing){e.preventDefault();submit()}});
  ui.quick.addEventListener("click",e=>{
    const b=e.target.closest("[data-quick]");if(!b||opts.readOnly)return;
    ui.input.value=(ui.input.value?ui.input.value.trimEnd()+" ":"")+b.dataset.quick;
    autosize();ui.send.disabled=false;ui.input.focus();
  });
  return ui;
}
function autosize(){
  /* empty (or measured while the modal is still hidden) → let CSS size one row */
  if(!ui.input.value){ui.input.style.height="";ui.input.style.overflowY="hidden";return}
  ui.input.style.height="auto";
  ui.input.style.height=Math.min(ui.input.scrollHeight,INPUT_MAX_H)+"px";
  ui.input.style.overflowY=ui.input.scrollHeight>INPUT_MAX_H?"auto":"hidden";
}
async function submit(){
  const text=ui.input.value.trim().slice(0,MAX_LEN);
  if(!text||!current||opts.readOnly)return;
  ui.input.value="";autosize();ui.send.disabled=true;
  try{await Api.post("/api/chat/"+encodeURIComponent(current),{text})}
  catch(err){ui.input.value=text;autosize();ui.send.disabled=false;alert(err.message);return}
  await loadThread();
  ui.input.focus();
}

const time=d=>d.toLocaleTimeString([],{hour:"2-digit",minute:"2-digit"});
function dayLabel(d){
  const today=new Date(),y=new Date(Date.now()-864e5);
  if(d.toDateString()===today.toDateString())return"Today";
  if(d.toDateString()===y.toDateString())return"Yesterday";
  return d.toLocaleDateString("en-GB",{day:"numeric",month:"short"});
}
const SAFETY=`<div class="chat-safety"><span>${ICON_SHIELD}</span><p><b>Stay safe</b>Never share verification codes or financial details. Arrange the donation through a licensed hospital.</p></div>`;

function render(){
  const msgs=thread.messages,seenAt=thread.peerReadAt||"";
  let html=SAFETY,lastDay="",prev=null;
  msgs.forEach((m,i)=>{
    const d=new Date(m.at),day=d.toDateString();
    if(day!==lastDay){lastDay=day;prev=null;html+=`<div class="chat-day"><span>${esc(dayLabel(d))}</span></div>`}
    if(m.from==="system"){prev=null;html+=`<div class="chat-sys"><span>${svg('<path d="M5 12.5l4.5 4.5L19 7.5"/>',11,2.4)}${esc(m.text)}</span></div>`;return}
    const next=msgs[i+1],mine=m.from===me;
    const joinPrev=!!prev&&prev.from===m.from&&d-new Date(prev.at)<GROUP_MS;
    const joinNext=!!next&&next.from===m.from&&new Date(next.at)-d<GROUP_MS&&new Date(next.at).toDateString()===day;
    const meta=joinNext?"":`<time>${time(d)}${mine?`<i class="tick ${m.at<=seenAt?"seen":""}" title="${m.at<=seenAt?"Seen":"Sent"}">${m.at<=seenAt?"✓✓":"✓"}</i>`:""}</time>`;
    html+=`<div class="chat-msg ${mine?"mine":"theirs"}${joinPrev?" join-prev":""}${joinNext?" join-next":""}"><p>${esc(m.text)}</p>${meta}</div>`;
    prev=m;
  });
  if(!msgs.length)html+=`<div class="chat-empty"><span>${ICON_CHAT_BIG}</span><b>Start the conversation</b><p>Say hello and agree on a time and hospital.</p></div>`;
  ui.body.innerHTML=html;
  ui.body.scrollTop=ui.body.scrollHeight;
}

function open(id,o={}){
  ensureUI();
  current=id;opts=o;opener=document.activeElement;
  ui.avatar.textContent=o.initials||(o.name||"?").split(/\s+/).map(w=>w[0]).join("").slice(0,2).toUpperCase();
  ui.title.textContent=o.name||"Conversation";
  ui.sub.textContent=o.sub||"";
  const chips=(o.meta||[]).filter(([,v])=>v!==undefined&&v!==null&&v!=="");
  ui.context.hidden=!chips.length;
  ui.context.innerHTML=chips.map(([k,v])=>`<span><i>${esc(k)}</i><b>${esc(v)}</b></span>`).join("");
  ui.quick.hidden=!!o.readOnly;
  ui.quick.innerHTML=(QUICK[me]||[]).map(t=>`<button type="button" data-quick="${esc(t)}">${esc(t)}</button>`).join("");
  ui.form.hidden=!!o.readOnly;ui.readonly.hidden=!o.readOnly;
  ui.input.value="";autosize();ui.send.disabled=true;
  ui.backdrop.hidden=false;document.body.style.overflow="hidden";
  thread={messages:[],peerReadAt:""};render();
  loadThread();
  clearInterval(pollTimer);pollTimer=setInterval(loadThread,POLL_MS);   // check for new messages
  if(!o.readOnly)setTimeout(()=>ui.input.focus({preventScroll:true}),50);
}
function close(){
  if(!isOpen())return;
  ui.backdrop.hidden=true;current=null;document.body.style.overflow="";
  clearInterval(pollTimer);pollTimer=null;
  refreshBadges();
  if(opener&&opener.isConnected)try{opener.focus({preventScroll:true})}catch{}
}

document.addEventListener("keydown",e=>{if(e.key==="Escape"&&isOpen()){e.stopPropagation();close()}},true);
window.BloodChat={
  init(o={}){me=o.me==="donor"?"donor":"recipient"},
  open,close,unread,refreshBadges,
  buttonHTML(id){const s=esc(id);return`<button class="small-btn chat-btn" type="button" data-chat="${s}">${ICON_CHAT}Chat<span class="chat-badge" data-chat-badge="${s}" hidden></span></button>`}
};
})();