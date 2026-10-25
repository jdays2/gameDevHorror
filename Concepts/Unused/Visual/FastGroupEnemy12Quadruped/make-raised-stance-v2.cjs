const fs=require('node:fs'),path=require('node:path');
const sharp=require('C:/Users/Mark/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const d=__dirname;
const sideName=s=>s<0?'screenUpper':'screenLower';
const plan={version:2,task:'TASK-005',status:'Prospective raised quadruped stance, fixed BEFORE redraw. This is a design proposal, not recovered3Dbones from a raster.',axes:{x:'forward is negative',y:'screenUpper is negative',z:'up, ground0'},units:'conceptual units',head:{center:[-.65,0,1.05],topRadii:[.45,.35]},spine:[[-.18,0,1.08],[0,0,1.10],[.4,0,1.15],[.85,0,.95]],fronts:[],rears:[],counts:{limbs:4,frontDigits:3,rearDigits:4,rearSupport:'3main+1lateral'},projection:{canvas:[1024,1024],scale:250,origin:[600,512],camera:'orthographic top-down, rays parallel to negativeZ'},limits:{trueSkinHeight:'Not measured. Bone heights are declared design choices.',occlusion:'Upper limb/thigh portions beneath torso, marked dashed in top guide; do not shorten the actual declared3Dbones.',individualMuscles:'Not reconstructed; functional groups will be declared separately.'}};
for(const s of [-1,1]){
 const f={side:sideName(s),shoulder:[0,s*.35,1.10],elbow:[-.10,s*.55,.45],wrist:[-.68,s*.60,.12],palm:[-.86,s*.62,.05],digits:[]};
 for(const [id,dy] of [[1,.20],[2,0],[3,-.20]])f.digits.push({id,proximal:[-1.00,s*(.62+dy*.7),.04],distal:[-1.17,s*(.62+dy*.9),.02],clawBase:[-1.22,s*(.62+dy),.01],clawTip:[-1.60,s*(.62+dy+Math.sign(dy)*.015),.02]});
 plan.fronts.push(f);
 const r={side:sideName(s),hip:[.85,s*.25,.90],knee:[.75,s*.47,.40],hock:[1.03,s*.36,.15],palm:[.95,s*.45,0],digits:[]};
 for(const [id,dy] of [[1,.11],[2,0],[3,-.11]])r.digits.push({id,base:[.84,s*(.45+dy*.65),0],tip:[.63,s*(.45+dy),0],role:'main support'});
 r.digits.push({id:4,base:[1.00,s*.39,.05],tip:[1.13,s*.29,.05],role:'lateral, not primary ground contact'});
 plan.rears.push(r);
}
const dist=(a,b)=>Math.hypot(...a.map((n,i)=>n-b[i]));
const p2=(p,view)=>view==='top'?[600+p[0]*250,512+p[1]*250]:view==='side'?[600+p[0]*250,430-p[2]*220]:[512+p[1]*300,430-p[2]*220];
const line=(points,view,color,w=6,dashed=false)=>`<polyline points="${points.map(p=>p2(p,view).join(',')).join(' ')}" fill="none" stroke="${color}" stroke-width="${w}" stroke-linecap="round" stroke-linejoin="round" ${dashed?'stroke-dasharray="10 8"':''}/>`;
const dot=(p,view,color)=>{const [x,y]=p2(p,view);return `<circle cx="${x}" cy="${y}" r="6" fill="${color}"/>`;};
const bones=view=>{
 let b=line([plan.head.center,...plan.spine],view,'#f5cc6f',4);
 for(const f of plan.fronts){b+=line([f.shoulder,f.elbow],view,'#68ddeb',6,view==='top');b+=line([f.elbow,f.wrist,f.palm],view,'#68ddeb');for(const j of [f.shoulder,f.elbow,f.wrist])b+=dot(j,view,'#f5cc6f');for(const q of f.digits)b+=line([f.palm,q.proximal,q.distal,q.clawBase],view,'#a5ecbc',3)+line([q.clawBase,q.clawTip],view,'#fff0ad',4);}
 for(const r of plan.rears){b+=line([r.hip,r.knee],view,'#bc9ce9',5,view==='top');b+=line([r.knee,r.hock,r.palm],view,'#bc9ce9',5);for(const j of [r.hip,r.knee,r.hock])b+=dot(j,view,'#f5cc6f');for(const q of r.digits)b+=line([r.palm,q.base,q.tip],view,q.id===4?'#f4a577':'#cfb7f5',3);}
 return b;
};
const topMass='<path d="M510 448Q563 385 655 420Q724 422 850 449L862 512L850 575Q724 602 655 604Q563 640 510 576Z" fill="#666c77"/><ellipse cx="437.5" cy="512" rx="112.5" ry="87.5" fill="#7b7f86"/>';
const top=`<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="1024"><rect width="1024" height="1024" fill="#282b30"/>${topMass}${bones('top')}</svg>`;
const profile=view=>`<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="512"><rect width="1024" height="512" fill="#282b30"/><line x1="60" y1="430" x2="960" y2="430" stroke="#858b97" stroke-width="2"/><text x="35" y="40" fill="#fff" font-size="23">${view==='side'?'ВИД СБОКУ — ВЫСОТА КОРПУСА':'ВИД СПЕРЕДИ — ОПОРЫ ПОД ТЕЛОМ'}</text>${bones(view)}<text x="35" y="480" fill="#aeb6c5" font-size="19">Пунктир сверху = часть кости скрыта телом, а не отсутствует</text></svg>`;
(async()=>{
 const metrics=plan.fronts.map(f=>({side:f.side,upper3D:dist(f.shoulder,f.elbow),upperTop:Math.hypot(f.shoulder[0]-f.elbow[0],f.shoulder[1]-f.elbow[1]),forearm3D:dist(f.elbow,f.wrist),palmHeight:f.palm[2]}));
 plan.contactPatches={fronts:plan.fronts.map(f=>({side:f.side,sole:[f.palm[0],f.palm[1],0],note:'Bone/palm pointZ.05 is above the skin sole contact; phalanx/claw points also lie inside their material thickness.'})),rears:plan.rears.map(r=>({side:r.side,sole:[r.palm[0],r.palm[1],0]}))};
 plan.segmentChecks={fronts:metrics,symmetry:'same3Dcoordinates mirrored aboutY=0; corresponding segment lengths equal by construction',contact:'all4sole contactsZ0; toe bones may lie slightly above the surface inside skin/material; torsoZ.95..1.15',support:'body axisY0lies between four main foot polygons; fore and rear contacts bracket trunkX'};
 fs.writeFileSync(path.join(d,'raised-stance-plan-v2.json'),JSON.stringify(plan,null,2)+'\n');
 await sharp(Buffer.from(top)).png().toFile(path.join(d,'raised-stance-top-control-v2.png'));
 await sharp(Buffer.from(profile('side'))).png().toFile(path.join(d,'raised-stance-side-v2.png'));
 await sharp(Buffer.from(profile('front'))).png().toFile(path.join(d,'raised-stance-front-v2.png'));
 const t=await sharp(Buffer.from(top)).png().toBuffer(),s=await sharp(Buffer.from(profile('side'))).png().toBuffer(),f=await sharp(Buffer.from(profile('front'))).png().toBuffer();
 await sharp({create:{width:2048,height:1024,channels:4,background:'#282b30'}}).composite([{input:t,left:0,top:0},{input:s,left:1024,top:0},{input:f,left:1024,top:512}]).png().toFile(path.join(d,'raised-stance-projections-v2.png'));
 console.log(JSON.stringify({frontMetrics:metrics,plan:'raised-stance-plan-v2.json',topControl:'raised-stance-top-control-v2.png'}));
})();
