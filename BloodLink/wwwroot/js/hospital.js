const $=id=>document.getElementById(id);
const reduceMotion=window.matchMedia("(prefers-reduced-motion: reduce)").matches;
const BLOOD_TYPES=["O+","O-","A+","A-","B+","B-","AB+","AB-"];
/* Data comes from the server (Controllers/Api/HospitalApiController.cs) — see loadDashboard() at the bottom. */
Api.requireLogin(["Hospital"]);
let donors=[];      // GET /api/hospital/donors
const read=(key,fallback)=>{try{const v=localStorage.getItem(key);return v?JSON.parse(v):fallback}catch{return fallback}};
const save=(key,val)=>localStorage.setItem(key,JSON.stringify(val));
let records=[];     // GET /api/hospital/records
let inventory={};   // GET /api/hospital/inventory  → { "O+": 52, ... }
let requestChart=null,inventoryChart=null,reportChart=null,toastTimer=null;
function esc(s){return String(s??"").replace(/[&<>"']/g,c=>({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"}[c]))}
function toast(message){const el=$("toast");el.textContent=message;el.classList.add("show");clearTimeout(toastTimer);toastTimer=setTimeout(()=>el.classList.remove("show"),2800)}
function statusClass(status){return status.toLowerCase()}
function priorityClass(p){return p.toLowerCase()}
function formatDate(value){if(!value)return "—";const d=new Date(value+"T12:00:00");return Number.isNaN(d.getTime())?esc(value):d.toLocaleDateString("en-GB",{day:"2-digit",month:"short",year:"numeric"})}
function recordStatusClass(status){return status==="Verified"?"verified":"pending-verification"}
function recordMatches(r,q){return [r.id,r.donor,r.donorEmail,r.recipient,r.recipientEmail,r.type,r.status,r.date].some(v=>String(v||"").toLowerCase().includes(q))}
function renderRecordRows(target,rows,withPrint=true){
 target.innerHTML=rows.length?rows.map(r=>`<tr><td><span class="request-id">${esc(r.id)}</span></td><td><b>${esc(r.donor)}</b>${r.donorEmail?`<small class="cell-subtext">${esc(r.donorEmail)}</small>`:""}</td><td>${esc(r.recipient)}${r.recipientEmail?`<small class="cell-subtext">${esc(r.recipientEmail)}</small>`:""}</td><td><span class="blood-type">${esc(r.type)}</span></td><td>${Number(r.units)||0}</td><td>${formatDate(r.date)}</td><td><span class="status-pill ${recordStatusClass(r.status)}">${esc(r.status)}</span></td><td><div class="record-actions"><button class="record-action print" data-print="${esc(r.id)}">Print</button></div></td></tr>`).join(""):'<tr><td colspan="8" class="empty-records">No donation records found.</td></tr>';
}
function renderRecords(){
 const verified=records.filter(r=>r.status==="Verified");
 $("recordTotal").textContent=records.length;
 $("recordVerified").textContent=verified.length;
 $("recordUnits").innerHTML=`${records.reduce((sum,r)=>sum+(Number(r.units)||0),0)} <small>units</small>`;
 $("recordPending").textContent=new Set(records.map(r=>r.type)).size;
 const requestCount=$("requestTableCount");if(requestCount)requestCount.textContent=records.length;
 const navRequestCount=$("navRequests");if(navRequestCount)navRequestCount.textContent=records.length;
 $("recordTableCount").textContent=records.length;
 const q=($("recordSearch")?.value||"").toLowerCase();
 const filtered=records.filter(r=>recordMatches(r,q));
 if($("donationRecordsBody"))renderRecordRows($("donationRecordsBody"),filtered);
 const recentBody=$("recentRequestsBody");
 if(recentBody)recentBody.innerHTML=records.slice(0,5).map(r=>`<tr><td><span class="request-id">${esc(r.id)}</span></td><td><b>${esc(r.donor)}</b><small class="cell-subtext">to ${esc(r.recipient)}</small></td><td><span class="blood-type">${esc(r.type)}</span></td><td>${Number(r.units)||0}</td><td>${formatDate(r.date)}</td><td><div class="record-actions">${r.status!=="Verified"?`<button class="record-action verify" data-verify="${esc(r.id)}">Verify</button>`:""}<button class="record-action print" data-print="${esc(r.id)}">Print</button></div></td></tr>`).join("")||'<tr><td colspan="7" class="empty-records">No donation records found.</td></tr>';
 if($("allRequestsBody"))renderRecordRows($("allRequestsBody"),filterRecords(),true);
}
function filterRecords(){return records}
function printRecord(id){
 const r=records.find(x=>x.id===id);if(!r)return;
 const w=window.open("","_blank","width=800,height=700");
 if(!w){toast("Please allow pop-ups to print the donation record.");return}
 w.document.write(`<!doctype html><html><head><title>Donation Verification ${esc(r.id)}</title><meta charset="utf-8"><style>body{font-family:Arial,sans-serif;color:#222;padding:36px;max-width:760px;margin:auto}.brand{font-size:24px;font-weight:bold;color:#c91835}.sub{color:#666;font-size:12px}.rule{height:3px;background:#c91835;margin:18px 0 26px}.title{font-size:22px;margin-bottom:6px}.badge{display:inline-block;border:1px solid #16835b;color:#16835b;padding:6px 9px;border-radius:5px;font-size:12px;font-weight:bold}table{border-collapse:collapse;width:100%;margin-top:22px}td{border:1px solid #ddd;padding:12px;font-size:13px}td:first-child{width:36%;background:#f8f8fa;font-weight:bold}.note{margin-top:20px;padding:13px;background:#f8f8fa;font-size:12px}.signatures{display:flex;gap:50px;margin-top:65px}.signatures div{flex:1;border-top:1px solid #999;padding-top:9px;font-size:11px;color:#555}.foot{margin-top:45px;border-top:1px solid #ddd;padding-top:12px;font-size:10px;color:#777}</style></head><body><div class="brand">♥ BloodLink</div><div class="sub">HOSPITAL DONATION DOCUMENTATION</div><div class="rule"></div><h1 class="title">Donation Verification Record</h1><p>Record ID: <b>${esc(r.id)}</b> &nbsp; <span class="badge">${esc(r.status)}</span></p><p class="sub">Generated on ${new Date().toLocaleDateString("en-GB",{day:"2-digit",month:"long",year:"numeric"})}</p><table><tr><td>Donor full name</td><td>${esc(r.donor)}</td></tr><tr><td>Donor email</td><td>${esc(r.donorEmail||"—")}</td></tr><tr><td>Recipient / patient</td><td>${esc(r.recipient)}</td></tr><tr><td>Recipient email</td><td>${esc(r.recipientEmail||"—")}</td></tr><tr><td>Blood type</td><td>${esc(r.type)}</td></tr><tr><td>Units documented</td><td>${Number(r.units)||0}</td></tr><tr><td>Donation date</td><td>${formatDate(r.date)}</td></tr><tr><td>Verification status</td><td>${esc(r.status)}</td></tr></table><div class="note"><b>Hospital notes</b><br>${esc(r.notes||"No additional notes provided.")}</div><div class="signatures"><div>Authorized hospital staff</div><div>Recipient / authorized representative</div></div><div class="foot">BloodLink demo record. Official use requires verification by authorized hospital personnel and secure storage in the hospital system.</div><script>window.onload=()=>window.print();<\/script></body></html>`);
 w.document.close();
}
function renderInventory(){
 const entries=BLOOD_TYPES.map(type=>({type,units:Number(inventory[type])||0}));const total=entries.reduce((s,x)=>s+x.units,0);
 $("totalUnits").innerHTML=`${total} <small>units</small>`;$("donutTotal").textContent=total;
 const low=entries.filter(x=>x.units<15).sort((a,b)=>a.units-b.units);
 $("lowStockList").innerHTML=low.length?low.map(x=>`<div class="stock-row ${x.units<8?"critical":"low"}"><div class="stock-type">${x.type}</div><div class="stock-info"><b>${x.type} blood</b><small>Minimum target: 15 units</small></div><div class="stock-right"><b>${x.units} units</b><small>${x.units===0?"Out of stock":x.units<8?"Critical":"Low stock"}</small></div><div class="stock-bar"><span style="width:${Math.min(100,x.units/15*100)}%"></span></div></div>`).join(""):'<p class="empty-state">No low stock alerts. Inventory is above the minimum threshold.</p>';
 const alertCount=low.length;$("inventory").querySelector(".stock-alert-count").textContent=`${alertCount} alert${alertCount===1?"":"s"}`;
 const bannerCount=$("lowStockBannerCount");if(bannerCount)bannerCount.textContent=`${alertCount} blood type${alertCount===1?"":"s"}`;
 const rack=$("bloodVials");
 if(rack)rack.innerHTML=entries.map((x,i)=>{const pct=Math.max(4,Math.min(100,x.units/80*100));const state=x.units<8?"critical":x.units<15?"low":"";
  return `<div class="vial ${state}" style="--fill:${pct.toFixed(1)}%;--d:${i*70}ms" title="${x.type}: ${x.units} units"><div class="vial-tube"><div class="vial-marks"></div><div class="vial-liquid"><svg class="vial-wave back" viewBox="0 0 120 10" preserveAspectRatio="none"><path d="M0 5 Q15 0 30 5 T60 5 T90 5 T120 5 V10 H0Z"/></svg><svg class="vial-wave" viewBox="0 0 120 10" preserveAspectRatio="none"><path d="M0 5 Q15 10 30 5 T60 5 T90 5 T120 5 V10 H0Z"/></svg><span class="bubble"></span><span class="bubble"></span><span class="bubble"></span></div></div><b>${x.type}</b><small>${x.units} units</small></div>`}).join("");
 if(inventoryChart){inventoryChart.data.datasets[0].data=entries.map(x=>x.units);inventoryChart.update()}
 $("bloodLegend").innerHTML=entries.map((x,i)=>`<div class="blood-legend-item"><i style="background:${chartColors[i]}"></i><span>${x.type}</span><b>${x.units}</b></div>`).join("");
}
const chartColors=["#c91835","#e55a70","#e69a4b","#edc26b","#348f77","#69b89f","#547ec5","#94addc"];
function initCharts(){
 if(typeof Chart==="undefined"){document.querySelectorAll(".chart-wrap").forEach(c=>c.innerHTML='<p style="font-size:10px;color:#999;padding:20px">Charts could not load. Check your internet connection.</p>');return}
 Chart.defaults.font.family='"DM Sans", sans-serif';Chart.defaults.font.size=12;Chart.defaults.color="#8b8c9e";
 Object.assign(Chart.defaults.plugins.tooltip,{backgroundColor:"rgba(27,28,40,.94)",padding:12,cornerRadius:10,titleFont:{weight:"700"},boxPadding:5,usePointStyle:true});
 const redFill=ctx=>{const {chart}=ctx;const {ctx:c,chartArea}=chart;if(!chartArea)return "rgba(212,29,61,.08)";const g=c.createLinearGradient(0,chartArea.top,0,chartArea.bottom);g.addColorStop(0,"rgba(212,29,61,.28)");g.addColorStop(1,"rgba(212,29,61,0)");return g};
 const rise={y:{duration:1100,easing:"easeOutQuart",from:ctx=>ctx.type==="data"&&ctx.chart.scales.y?ctx.chart.scales.y.getPixelForValue(0):undefined,delay:ctx=>ctx.type==="data"&&ctx.mode==="default"?ctx.dataIndex*90+ctx.datasetIndex*160:0}};
 requestChart=new Chart($("requestsChart"),{type:"line",data:{labels:["May","Jun","Jul","Aug","Sep","Oct"],datasets:[{label:"Donation records",data:[82,98,90,125,112,136],borderColor:"#d41d3d",backgroundColor:redFill,fill:true,tension:.42,pointRadius:4,pointHoverRadius:7,pointBackgroundColor:"#fff",pointBorderWidth:2.5,borderWidth:3},{label:"Verified transfers",data:[68,80,74,102,91,84],borderColor:"#38b484",backgroundColor:"transparent",tension:.42,pointRadius:4,pointHoverRadius:7,pointBackgroundColor:"#fff",pointBorderWidth:2.5,borderWidth:3,borderDash:[6,5]}]},options:{responsive:true,maintainAspectRatio:false,animations:rise,interaction:{mode:"index",intersect:false},plugins:{legend:{display:false}},scales:{x:{grid:{display:false},border:{display:false}},y:{beginAtZero:true,grid:{color:"#f1eef2"},border:{display:false},ticks:{maxTicksLimit:5}}}}});
 inventoryChart=new Chart($("inventoryChart"),{type:"doughnut",data:{labels:BLOOD_TYPES,datasets:[{data:BLOOD_TYPES.map(t=>inventory[t]||0),backgroundColor:chartColors,borderColor:"#fff",borderWidth:3,borderRadius:6,hoverOffset:10,spacing:2}]},options:{responsive:true,maintainAspectRatio:false,cutout:"74%",animation:{animateRotate:true,animateScale:true,duration:1400,easing:"easeOutQuart"},plugins:{legend:{display:false},tooltip:{callbacks:{label:ctx=>`${ctx.label}: ${ctx.raw} units`}}}}});
 reportChart=new Chart($("reportChart"),{type:"bar",data:{labels:["May","Jun","Jul","Aug","Sep","Oct"],datasets:[{label:"Donation records",data:[121,140,133,159,151,173],backgroundColor:"#d41d3d",borderRadius:8,maxBarThickness:30},{label:"Verified transfers",data:[88,102,95,123,116,94],backgroundColor:"#7fd0b0",borderRadius:8,maxBarThickness:30}]},options:{responsive:true,maintainAspectRatio:false,animations:rise,plugins:{legend:{position:"bottom",labels:{usePointStyle:true,boxWidth:7,boxHeight:7,padding:18}}},scales:{x:{grid:{display:false},border:{display:false}},y:{beginAtZero:true,grid:{color:"#f1eef2"},border:{display:false}}}}});
}
function navigate(section){
 // Inventory is a panel inside Overview, so reveal Overview and scroll to that panel.
 const targetSection=section==="inventory"?"overview":section;
 document.querySelectorAll(".page-section").forEach(el=>{
   const active=el.id===targetSection;
   el.classList.toggle("hidden-section",!active);
   el.classList.toggle("visible",active&&el.id!=="overview");
   if(el.id==="overview")el.style.display=active?"block":"none";
 });
 document.querySelectorAll(".nav-item").forEach(a=>a.classList.toggle("active",
   a.dataset.section===section || (section==="all-requests"&&a.dataset.section==="donation-records") || (section==="inventory"&&a.dataset.section==="overview")));
 const labels={overview:"Overview","donation-records":"Donation verification",requests:"Donation verification",inventory:"Blood inventory",donors:"Donor directory",reports:"Reports & analytics",profile:"Hospital profile",settings:"Settings","all-requests":"All blood requests",ads:"Advertisements","hall-bookings":"Hall bookings",appointments:"Donation appointments"};
 $("crumbTitle").textContent=labels[section]||"Overview";
 if(section==="donation-records"||section==="all-requests")renderRecords();
 if(section==="inventory")renderInventory();
 if(section==="donors")renderDonors();
 if(section==="appointments"&&typeof renderAppointments==="function")renderAppointments();
 if(section==="ads"&&typeof renderAds==="function"){renderAds();updateAdPreview()}
 if(section==="hall-bookings"&&typeof window.renderHallBookings==="function")window.renderHallBookings();
 if(section==="reports"&&reportChart)setTimeout(()=>reportChart.resize(),80);
 if(section==="overview"){if(requestChart)setTimeout(()=>requestChart.resize(),80);if(inventoryChart)setTimeout(()=>inventoryChart.resize(),80)}
 $("sidebar").classList.remove("open");
 const menu=$("menuButton");if(menu)menu.setAttribute("aria-expanded","false");
 if(section==="inventory")setTimeout(()=>$("inventory").scrollIntoView({behavior:"smooth",block:"start"}),60);
 else window.scrollTo({top:0,behavior:"smooth"});
}
function getFilteredDonors(){
 const q=($("donorSearch")?.value||"").trim().toLowerCase();
 const blood=$("donorBloodFilter")?.value||"all";
 const availability=$("donorAvailabilityFilter")?.value||"all";
 return donors.filter(d=>{
  const matchesQuery=[d.name,d.email,d.type,d.location,d.availability].some(v=>String(v).toLowerCase().includes(q));
  const matchesBlood=blood==="all"||d.type===blood;
  const matchesAvailability=availability==="all"||d.availability===availability;
  return matchesQuery&&matchesBlood&&matchesAvailability;
 });
}
function renderDonors(){
 const filtered=getFilteredDonors();
 const count=$("donorResultsCount");if(count)count.textContent=filtered.length;
 const body=$("donorTableBody");
 if(!body)return;
 body.innerHTML=filtered.map(d=>`<tr>
  <td><b>${esc(d.name)}</b></td>
  <td><a class="donor-email-link" href="mailto:${encodeURIComponent(d.email)}">${esc(d.email)}</a></td>
  <td><span class="blood-type">${esc(d.type)}</span></td>
  <td>${esc(d.location)}</td>
  <td>${esc(d.last)}<small class="cell-subtext">${d.donations} donation${d.donations===1?"":"s"} here</small></td>
  <td><span class="status-pill ${d.availability==="Available"?"fulfilled":"pending"}">${esc(d.availability)}</span>${d.availableAgainOn?`<small class="cell-subtext">again on ${formatDate(d.availableAgainOn)}</small>`:""}</td>
  <td><button class="donor-action" type="button" data-email-donor="${esc(d.email)}"${d.availability==="Available"?"":" disabled title=\"Not available right now\""}>Email</button></td>
 </tr>`).join("")||`<tr><td colspan="7" class="empty-records">${donors.length?"No donors match your filters.":"No donors yet. Donors appear here after you save a donation record with their email."}</td></tr>`;
}
// Sends a "we need blood" email to donors — POST /api/hospital/donors/email.
// The server sends one email per donor and skips donors who are not available.
async function sendDonorEmails(emails){
 const bloodType=$("donorBloodFilter")?.value||"all";
 try{
  const r=await Api.post("/api/hospital/donors/email",{emails,bloodType:bloodType==="all"?"":bloodType});
  toast(`Email sent to ${r.sent} donor${r.sent===1?"":"s"}${r.skipped?` · ${r.skipped} not available (skipped)`:""}${r.failed?` · ${r.failed} failed`:""}.`);
  if(r.sent)bloodBurst("#13895f");
 }catch(err){toast(err.message)}
}
function emailFilteredDonors(){
 const filtered=getFilteredDonors();
 if(!filtered.length){toast("No donors match the current filters.");return}
 const available=filtered.filter(d=>d.availability==="Available").length;
 if(!available){toast("None of these donors is available right now.");return}
 const bloodType=$("donorBloodFilter")?.value||"all";
 if(!confirm(`Send a blood donation request${bloodType==="all"?"":` for ${bloodType}`} to ${available} available donor${available===1?"":"s"}?`))return;
 sendDonorEmails(filtered.map(d=>d.email));
}

document.querySelectorAll(".nav-item[data-section]").forEach(a=>a.addEventListener("click",e=>{e.preventDefault();navigate(a.dataset.section)}));
document.querySelectorAll('a[href="#inventory"]').forEach(a=>a.addEventListener("click",e=>{e.preventDefault();navigate("inventory")}));
if($("viewAllRequests"))$("viewAllRequests").addEventListener("click",e=>{e.preventDefault();navigate("all-requests")});
$("manageInventory").addEventListener("click",()=>navigate("inventory"));
if($("recentRequestsBody"))$("recentRequestsBody").addEventListener("click",e=>{const p=e.target.closest("[data-print]");const v=e.target.closest("[data-verify]");if(p)printRecord(p.dataset.print);if(v)verifyRecord(v.dataset.verify);});
$("donationRecordsBody").addEventListener("click",e=>{const p=e.target.closest("[data-print]");const v=e.target.closest("[data-verify]");if(p)printRecord(p.dataset.print);if(v)verifyRecord(v.dataset.verify);});
if($("allRequestsBody"))$("allRequestsBody").addEventListener("click",e=>{const p=e.target.closest("[data-print]");const v=e.target.closest("[data-verify]");if(p)printRecord(p.dataset.print);if(v)verifyRecord(v.dataset.verify);});
async function verifyRecord(id){const r=records.find(x=>x.id===id);if(!r)return;if(!confirm(`Confirm that donation ${id} has been checked and can be marked Verified?`))return;
 try{await Api.put(`/api/hospital/records/${encodeURIComponent(id)}/verify`)}catch(err){toast(err.message);return}
 r.status="Verified";renderRecords();bloodBurst("#13895f");flashRow(id);toast(`${id} verified. You can now print its record.`);}
$("recordSearch").addEventListener("input",renderRecords);
$("newDonationButton").addEventListener("click",()=>{$("createRecordPanel").scrollIntoView({behavior:"smooth",block:"start"});$("donationForm").elements.donor.focus()});
$("donationDate").value=new Date().toISOString().slice(0,10);
$("donationForm").addEventListener("submit",async e=>{e.preventDefault();const form=e.currentTarget;const fd=new FormData(form);
 // POST /api/hospital/records — the server returns the saved record.
 let rec;
 try{rec=await Api.post("/api/hospital/records",{donor:fd.get("donor").trim(),donorEmail:fd.get("donorEmail").trim(),recipient:fd.get("recipient").trim(),recipientEmail:fd.get("recipientEmail").trim(),type:fd.get("type"),units:Number(fd.get("units")),date:fd.get("date"),notes:fd.get("notes").trim()})}
 catch(err){toast(err.message);return}
 records.unshift(rec);form.reset();
 // the donor is now in the directory → reload it
 Api.get("/api/hospital/donors").then(list=>{donors=list;renderDonors()}).catch(()=>{});
 $("donationDate").value=new Date().toISOString().slice(0,10);renderRecords();bloodBurst();flashRow(rec.id);toast(`Donation record ${rec.id} saved.`);$("recordTableCount")?.scrollIntoView({behavior:"smooth",block:"center"});});

$("donorSearch").addEventListener("input",renderDonors);
$("donorBloodFilter")?.addEventListener("change",renderDonors);
$("donorAvailabilityFilter")?.addEventListener("change",renderDonors);
$("emailDonorsButton")?.addEventListener("click",emailFilteredDonors);
$("donorTableBody")?.addEventListener("click",e=>{
 const button=e.target.closest("[data-email-donor]");
 if(button&&confirm(`Send a blood donation request to ${button.dataset.emailDonor}?`))sendDonorEmails([button.dataset.emailDonor]);
});
$("exportReport").addEventListener("click",()=>{
 const rows=[["Record ID","Donor","Donor Email","Recipient / Patient","Recipient Email","Blood Type","Units","Donation Date","Verification Status","Notes"],...records.map(r=>[r.id,r.donor,r.donorEmail,r.recipient,r.recipientEmail,r.type,r.units,r.date,r.status,r.notes])];
 const csv=rows.map(row=>row.map(v=>`"${String(v??"").replace(/"/g,'""')}"`).join(",")).join("\r\n");
 const blob=new Blob([csv],{type:"text/csv;charset=utf-8;"}),url=URL.createObjectURL(blob),a=document.createElement("a");a.href=url;a.download="bloodlink-donation-verification-report.csv";a.click();URL.revokeObjectURL(url);toast("Donation verification CSV exported.");
});
$("dismissAlert").addEventListener("click",()=>{const b=$("overview").querySelector(".alert-banner");if(!b)return;if(reduceMotion){b.remove();return}b.animate([{opacity:1,transform:"none",height:b.offsetHeight+"px",marginBottom:"20px"},{opacity:0,transform:"translateX(30px)",height:"0px",marginBottom:"0px",paddingTop:"0px",paddingBottom:"0px"}],{duration:450,easing:"cubic-bezier(.22,.8,.2,1)"}).onfinish=()=>b.remove();});
$("notificationButton").addEventListener("click",()=>{const low=BLOOD_TYPES.filter(t=>(Number(inventory[t])||0)<15).length,pending=records.filter(r=>r.status!=="Verified").length;const due=typeof window.hallPaymentsDue==="function"?window.hallPaymentsDue():0;const apPend=typeof apptPendingCount==="function"?apptPendingCount():0;toast(`You have ${low} low-stock alert${low===1?"":"s"}, ${pending} record${pending===1?"":"s"} awaiting verification${apPend?`, ${apPend} appointment request${apPend===1?"":"s"} to review`:""}${due?` and ${due} hall booking${due===1?"":"s"} ready for payment`:""}.`)});
$("menuButton").addEventListener("click",()=>{
 const opened=$("sidebar").classList.toggle("open");
 $("menuButton").setAttribute("aria-expanded",String(opened));
});
// "Sign out" is handled by api.js (the button has data-logout).
$("chartRange").addEventListener("change",()=>{
 if(!requestChart)return;const v=$("chartRange").value;
 const labels=v==="7"?["Mon","Tue","Wed","Thu","Fri","Sat","Sun"]:v==="12"?["Nov","Dec","Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct"]:["May","Jun","Jul","Aug","Sep","Oct"];
 const incoming=v==="7"?[18,22,16,27,24,19,30]:v==="12"?[70,78,85,76,91,101,82,98,90,125,112,136]:[82,98,90,125,112,136];
 const fulfilled=v==="7"?[12,18,12,20,17,15,22]:v==="12"?[55,61,68,63,71,78,68,80,74,102,91,84]:[68,80,74,102,91,84];
 requestChart.data.labels=labels;requestChart.data.datasets[0].data=incoming;requestChart.data.datasets[1].data=fulfilled;requestChart.update();
});
$("saveHospitalProfile").addEventListener("click",async()=>{const data={name:$("hospitalName").value,email:$("hospitalEmail").value,phone:$("hospitalPhone").value,city:$("hospitalCity").value,address:$("hospitalAddress").value};
 try{await Api.put("/api/hospital/profile",data)}catch(err){toast(err.message);return}
 showHospitalName(data.name);toast("Hospital profile saved.")});
["lowStockSetting","urgentSetting","compactSetting"].forEach(id=>$(id).addEventListener("change",()=>{
 const settings={lowStock:$("lowStockSetting").checked,urgent:$("urgentSetting").checked,compact:$("compactSetting").checked};save("bloodlinkHospitalSettings",settings);
 const banner=document.querySelector(".alert-banner");if(banner)banner.style.display=settings.lowStock?"flex":"none";
 document.querySelectorAll(".priority.urgent").forEach(el=>el.closest("tr").style.display=settings.urgent?"":"none");
 document.querySelectorAll("table").forEach(t=>t.classList.toggle("compact-table",settings.compact));
 $("settingsMessage").textContent="Settings saved in this browser.";
}));
$("todayDate").textContent=new Date().toLocaleDateString("en-GB",{day:"numeric",month:"short",year:"numeric"});
// Personalize the greeting to the visitor's local time.
const greetingHeading=document.querySelector(".welcome-row h1");
if(greetingHeading){const hour=new Date().getHours();const greeting=hour<12?"Good morning, ":hour<18?"Good afternoon, ":"Good evening, ";greetingHeading.childNodes[0].textContent=greeting;}
// Hospital profile from the server (GET /api/hospital/profile).
function showHospitalName(name){const switchName=document.querySelector(".hospital-switch b");if(switchName&&name)switchName.textContent=name}
function fillProfile(p){
 const fields={hospitalName:p.name,hospitalEmail:p.email,hospitalPhone:p.phone,hospitalCity:p.city,hospitalAddress:p.address};
 Object.entries(fields).forEach(([id,value])=>{if($(id)&&value!==undefined)$(id).value=value});
 showHospitalName(p.name);
}
// Display preferences stay in this browser.
const savedSettings=read("bloodlinkHospitalSettings",null);
if(savedSettings){
 if($("lowStockSetting"))$("lowStockSetting").checked=savedSettings.lowStock!==false;
 if($("urgentSetting"))$("urgentSetting").checked=savedSettings.urgent!==false;
 if($("compactSetting"))$("compactSetting").checked=!!savedSettings.compact;
 document.querySelectorAll("table").forEach(t=>t.classList.toggle("compact-table",!!savedSettings.compact));
 if($("overview")?.querySelector(".alert-banner"))$("overview").querySelector(".alert-banner").style.display=savedSettings.lowStock===false?"none":"flex";
}
// Reveal content as it enters view, without breaking reduced-motion preferences.
if("IntersectionObserver" in window && !window.matchMedia("(prefers-reduced-motion: reduce)").matches){
 const revealObserver=new IntersectionObserver(entries=>entries.forEach(entry=>{
   if(entry.isIntersecting){entry.target.classList.add("is-visible");revealObserver.unobserve(entry.target)}
 }),{threshold:.12});
 document.querySelectorAll(".metric-card,.panel,.alert-banner").forEach(el=>revealObserver.observe(el));
}
// Close the mobile drawer when tapping outside it.
document.addEventListener("click",event=>{
 if(window.innerWidth<=760 && $("sidebar").classList.contains("open") && !$("sidebar").contains(event.target) && !$("menuButton").contains(event.target)){
   $("sidebar").classList.remove("open");$("menuButton").setAttribute("aria-expanded","false");
 }
});
renderRecords();renderInventory();renderDonors();initCharts();

// Load everything from the server, then draw the dashboard again.
async function loadDashboard(){
 try{
  const [profile,inv,recs,donorList]=await Promise.all([
   Api.get("/api/hospital/profile"),Api.get("/api/hospital/inventory"),Api.get("/api/hospital/records"),Api.get("/api/hospital/donors")]);
  fillProfile(profile);inventory=inv;records=recs;donors=donorList;
  renderRecords();renderInventory();renderDonors();
 }catch(err){toast(err.message)}
}

/* =====================================================================
   Motion layer — heartbeat-themed interactions
   ===================================================================== */
// ECG traces: build a heartbeat waveform path sized to each SVG's viewBox.
function ecgPath(w,h,cycles){
 const mid=h*.55,step=w/cycles;let d=`M0 ${mid}`;
 for(let i=0;i<cycles;i++){
  const x=i*step,u=step/20;
  d+=` L${x+u*5} ${mid} Q${x+u*6} ${mid-h*.12} ${x+u*7} ${mid}`+        // P wave
     ` L${x+u*8} ${mid} L${x+u*8.6} ${mid+h*.12} L${x+u*9.4} ${h*.06}`+    // Q-R
     ` L${x+u*10.3} ${h*.94} L${x+u*11} ${mid} L${x+u*12.5} ${mid}`+        // S
     ` Q${x+u*14} ${mid-h*.2} ${x+u*15.5} ${mid} L${x+step} ${mid}`;        // T wave
 }
 return d;
}
document.querySelectorAll("svg.ecg").forEach(svg=>{
 const [, , w, h]=svg.getAttribute("viewBox").split(" ").map(Number);
 const d=ecgPath(w,h,Number(svg.dataset.cycles)||3);
 svg.querySelectorAll("path").forEach(p=>{p.setAttribute("d",d);p.setAttribute("pathLength","1000")});
});

// Intro loader: one heartbeat, then reveal the dashboard.
(function(){
 const loader=$("pageLoader");if(!loader)return;
 const hide=()=>{loader.classList.add("done");setTimeout(()=>loader.remove(),700);countUp(document.querySelector(".page-section:not(.hidden-section)"))};
 if(reduceMotion){loader.remove();return}
 const started=performance.now();
 const go=()=>setTimeout(hide,Math.max(0,1100-(performance.now()-started)));
 if(document.readyState==="complete")go();else window.addEventListener("load",go,{once:true});
 setTimeout(()=>{if(loader.isConnected)hide()},3000);
})();

// Live clock in the hero.
function tickClock(){const c=$("liveClock");if(c)c.textContent=new Date().toLocaleTimeString("en-GB",{hour:"2-digit",minute:"2-digit"})}
tickClock();setInterval(tickClock,15000);

// Count-up for metric numbers (keeps units like "<small>units</small>").
function countUp(root){
 if(reduceMotion||!root)return;
 root.querySelectorAll(".metric-value, .donut-center strong, .donor-results-count b").forEach(el=>{
  const node=[...el.childNodes].find(n=>n.nodeType===3&&/\d/.test(n.textContent));if(!node)return;
  const raw=node.textContent,m=raw.match(/\d[\d,]*(\.\d+)?/);if(!m)return;
  const target=parseFloat(m[0].replace(/,/g,"")),dec=(m[1]||"").length-(m[1]?1:0),comma=m[0].includes(",");
  const fmt=v=>raw.replace(m[0],comma?v.toLocaleString("en-US",{minimumFractionDigits:dec,maximumFractionDigits:dec}):v.toFixed(dec));
  const start=performance.now(),dur=1200;
  (function tick(now){
   if(!node.isConnected)return;
   const p=Math.min(1,(now-start)/dur),e=1-Math.pow(1-p,4);
   node.textContent=p<1?fmt(target*e):raw;
   if(p<1)requestAnimationFrame(tick);
  })(start);
 });
}

// Section changes: animate breadcrumb and re-run counters.
const baseNavigate=navigate;
navigate=function(section){
 baseNavigate(section);
 const crumb=$("crumbTitle");crumb.classList.remove("swap");void crumb.offsetWidth;crumb.classList.add("swap");
 const target=section==="inventory"?"overview":section;
 countUp($(target));
};

// Ripple on buttons.
document.addEventListener("pointerdown",e=>{
 window.__lastPointer={x:e.clientX,y:e.clientY};
 const b=e.target.closest(".primary-button,.full-button,.record-action,.donor-action,.icon-button");
 if(!b||reduceMotion)return;
 const r=b.getBoundingClientRect(),size=Math.max(r.width,r.height)*2.2,s=document.createElement("span");
 s.className="ripple";s.style.cssText=`width:${size}px;height:${size}px;left:${e.clientX-r.left-size/2}px;top:${e.clientY-r.top-size/2}px`;
 b.appendChild(s);s.addEventListener("animationend",()=>s.remove());
},true);

// Cursor-following glow on cards and panels.
document.addEventListener("pointermove",e=>{
 const c=e.target.closest&&e.target.closest(".metric-card,.panel:not(.table-panel)");if(!c)return;
 const r=c.getBoundingClientRect();c.style.setProperty("--mx",`${e.clientX-r.left}px`);c.style.setProperty("--my",`${e.clientY-r.top}px`);
},{passive:true});

// Blood-drop burst for successful actions.
function bloodBurst(color){
 if(reduceMotion)return;
 const {x,y}=window.__lastPointer||{x:innerWidth/2,y:innerHeight/2};
 const palette=color?[color,color,"#5fd1a2"]:["#d41d3d","#ef4562","#9e0d28"];
 const ring=document.createElement("span");ring.className="burst-ring";ring.style.cssText=`left:${x}px;top:${y}px;border-color:${palette[0]}`;document.body.appendChild(ring);
 ring.addEventListener("animationend",()=>ring.remove());
 for(let i=0;i<16;i++){
  const a=(Math.PI*2*i)/16+Math.random()*.4,dist=50+Math.random()*60,d=document.createElement("span");
  d.className="burst-drop";
  d.style.cssText=`left:${x}px;top:${y}px;--tx:${Math.cos(a)*dist}px;--ty:${Math.sin(a)*dist+20}px;--r:${a*57+45}deg;--c:${palette[i%3]};width:${6+Math.random()*7}px;height:${6+Math.random()*7}px`;
  document.body.appendChild(d);d.addEventListener("animationend",()=>d.remove());
 }
}
function flashRow(id){
 document.querySelectorAll(`[data-print="${CSS.escape(id)}"]`).forEach(b=>{const tr=b.closest("tr");if(tr){tr.classList.remove("row-flash");void tr.offsetWidth;tr.classList.add("row-flash")}});
}
/* =====================================================================
   Advertisements — create paid hospital ads, checkout, and campaign list
   Shared data/template lives in ads-data.js (window.BloodLinkAds).
   ===================================================================== */
const ADS=window.BloodLinkAds;
let ads=[];   // my hospital's ads — GET /api/hospital/ads
let selectedPlan="featured";
let pendingAd=null;
const hospitalName=()=>($("hospitalName")?.value||"Al Hayat Hospital").trim();
const myAds=()=>ads.filter(a=>a.owner==="self");

function renderPlans(){
 $("planGrid").innerHTML=Object.values(ADS.PLANS).map(p=>`<label class="plan-card${p.id===selectedPlan?" selected":""}${p.id==="featured"?" popular":""}">
  <input type="radio" name="adPlan" value="${p.id}"${p.id===selectedPlan?" checked":""}>
  ${p.id==="featured"?'<span class="plan-flag">Most chosen</span>':""}
  <span class="plan-name">${p.name}</span>
  <span class="plan-price"><b>${p.price}</b> ${ADS.CURRENCY}<small>/ ${p.days} days</small></span>
  <ul>${p.perks.map(x=>`<li>${x}</li>`).join("")}</ul>
  <span class="plan-pick">${p.id===selectedPlan?"Selected":"Choose plan"}</span>
 </label>`).join("");
}
function readAdForm(){
 const f=$("adForm"),fd=new FormData(f);
 const get=k=>String(fd.get(k)||"").trim();
 const startDate=get("startDate")||ADS.today();
 return {title:get("title"),department:get("department"),category:get("category"),badge:get("badge"),eventDate:get("eventDate"),startTime:get("startTime"),endTime:get("endTime"),
  location:get("location"),phone:get("phone"),description:get("description"),booking:!!fd.get("booking"),plan:selectedPlan,startDate,endDate:ADS.endDate(startDate,selectedPlan),
  hospital:hospitalName(),owner:"self"};
}
function updateAdPreview(){
 if(!ADS)return;
 const ad=readAdForm();
 $("adPreview").innerHTML=ADS.cardHTML(ad,{preview:true});
 const p=ADS.PLANS[selectedPlan],f=ADS.fees(selectedPlan);
 $("summaryPlan").textContent=`${p.name} plan · ${p.days} days`;
 $("summaryRun").textContent=`Live ${ADS.formatDate(ad.startDate)} → ${ADS.formatDate(ad.endDate)}`;
 $("summaryFee").textContent=ADS.money(f.fee);$("summaryVat").textContent=ADS.money(f.vat);$("summaryTotal").textContent=ADS.money(f.total);
 $("adDescCount").textContent=ad.description.length;
}
function validateAd(ad){
 const t=ADS.today();
 if(!ad.title||!ad.department||!ad.category||!ad.eventDate||!ad.startTime||!ad.endTime||!ad.location||!ad.phone||!ad.description)return "Please fill in all required fields.";
 if(ad.description.length<20)return "Please write a description of at least 20 characters.";
 if(ad.startDate<t)return "The ad can't go live on a past date.";
 if(ad.eventDate<t)return "The event date can't be in the past.";
 if(ad.eventDate<ad.startDate)return "The ad must go live on or before the event date.";
 if(ad.endTime<=ad.startTime)return "The end time must be after the start time.";
 if(!/^[+\d][\d\s-]{6,}$/.test(ad.phone))return "Please enter a valid contact phone number.";
 return "";
}
function adStatusClass(s){return {Active:"verified",Scheduled:"matched",Expired:"pending",Stopped:"stopped"}[s]||"pending"}
function renderAds(){
 if(!ADS)return;
 const mine=myAds(),filter=$("adStatusFilter")?.value||"all";
 const withStatus=mine.map(a=>({...a,_s:ADS.status(a)}));
 $("adActive").textContent=withStatus.filter(a=>a._s==="Active").length;
 $("adScheduled").textContent=withStatus.filter(a=>a._s==="Scheduled").length;
 $("adViews").textContent=mine.reduce((s,a)=>s+(Number(a.views)||0),0).toLocaleString("en-US");
 $("adSpent").innerHTML=`${Math.round(mine.reduce((s,a)=>s+(Number(a.paid)||0),0)).toLocaleString("en-US")} <small>${ADS.CURRENCY}</small>`;
 $("navAds").textContent=withStatus.filter(a=>a._s==="Active").length;
 const rows=withStatus.filter(a=>filter==="all"||a._s===filter);
 $("adTableCount").textContent=mine.length;
 $("adTableBody").innerHTML=rows.length?rows.map(a=>`<tr>
  <td><span class="request-id">${ADS.esc(a.id)}</span></td>
  <td><b>${ADS.esc(a.title)}</b><small class="cell-subtext">${ADS.esc(a.department)} · event ${ADS.formatDate(a.eventDate)}</small></td>
  <td><span class="plan-chip plan-${ADS.esc(a.plan)}">${ADS.esc(ADS.PLANS[a.plan]?.name||a.plan)}</span></td>
  <td>${ADS.formatDate(a.startDate,{day:"2-digit",month:"short"})} → ${ADS.formatDate(a.endDate,{day:"2-digit",month:"short"})}</td>
  <td><span class="status-pill ${adStatusClass(a._s)}">${a._s}</span></td>
  <td>${(Number(a.views)||0).toLocaleString("en-US")}</td>
  <td>${ADS.money(a.paid||0)}</td>
  <td><div class="record-actions"><a class="record-action print" href="/Home/Ads#${encodeURIComponent(a.id)}" target="_blank" rel="noopener">View</a>${a._s==="Active"||a._s==="Scheduled"?`<button class="record-action stop" type="button" data-stop-ad="${ADS.esc(a.id)}">Stop</button>`:""}</div></td>
 </tr>`).join(""):'<tr><td colspan="8" class="empty-records">No advertisements yet. Create your first one above.</td></tr>';
}

/* ---------- Checkout ---------- */
const dlg=$("checkoutDialog");
function showStep(step){dlg.querySelectorAll(".checkout-step").forEach(s=>s.hidden=s.dataset.step!==step)}
// Visa payment with Stripe (stripe-pay.js). The card field is created the first time the dialog opens.
let adCard=null;        // the Stripe card field
let unpaidAdId=null;    // the ad created on the server and waiting for payment (reused if the card fails)
function openCheckout(){
 const ad=readAdForm(),err=validateAd(ad),box=$("adFormError");
 box.hidden=!err;box.textContent=err;
 if(err){$("adForm").reportValidity?.();box.scrollIntoView({behavior:"smooth",block:"center"});return}
 pendingAd=ad;unpaidAdId=null;
 const f=ADS.fees(selectedPlan),p=ADS.PLANS[selectedPlan];
 $("coTitle").textContent=ad.title;
 $("coMeta").textContent=`${p.name} plan · ${p.days} days · live from ${ADS.formatDate(ad.startDate)}`;
 $("coTotal").textContent=ADS.money(f.total);$("payAmount").textContent=ADS.money(f.total);
 $("payError").hidden=true;$("adTerms").checked=false;
 showStep("pay");
 if(typeof dlg.showModal==="function")dlg.showModal();else dlg.setAttribute("open","");
 if(adCard)adCard.clear();
 else StripePay.mount($("adCard"),$("payError")).then(c=>{adCard=c}).catch(e=>{$("payError").hidden=false;$("payError").textContent=e.message;$("payButton").disabled=true});
}
function closeCheckout(){if(dlg.open)dlg.close();else dlg.removeAttribute("open")}
async function pay(){
 const box=$("payError");
 const fail=msg=>{showStep("pay");box.hidden=false;box.textContent=msg};
 if(!adCard)return fail("The payment form is not ready yet.");
 if(!$("adTerms").checked)return fail("Please confirm the ad details and fee terms.");
 box.hidden=true;
 showStep("processing");
 try{
  // 1. Save the ad (hidden until paid). If a card failed before, pay the same ad again.
  if(!unpaidAdId)unpaidAdId=(await Api.post("/api/hospital/ads",pendingAd)).id;
  // 2. Pay with Visa — the server calculates the amount.
  const result=await adCard.pay(`/api/payments/ad/${unpaidAdId}`);
  // 3. Reload my ads to show the paid one.
  ads=await ADS.fetchMine();
  const ad=ads.find(a=>a.id===unpaidAdId);unpaidAdId=null;adCard.clear();
  const live=ADS.status(ad)==="Active";
  $("successText").textContent=live?`"${ad.title}" is now live on the public ads page until ${ADS.formatDate(ad.endDate)}.`:`"${ad.title}" is scheduled to go live on ${ADS.formatDate(ad.startDate)}.`;
  $("rcptId").textContent=ad.receipt;$("rcptAmount").textContent=ADS.money(ad.paid);
  $("rcptMethod").textContent=`Visa •••• ${result.last4}`;
  $("viewPublicAd").href=`/Home/Ads#${encodeURIComponent(ad.id)}`;
  showStep("success");
  $("adForm").reset();resetAdFormDefaults();updateAdPreview();renderAds();bloodBurst("#13895f");
  toast(`Payment received — ${ad.id} ${live?"is live":"is scheduled"}.`);
 }catch(err){fail(err.message)}
}
function resetAdFormDefaults(){
 const t=ADS.today();
 $("adStartDate").value=t;$("adStartDate").min=t;$("adEventDate").min=t;
 if(!$("adEventDate").value)$("adEventDate").value=ADS.addDays(t,7);
 if(!$("adPhone").value)$("adPhone").value=$("hospitalPhone")?.value||"";
}
function initAds(){
 if(!ADS||!$("adForm"))return;
 $("adDepartment").innerHTML='<option value="">Select department</option>'+ADS.DEPARTMENTS.map(d=>`<option${d==="Orthopedics"?" selected":""}>${d}</option>`).join("");
 $("adCategory").innerHTML='<option value="">Select ad type</option>'+Object.keys(ADS.CATEGORIES).map(c=>`<option${c==="Free consultation day"?" selected":""}>${c}</option>`).join("");
 resetAdFormDefaults();renderPlans();updateAdPreview();renderAds();
 $("planGrid").addEventListener("change",e=>{if(e.target.name==="adPlan"){selectedPlan=e.target.value;renderPlans();updateAdPreview()}});
 $("adForm").addEventListener("input",()=>{updateAdPreview();$("adFormError").hidden=true});
 $("adForm").addEventListener("change",updateAdPreview);
 $("adForm").addEventListener("submit",e=>{e.preventDefault();openCheckout()});
 $("proceedPayment").addEventListener("click",openCheckout);
 $("newAdButton").addEventListener("click",()=>{$("adBuilder").scrollIntoView({behavior:"smooth",block:"start"});setTimeout(()=>$("adForm").elements.title.focus({preventScroll:true}),400)});
 $("adStatusFilter").addEventListener("change",renderAds);
 $("adTableBody").addEventListener("click",e=>{
  const b=e.target.closest("[data-stop-ad]");if(!b)return;
  const ad=ads.find(a=>a.id===b.dataset.stopAd);if(!ad)return;
  if(!confirm(`Stop ${ad.id}? It will be removed from the public page. Posting fees are non-refundable.`))return;
  Api.put(`/api/hospital/ads/${encodeURIComponent(ad.id)}/stop`).then(()=>{ad.cancelled=true;renderAds();toast(`${ad.id} stopped and removed from the public page.`)}).catch(err=>toast(err.message));
 });
 // Checkout interactions
 dlg.addEventListener("click",e=>{if(!dlg.querySelector('[data-step="processing"]').hidden)return;if(e.target.closest("[data-close]")||e.target===dlg)closeCheckout()});
 dlg.addEventListener("cancel",e=>{if(!dlg.querySelector('[data-step="processing"]').hidden)e.preventDefault()});
 $("payButton").addEventListener("click",pay);
 // Keep in sync if the public page updates views in another tab.
}
initAds();
/* =====================================================================
   Donation appointments — donors book a visit (/Donor/Appointments),
   the hospital reviews the eligibility answers and confirms a date + time.
   Data: GET /api/hospital/appointments, PUT /api/hospital/appointments/{id}
   ===================================================================== */
const APPT_CAPACITY=3,APPT_OPEN="08:00",APPT_CLOSE="16:00";
let appts=[];
let apptFilter="All",apptCurrent=null,apptAction=null;
const apptPad=n=>String(n).padStart(2,"0");
const apptYmd=d=>`${d.getFullYear()}-${apptPad(d.getMonth()+1)}-${apptPad(d.getDate())}`;
const apptToday=()=>apptYmd(new Date());
const apptAdd=(s,n)=>{const d=new Date(s+"T12:00:00");d.setDate(d.getDate()+n);return apptYmd(d)};
const apptIsFri=s=>new Date(s+"T12:00:00").getDay()===5;
const apptDate=(s,long)=>{if(!s)return "—";const d=new Date(s+"T12:00:00");return Number.isNaN(d.getTime())?esc(s):d.toLocaleDateString("en-GB",long?{weekday:"long",day:"numeric",month:"long",year:"numeric"}:{weekday:"short",day:"numeric",month:"short",year:"numeric"})};
const apptTime=t=>{if(!/^\d{2}:\d{2}$/.test(t||""))return "—";const [h,m]=t.split(":").map(Number);return `${h%12||12}:${apptPad(m)} ${h<12?"AM":"PM"}`};
const myAppts=()=>appts;   // the server returns only this hospital's appointments
const apptPendingCount=()=>myAppts().filter(a=>a.status==="Pending").length;
async function loadAppointments(){try{appts=await Api.get("/api/hospital/appointments")}catch(err){toast(err.message)}renderAppointments()}

function apptStatusPill(s){const m={Pending:["pending","Pending"],Approved:["verified","Confirmed"],Completed:["matched","Completed"],Declined:["declined","Declined"],Cancelled:["stopped","Cancelled"]}[s]||["pending",s];return `<span class="status-pill ${m[0]}">${m[1]}</span>`}
function apptEligPill(e){const s=e&&e.state;return s==="ok"?'<span class="status-pill fulfilled">Eligible</span>':s==="review"?'<span class="status-pill pending">Needs review</span>':'<span class="status-pill stopped">—</span>'}
const apptGroup=a=>a.status==="Declined"||a.status==="Cancelled"?"Closed":a.status;
function renderAppointments(){
 const list=myAppts(),t=apptToday();
 const set=(id,v)=>{const el=$(id);if(el)el.textContent=v};
 const pending=list.filter(a=>a.status==="Pending");
 set("apPending",pending.length);
 set("apUpcoming",list.filter(a=>a.status==="Approved"&&a.confirmedDate>=t).length);
 set("apToday",list.filter(a=>a.status==="Approved"&&a.confirmedDate===t).length);
 set("apReview",pending.filter(a=>a.eligibility&&a.eligibility.state==="review").length);
 set("navAppointments",pending.length);
 document.querySelectorAll("[data-ap-count]").forEach(el=>{const k=el.dataset.apCount;el.textContent=k==="All"?list.length:list.filter(a=>apptGroup(a)===k).length});
 const q=($("apSearch")?.value||"").trim().toLowerCase();
 const rows=list.filter(a=>(apptFilter==="All"||apptGroup(a)===apptFilter)&&(!q||[a.id,a.donor?.name,a.donor?.phone,a.donor?.email,a.donor?.type].some(v=>String(v||"").toLowerCase().includes(q))))
  .sort((a,b)=>{const pa=a.status==="Pending"?0:1,pb=b.status==="Pending"?0:1;if(pa!==pb)return pa-pb;return pa===0?(a.requestedDate+a.requestedTime).localeCompare(b.requestedDate+b.requestedTime):String(b.updatedAt||"").localeCompare(String(a.updatedAt||""))});
 set("apCount",rows.length);
 $("apptBody").innerHTML=rows.length?rows.map(a=>{
  const when=a.status==="Approved"?`<b>${apptDate(a.confirmedDate)}</b><small class="cell-subtext">Confirmed · ${apptTime(a.confirmedTime)}</small>`:`<b>${apptDate(a.requestedDate)}</b><small class="cell-subtext">Requested · ${apptTime(a.requestedTime)}</small>`;
  const label=a.status==="Pending"?"Review":a.status==="Approved"?"Manage":"View";
  return `<tr><td><span class="request-id">${esc(a.id)}</span></td><td><b>${esc(a.donor?.name)}</b><small class="cell-subtext">${esc(a.donor?.phone)}</small></td><td><span class="blood-type">${esc(a.donor?.type)}</span></td><td>${when}</td><td>${apptEligPill(a.eligibility)}</td><td>${apptStatusPill(a.status)}</td><td><div class="record-actions"><button class="record-action ${a.status==="Pending"?"review":"print"}" data-appt-open="${esc(a.id)}">${label}</button></div></td></tr>`}).join(""):'<tr><td colspan="7" class="empty-records">No appointment requests here yet.</td></tr>';
}

/* ---------- Review dialog ---------- */
const apptDlg=$("apptDialog");
function apptTimeOptions(selected){
 const [oh,om]=APPT_OPEN.split(":").map(Number),[ch,cm]=APPT_CLOSE.split(":").map(Number);const out=[];
 for(let m=oh*60+om;m<=ch*60+cm-30;m+=30){const t=`${apptPad(Math.floor(m/60))}:${apptPad(m%60)}`;out.push(t)}
 if(selected&&!out.includes(selected)){out.push(selected);out.sort()}
 return out.map(t=>`<option value="${t}"${t===selected?" selected":""}>${apptTime(t)}</option>`).join("");
}
function openAppointment(id){
 apptCurrent=id;apptAction=null;renderApptDialog();
 if(typeof apptDlg.showModal==="function")apptDlg.showModal();else apptDlg.setAttribute("open","");
}
function closeAppointment(){if(apptDlg.open)apptDlg.close();else apptDlg.removeAttribute("open");apptCurrent=null;apptAction=null}
function renderApptDialog(){
 const a=appts.find(x=>x.id===apptCurrent);if(!a)return;
 const e=a.eligibility||{},d=a.donor||{};
 const row=(l,v,warn)=>`<li class="${warn?"warn":""}"><span>${l}</span><b>${v}</b></li>`;
 const yn=(v,detail)=>v==="yes"?`Yes${detail?` — ${esc(detail)}`:""}`:"No";
 const answers=[
  row("Age",`${Number(e.age)||"—"} years`),row("Weight",`${Number(e.weight)||"—"} kg`),
  row("Last donation",e.lastDonation?apptDate(e.lastDonation):"Never donated"),
  row("Feeling well today",e.well==="no"?"No":"Yes",e.well==="no"),
  row("Taking medication",yn(e.meds,e.medsDetail),e.meds==="yes"),
  row("Travelled abroad (3 months)",yn(e.travel,e.travelDetail),e.travel==="yes"),
  row("Tattoo, surgery or dental (6 months)",yn(e.procedure),e.procedure==="yes")].join("");
 const verdict=e.state==="review"?`<div class="ap-verdict review">! Flagged answers — check these before approving.</div>`:`<div class="ap-verdict ok">✓ All eligibility answers look fine.</div>`;
 const editable=a.status==="Pending"||a.status==="Approved";
 const t=apptToday();
 let decide="",foot="";
 if(editable){
  const cd=a.confirmedDate||(a.requestedDate<t?t:a.requestedDate),ct=a.confirmedTime||a.requestedTime;
  decide=`<div><h3 class="ap-sec-title">${a.status==="Pending"?"CONFIRM A DATE AND TIME":"CONFIRMED APPOINTMENT"}</h3>
   <div class="ap-decide">
    <label>Date<input type="date" id="apptConfDate" min="${t}" value="${esc(cd)}"></label>
    <label>Time<select id="apptConfTime">${apptTimeOptions(ct)}</select></label>
    <p class="ap-cap" id="apptCap"></p>
    <label class="wide">Message to the donor <small class="field-hint">optional</small><textarea id="apptNote" rows="2" maxlength="160" placeholder="e.g. Please bring your ID and arrive 10 minutes early.">${esc(a.hospitalNote||"")}</textarea></label>
    ${a.status==="Approved"?`<label class="check-label ap-stock"><input type="checkbox" id="apptStock" checked> When marked completed: add 1 unit to the ${esc(d.type)} stock and create a donation record</label>`:""}
    ${apptAction?`<label class="wide ap-reason">${apptAction==="decline"?"Reason for declining":"Reason for cancelling"} <small class="field-hint">shown to the donor</small><textarea id="apptReason" rows="2" maxlength="160" placeholder="${apptAction==="decline"?"e.g. Please wait 2 weeks after finishing antibiotics.":"e.g. We are closed that day due to maintenance."}"></textarea></label>`:""}
   </div></div>`;
  if(apptAction)foot=`<button class="secondary-button" type="button" data-appt-act="back">Back</button><button class="primary-button" type="button" data-appt-act="${apptAction}-go">${apptAction==="decline"?"Confirm decline":"Confirm cancellation"}</button>`;
  else if(a.status==="Pending")foot=`<button class="secondary-button danger" type="button" data-appt-act="decline">Decline…</button><button class="primary-button" type="button" data-appt-act="approve">Approve &amp; notify donor <span>→</span></button>`;
  else foot=`<button class="secondary-button danger" type="button" data-appt-act="cancel">Cancel appointment…</button><button class="secondary-button" type="button" data-appt-act="complete">Mark completed</button><button class="primary-button" type="button" data-appt-act="approve">Update time &amp; notify <span>→</span></button>`;
 }else{
  const msg=a.status==="Declined"?`Declined. Reason sent to the donor: <b>${esc(a.declineReason||"—")}</b>`:a.status==="Cancelled"?`Cancelled${a.cancelledBy==="donor"?" by the donor":" by the hospital"}.${a.declineReason?` Reason: <b>${esc(a.declineReason)}</b>`:""}`:`Completed. Donation took place on <b>${apptDate(a.confirmedDate||a.requestedDate)}</b> at <b>${apptTime(a.confirmedTime)}</b>.`;
  decide=`<div class="ap-summary-done">${msg}</div>`;
  foot=`<button class="primary-button" type="button" data-appt-act="close">Close</button>`;
 }
 $("apptDialogBody").innerHTML=`<header class="checkout-head"><div><span class="section-kicker">DONATION APPOINTMENT · ${esc(a.id)}</span><h2 id="apptTitle">${esc(d.name)}</h2></div><button class="checkout-close" type="button" data-appt-act="close" aria-label="Close">×</button></header>
  <div class="ap-donor-grid"><div><small>PHONE</small><b><a href="tel:${esc(d.phone)}">${esc(d.phone)}</a></b></div><div><small>BLOOD TYPE</small><b>${esc(d.type)}</b></div><div><small>EMAIL</small><b>${d.email?`<a href="mailto:${esc(d.email)}">${esc(d.email)}</a>`:"—"}</b></div><div><small>STATUS</small><b>${apptStatusPill(a.status)}</b></div></div>
  <div class="ap-requested"><span>Requested <b>${apptDate(a.requestedDate,true)} · ${apptTime(a.requestedTime)}</b></span>${a.notes?`<p class="ap-notes" style="flex-basis:100%;margin:0">“${esc(a.notes)}”</p>`:""}</div>
  <div><h3 class="ap-sec-title">ELIGIBILITY ANSWERS</h3><ul class="ap-answers">${answers}</ul><div style="margin-top:10px">${verdict}</div></div>
  ${decide}<div class="ap-dialog-actions">${foot}</div>`;
 updateApptCapacity();
}
function updateApptCapacity(){
 const el=$("apptCap");if(!el)return;
 const d=$("apptConfDate")?.value,t=$("apptConfTime")?.value;if(!d||!t){el.textContent="";return}
 const n=myAppts().filter(x=>x.id!==apptCurrent&&x.status==="Approved"&&x.confirmedDate===d&&x.confirmedTime===t).length;
 el.className="ap-cap"+(n>=APPT_CAPACITY?" warn":"");
 el.textContent=n===0?"No other donors are booked at this time.":n>=APPT_CAPACITY?`This slot is full (${n} donors already confirmed). Consider another time.`:`${n} other donor${n===1?" is":"s are"} already confirmed at this time.`;
}
// Sends the hospital's decision to the server: PUT /api/hospital/appointments/{id}
async function sendDecision(a,body){
 try{const saved=await Api.put(`/api/hospital/appointments/${encodeURIComponent(a.id)}`,body);Object.assign(a,saved);return true}
 catch(err){toast(err.message);return false}
}
async function apptDecide(act){
 const a=appts.find(x=>x.id===apptCurrent);if(!a)return;
 const t=apptToday();
 if(act==="approve"){
  const d=$("apptConfDate").value,tm=$("apptConfTime").value;
  if(!d||!tm){toast("Please choose a date and time.");return}
  if(d<t){toast("The date can't be in the past.");return}
  if(apptIsFri(d)){toast("Fridays are closed. Please choose another day.");return}
  const wasPending=a.status==="Pending";
  if(!await sendDecision(a,{action:"approve",date:d,time:tm,note:$("apptNote").value.trim()}))return;
  renderAppointments();closeAppointment();bloodBurst("#13895f");
  toast(`${wasPending?"Approved":"Updated"}. ${a.donor.name} will see ${apptDate(d)} at ${apptTime(tm)}.`);return;
 }
 if(act==="decline"||act==="cancel"){apptAction=act;renderApptDialog();$("apptReason")?.focus();return}
 if(act==="back"){apptAction=null;renderApptDialog();return}
 if(act==="decline-go"||act==="cancel-go"){
  const reason=($("apptReason")?.value||"").trim();
  if(reason.length<3){toast("Please write a short reason for the donor.");$("apptReason")?.focus();return}
  const declining=act==="decline-go";
  if(!await sendDecision(a,{action:declining?"decline":"cancel",reason}))return;
  renderAppointments();closeAppointment();toast(declining?`Request ${a.id} declined. The donor was notified.`:`Appointment ${a.id} cancelled. The donor was notified.`);return;
 }
 if(act==="complete"){
  const addStock=!!$("apptStock")?.checked;
  if(!await sendDecision(a,{action:"complete",addStock}))return;
  // The server added a donation record (and maybe 1 unit of stock) — reload them.
  loadDashboard();
  renderAppointments();closeAppointment();bloodBurst("#13895f");toast(`${a.donor.name}'s donation was completed${addStock?" and added to stock":""}.`);return;
 }
 if(act==="close")closeAppointment();
}
function initAppointments(){
 if(!$("apptBody"))return;
 $("apptBody").addEventListener("click",e=>{const b=e.target.closest("[data-appt-open]");if(b)openAppointment(b.dataset.apptOpen)});
 document.querySelectorAll("[data-ap-filter]").forEach(b=>b.addEventListener("click",()=>{apptFilter=b.dataset.apFilter;document.querySelectorAll("[data-ap-filter]").forEach(x=>x.classList.toggle("active",x===b));renderAppointments()}));
 $("apSearch")?.addEventListener("input",renderAppointments);
 apptDlg.addEventListener("click",e=>{const b=e.target.closest("[data-appt-act]");if(b){apptDecide(b.dataset.apptAct);return}if(e.target===apptDlg)closeAppointment()});
 apptDlg.addEventListener("change",e=>{if(e.target.id==="apptConfDate"||e.target.id==="apptConfTime")updateApptCapacity()});
 apptDlg.addEventListener("close",()=>{apptCurrent=null;apptAction=null});
 renderAppointments();
 }
initAppointments();

/* =====================================================================
   Start — load the real data only when BloodLink has approved this hospital.
   Before that, hospital-onboarding.js shows the verification window.
   ===================================================================== */
function startDashboard(){
 loadDashboard();
 loadAppointments();
 if(ADS)ADS.fetchMine().then(list=>{ads=list;renderAds()});
}
HospitalOnboarding.start(startDashboard);

// Open a section directly from the URL (e.g. /Hospital#ads from the public page).
(function(){const h=location.hash.slice(1);if(h&&document.querySelector(`.nav-item[data-section="${CSS.escape(h)}"]`))navigate(h)})();