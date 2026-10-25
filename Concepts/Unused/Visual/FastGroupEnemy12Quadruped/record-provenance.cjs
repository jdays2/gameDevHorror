const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const d = __dirname;
const project = path.resolve(d, '../../../..');
const hash = f => crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
for (const name of fs.readdirSync(d)) {
  const f=path.join(d,name);
  if (fs.statSync(f).isFile() && /\.(md|txt|cjs|json)$/.test(name)) {
    const normalized=fs.readFileSync(f,'utf8').replace(/^\uFEFF/,'').replace(/\r\n/g,'\n').trimEnd()+'\n';
    fs.writeFileSync(f,normalized);
  }
}
const generations = [
  ['massing-v1.png', 'massing-v1-prompt.txt', 'exec-20eec6ae-f854-4d70-af27-3b7fc10aea7d.png', ['Recent owner white skeleton (oval explicitly confirmed as head)', 'Viewed current pink Stalker sheet, style only; exact file not encoded in generation call'], 'Owner chose variant 3; original sketch toe count later superseded'],
  ['selected-03-long-claws.png', 'selected-03-long-claws-prompt.txt', 'exec-6b8a4c51-1cc8-4d26-989c-da099e127572.png', ['selected-03-reference.png'], 'Owner likes direction; requested straighter claws/anatomy afterward'],
  ['selected-03-anatomy-v2.png', 'selected-03-anatomy-v2-prompt.txt', 'exec-976b609f-3ec1-4cb7-a854-5a7f39a0b6d4.png', ['selected-03-long-claws.png', 'owner-claw-annotation.png', 'anatomy-bones-v1.png'], 'Rejected stance: too wide/flat despite improved claws'],
  ['sprite-base-v3-raised.png', 'sprite-base-v3-raised-prompt.txt', 'exec-e0581bac-a31f-4d3d-9239-4c75880e6f9d.png', ['raised-stance-top-control-v2.png', 'selected-03-anatomy-v2.png', 'raised-stance-projections-v2.png'], 'FAIL: extra rear digit'],
  ['sprite-base-v4-raised.png', 'sprite-base-v4-raised-prompt.txt', 'exec-eb3dc642-fedb-4ef9-877a-962a4b504320.png', ['sprite-base-v3-raised.png', 'raised-v3-rear-detail.png'], 'FAIL: rear foot rotation/pose fit; third main rear digit poorly readable'],
  ['sprite-base-v5-raised.png', 'sprite-base-v5-raised-prompt.txt', 'exec-9857b6a9-e7e6-423d-afd1-c626d4dc3828.png', ['sprite-base-v4-raised.png', 'raised-v4-bones-check.png', 'raised-stance-top-control-v2.png'], 'Topology improved; no full fit PASS. V2 skeleton knee occlusion itself required a prospective pose correction']
];
if (fs.existsSync(path.join(d, 'generation-final.json'))) generations.push(...JSON.parse(fs.readFileSync(path.join(d, 'generation-final.json'))).generations);
const ref = n => fs.existsSync(path.join(d, n)) ? {file:n, sha256:hash(path.join(d,n))} : {reference:n, availability:'Conversation input; not an independently preserved source file'};
const prov = {
  task:'TASK-005', date:'2026-10-25', method:'Built-in image_gen, transparent_background=true; direct generated artwork. No CLI fallback, body-part collage, raster warping or 3D art render.',
  scope:'ONE corrected static base sprite; no animation, Unity import, game balance or scene changes. Owner acceptance remains separate.',
  final:fs.existsSync(path.join(d,'generation-final.json')) ? JSON.parse(fs.readFileSync(path.join(d,'generation-final.json'))).current : null,
  generationRoot:'$CODEX_HOME/generated_images/01a16f34-2ca0-7c33-8880-673b5909d2dd',
  generations:generations.map(([output,prompt,original,inputs,status])=>({output:ref(output),prompt:ref(prompt),original,inputs:inputs.map(ref),status})),
  crop:{source:ref('massing-v1.png'),output:ref('selected-03-reference.png'),rect:{left:1024,top:0,width:512,height:850},operation:'Whole third-column crop excluding label only; not body-part assembly'},
  ownerAnnotation:{file:ref('owner-claw-annotation.png'),source:'Owner supplied codex-clipboard-5b6cdfca-33a4-4041-b3c2-ff63ce2644a1.png'},
  technicalProcessing:'Sharp renders declared technical skeletal/muscle diagrams, composites diagnostic overlays/review backgrounds, crops whole column/rear detail and measures alpha/framing. It does not alter the clean generated art.',
  anatomyLimits:'Declared spatial skeleton and functional muscle aggregates are design controls, not reconstruction of hidden individual muscles, true skin depth or measured mass. Failed art/schemes retained as failed evidence.',
  externalSync:'No Notion write or push attempted in this revision after earlier automatic approval rejection. Local art and documentation retained.'
};
fs.writeFileSync(path.join(d,'provenance.json'), JSON.stringify(prov,null,2)+'\n');
const sums = {};
for (const name of fs.readdirSync(d).sort()) if (name !== 'sha256.json' && fs.statSync(path.join(d,name)).isFile()) sums[name] = hash(path.join(d,name));
fs.writeFileSync(path.join(d,'sha256.json'),JSON.stringify(sums,null,2)+'\n');
const cataloguePath=path.join(project,'Concepts/catalog.json');
const oldText=fs.readFileSync(cataloguePath,'utf8');
const old=JSON.parse(oldText);
const prefix='Concepts/Unused/Visual/FastGroupEnemy12Quadruped/';
const additions=Object.keys(sums).concat('sha256.json').map(n=>({from:'TASK-005 quadruped direct image_gen study and independent anatomy guides; see provenance.json',to:prefix+n,reason:'Owner chose variant 3; corrected compact raised stance. One static base only. Historical failures retained with explicit validation; owner art acceptance pending.',sha256:hash(path.join(d,n))}));
const marker='  {\n    "from": "TASK-005 quadruped direct image_gen study and independent anatomy guides; see provenance.json"';
const ownStart=oldText.indexOf(marker);
if(ownStart>=0 && old.slice(old.findIndex(e=>e.to.startsWith(prefix))).some(e=>!e.to.startsWith(prefix))) throw Error('Own catalogue entries are not a final block; preserve later entries manually.');
const closing=oldText.lastIndexOf(']');
const retained=(ownStart>=0?oldText.slice(0,ownStart).trimEnd().replace(/,$/,''):oldText.slice(0,closing).trimEnd());
const body=additions.map(e=>JSON.stringify(e,null,2).split('\n').map(l=>'  '+l).join('\n')).join(',\n');
fs.writeFileSync(cataloguePath,retained+',\n'+body+'\n]\n');
console.log(JSON.stringify({files:Object.keys(sums).length,catalogueWritten:additions.length,final:prov.final}));
