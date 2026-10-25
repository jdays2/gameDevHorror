const fs=require('fs'),path=require('path'),crypto=require('crypto');
const sharp=require('C:/Users/Mark/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const d=__dirname;
const hash=f=>crypto.createHash('sha256').update(fs.readFileSync(path.join(d,f))).digest('hex');
const delays=[180,50,110,140];
(async()=>{
 const timing={frameOrder:[0,1,2,3],delayMilliseconds:delays,loop:true,totalMilliseconds:delays.reduce((a,b)=>a+b),changeReason:'Parent requested a shorter phase 1 after reviewing v2; timing only, no drawing or guide-position edits.',interpolation:false,newFrames:false};
 fs.writeFileSync(path.join(d,'preview-timing.json'),JSON.stringify(timing,null,2));
 for(const v of ['v1','v2']){
  const t=JSON.parse(fs.readFileSync(path.join(d,`paired-${v}-technical.json`))),[w,h]=t.cell;
  const transparent=[],dark=[];
  for(let i=0;i<4;i++){
   const p=path.join(d,`paired-${v}-frame-${i}.png`);
   transparent.push(await sharp(p).ensureAlpha().raw().toBuffer());
   dark.push(await sharp(p).flatten({background:'#282b30'}).ensureAlpha().raw().toBuffer());
  }
  const raw={width:w,height:h*4,channels:4,pageHeight:h};
  await sharp(Buffer.concat(transparent),{raw}).gif({delay:delays,loop:0,effort:7}).toFile(path.join(d,`paired-${v}-transparent-preview.gif`));
  await sharp(Buffer.concat(dark),{raw}).gif({delay:delays,loop:0,effort:7}).toFile(path.join(d,`paired-${v}-preview.gif`));
  await sharp({create:{width:w*4,height:h,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite([0,1,2,3].map(i=>({input:path.join(d,`paired-${v}-frame-${i}.png`),left:i*w,top:0}))).png().toFile(path.join(d,`paired-${v}-strip.png`));
 }
 const bg=Buffer.from('<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="750"><rect width="1200" height="250" fill="#e8e7e3"/><rect y="250" width="1200" height="250" fill="#282b30"/><pattern id="p" width="24" height="24" patternUnits="userSpaceOnUse"><rect width="24" height="24" fill="#999"/><path d="M0 0H12V12H0ZM12 12H24V24H12Z" fill="#bbb"/></pattern><rect y="500" width="1200" height="250" fill="url(#p)"/></svg>');
 const comps=[];
 for(let row=0;row<3;row++)for(let i=0;i<4;i++)comps.push({input:await sharp(path.join(d,`paired-v2-frame-${i}.png`)).resize(300,200).png().toBuffer(),left:i*300,top:row*250+25});
 await sharp(bg).composite(comps).png().toFile(path.join(d,'paired-v2-rgba-review.png'));
 const validation={task:'TASK-005',bestPreview:'paired-v2-preview.gif',independentGuide:'phase-anatomy-v1.json',guideChangedAfterGeneration:false,attempts:[{id:'v1',visibleTopology:'Two feet × three short claws; no horizontal arms; paired movement',targetProjectionZones:'FAIL: foot spread/phase2 displacement differ from guide',cellClipping:'FAIL: phase2 rump crosses into phase3 cell',musclePass:'NOT RUN after failed projection/clipping gate'},{id:'v2',visibleTopology:'PASS for visible parts: two broad feet × three claws, same short visible attachments, no extra joint/limb or old horizontal arms',targetProjectionZones:'FAIL / residual deviation: lower foot outside intended zone, feet spread wider than plan; guide retained without fitting',hiddenBones:'NOT OBSERVABLE / not measured; no recovered 3D-length claim',pairedTiming:'Visible paired phase retained: 0/1 close, 2 both feet behind, 3 paired return. Phase1 abbreviated to50ms, no generated intermediate frames.',cellClipping:'PASS: all four cells borderPixels0',muscleVolumesAndTopology:'NOT COMPLETED: hidden groups unavailable, full scheme alignment failed. Visible skin-covered foot masses inspected only as external continuity; no new exposed random bands observed.',skinAndIdentity:'Large maw, two yellow eyes, two swept horns, dark continuous skin, blood, short rump retained. Mild shape/teeth/texture drift remains; no exact-rig claim.',loop:'Four-frame jerk preview; abrupt paired return and mild contour drift remain. Not a finished smooth locomotion cycle.',unity:'NOT RUN',ownerAcceptance:'PENDING'}],timing};
 fs.writeFileSync(path.join(d,'validation.json'),JSON.stringify(validation,null,2));
 const src=['exec-9bbce11a-5368-4f3f-b99f-a11ba39d5c7e.png','exec-c35286cc-7103-4766-b4ed-80a20aa12f0b.png'];
 const provenance={task:'TASK-005',method:'built-in image_gen direct full-frame painting',transparentBackgroundRequested:true,source:{file:'character-source-v5.png',origin:'../FastGroupEnemy09Standing2D/standing-v5-wide-feet.png',sha256:hash('character-source-v5.png')},guide:{file:'phase-anatomy-v1.json',createdBeforeGeneration:true,unchanged:true,sha256:hash('phase-anatomy-v1.json')},attempts:[1,2].map(n=>({id:'v'+n,prompt:`paired-v${n}-prompt.txt`,promptSha256:hash(`paired-v${n}-prompt.txt`),output:`paired-v${n}-source.png`,outputSha256:hash(`paired-v${n}-source.png`),original:`C:/Users/Mark/.codex/generated_images/01a16fce-d886-7960-9f97-a93f5a1b7f70/${src[n-1]}`,references:n===1?['paired-phase-guide.png','character-source-v5.png']:['paired-v1-source.png','paired-phase-guide.png','character-source-v5.png']})),processing:['copy exact sources','crop fixed cells','draw technical independent bone/projection overlay','package four exact frame rasters as GIF/strip','compose neutral backgrounds for review only'],notUsed:['collage or cutout animation','mesh warp','optical flow','new or interpolated frames','3D source']};
 fs.writeFileSync(path.join(d,'provenance.json'),JSON.stringify(provenance,null,2));
 const hashes=fs.readdirSync(d).filter(f=>fs.statSync(path.join(d,f)).isFile()&&f!=='sha256.json').map(file=>({file,sha256:hash(file)}));
 fs.writeFileSync(path.join(d,'sha256.json'),JSON.stringify(hashes,null,2));
 console.log(JSON.stringify({preview:'paired-v2-preview.gif',frames:4,delays,files:hashes.length}));
})();
