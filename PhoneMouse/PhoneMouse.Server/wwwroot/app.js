const $ = id => document.getElementById(id);
let socket = null, moveQueue = [], moveTimer = null, reconnectTimer = null;
let connectedOnce = false, manuallyClosed = false;
const pointers = new Map();
let lastTap = 0;

function setStatus(connected) {
  $("status").classList.toggle("connected", connected);
  $("status").querySelector("span").textContent = connected ? "Подключено" : "Нет соединения";
}
function send(data) {
  if (socket && socket.readyState === WebSocket.OPEN) socket.send(JSON.stringify(data));
}
function connect() {
  clearTimeout(reconnectTimer);
  const code = $("code").value.trim();
  if (!code) { $("connectError").textContent = "Введите код с компьютера."; return; }
  const scheme = location.protocol === "https:" ? "wss:" : "ws:";
  socket = new WebSocket(`${scheme}//${location.host}/ws?code=${encodeURIComponent(code)}`);
  $("connectError").textContent = "Подключаемся...";
  socket.onopen = () => {
    localStorage.setItem("phoneMouseCode", code);
    connectedOnce = true;
    $("connectPanel").classList.add("hidden");
    $("controlPanel").classList.remove("hidden");
    $("connectError").textContent = "";
    setStatus(true);
  };
  socket.onclose = () => {
    setStatus(false);
    if (manuallyClosed) {
      connectedOnce = false;
      $("controlPanel").classList.add("hidden");
      $("connectPanel").classList.remove("hidden");
      manuallyClosed = false;
      return;
    }
    if (connectedOnce) {
      $("connectError").textContent = "Связь потеряна. Повторное подключение...";
      reconnectTimer = setTimeout(connect, 1800);
    } else {
      $("controlPanel").classList.add("hidden");
      $("connectPanel").classList.remove("hidden");
      $("connectError").textContent = "Не удалось подключиться. Проверьте Wi-Fi и код.";
    }
  };
  socket.onerror = () => { $("connectError").textContent = "Ошибка соединения. Проверьте адрес компьютера."; };
}
$("connectBtn").addEventListener("click", connect);
$("code").value = localStorage.getItem("phoneMouseCode") || "";

const pad = $("pad");
pad.addEventListener("pointerdown", e => {
  e.preventDefault();
  pad.setPointerCapture(e.pointerId);
  pointers.set(e.pointerId, {x:e.clientX, y:e.clientY});
});
pad.addEventListener("pointermove", e => {
  if (!pointers.has(e.pointerId)) return;
  const previous = pointers.get(e.pointerId);
  const dx = e.clientX - previous.x, dy = e.clientY - previous.y;
  pointers.set(e.pointerId, {x:e.clientX, y:e.clientY});
  if (pointers.size >= 2) {
    send({type:"scroll", delta:Math.round(-dy * 9)});
  } else {
    const sensitivity = Number($("sensitivity")?.value || 1.8);
    moveQueue.push({dx:dx*sensitivity,dy:dy*sensitivity});
    if (!moveTimer) moveTimer = setTimeout(() => {
      const total = moveQueue.reduce((a,m)=>({dx:a.dx+m.dx,dy:a.dy+m.dy}),{dx:0,dy:0});
      send({type:"move",dx:total.dx,dy:total.dy});
      moveQueue=[]; moveTimer=null;
    }, 10);
  }
});
function endPointer(e) { pointers.delete(e.pointerId); }
pad.addEventListener("pointerup", endPointer);
pad.addEventListener("pointercancel", endPointer);

$("leftClick").addEventListener("click",()=>send({type:"click",button:"left"}));
$("rightClick").addEventListener("click",()=>send({type:"click",button:"right"}));
$("scrollUp").addEventListener("click",()=>send({type:"scroll",delta:480}));
$("scrollDown").addEventListener("click",()=>send({type:"scroll",delta:-480}));
$("sendText").addEventListener("click",()=>{
  const value=$("typing").value;
  if(value){
    for(let i=0;i<value.length;i+=1800) send({type:"text",value:value.slice(i,i+1800)});
    $("typing").value="";
  }
});
document.querySelectorAll("[data-key]").forEach(b=>b.addEventListener("click",()=>send({type:"key",value:b.dataset.key})));
document.querySelectorAll("[data-hotkey]").forEach(b=>b.addEventListener("click",()=>send({type:"hotkey",keys:b.dataset.hotkey.split("+")})));
$("disconnectBtn").addEventListener("click",()=>{manuallyClosed=true; connectedOnce=false; clearTimeout(reconnectTimer); if(socket)socket.close();});
if (new URLSearchParams(location.search).has("code")) {
  $("code").value = new URLSearchParams(location.search).get("code");
  connect();
}
if ("serviceWorker" in navigator && location.protocol === "https:") navigator.serviceWorker.register("/sw.js").catch(()=>{});