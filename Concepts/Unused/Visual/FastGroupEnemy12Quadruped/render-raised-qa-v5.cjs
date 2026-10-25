const fs=require('fs'),path=require('path');
const sharp=require('C:/Users/Mark/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const d=__dirname,p=JSON.parse(fs.readFileSync(path.join(d,'raised-stance-plan-v2.json'))),c=JSON.parse(fs.readFileSync(path.join(d,'raised-canonical-transform-v5.json')));
const P=a=>[600+250*a[0],512+250*a[1]],line=(a,col,w=3,dash=false)=>`<polyline points="${a.map(q=>P(q).join(',')).join(' ')}" fill="none" stroke="${col}" stroke-width="${w}" stroke-linecap="round" stroke-linejoin="round" ${dash?'stroke-dasharray="7 5"':''}/>`;
const dot=(a,col)=>{const q=P(a);return `<circle cx="${q[0]}" cy="${q[1]}" r="4" fill="${col}" stroke="#222" stroke-width="1"/>`;};
let b=line([p.head.center,...p.spine],'#f5cc6f',3);
for(const f of p.fronts){b+=line([f.shoulder,f.elbow],'#68ddeb',4,true)+line([f.elbow,f.wrist,f.palm],'#68ddeb',4)+[f.shoulder,f.elbow,f.wrist,f.palm].map(q=>dot(q,'#68ddeb')).join('');for(const t of f.digits)b+=line([f.palm,t.proximal,t.distal,t.clawBase],'#b1f1c4',2)+line([t.clawBase,t.clawTip],'#fff0ad',2);}
for(const r of p.rears){b+=line([r.hip,r.knee],'#bf9ee9',3,true)+line([r.knee,r.hock,r.palm],'#bf9ee9',3)+[r.hip,r.knee,r.hock,r.palm].map(q=>dot(q,'#bf9ee9')).join('');for(const t of r.digits)b+=line([r.palm,t.base,t.tip],t.id===4?'#f7ac77':'#d1b7f5',2);}
const svg=Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="1254" height="1254"><g transform="translate(${c.translate[0]} ${c.translate[1]}) scale(${c.scale})">${b}</g></svg>`);
(async()=>{await sharp(svg).png().toFile(path.join(d,'raised-v5-bones-layer.png'));await sharp(path.join(d,'sprite-base-v5-raised.png')).composite([{input:svg}]).png().toFile(path.join(d,'raised-v5-bones-check.png'));console.log(JSON.stringify({scale:c.scale,translation:c.translate,fit:'global only; no limb adjustment'}));})();
