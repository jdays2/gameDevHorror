const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=__dirname,project=path.resolve(root,'../../../..');
const hash=f=>crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
const walk=d=>fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(path.join(d,e.name)):[path.join(d,e.name)]);
const sources=['Assets/EmergencyShift/Prefabs/UI/MainMenu/MainMenuUI.prefab','Assets/EmergencyShift/Prefabs/UI/PauseMenuUI.prefab','Assets/EmergencyShift/Data/UI/MainMenu/NightShift.asset','Assets/EmergencyShift/Editor/Authoring/PauseMenuAuthoring.cs','Docs/Validation/MainMenu01/home.png','Concepts/Unused/UI/DeathScreen01/Sources/world-reference.png','Assets/EmergencyShift/Fonts/MainMenu/NotoSans-Medium.ttf','Assets/EmergencyShift/Fonts/MainMenu/NotoSans-OFL.txt','Assets/EmergencyShift/Fonts/MainMenu/BarlowCondensed-Bold.ttf','Assets/EmergencyShift/Fonts/MainMenu/BarlowCondensed-OFL.txt'];
const validation=JSON.parse(fs.readFileSync(path.join(root,'validation.json'),'utf8'));
let broken=[];for(const file of walk(root).filter(f=>f.endsWith('.md')))for(const m of fs.readFileSync(file,'utf8').matchAll(/\]\(([^)]+)\)/g)){const href=m[1].split('#')[0];if(href&&!/^[a-z]+:\/\//i.test(href)&&!fs.existsSync(path.resolve(path.dirname(file),href)))broken.push({file,href})}
const out={task:'TASK-006',date:'2026-10-31',stage:'Concepts C–F for owner review; no implementation',
 owner_decision:'No music on death screen. More visual concepts based on current main and pause menus.',
 recommendation:'D — continuation of pause-menu layout; owner choice pending',artistic_approval:false,
 source_assessment:'Main menu inspected via saved screenshot plus current prefab/style; pause inspected via actual serialized prefab and authoring source, not a live Unity capture.',
 visual_creation:'Original HTML/CSS UI and browser PNG renders; no AI raster generation. Existing archived live-player frame is only a layout background, not a death capture.',
 music:'None; no generation or playback in current mockups. Previous sources retained as history.',
 unity_used:false,assets_runtime_scenes_changed:false,authoring_review:'not applicable',bug053:'Context updated for no-music design; not reproduced or fixed',
 mainMenu:'Remains deferred; normal Play from open level preserved',
 verification:{combinations:validation.results.length,layoutFailures:validation.failures.length,interaction:validation.interaction,brokenLocalLinks:broken},
 sources:sources.map(file=>({file,sha256:hash(path.join(project,file))})),
 files:walk(root).filter(f=>path.basename(f)!=='provenance.json').sort().map(f=>({file:path.relative(root,f).replaceAll('\\','/'),bytes:fs.statSync(f).size,sha256:hash(f)}))};
fs.writeFileSync(path.join(root,'provenance.json'),JSON.stringify(out,null,2)+'\n');console.log(JSON.stringify({files:out.files.length,combinations:validation.results.length,failures:validation.failures.length,brokenLinks:broken}));if(broken.length||validation.failures.length)process.exit(1);
