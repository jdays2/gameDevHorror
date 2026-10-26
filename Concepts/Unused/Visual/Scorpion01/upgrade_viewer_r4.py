"""One-time review UI migration for the paired emissive sprite export."""
from pathlib import Path

p=Path(__file__).with_name('viewer.html')
s=p.read_text(encoding='utf-8')
s=s.replace('Скорпион r3 — большие передние клешни, десять глаз и массивный хвост.','Скорпион r4 — спокойная серо-зелёная палитра, настоящие клешни и красное свечение глаз.')
s=s.replace('<option value="light">Светлый</option>','<option value="light">Светлый</option><option value="night">Ночь</option>')
s=s.replace('Крупные клешни клацают в беге и смыкаются перед головой в атаке. Десять глаз, массивный хвост, восемь опорных ног. Скорость пробы 2.4 ед/с.','Клешни с широкой ладонью и шарнирным пальцем, пять сегментов хвоста и отдельное загнутое жало. Красные глаза светятся сами. «Ночь» показывает этот слой на затемнённом теле; это художественный просмотр, не замер производительности.')
start=s.index('function render(){')
end=s.index('async function init()',start)
s=s[:start]+'''function drawFrame(im,cell,columns,pivot,frame,scale,single=false){
  if(single){ctx.drawImage(im,-pivot[0]*scale,-pivot[1]*scale,im.width*scale,im.height*scale);return}
  ctx.drawImage(im,(frame%columns)*cell,Math.floor(frame/columns)*cell,cell,cell,-pivot[0]*scale,-pivot[1]*scale,cell*scale,cell*scale);
}
function render(){
  if(!clips.scorpion)return;
  const family=$('family').value,c=active(),night=$('background').value==='night',dark=$('background').value!=='light',large=$('close').checked;
  const n=large?1:Number($('count').value),scale=large?1.2:.66;
  ctx.fillStyle=night?'#050708':dark?'#262d32':'#e4e4de';ctx.fillRect(0,0,1280,700);
  const speed=c.key==='Run'?(family==='scorpion'?2.4:1.65):c.key==='Walk'?.55:0;
  const drift=speed*time*200*scale;ctx.strokeStyle=night?'#0e1518':dark?'#354047':'#ced0ca';ctx.lineWidth=1;
  for(let x=0;x<1280;x+=100){ctx.beginPath();ctx.moveTo(x,0);ctx.lineTo(x,700);ctx.stroke()}
  for(let y=-100;y<800;y+=100){const at=y+drift%100;ctx.beginPath();ctx.moveTo(0,at);ctx.lineTo(1280,at);ctx.stroke()}
  const duration=c.ms.reduce((a,b)=>a+b,0)/1000;let shown=0;
  for(let i=0;i<n;i++){
    let age=time;
    if(c.loop&&speed)age=time*speed/c.reference;else if(!c.loop&&!c.dead)age=time%(duration+.9)-(n===3?i*.15:0);
    if(c.loop&&n===3)age+=duration*[0,.31,.67][i];
    const frame=override===null?indexAt(c,age):override%c.ms.length;if(i===0)shown=frame;
    const x=n===1?640:270+i*370,y=350+(n===3?[12,-20,18][i]:0);
    ctx.save();ctx.translate(x,y);if(family==='claw')ctx.rotate(Math.PI/2);
    ctx.filter=night?'brightness(15%) saturate(70%)':c.dead?'brightness(60%) saturate(85%)':'none';
    drawFrame(c.image,512,c.columns,c.pivot,frame,scale,c.single);
    if(c.emission){ctx.filter='none';ctx.globalCompositeOperation='lighter';const e=c.emission;drawFrame(e.image,e.cell,e.columns,e.pivot,frame,scale*200/e.ppu)}
    ctx.restore();ctx.fillStyle=dark?'#cad4da':'#465058';ctx.font='16px system-ui';ctx.textAlign='center';ctx.fillText(n===1?c.label:'Особь '+(i+1),x,650);
  }
  $('status').textContent=String(shown+1).padStart(2,'0')+' / '+c.ms.length;
  if(override===null)$('phase').value=Math.round(shown/Math.max(1,c.ms.length-1)*999);
  window.__reviewAudit={family,state:c.key,count:n,frame:shown,loaded:true,decodedImages:images.size};
}
''' +s[end:]
needle='pivot:state.pivotPixelsTopLeft})'
replacement='pivot:state.pivotPixelsTopLeft,emission:state.emission?{image:await loadImage(url(state.emission.asset)),cell:state.emission.cell,columns:state.emission.columns,pivot:state.emission.pivotPixelsTopLeft,ppu:state.emission.pixelsPerUnit}:null})'
assert needle in s
s=s.replace(needle,replacement)
p.write_text(s,encoding='utf-8',newline='\n')
