/* Halls and booking requests come from the server (Controllers/Api/HallsController.cs) */
let halls = [];        // GET /api/halls
let myRequests = [];   // GET /api/halls/bookings/mine (only when signed in)
const $ = id => document.getElementById(id);
let selectedPaymentRequest = null;

function readRequests(){return myRequests}
async function loadHalls(){try{halls=await Api.get("/api/halls")}catch(err){showToast(err.message)}renderHalls()}
async function loadRequests(){if(!Api.isLoggedIn())return;try{myRequests=await Api.get("/api/halls/bookings/mine")}catch{}renderRequests()}
function money(value){return `${value} JOD`}
function esc(value){return String(value??"").replace(/[&<>"']/g,c=>({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"}[c]))}
function showToast(message){$("toast").textContent=message;$("toast").classList.add("show");clearTimeout(showToast.timer);showToast.timer=setTimeout(()=>$("toast").classList.remove("show"),3200)}
function renderHalls(){
 let rows=[...halls];
 const q=$("searchInput").value.trim().toLowerCase(),gov=$("governorateFilter").value,entity=$("entityFilter").value,event=$("eventFilter").value,capacity=Number($("capacityFilter").value||0);
 rows=rows.filter(h=>(!q||[h.name,h.location,h.governorate,h.entity].some(v=>v.toLowerCase().includes(q)))&&(!gov||h.governorate===gov)&&(!entity||h.entity===entity)&&(!event||h.events.includes(event))&&(!capacity||h.capacity>=capacity));
 const sort=$("sortFilter").value;if(sort==="priceLow")rows.sort((a,b)=>a.price-b.price);if(sort==="capacity")rows.sort((a,b)=>b.capacity-a.capacity);
 $("resultsCount").textContent=rows.length;
 $("hallGrid").innerHTML=rows.map(h=>`<article class="hall-card">
  <div class="hall-image"><img src="${h.image}" alt="${esc(h.name)}" loading="lazy"><span class="image-tag ${h.tagClass}">${esc(h.tag)}</span><button class="favorite" type="button" aria-label="Add to favorites" data-favorite="${h.id}">♡</button></div>
  <div class="hall-body"><div class="hall-title-row"><h3>${esc(h.name)}</h3><span class="rating">★ ${h.rating}</span></div>
  <div class="hall-location"><span>●</span>${esc(h.location)} · ${esc(h.entity)}</div><p class="hall-desc">${esc(h.description)}</p>
  <div class="hall-meta"><div class="meta-item"><span>♙</span><b>${h.capacity}</b><small>people</small></div><div class="meta-item"><span>▣</span><b>${h.events.map(esc).join(", ")}</b><small>event types</small></div><div class="meta-item"><span>✓</span><b>Equipment</b><small>${h.features[0]}</small></div></div>
  <div class="hall-footer"><div class="price"><b>${h.price}</b> <em>JOD</em><small>per event / day</small></div><button class="details-btn" type="button" data-details="${h.id}">Details</button><button class="book-btn" type="button" data-book="${h.id}">Book venue →</button></div></div></article>`).join("");
 $("noResults").classList.toggle("hidden",rows.length!==0);$("hallGrid").classList.toggle("hidden",rows.length===0);
}
function openModal(id){$(id).classList.remove("hidden");document.body.style.overflow="hidden"}
function closeModal(id){$(id).classList.add("hidden");if(document.querySelectorAll(".modal-backdrop:not(.hidden)").length===0)document.body.style.overflow=""}
function startBooking(id){if(!Api.isLoggedIn()){showToast("Please sign in to book a venue.");setTimeout(()=>Api.requireLogin(),900);return}const hall=halls.find(h=>h.id===Number(id));if(!hall)return;$("selectedHallId").value=hall.id;$("selectedHallName").textContent=`${hall.name} · ${hall.governorate} · ${money(hall.price)}`;const minDate=new Date();minDate.setDate(minDate.getDate()+1);$("bookingForm").elements.eventDate.min=minDate.toISOString().slice(0,10);openModal("bookingModal")}
function statusInfo(status){if(status==="Pending approval")return ["Pending approval",""];if(status==="Approved")return ["Approved","approved"];if(status==="Paid")return ["Paid · Booking confirmed","paid"];return ["Rejected","rejected"]}
function renderRequests(){
 const rows=readRequests();$("requestCount").textContent=rows.length;
 if(!rows.length){$("requestsList").innerHTML='<div class="empty-requests">No requests yet.<br>Your request will appear here after you choose a venue and submit the documents.</div>';return}
 $("requestsList").innerHTML=[...rows].reverse().map(r=>{const [label,cls]=statusInfo(r.status);return `<article class="request-card"><div><h3>${esc(r.hallName)}</h3><p>Request ID: ${esc(r.id)} · ${esc(r.eventDate)} · ${esc(r.organization)}</p><p style="margin-top:5px">Booking fee: ${money(r.price)}</p></div><span class="request-status ${cls}">${label}</span>${r.status==="Approved"?`<div class="request-actions"><button class="small-btn pay" data-pay="${esc(r.id)}">Pay and confirm booking</button></div>`:""}</article>`}).join("");
}
// Visa payment with Stripe (stripe-pay.js). The card field is created the first time.
let hallCard=null;
function openPayment(id){const r=readRequests().find(x=>x.id===id);if(!r||r.status!=="Approved")return;selectedPaymentRequest=id;$("paymentHallName").textContent=r.hallName;$("paymentAmount").textContent=money(r.price);$("hallPayError").hidden=true;openModal("paymentModal");
 if(hallCard)hallCard.clear();
 else StripePay.mount($("hallCard"),$("hallPayError")).then(c=>{hallCard=c}).catch(e=>{$("hallPayError").hidden=false;$("hallPayError").textContent=e.message;$("confirmPayment").disabled=true})}
$("searchInput").addEventListener("input",renderHalls);
["governorateFilter","entityFilter","eventFilter","capacityFilter","sortFilter"].forEach(id=>$(id).addEventListener("change",renderHalls));
function resetFilters(){["searchInput","governorateFilter","entityFilter","eventFilter","capacityFilter"].forEach(id=>$(id).value="");$("sortFilter").value="recommended";renderHalls()}
$("resetFilters").addEventListener("click",resetFilters);$("resetEmpty").addEventListener("click",resetFilters);
$("hallGrid").addEventListener("click",e=>{
 const book=e.target.closest("[data-book]"),details=e.target.closest("[data-details]"),fav=e.target.closest("[data-favorite]");
 if(book)startBooking(book.dataset.book);
 if(details){const h=halls.find(x=>x.id===Number(details.dataset.details));if(h)showToast(`${h.name} — ${h.capacity} people — ${money(h.price)} — ${h.features.join(", ")}`)}
 if(fav){fav.classList.toggle("selected");fav.textContent=fav.classList.contains("selected")?"♥":"♡"}
});
document.querySelectorAll("[data-close]").forEach(btn=>btn.addEventListener("click",()=>closeModal(btn.dataset.close)));
document.querySelectorAll(".modal-backdrop").forEach(backdrop=>backdrop.addEventListener("click",e=>{if(e.target===backdrop)closeModal(backdrop.id)}));
document.addEventListener("keydown",e=>{if(e.key==="Escape")document.querySelectorAll(".modal-backdrop:not(.hidden)").forEach(m=>closeModal(m.id))});
document.querySelectorAll('input[type="file"]').forEach(input=>input.addEventListener("change",()=>{const name=input.parentElement.querySelector(".file-name");if(name)name.textContent=input.files[0]?.name||"Choose a file"}));
$("bookingForm").addEventListener("submit",async e=>{
 e.preventDefault();const form=e.currentTarget;if(!form.reportValidity())return;
 const fd=new FormData(form);const fileInputs=[...form.querySelectorAll('input[type="file"]')];
 for(const input of fileInputs){const file=input.files[0];if(!file){showToast("Please attach all required documents.");return}if(file.size>10*1024*1024){showToast(`File ${file.name} is larger than 10MB.`);return}}
 const hall=halls.find(h=>h.id===Number($("selectedHallId").value));
 // POST /api/halls/bookings — only the document file names are sent in this version.
 const request={hallId:hall.id,organization:fd.get("organization"),contactName:fd.get("contactName"),email:fd.get("email"),phone:fd.get("phone"),eventType:fd.get("eventType"),eventDate:fd.get("eventDate"),attendees:Number(fd.get("attendees")),eventTime:fd.get("eventTime"),description:fd.get("description"),documents:fileInputs.map(i=>({label:i.parentElement.querySelector("b").textContent,fileName:i.files[0].name}))};
 try{await Api.post("/api/halls/bookings",request)}catch(err){showToast(err.message);return}
 await loadRequests();form.reset();document.querySelectorAll(".file-name").forEach(el=>el.textContent="Choose a file");closeModal("bookingModal");renderRequests();showToast("Your request has been submitted. Your documents will be reviewed before payment is enabled.");openModal("requestsModal");
});
$("myBookingsBtn").addEventListener("click",async()=>{if(!Api.isLoggedIn()){showToast("Please sign in to see your requests.");return}await loadRequests();openModal("requestsModal")});
$("requestsList").addEventListener("click",e=>{const pay=e.target.closest("[data-pay]");if(pay)openPayment(pay.dataset.pay)});
$("confirmPayment").addEventListener("click",async()=>{
 if(!selectedPaymentRequest||!hallCard)return;const r=readRequests().find(x=>x.id===selectedPaymentRequest);if(!r||r.status!=="Approved")return;
 const btn=$("confirmPayment"),label=btn.textContent;btn.disabled=true;btn.textContent="Processing payment…";
 // POST /api/payments/hall-booking/{id} — Visa only, the server takes the price from the database.
 try{const result=await hallCard.pay(`/api/payments/hall-booking/${encodeURIComponent(r.id)}`);
  closeModal("paymentModal");hallCard.clear();await loadRequests();showToast(`Paid with Visa •••• ${result.last4}. Your booking is confirmed.`)}
 catch(err){$("hallPayError").hidden=false;$("hallPayError").textContent=err.message}
 finally{btn.disabled=false;btn.textContent=label}
});
renderHalls();renderRequests();loadHalls();loadRequests();
