const $ = id => document.getElementById(id);
let socket = null, moveQueue = [], moveTimer = null, reconnectTimer = null;
let connectedOnce = false, manuallyClosed = false;
const pointers = new Map();
let gesture = null, tapTimer = null, lastTapAt = 0, longPressTimer = null;
let dragging = false, pinchStart = 0, pinchSent = false;

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
function averagePoint() {
  const pts = [...pointers.values()];
  return {x:pts.reduce((s,p)=>s+p.x,0)/pts.length,y:pts.reduce((s,p)=>s+p.y,0)/pts.length};
}
function distance(a,b) { return Math.hypot(a.x-b.x,a.y-b.y); }
function resetGesture() {
  clearTimeout(longPressTimer); gesture=null; pointers.clear(); dragging=false; pinchStart=0; pinchSent=false;
}
pad.addEventListener("contextmenu", e => e.preventDefault());
pad.addEventListener("pointerdown", e => {
  e.preventDefault(); pad.setPointerCapture(e.pointerId);
  pointers.set(e.pointerId,{x:e.clientX,y:e.clientY});
  if (pointers.size===1) {
    gesture={kind:"one",start:{x:e.clientX,y:e.clientY},moved:false,multi:false};
    longPressTimer=setTimeout(()=>{
      if(pointers.size===1 && gesture?.kind==="one" && !gesture.moved){
        send({type:"mouseDown",button:"left"}); dragging=true;
        $("padHint").textContent="Перетаскивание — отпусти палец";
      }
    },480);
  } else if(pointers.size===2 && gesture) {
    clearTimeout(longPressTimer); gesture.kind="two"; gesture.multi=true;
    gesture.startCenter=averagePoint(); gesture.lastCenter=gesture.startCenter;
    const pts=[...pointers.values()]; pinchStart=distance(pts[0],pts[1]); pinchSent=false;
  } else if(pointers.size===3 && gesture) {
    clearTimeout(longPressTimer); gesture.kind="three"; gesture.multi=true;
    gesture.startCenter=averagePoint(); gesture.lastCenter=gesture.startCenter; gesture.moved=false;
  } else if(pointers.size>3 && gesture) { clearTimeout(longPressTimer); gesture.kind="unsupported"; gesture.multi=true; }
});
pad.addEventListener("pointermove", e => {
  if(!pointers.has(e.pointerId)||!gesture)return;
  const previous=pointers.get(e.pointerId), current={x:e.clientX,y:e.clientY}; pointers.set(e.pointerId,current);
  if(gesture.kind==="one"&&pointers.size===1) {
    const dx=current.x-previous.x,dy=current.y-previous.y;
    if(Math.hypot(current.x-gesture.start.x,current.y-gesture.start.y)>7){gesture.moved=true;clearTimeout(longPressTimer);}
    if(gesture.moved){
      const sensitivity=Number($("sensitivity").value||1.8); moveQueue.push({dx:dx*sensitivity,dy:dy*sensitivity});
      if(!moveTimer)moveTimer=setTimeout(()=>{
        const total=moveQueue.reduce((a,m)=>({dx:a.dx+m.dx,dy:a.dy+m.dy}),{dx:0,dy:0});
        send({type:"move",dx:total.dx,dy:total.dy});moveQueue=[];moveTimer=null;
      },8);
    }
  } else if(gesture.kind==="two"&&pointers.size===2) {
    const center=averagePoint(),deltaY=center.y-gesture.lastCenter.y;
    if(Math.abs(deltaY)>1){gesture.moved=true;send({type:"scroll",delta:Math.round(-deltaY*5)});}
    gesture.lastCenter=center;
    const pts=[...pointers.values()],d=distance(pts[0],pts[1]);
    if(!pinchSent&&pinchStart>0&&Math.abs(d-pinchStart)>38){
      send({type:"hotkey",keys:d>pinchStart?["ctrl","+"]:["ctrl","-"]});pinchSent=true;gesture.moved=true;
    }
  } else if(gesture.kind==="three"&&pointers.size===3) {
    const center=averagePoint(),dx=center.x-gesture.startCenter.x,dy=center.y-gesture.startCenter.y;
    if(Math.hypot(dx,dy)>45)gesture.moved=true;
    if(!gesture.actionSent&&Math.abs(dx)>65&&Math.abs(dx)>Math.abs(dy)*1.15){
      send({type:"hotkey",keys:dx<0?["win","tab"]:["alt","tab"]});gesture.actionSent=true;
    }
    gesture.lastCenter=center;
  }
});
function finishPointer(e,cancelled=false) {
  if(!pointers.has(e.pointerId))return;
  pointers.delete(e.pointerId);
  if(dragging){send({type:"mouseUp",button:"left"});dragging=false;$("padHint").textContent="Веди пальцем · тап — клик · два пальца — прокрутка";}
  if(pointers.size!==0)return;
  clearTimeout(longPressTimer);
  if(!cancelled&&gesture){
    if(gesture.kind==="one"&&!gesture.moved&&!dragging){
      const now=Date.now();
      if(now-lastTapAt<280){clearTimeout(tapTimer);tapTimer=null;send({type:"doubleClick",button:"left"});lastTapAt=0;}
      else{lastTapAt=now;tapTimer=setTimeout(()=>{send({type:"click",button:"left"});tapTimer=null;},230);}
    }
    else if(gesture.kind==="two"&&!gesture.moved)send({type:"click",button:"right"});
  }
  resetGesture();
}
pad.addEventListener("pointerup",e=>finishPointer(e,false));
pad.addEventListener("pointercancel",e=>finishPointer(e,true));

$("leftClick").addEventListener("click",()=>send({type:"click",button:"left"}));
$("rightClick").addEventListener("click",()=>send({type:"click",button:"right"}));
$("scrollUp").addEventListener("click",()=>send({type:"scroll",delta:480}));
$("scrollDown").addEventListener("click",()=>send({type:"scroll",delta:-480}));
$("volumeUp").addEventListener("click",()=>send({type:"volume",action:"up"}));
$("volumeDown").addEventListener("click",()=>send({type:"volume",action:"down"}));
$("volumeMute").addEventListener("click",()=>send({type:"volume",action:"mute"}));
$("sendText").addEventListener("click",()=>{
  const value=$("typing").value;
  if(value){for(let i=0;i<value.length;i+=1800)send({type:"text",value:value.slice(i,i+1800)});$("typing").value="";}
});
document.querySelectorAll("[data-key]").forEach(b=>b.addEventListener("click",()=>send({type:"key",value:b.dataset.key})));
document.querySelectorAll("[data-hotkey]").forEach(b=>b.addEventListener("click",()=>send({type:"hotkey",keys:b.dataset.hotkey.split("+")})));
$("disconnectBtn").addEventListener("click",()=>{manuallyClosed=true;connectedOnce=false;clearTimeout(reconnectTimer);if(socket)socket.close();});
if(new URLSearchParams(location.search).has("code")){$("code").value=new URLSearchParams(location.search).get("code");connect();}
if("serviceWorker"in navigator&&location.protocol==="https:")navigator.serviceWorker.register("/sw.js").catch(()=>{});
