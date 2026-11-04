"use strict";
const $ = id => document.getElementById(id);
const ui = Object.fromEntries(["clip","play","prev","next","frame","time","view","speed","species","control","stats","guide","surface"].map(id => [id,$(id)]));
let playing = false;
let elapsed = 0;
let lastTime;
const add = (a,b) => a.map((v,i)=>v+b[i]);
const sub = (a,b) => a.map((v,i)=>v-b[i]);
function hull(points) {
  const sorted = points.map(p=>[p[0],p[1]]).sort((a,b)=>a[0]-b[0] || a[1]-b[1]);
  const cross = (a,b,c)=>(b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0]);
  const build = pts => {const out=[]; for (const p of pts) {while(out.length>1 && cross(out.at(-2),out.at(-1),p)<=0)out.pop();out.push(p);}return out;};
  return build(sorted).slice(0,-1).concat(build([...sorted].reverse()).slice(0,-1));
}
function draw(canvas, pose, mode) {
  const ctx=canvas.getContext("2d"), size=576, scale=size/2.8;
  const kind=ui.view.value;
  const project = p => {
    const q=sub(p,pose.root);
    if(kind==="side")return [size/2+q[1]*scale,size*.78-p[2]*scale];
    if(kind==="front")return [size/2+q[0]*scale,size*.78-p[2]*scale];
    return [size/2+q[0]*scale,size/2-q[1]*scale];
  };
  const path=(points,color,width=1,fill=false)=>{
    if(!points.length)return;
    ctx.beginPath();ctx.moveTo(...project(points[0]));for(const p of points.slice(1))ctx.lineTo(...project(p));
    ctx.strokeStyle=color;ctx.lineWidth=width;ctx.lineJoin="round";ctx.lineCap="round";
    if(fill){ctx.closePath();ctx.fillStyle=color;ctx.fill();}else ctx.stroke();
  };
  const dot=(p,r,color,fill=true)=>{const [x,y]=project(p);ctx.beginPath();ctx.arc(x,y,r,0,Math.PI*2);ctx.fillStyle=color;ctx.strokeStyle=color;ctx.lineWidth=2;if(fill)ctx.fill();else ctx.stroke();};
  ctx.clearRect(0,0,size,size);ctx.fillStyle="#17252c";ctx.fillRect(0,0,size,size);
  for(let i=-1.4;i<=1.401;i+=.2){
    const rootGround=[pose.root[0],pose.root[1],0];
    path([add(rootGround,[i,-1.4,0]),add(rootGround,[i,1.4,0])],"#29404a");
    path([add(rootGround,[-1.4,i,0]),add(rootGround,[1.4,i,0])],"#29404a");
  }
  const supports=pose.limbs.filter(l=>l.planted).map(l=>l.foot);
  if(kind==="top")path(hull(supports).map(p=>[...p,0]),"#28463f",1,true);
  for(const l of pose.limbs){
    const chain=[l.hip,l.knee,l.foot];
    path(chain,mode==="D"?"#83b8ce":"#b99aaa",mode==="D"?4:22);
    if(mode==="C"){
      for(const p of chain.slice(0,2))dot(p,11,"#b99aaa");
      path(chain,"#efd6df",2);
      for(let j=0;j<2;j++){
        const a=project(chain[j]),b=project(chain[j+1]);const dx=b[0]-a[0],dy=b[1]-a[1],len=Math.hypot(dx,dy)||1;
        for(const t of [.25,.5,.75]){const x=a[0]+dx*t,y=a[1]+dy*t;ctx.beginPath();ctx.moveTo(x-dy/len*10,y+dx/len*10);ctx.lineTo(x+dy/len*10,y-dx/len*10);ctx.strokeStyle="#655c6d";ctx.lineWidth=1;ctx.stroke();}
      }
    } else for(const p of chain)dot(p,4,"#e6d6a7");
    dot(l.foot,7,l.planted?"#80d7a5":"#ecb26d",false);
    const [x,y]=project(l.foot);ctx.font="14px system-ui";ctx.fillStyle=l.planted?"#80d7a5":"#ecb26d";ctx.fillText(l.id,x+10,y-8);
  }
  const c=Math.cos(pose.yaw),s=Math.sin(pose.yaw);
  const body=[[-.29,-.23,.13],[.29,-.23,.13],[0,.30,.13]].map(p=>add(pose.root,[p[0]*c-p[1]*s,p[0]*s+p[1]*c,p[2]]));
  path(body,mode==="D"?"#a8c1cb":"#c7aab9",1,true);
  if(kind==="top"){
    const eye=add(pose.root,[0,0,.14]);dot(eye,20+pose.mouth*9,"#17252c");
    const ground=[pose.root[0],pose.root[1],0];path([add(ground,[-.035,0,0]),add(ground,[.035,0,0])],"#ffe49d",2);path([add(ground,[0,-.035,0]),add(ground,[0,.035,0])],"#ffe49d",2);
  }
  ctx.fillStyle="#f1c48d";ctx.font="17px system-ui";ctx.fillText("СХЕМА МЕТОДА · НЕ НОВЫЙ SPRITE",16,550);
  ctx.fillStyle="#b6c8d0";ctx.font="15px system-ui";ctx.fillText(kind==="top"?"Строго сверху · XY":"Проектная высота Z · без перспективы",16,28);
}
function render(){
  const index=Number(ui.frame.value),pose=window.RESEARCH_POSES[ui.clip.value][index];
  draw(ui.guide,pose,"D");draw(ui.surface,pose,"C");
  ui.time.value=`${index}/48 · ${(index/48).toFixed(2)} c`;
  ui.stats.textContent=`Одна и та же поза для D и C. Опор: ${pose.limbs.filter(l=>l.planted).length}/6. Root Y=${pose.root[1].toFixed(3)}; yaw=${(pose.yaw*180/Math.PI).toFixed(1)}°. Проверка: только кинематика схемы, без skin/art/Unity PASS.`;
}
function stop(){playing=false;ui.play.textContent="Играть";}
ui.play.onclick=()=>{playing=!playing;if(playing){if(Number(ui.frame.value)===48)ui.frame.value=0;elapsed=Number(ui.frame.value)/48;}ui.play.textContent=playing?"Пауза":"Играть";};
for(const [button,delta] of [[ui.prev,-1],[ui.next,1]])button.onclick=()=>{stop();ui.frame.value=Math.max(0,Math.min(48,Number(ui.frame.value)+delta));render();};
ui.frame.oninput=()=>{stop();render();};ui.clip.onchange=()=>{stop();ui.frame.value=0;render();};ui.view.onchange=render;
ui.species.onchange=()=>{ui.control.src=`control-${ui.species.value}.png`;ui.control.alt=`Неизменённый целый Walk00 · ${ui.species.value}`;};
function tick(now){
  if(lastTime!==undefined && playing){
    elapsed+=Math.min((now-lastTime)/1000,.1)*Number(ui.speed.value);
    if(elapsed>=1){if(ui.clip.value==="walk")elapsed%=1;else{elapsed=1;stop();}}
    ui.frame.value=Math.round(elapsed*48);render();
  }
  lastTime=now;requestAnimationFrame(tick);
}
render();requestAnimationFrame(tick);
