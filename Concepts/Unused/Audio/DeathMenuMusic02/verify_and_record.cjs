// Verify playable local previews and record provenance. No Unity/external writes.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const {pathToFileURL}=require('node:url');
const {chromium}=require('C:/Users/Mark/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=__dirname,project=path.resolve(root,'../../../..');
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const walk=dir=>fs.readdirSync(dir,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(path.join(dir,e.name)):[path.join(dir,e.name)]);
(async()=>{
 const browser=await chromium.launch({headless:true});const page=await browser.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto(pathToFileURL(path.join(root,'Directions04/review.html')).href);
 await page.waitForFunction(()=>[...document.querySelectorAll('audio')].every(a=>Number.isFinite(a.duration)),{timeout:10000});
 const players=await page.locator('audio').evaluateAll(as=>as.map(a=>({file:a.getAttribute('src'),duration:a.duration,error:a.error?.code??null,readyState:a.readyState})));
 const ui=path.join(project,'Concepts/Unused/UI/DeathScreen01/review.html');await page.goto(pathToFileURL(ui).href);
 const selection=[];for(const id of ['a_piano','b_guitar','c_cello','d_vibraphone']){await page.selectOption('#music',id);await page.waitForFunction(()=>Number.isFinite(document.querySelector('audio').duration),{timeout:10000});selection.push(await page.locator('audio').evaluate(a=>({file:a.getAttribute('src'),duration:a.duration,error:a.error?.code??null})))}
 await browser.close();
 let broken=[];for(const file of walk(root).filter(f=>f.endsWith('.md'))){for(const m of fs.readFileSync(file,'utf8').matchAll(/\]\(([^)]+)\)/g)){const href=m[1].split('#')[0];if(href&&!/^[a-z]+:\/\//i.test(href)&&!fs.existsSync(path.resolve(path.dirname(file),href)))broken.push({file,href})}}
 const validation={scope:'Local audio browser metadata and selectors, not a listening judgment',players,visualReviewAudioSelection:selection,jsErrors:errors,brokenLocalMarkdownLinks:broken};
 fs.writeFileSync(path.join(root,'preview-validation.json'),JSON.stringify(validation,null,2)+'\n');
 if(errors.length||broken.length||players.some((a,i)=>a.error||Math.abs(a.duration-(i===4?83:20))>.1)||selection.some(a=>a.error||Math.abs(a.duration-20)>.1))throw new Error(JSON.stringify(validation));
 const data={task:'TASK-006',date:'2026-10-31',stage:'Four20second samples ready for owner direction choice; integration not begun',
 owner_direction:'No synth-led melody; prepare several15–20second samples and let the owner choose.',
 current:'Directions04/review.html',approved:false,unity_used:false,authoring_review:'not applicable',
 music_generation:'Runway text-only;03intermediate4credits (not recommended),four new directions4credits each (16);family total20. Earlier01/02 belong to another retained family. No reference audio uploaded.',
 rights:'AI-generated originals in connected account; no external samples added; not asserted CC0.',
 reference:{file:'Concepts/Unused/Audio/MenuThemes02/Originals/below_the_ward_02.mp3',sha256:hash(path.join(project,'Concepts/Unused/Audio/MenuThemes02/Originals/below_the_ward_02.mp3'))},
 listening:'No direct auditory review by agent. Instrument names are requested directions, not verified credits. Owner chooses by listening; previous synth direction withdrawn.',
 files:walk(root).filter(f=>path.basename(f)!=='provenance.json').sort().map(f=>({file:path.relative(root,f).replaceAll('\\','/'),bytes:fs.statSync(f).size,sha256:hash(f)}))};
 fs.writeFileSync(path.join(root,'provenance.json'),JSON.stringify(data,null,2)+'\n');
 console.log(JSON.stringify({players:players.length,selectedTracks:selection.length,jsErrors:errors.length,brokenLinks:broken.length,files:data.files.length}));
})().catch(e=>{console.error(e);process.exit(1)});
