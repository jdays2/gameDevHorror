// Content provenance only. Run after rendering/processing; no Unity or external writes.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=__dirname,project=path.resolve(root,'../../../..');
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
function walk(dir){return fs.readdirSync(dir,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(path.join(dir,e.name)):[path.join(dir,e.name)])}
const sources=['Docs/Validation/WeaponFeedback51/Preview/morgue-after.png','Docs/Validation/MainMenu01/home.png','Assets/EmergencyShift/Fonts/MainMenu/NotoSans-Medium.ttf','Assets/EmergencyShift/Fonts/MainMenu/NotoSans-OFL.txt','Concepts/Unused/Audio/MenuThemes02/Originals/below_the_ward_02.mp3','Concepts/Unused/Audio/MenuThemes02/acceptance.json'];
const data={task:'TASK-006',date:'2026-10-31',stage:'Concept for owner review; no runtime integration',
 notion:'https://app.notion.com/p/40a8d6dc06b6815d9d03f15b064cc6b9',recommendation:{visual:'A — Silence',music:'Last Ward02 audition, not artistically accepted'},
 artistic_acceptance:false,unity_used:false,assets_scenes_runtime_changed:false,authoring_review:'not applicable',bug053:'Context only; not reproduced or fixed',mainMenu:'Deferred; normal Play from open level preserved',
 visual_creation:'Original HTML/CSS UI; PNG browser renders; no AI raster generation. B uses an unchanged archived project frame with CSS desaturation/veil. The source frame shows a live placeholder, not an actual death capture.',
 font:'Existing project Noto Sans Medium; SIL OFL1.1 retained next to source font',
 music_creation:'Two short text-only Runway generations,4credits each;8total. No reference audio uploaded; exact requests and IDs in Audio/request*.json. AI-generated originals, not CC0 recordings; no external samples added.',
 listening:'No direct auditory review by agent. Signal/CLAP comparisons are diagnostic only. Owner chooses music and loop naturalness.',
 external_attachments:{status:'Not uploaded',reason:'Auto-review rejected upload of concept PNG to Notion for insufficiently explicit authorization of the specific export; no retry. Text and local paths saved in Notion.'},
 sources:sources.map(file=>({file,sha256:hash(path.join(project,file))})),
 files:walk(root).filter(file=>path.basename(file)!=='provenance.json').sort().map(file=>({file:path.relative(root,file).replaceAll('\\','/'),bytes:fs.statSync(file).size,sha256:hash(file)}))};
fs.writeFileSync(path.join(root,'provenance.json'),JSON.stringify(data,null,2)+'\n');
let broken=[];for(const file of walk(root).filter(f=>f.endsWith('.md'))){const s=fs.readFileSync(file,'utf8');for(const m of s.matchAll(/\]\(([^)]+)\)/g)){const href=m[1].split('#')[0];if(href&&!/^[a-z]+:\/\//i.test(href)&&!fs.existsSync(path.resolve(path.dirname(file),href)))broken.push({file,href})}}
console.log(JSON.stringify({files:data.files.length,bytes:data.files.reduce((s,f)=>s+f.bytes,0),brokenLocalMarkdownLinks:broken,backgroundUnchanged:hash(path.join(root,'Sources/world-reference.png'))===data.sources[0].sha256}));if(broken.length)process.exit(1);
