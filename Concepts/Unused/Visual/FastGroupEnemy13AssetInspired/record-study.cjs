const fs=require('fs'),path=require('path'),crypto=require('crypto');
const d=__dirname,project=path.resolve(d,'../../../..'),prefix='Concepts/Unused/Visual/FastGroupEnemy13AssetInspired/';
const hash=f=>crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
const cache='C:/Users/Mark/.codex/generated_images/01a16f34-2ca0-7c33-8880-673b5909d2dd/';
const temp='C:/Users/Mark/AppData/Local/Temp/';
const beige=temp+'codex-clipboard-5de54413-d1c9-46e5-81e1-7ae851e533c5.png';
const oldPlan=path.resolve(d,'../FastGroupEnemy12Quadruped/pose-v3-control-1254.png');
const oldArt=path.resolve(d,'../FastGroupEnemy12Quadruped/sprite-base-v7-raised.png');
const stalker=path.join(project,'Assets/EmergencyShift/Art/Enemies/Stalker/PaleFlesh/walk-sheet.png');
const local=f=>path.join(d,f);
const jobs=[
 ['base-v1','exec-dc16561a-b3c6-439b-a28e-e4db6bbe9b63.png',[oldPlan,oldArt,beige,stalker],'rejected: insufficient new design'],
 ['base-v2','exec-42011cfb-d44f-4a64-9a0e-3963c7c19033.png',[beige,oldPlan,stalker],'OWNER_SELECTED appearance; extra fifth lower-rear claw rejected'],
 ['base-v3','exec-dffd6ad0-64fe-47be-9bd7-a393fcf96124.png',[local('base-v2.png'),local('base-v2-rear-detail.png')],'current static source; remove only redundant rear claw; coarse static validation with limits'],
 ['run-sheet-v1','exec-b1df94ce-4436-4739-a756-c822f9d3e77d.png',[local('base-v3.png'),local('run-phase-guide-v4.png')],'REJECTED bone/choreography'],
 ['attack-sheet-v1','exec-101769cc-3aab-4cf6-84cd-2dafe3032183.png',[local('base-v3.png'),local('attack-phase-guide-v4.png')],'REJECTED bone/choreography'],
 ['run-sheet-v2','exec-55485273-c403-46ba-837f-6f32761e0726.png',[local('neutral-camera-template.png'),local('run-phase-guide-v4.png'),local('base-v3.png')],'REJECTED bone/choreography'],
 ['attack-sheet-v2','exec-2f0a268c-2bcf-4cba-bce5-537aa9de8ff9.png',[local('neutral-camera-template.png'),local('attack-phase-guide-v4.png'),local('base-v3.png')],'REJECTED bone/choreography and cross-cell claw'],
 ['run-frame02-v3','exec-7a4c7da4-a5f9-499d-9472-3536516ebc3e.png',[local('FrameControls/neutral-1024.png'),local('FrameControls/run-02-guide.png')],'REJECTED pilot; do not expand'],
 ['attack-frame02-v3','exec-eb6e89f2-bd73-4515-8f0f-4c8a787525bc.png',[local('FrameControls/neutral-1024.png'),local('FrameControls/attack-02-guide.png')],'REJECTED pilot; wrists closer but body/elbows fail'],
 ['run-frame02-v4','exec-e2d74019-2906-473b-869c-8512d540bcc8.png',[local('FrameControls/neutral-1254.png'),local('FrameControls/run-02-full1254-guide.png')],'REJECTED pilot; native canvas did not fix articulation'],
 ['attack-frame02-v4','exec-d72792e3-aee3-47d2-9a3e-c7045d02e216.png',[local('FrameControls/neutral-1254.png'),local('FrameControls/attack-02-full1254-guide.png')],'REJECTED pilot; body/limb displacement']
];
function enumerate(dir){return fs.readdirSync(dir,{withFileTypes:true}).flatMap(e=>e.isDirectory()?enumerate(path.join(dir,e.name)):[path.join(dir,e.name)]);}
function reference(f){return {file:f.startsWith(d)?path.relative(d,f).replaceAll('\\','/'):f.replaceAll('\\','/'),sha256:fs.existsSync(f)?hash(f):null};}
// Normalize owned text before recording any input hashes.
for(const f of enumerate(d))if(/\.(json|md|txt|cjs|py)$/.test(f)&&!f.endsWith('sha256.json'))fs.writeFileSync(f,fs.readFileSync(f,'utf8').replace(/\r\n/g,'\n').trimEnd()+'\n');
const provenance={task:'TASK-005',date:'2026-10-25',tool:'built-in image_gen; transparent_background=true; no CLI/API fallback',ownerSelection:reference(local('owner-selected-v2.json')),currentStaticSource:'base-v3.png',animationStatus:'CYCLES_NOT_READY; no animation PASS, no Unity import',anatomy:'anatomy-plan-v4.json',musclePlan:'anatomy-muscle-plan-v4.json',validationOrder:'bones FIRST; only GO proceeds to volumes, numbered groups, skin, motion. Animation trials stopped at failed bone pass.',inspiration:{ownerProvidedReference:reference(beige),attributionUrl:'https://dev.craftpix.net/product/tds-monster-character-sprites/',packPurchasedOrDownloaded:false},generations:jobs.map(([name,generated,inputs,status])=>({art:reference(local(name+'.png')),prompt:reference(local(name+'-prompt.txt')),generatedSource:cache+generated,inputImages:inputs.map(reference),status})),processing:{art:'No body-part collage/warp, no procedural pose manufacture. Direct image_gen sheet or whole frame.',cameraReferences:'Technical unchanged whole-image templates only; separate diagram cells.',export:'Whole equal cells only, no per-frame auto-crop/registration. GIF shared palette on dark background; PNG alpha preserved.',v3Pilot:'1254 output uniformly resized to1024 for declared camera comparison; no landmark fit.',v4Pilot:'1254 input/output; only uniform cell512→1254 camera.',externalSync:'No Notion write or push; previous auto-review rejected external transfer, not bypassed.'}};
fs.writeFileSync(local('provenance.json'),JSON.stringify(provenance,null,2)+'\n');
let files=enumerate(d).filter(f=>path.basename(f)!=='sha256.json').sort();
const manifest={algorithm:'SHA-256',scope:'This study only, excluding this manifest itself.',files:files.map(f=>({file:path.relative(d,f).replaceAll('\\','/'),sha256:hash(f)}))};
fs.writeFileSync(local('sha256.json'),JSON.stringify(manifest,null,2)+'\n');
files.push(local('sha256.json'));
const catalogFile=path.join(project,'Concepts/catalog.json'),original=fs.readFileSync(catalogFile,'utf8');
const rows=JSON.parse(original),owned=rows.filter(r=>r.to?.startsWith(prefix));
let prefixText=original.trimEnd().slice(0,-1).trimEnd();
const marker='TASK-005 asset-inspired direct image_gen study; see provenance.json';
if(owned.length){
 const first=rows.findIndex(r=>r.to?.startsWith(prefix));
 if(rows.slice(first).some(r=>!r.to?.startsWith(prefix)))throw Error('Owned catalog block no longer at tail; preserve concurrent entries and reconcile manually.');
 const needle='  {\n    "from": "'+marker+'"';
 const index=original.indexOf(needle);
 if(index<0)throw Error('Cannot find exact owned catalog marker');
 prefixText=original.slice(0,index).trimEnd().replace(/,$/,'');
}
const entries=files.sort().map(f=>({from:marker,to:prefix+path.relative(d,f).replaceAll('\\','/'),reason:'Owner-selected V2 appearance, corrected static source V3 and declared anatomy V4. Run/attack direct-generation trials rejected; not production assets.',sha256:hash(f)}));
const block=entries.map(r=>JSON.stringify(r,null,2).split('\n').map(s=>'  '+s).join('\n')).join(',\n');
fs.writeFileSync(catalogFile,prefixText+',\n'+block+'\n]\n');
console.log(JSON.stringify({files:files.length,generations:jobs.length,catalogExistingRows:rows.length,catalogOwnedRows:entries.length}));
