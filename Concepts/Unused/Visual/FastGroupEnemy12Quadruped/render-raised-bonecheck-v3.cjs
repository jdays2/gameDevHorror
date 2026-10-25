const fs=require('fs'),path=require('path');
const sharp=require('C:/Users/Mark/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const d=__dirname,v=Number(process.argv[2]||6),p=JSON.parse(fs.readFileSync(path.join(d,'raised-stance-plan-v3.json'))),c=JSON.parse(fs.readFileSync(path.join(d,`raised-canonical-transform-v${v}.json`)));
const P=a=>[600+250*a[0],512+250*a[1]];
const line=(a,col,w=3,dash=false)=>`<polyline points="${a.map(q=>P(q).join(',')).join(' ')}" fill="none" stroke="${col}" stroke-width="${w}" stroke-linecap="round" stroke-linejoin="round" ${dash?'stroke-dasharray="7 5"':''}/>`;
const dot=(a,col,hidden=false)=>{const q=P(a);return `<circle cx="${q[0]}" cy="${q[1]}" r="4" fill="${hidden?'#30353c':col}" stroke="${col}" stroke-width="1.5" ${hidden?'stroke-dasharray="2 2"':''}/>`;};
const envelope='<path d="M510 448Q563 385 655 420Q724 422 850 449L862 512L850 575Q724 602 655 604Q563 640 510 576Z"/><ellipse cx="437.5" cy="512" rx="112.5" ry="87.5"/>';
const defs=`<defs><clipPath id="in">${envelope}</clipPath><mask id="out"><rect width="1024" height="1024" fill="white"/><g fill="black">${envelope}</g></mask></defs>`;
const chain=(a,col,w=4)=>`<g clip-path="url(#in)">${line(a,col,w,true)}</g><g mask="url(#out)">${line(a,col,w)}</g>`;
let b=line([p.head.center,...p.spine],'#f5cc6f',3);
for(const f of p.fronts){b+=chain([f.shoulder,f.elbow,f.wrist,f.palm],'#68ddeb')+dot(f.shoulder,'#68ddeb',true)+[f.elbow,f.wrist,f.palm].map(q=>dot(q,'#68ddeb')).join('');for(const t of f.digits)b+=line([f.palm,t.proximal,t.distal,t.clawBase],'#b1f1c4',2)+line([t.clawBase,t.clawTip],'#fff0ad',2);}
for(const r of p.rears){b+=chain([r.hip,r.knee,r.hock,r.palm],'#bf9ee9',3)+[r.hip,r.knee].map(q=>dot(q,'#bf9ee9',true)).join('')+[r.hock,r.palm].map(q=>dot(q,'#bf9ee9')).join('');for(const t of r.digits)b+=line([r.palm,t.base,t.tip],t.id===4?'#f7ac77':'#d1b7f5',2);}
const svg=Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="1254" height="1254"><g transform="translate(${c.translate[0]} ${c.translate[1]}) scale(${c.scale})">${defs}${b}</g></svg>`);
(async()=>{await sharp(svg).png().toFile(path.join(d,`raised-v${v}-bones-layer.png`));await sharp(path.join(d,`sprite-base-v${v}-raised.png`)).composite([{input:svg}]).png().toFile(path.join(d,`raised-v${v}-bones-check.png`));console.log(JSON.stringify({v,plan:3,scale:c.scale,translation:c.translate,limbRefit:false}));})().catch(e=>{console.error(e);process.exit(1)});
