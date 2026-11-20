/* =====================================================================
   BloodLink — Report an issue (user page)
   - Live "specimen ticket" preview of the report
   - Inline validation, attachments, confirmation
   Sends the report to the server (POST /api/reports — Controllers/Api/ReportsController.cs),
   so it appears in Admin → Reports as "New".
   ===================================================================== */
const $=id=>document.getElementById(id);
const MIN_BODY=20,MAX_FILES=5;
const PRIORITY_LABEL={low:"Low priority",medium:"Medium priority",high:"High priority"};


const form=$("reportForm"),ticket=$("ticket"),page=$("page"),done=$("done");
let files=[];

/* ---------- Reference number + barcode ---------- */
// The real number is given by the server when the report is sent.
function nextId(){
  return "RP-NEW";
}
// Decorative barcode derived from the reference, so each ticket looks unique.
function drawCode(id){
  const H=40;let x=0,bars="";
  const add=(w,dark)=>{if(dark)bars+=`<rect x="${x}" y="0" width="${w}" height="${H}"/>`;x+=w};
  [1,1,1,2].forEach((w,i)=>add(w,i%2===0));
  for(const ch of id){
    const c=ch.charCodeAt(0);
    for(let i=0;i<5;i++)add(1+((c>>i)&1)+(i===4?1:0),i%2===0);
    add(2,false);
  }
  [2,1,1,1].forEach((w,i)=>add(w,i%2===0));
  const svg=$("tkCode");
  svg.setAttribute("viewBox",`0 0 ${x} ${H}`);
  svg.innerHTML=bars;
}

/* ---------- Form data + live ticket ---------- */
function readForm(){
  const fd=new FormData(form),get=k=>String(fd.get(k)||"").trim();
  return{
    from:get("from"),role:get("role"),email:get("email"),
    category:get("category"),related:get("related"),
    title:get("title"),body:get("body"),
    priority:get("priority")||"medium",
    attachments:files.map(f=>f.name)
  };
}
function updateTicket(){
  const d=readForm();
  $("tkFrom").textContent=[d.from,d.role].filter(Boolean).join(", ");
  $("tkCat").textContent=d.category;
  $("tkAbout").textContent=d.related;
  $("tkTitle").textContent=d.title;
  $("tkText").textContent=d.body;
  $("tkPrio").textContent=PRIORITY_LABEL[d.priority];
  ticket.dataset.prio=d.priority;
  $("descCount").textContent=d.body.length;
}

/* ---------- Validation ---------- */
const RULES={
  from:d=>d.from?"":"Enter your name.",
  email:d=>!d.email?"Enter your email so we can reply.":/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(d.email)?"":"Enter a valid email, like name@example.com.",
  role:d=>d.role?"":"Choose who you are.",
  category:d=>d.category?"":"Pick the closest category.",
  related:d=>d.related?"":"Say what this report is about.",
  title:d=>d.title?"":"Add a short title.",
  body:d=>d.body.length>=MIN_BODY?"":`Describe what happened in at least ${MIN_BODY} characters.`
};
function setError(name,msg){
  const holder=form.querySelector(`[data-field="${name}"]`),out=$("err-"+name);
  if(!holder||!out)return;
  out.textContent=msg;out.hidden=!msg;
  holder.classList.toggle("has-error",!!msg);
  holder.querySelectorAll(".inp").forEach(el=>el.setAttribute("aria-invalid",msg?"true":"false"));
}
function validateAll(){
  const d=readForm();let first=null;
  for(const name of Object.keys(RULES)){
    const msg=RULES[name](d);
    setError(name,msg);
    if(msg&&!first)first=name;
  }
  $("formError").textContent="Check the highlighted fields and try again.";
  $("formError").hidden=!first;
  return first;
}

/* ---------- Attachments ---------- */
function renderFiles(){
  const list=$("fileList");list.textContent="";
  files.forEach((f,i)=>{
    const li=document.createElement("li"),name=document.createElement("span"),rm=document.createElement("button");
    li.className="chip";name.textContent=f.name;name.title=f.name;
    rm.type="button";rm.textContent="×";rm.setAttribute("aria-label",`Remove ${f.name}`);
    rm.addEventListener("click",()=>{files.splice(i,1);renderFiles();$("fileMsg").textContent=""});
    li.append(name,rm);list.append(li);
  });
}
function addFiles(incoming){
  const msg=$("fileMsg");msg.textContent="";
  for(const f of incoming){
    if(files.length>=MAX_FILES){msg.textContent=`You can attach up to ${MAX_FILES} files.`;break}
    if(!files.some(x=>x.name===f.name&&x.size===f.size))files.push(f);
  }
  renderFiles();
}
$("fileInput").addEventListener("change",e=>{addFiles(e.target.files);e.target.value=""});
const drop=$("dropzone");
["dragenter","dragover"].forEach(ev=>drop.addEventListener(ev,e=>{e.preventDefault();drop.classList.add("over")}));
["dragleave","drop"].forEach(ev=>drop.addEventListener(ev,e=>{e.preventDefault();drop.classList.remove("over")}));
drop.addEventListener("drop",e=>addFiles(e.dataTransfer.files));

/* ---------- Saving ---------- */
// POST /api/reports — returns the saved report (with its id).
async function saveReport(d){
  return await Api.post("/api/reports",{...d,attachments:d.attachments.slice(0,MAX_FILES)});
}

/* ---------- Events ---------- */
form.addEventListener("input",e=>{
  const holder=e.target.closest("[data-field]");
  if(holder&&holder.classList.contains("has-error"))setError(holder.dataset.field,"");
  updateTicket();
});
form.addEventListener("change",e=>{
  const holder=e.target.closest("[data-field]");
  if(holder&&holder.classList.contains("has-error"))setError(holder.dataset.field,"");
  updateTicket();
});

form.addEventListener("submit",async e=>{
  e.preventDefault();
  const firstBad=validateAll();
  if(firstBad){
    const target=form.querySelector(`[data-field="${firstBad}"]`);
    target.scrollIntoView({behavior:"smooth",block:"center"});
    target.querySelector("input,textarea")?.focus({preventScroll:true});
    return;
  }
  let rec;
  try{rec=await saveReport(readForm())}
  catch(err){
    $("formError").textContent=err.message;
    $("formError").hidden=false;
    return;
  }
  showDone({...rec,id:"RP-"+rec.id});
});

function showDone(rec){
  $("doneId").textContent=rec.id;
  $("tkId").textContent=rec.id;drawCode(rec.id);
  $("tkStatus").textContent="Received "+new Date().toLocaleString("en-GB",{day:"numeric",month:"short",hour:"2-digit",minute:"2-digit"});
  ticket.classList.add("sent");page.classList.add("is-sent");
  form.hidden=true;done.hidden=false;
  window.scrollTo({top:0,behavior:"smooth"});
  done.focus({preventScroll:true});
}

$("copyId").addEventListener("click",async()=>{
  const label=$("copyId").firstElementChild,id=$("doneId").textContent;
  try{await navigator.clipboard.writeText(id);label.textContent="Copied"}
  catch{
    const r=document.createRange();r.selectNodeContents($("doneId"));
    const s=getSelection();s.removeAllRanges();s.addRange(r);label.textContent="Press Ctrl+C to copy";
  }
  setTimeout(()=>{label.textContent="Copy reference"},2000);
});

$("again").addEventListener("click",()=>{
  form.reset();files=[];renderFiles();$("fileMsg").textContent="";
  Object.keys(RULES).forEach(n=>setError(n,""));
  $("formError").hidden=true;
  $("formError").textContent="Check the highlighted fields and try again.";
  ticket.classList.remove("sent");page.classList.remove("is-sent");
  $("tkStatus").textContent="Not sent yet";
  const id=nextId();$("tkId").textContent=id;drawCode(id);
  updateTicket();
  done.hidden=true;form.hidden=false;
  window.scrollTo({top:0,behavior:"smooth"});
  $("from").focus({preventScroll:true});
});

/* ---------- Boot ---------- */
(function(){const id=nextId();$("tkId").textContent=id;drawCode(id);updateTicket()})();