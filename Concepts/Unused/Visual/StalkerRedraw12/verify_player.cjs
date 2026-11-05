// Runtime smoke checks without opening or bypassing the blocked file URL.
const fs=require('fs'),path=require('path'),vm=require('vm'),assert=require('assert');
const root=__dirname,html=fs.readFileSync(path.join(root,'ReviewPlayer/player.html'),'utf8');
const scripts=[...html.matchAll(/<script>([\s\S]*?)<\/script>/g)];
assert.equal(scripts.length,1);const script=new vm.Script(scripts[0][1],{filename:'player.html'});
const data=JSON.parse(fs.readFileSync(path.join(root,'ReviewPlayer/data.json'),'utf8'));
assert(Buffer.byteLength(html)<3_000_000,'Viewer regressed to a huge file');
assert(html.includes('data:image/png;base64,'),'No static fallback');
assert(!/\b(fetch|XMLHttpRequest|WebSocket)\s*\(/.test(scripts[0][1]),'Network-dependent viewer');
for(const [s,n] of Object.entries({movement:36,sleep_entry:20,awakening:20,attack:32})){
  assert.equal(data.states[s].length,n);
  for(const f of data.states[s])for(const key of ['image','ref'])assert(data.images[f[key]],'Unresolved image');
}
for(const uri of Object.values(data.images)){
  const b=Buffer.from(uri.split(',')[1],'base64');
  assert.equal(b.subarray(0,4).toString(),'RIFF');assert.equal(b.subarray(8,12).toString(),'WEBP');
}

function boot(failImages=false){
  const pending=[],raf=[],draws=[];let invalidDraws=0;
  const elements={};
  for(const match of html.matchAll(/\bid="([^"]+)"/g)){
    const id=match[1];
    elements[id]={id,value:id==='fps'?'14':'0',textContent:'',checked:false,hidden:false,children:[],classList:{add(){}},append(child){this.children.push(child);}};
  }
  elements['stalker-preview'].querySelector=selector=>elements[selector.slice(1)];
  for(const id of ['current','neighbor'])elements[id].getContext=()=>({
    globalAlpha:1,fillRect(){},beginPath(){},moveTo(){},lineTo(){},stroke(){},arc(){},
    drawImage(image,...rect){
      if(!image.complete || !image.naturalWidth){invalidDraws++;throw Error('Broken image draw');}
      assert(rect.every(Number.isFinite));draws.push({id,rect,alpha:this.globalAlpha});
    }
  });
  class FakeImage{
    constructor(){this.complete=false;this.naturalWidth=0;}
    set src(value){this.url=value;pending.push(this);}
  }
  const context=vm.createContext({document:{getElementById:id=>elements[id],createElement:()=>({})},Image:FakeImage,requestAnimationFrame:callback=>raf.push(callback),Map,Math,Number,Boolean});
  script.runInContext(context,{timeout:1000});
  function flush(){let steps=0;while(pending.length){assert(++steps<200);const im=pending.shift();im.complete=true;if(failImages){im.onerror();}else{im.naturalWidth=160;im.onload();}}}
  function tick(time){assert(raf.length);raf.shift()(time);}
  return {elements,pending,raf,draws,flush,tick,invalid:()=>invalidDraws};
}

const normal=boot();
assert.equal(normal.elements.fallback.hidden,false,'Fallback disappeared before first decode');
assert(normal.pending.length<=2,'Eagerly decoding entire animation library');
normal.tick(100);assert.equal(normal.elements.counter.textContent,'1 / 36','Advanced before image decoded');
normal.flush();assert(normal.elements.fallback.hidden);assert(normal.draws.length>0);
normal.tick(200);normal.flush();assert.equal(normal.elements.counter.textContent,'2 / 36');
normal.elements.play.onclick();normal.tick(1000);assert.equal(normal.elements.counter.textContent,'2 / 36','Pause failed');
normal.elements.next.onclick();normal.flush();assert.equal(normal.elements.counter.textContent,'3 / 36');
normal.elements.prev.onclick();normal.flush();assert.equal(normal.elements.counter.textContent,'2 / 36');
for(const [state,count] of Object.entries({sleep_entry:20,awakening:20,attack:32,movement:36})){
  normal.elements.state.value=state;normal.elements.state.onchange();normal.flush();
  assert.equal(normal.elements.counter.textContent,'1 / '+count);
  normal.elements.frame.value=String(count-1);normal.elements.frame.oninput();normal.flush();
  assert.equal(normal.elements.counter.textContent,count+' / '+count);
}
normal.elements.onion.checked=true;normal.elements.onion.onchange();normal.flush();
normal.elements.bones.checked=true;normal.elements.bones.onchange();
normal.elements.source.checked=true;normal.elements.source.onchange();normal.flush();
assert(normal.draws.some(x=>x.alpha===.22),'No neighboring whole-frame overlay');
assert(normal.draws.some(x=>x.alpha===.35),'No original whole-frame overlay');
assert.equal(normal.invalid(),0);

normal.elements.state.value='sleep_entry';normal.elements.state.onchange();normal.flush();
normal.elements.frame.value='18';normal.elements.frame.oninput();normal.flush();normal.elements.play.onclick();
normal.tick(4000);normal.flush();normal.tick(5000);
assert.equal(normal.elements.counter.textContent,'20 / 20');
assert.equal(normal.elements.play.textContent,'Играть','One-way state did not stop');
normal.elements.state.value='movement';normal.elements.state.onchange();normal.flush();
normal.elements.frame.value='35';normal.elements.frame.oninput();normal.flush();normal.elements.play.onclick();normal.tick(6000);
assert.equal(normal.elements.counter.textContent,'1 / 36','Movement loop did not wrap');

const broken=boot(true);broken.flush();broken.tick(1000);
assert.equal(broken.invalid(),0,'Tried drawImage on a failed decode');
assert.equal(broken.elements.fallback.hidden,false,'Lost fallback on image error');
assert(broken.elements['load-message'].textContent.includes('Не удалось'),'No visible load error');
const result={syntax:'PASS',states:4,imageReferences:216,embeddedImages:Object.keys(data.images).length,firstAsyncFrame:'PASS',lazyLoading:'PASS',playPauseStepSeek:'PASS',allStates:'PASS',wholeFrameOverlays:'PASS',failedImageFallback:'PASS',browserUI:'NOT RUN; runtime DOM/Canvas behavior stubbed, no browser URL policy bypass',artAcceptance:'UNCHANGED / not accepted'};
fs.writeFileSync(path.join(root,'../../../../Docs/Validation/Task007Redraw12/preview-runtime-checks.json'),JSON.stringify(result,null,2));
console.log(JSON.stringify(result));
