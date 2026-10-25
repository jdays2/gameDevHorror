const fs=require('fs');
const path=require('path');
const sharp=require('C:/Users/Mark/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const d=__dirname;
const phases=[
 {id:0,name:'paired plant and compression',footX:475,headX:220,rearX:656,spineY:256,delay:180},
 {id:1,name:'paired push; body pulled',footX:510,headX:212,rearX:670,spineY:252,delay:110},
 {id:2,name:'paired thrust completion; brief extension',footX:542,headX:204,rearX:682,spineY:256,delay:110},
 {id:3,name:'paired rapid recovery and compression',footX:493,headX:218,rearX:657,spineY:260,delay:140}
];
for(const f of phases){
 f.root=[505,256];
 f.bodyAxis=[[f.headX,256],[350,256],[505,f.spineY],[f.rearX,256]];
 f.feet=[-1,1].map((side,i)=>{
  const y=256+side*126;
  const palm=[f.footX,y],ankle=[f.footX+30,y-side*15];
  const hip=[510,256+side*70],knee=[510,256+side*92];
  const digits=[-1,0,1].map((n,j)=>({id:`${i?'L':'U'}-digit-${j+1}`,base:[palm[0]-14,palm[1]+n*18],tip:[palm[0]-38,palm[1]+n*20]}));
  return{side:i?'lower':'upper',hip,knee,ankle,palm,digits,envelope:{center:palm,radius:[51,40]},visible:'palm and three claws; hip/knee/most ankle hidden under body; dashed chain is only projected topology'};
 });
}
const template={version:1,createdBeforeGeneration:true,source:'character-source-v5.png',canvas:[1536,1024],cell:[768,512],pivot:[505,256],camera:'strict overhead orthographic; facing LEFT; no camera yaw, scaling, framing or vertical human sway',stance:'standing on two weight-bearing legs under raised torso. Visible feet are broad, short and spread; long horizontal arms forbidden.',cycle:'almost paired impulses, never human left/right opposition',topology:{legs:2,digitsPerFoot:3,claw:'short curved',jawRowsPerJaw:1,horns:2,eyes:2,muscleGroupsPerLeg:8},limits:{hidden3DLengthValidation:'NOT POSSIBLE FROM TOP SPRITE; no recovered 3D-length claim',pairFootXDifference:12,footCenterEnvelopeRadius:[45,35],bodyPivotTolerance:18,rigidHeadScaleToleranceFraction:0.06,minimumPhase0to2PairedFootExcursion:25,notes:'These are predetermined review limits in 768×512 cell coordinates; not adjusted to generated errors. Soft outline / surface folding can vary mildly. Bone gate precedes muscle artwork pass.'},muscleGroups:[['M1','hip/proximal cap; mostly occluded'],['M2','proximal flexor; mostly occluded'],['M3','proximal extensor; mostly occluded'],['M4','distal flexor; mostly occluded'],['M5','distal extensor; mostly occluded'],['M6','digit 1 bundle'],['M7','digit 2 bundle'],['M8','digit 3 bundle']],phases};
fs.writeFileSync(path.join(d,'phase-anatomy-v1.json'),JSON.stringify(template,null,2));
const line=(p,c,w=3,dash='')=>`<polyline points="${p.map(q=>q.join(',')).join(' ')}" fill="none" stroke="${c}" stroke-width="${w}" stroke-linecap="round" stroke-linejoin="round" ${dash?'stroke-dasharray="'+dash+'"':''}/>`;
const joints=p=>p.map(q=>`<circle cx="${q[0]}" cy="${q[1]}" r="4" fill="#ffcb57"/>`).join('');
const bones=f=>line(f.bodyAxis,'#ffda69')+f.feet.map(l=>line([f.root,l.hip,l.knee,l.ankle],'#49e4ff',2,'6 5')+line([l.ankle,l.palm],'#49e4ff',3)+l.digits.map(t=>line([l.palm,t.base,t.tip],'#b7f78e',3)).join('')+joints([l.hip,l.knee,l.ankle,l.palm])).join('');
const stencil=f=>`<g fill="#8e9199"><path d="M${f.headX+50} 210 Q350 120 505 145 Q620 155 ${f.rearX} 256 Q620 360 505 367 Q350 392 ${f.headX+50} 302Z"/><ellipse cx="${f.headX}" cy="256" rx="109" ry="98"/>${f.feet.map(l=>`<ellipse cx="${l.palm[0]}" cy="${l.palm[1]}" rx="51" ry="40"/>`).join('')}</g><ellipse cx="${f.headX-12}" cy="256" rx="85" ry="80" fill="#404147"/>`;
const all=(render)=>phases.map((f,i)=>`<g transform="translate(${i%2*768},${Math.floor(i/2)*512})">${render(f)}</g>`).join('');
const wrap=inner=>Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="1536" height="1024">${inner}</svg>`);
(async()=>{
 await sharp(wrap(all(bones))).png().toFile(path.join(d,'paired-bones-layer.png'));
 await sharp(wrap('<rect width="1536" height="1024" fill="#303338"/>'+all(f=>stencil(f)+bones(f)))).png().toFile(path.join(d,'paired-phase-guide.png'));
 console.log('Independent four-phase plan saved before image generation.');
})();
