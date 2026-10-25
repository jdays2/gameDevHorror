const fs=require('fs'),path=require('path');
const sharp=require('C:/Users/Mark/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const d=__dirname,p=JSON.parse(fs.readFileSync(path.join(d,'raised-stance-plan-v2.json'))),m=JSON.parse(fs.readFileSync(path.join(d,'raised-stance-muscle-plan-v2.json'))),c=JSON.parse(fs.readFileSync(path.join(d,'raised-canonical-transform-v5.json')));
const esc=s=>String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;');
const text=(x,y,s,n=22,col='#e9eef4',weight=400)=>`<text x="${x}" y="${y}" font-size="${n}" font-weight="${weight}" font-family="Arial, sans-serif" fill="${col}">${esc(s)}</text>`;
const line=(a,col,w=3,dash=false)=>`<polyline points="${a.map(q=>q.join(',')).join(' ')}" fill="none" stroke="${col}" stroke-width="${w}" stroke-linejoin="round" stroke-linecap="round" ${dash?'stroke-dasharray="7 5"':''}/>`;
const dot=(q,col,r=4)=>`<circle cx="${q[0]}" cy="${q[1]}" r="${r}" fill="${col}" stroke="#17212b" stroke-width="1"/>`;
const top=a=>[600+250*a[0],512+250*a[1]];
const svg=(w,h,b)=>Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}"><defs><marker id="arr" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="5" markerHeight="5" orient="auto-start-reverse"><path d="M0 0L10 5L0 10Z" fill="#f5e7cb"/></marker></defs>${b}</svg>`);
function bones(pr,occlusion=true){let b=line([p.head.center,...p.spine].map(pr),'#f5cc6f',3);for(const f of p.fronts){b+=line([f.shoulder,f.elbow].map(pr),'#68ddeb',4,occlusion)+line([f.elbow,f.wrist,f.palm].map(pr),'#68ddeb',4);b+=[f.shoulder,f.elbow,f.wrist,f.palm].map(q=>dot(pr(q),'#68ddeb')).join('');for(const t of f.digits)b+=line([f.palm,t.proximal,t.distal,t.clawBase].map(pr),'#b1f1c4',2)+line([t.clawBase,t.clawTip].map(pr),'#fff0ad',2);}for(const r of p.rears){b+=line([r.hip,r.knee].map(pr),'#bf9ee9',3,occlusion)+line([r.knee,r.hock,r.palm].map(pr),'#bf9ee9',3);b+=[r.hip,r.knee,r.hock,r.palm].map(q=>dot(pr(q),'#bf9ee9')).join('');for(const t of r.digits)b+=line([r.palm,t.base,t.tip].map(pr),t.id===4?'#f7ac77':'#d1b7f5',2);}return b;}
function muscles(pr,labelsize=15){let b='';for(const g of m.groups){const a=pr(g.from),z=pr(g.to),mid=[(a[0]+z[0])/2,(a[1]+z[1])/2],len=Math.hypot(z[0]-a[0],z[1]-a[1]),ang=Math.atan2(z[1]-a[1],z[0]-a[0])*180/Math.PI;b+=`<ellipse cx="${mid[0]}" cy="${mid[1]}" rx="${Math.max(len/2,5)}" ry="${g.radius*250}" transform="rotate(${ang} ${mid[0]} ${mid[1]})" fill="${g.color}" fill-opacity="${g.occluded?.08:.25}" stroke="${g.color}" stroke-width="2" ${g.occluded?'stroke-dasharray="5 4"':''}/>`+line([a,z],g.color,2,g.occluded)+`<text x="${mid[0]}" y="${mid[1]}" font-family="Arial" font-size="${labelsize}" fill="white" stroke="#17212b" stroke-width="3" paint-order="stroke">${g.id}</text>`;}for(const t of m.tendons)b+=line(t.points.map(pr),'#f1eddd',2);return b;}
const body='<ellipse cx="437.5" cy="512" rx="112.5" ry="87.5" fill="#3f4855" stroke="#77818f" stroke-width="2"/><path d="M510 448Q563 385 655 420Q724 422 850 449L862 512L850 575Q724 602 655 604Q563 640 510 576Z" fill="#35404b" stroke="#77818f" stroke-width="2"/>';
async function run(){
 const layer=svg(1254,1254,`<g transform="translate(${c.translate[0]} ${c.translate[1]}) scale(${c.scale})">${muscles(top)}</g>`);
 await sharp(layer).png().toFile(path.join(d,'raised-v5-muscles-layer.png'));
 await sharp(path.join(d,'sprite-base-v5-raised.png')).composite([{input:layer}]).png().toFile(path.join(d,'raised-v5-muscles-check.png'));
 const validation={version:4,task:'TASK-005',art:'sprite-base-v5-raised.png',bonePlan:'raised-stance-plan-v2.json',musclePlan:'raised-stance-muscle-plan-v2.json',order:['unchanged prospective bones with one global camera registration','visible skin/functional-volume diagnostic only after bone findings','presentation map'],registrationFile:'raised-canonical-transform-v5.json',registration:{uniformScale:c.scale,translation:c.translate,rotationDegrees:0,limbRefit:false},overall:'PARTIAL — useful static design candidate, NOT full anatomy/pose PASS',bones:{limbs:'PASS: four connected visible limb chains; no extra limb or obvious extra wrist joint',front:'PASS topology: three straight claws per hand, one coherent carpal block; coarse forearm chain agrees',frontProjection:'PARTIAL: visible wrists/hand spread remain slightly farther outward than fixed plan',axis:'PASS coarse head-crown/neck/body axis and short caudal mass',raisedStance:'PARTIAL: compact/occluded upper chains improve raised reading, but top raster cannot prove the declared Z heights or ground load',rearProjection:'FAIL exact planned alignment: rear feet/toes rotate diagonally outward; unchanged left-directed toe rays cross leg tissue; rear contact positions differ',rearDigits:'LIMITED: two large primary claws + one lateral are clear on each foot; proposed third main toe is substantially occluded, 3+1 not independently established with confidence'},muscles:{status:'DIAGNOSTIC / NOT full PASS because rear bone registration diverges',front:'Visible forearm masses flow toward one wrist and three digit roots; no obvious new free muscle block or detached hand',trunk:'Paired longitudinal and flank skin relief is broadly compatible with declared functional directions; visible texture is not an individual muscle count',rear:'Visible leg/foot tissue is connected, but RU3/RU4 and RL3/RL4 axes diverge from fixed plan with rotated rear feet; no insertion match claimed',counts:'Plan only: 5 functional aggregates per front limb, 4 per rear limb, 4 trunk =22, plus3 digital tendon routes per front; these are NOT a verified count of anatomical muscles',hidden:'True3Dsegment lengths, individual origins/insertions, occluded muscles, tendon wrapping and exact sole height unverified'},presentation:{map:'body-map-raised-v2.png',allowed:'Show as one static concept plus diagnostic map with rear mismatch marked',notAllowed:'Do not label rig-ready, animation-ready or fully anatomically matched'},remainingPriority:['Clarify/align rear paw direction and main3+1 digit visibility against unchanged prospective plan','Preserve current front three claws and single carpal block while making any future correction']};
 validation.version=5;
 validation.overall='LIMITED GO for one static concept and visible topology; exact projected skeleton alignment PARTIAL, hidden anatomy unverified';
 validation.bones.rearProjection='PARTIAL: outward rotation fixed; all3main digits face left, but actual rear foot/short leg remains more compact and farther outward than prospective guide. Planned knee lies over digit tissue, so exact chain/sole correspondence NOT passed.';
 validation.bones.rearDigits='PASS visible count: three separated main digits plus one lateral on each rear foot. No extra fifth digit.';
 validation.muscles.status='LIMITED visual diagnostic: topology permits visible aggregate review; exact rear muscle-to-bone correspondence remains unpassed';
 validation.muscles.rear='Continuous skin links body to rear leg and one3+1foot; no detached digit or isolated muscle block. Planned RU3/RU4 and RL3/RL4 positions still diverge with the remaining projected rear-chain offset; hidden insertions not inferred.';
 validation.presentation.allowed='Show v5 as one static design candidate with current map and explicit residual rear-chain mismatch';
 validation.remainingPriority=['If exact prospective-pose conformity is required later: align rear short-leg/foot positions, not only toe direction; keep guide unchanged','Preserve current four limbs, front3 straight claws and rear3+1 visible digits. No animation/rig readiness claimed'];
 fs.writeFileSync(path.join(d,'validation-v5.json'),JSON.stringify(validation,null,2));
 let b='<rect width="1800" height="1200" fill="#151c25"/><path d="M600 70V1180M1200 70V1180" stroke="#465362"/>';
 b+=text(28,42,'КАРТА ТЕЛОСЛОЖЕНИЯ · ПРИПОДНЯТАЯ ЧЕТВЕРОНОГАЯ СТОЙКА',29,'#fff',700);
 b+=text(28,84,'1. Заданный пространственный каркас',23,'#b5e5ed',700);
 b+=text(28,118,'Сверху · скрытое — пунктир',20);
 b+=`<g transform="translate(-95 -80) scale(.69)">${body}${bones(top)}</g>`;
 b+=text(28,436,'Сбоку · Z вверх, опоры на земле',20);
 const side=a=>[305+a[0]*175,665-a[2]*150];
 b+=line([[35,665],[570,665]],'#758591',2)+bones(side,false)+text(35,695,'Схема высоты; не измерение кожи',18,'#c5cdd6');
 b+=text(28,740,'Спереди · 4 опорные подошвы Z=0',20);
 const front=a=>[300+a[1]*220,1030-a[2]*185];
 b+=line([[35,1030],[570,1030]],'#758591',2)+bones(front,false);
 b+=text(28,1080,'Плечо: 3D 0,687 / сверху 0,224',19,'#d1d9e3');
 b+=text(28,1111,'Длина уходит в Z, а не исчезает.',19,'#d1d9e3');
 b+=text(628,84,'2. Функциональные объёмы · сверху',23,'#ddcfb1',700);
 b+=text(628,118,'План под цельной кожей, не атлас',20);
 b+=`<g transform="translate(505 -80) scale(.69)">${body}${muscles(top,18)}</g>`;
 b+=text(628,436,'Сравнение с v5 · один общий transform',20);
 const art=await sharp(path.join(d,'raised-v5-muscles-check.png')).resize(550,550).flatten({background:'#252e39'}).png().toBuffer();
 b+=`<image x="625" y="450" width="550" height="550" href="data:image/png;base64,${art.toString('base64')}"/>`;
 b+=text(628,1040,'Задние пальцы: 3+1, направлены влево.',21,'#ffb5a6',700);
 b+=text(628,1073,'Точная задняя цепь: совпадение частичное.',21,'#ffb5a6');
 b+=text(628,1111,'Ось from→to задаёт направление;',18,'#c5cdd6');
 b+=text(628,1139,'скрытые прикрепления не восстановлены.',18,'#c5cdd6');
 b+=text(1228,84,'3. ID, состав и ограничения',23,'#e8d6e9',700);
 const rows=[
 ['FU / FL — передняя верхняя / нижняя',22,'#68ddeb',700],
 ['1 — плечевой подвес',21],['2 — сгибатель верхнего сегмента',21],['3 — разгибатель верхнего сегмента',21],['4 — пальцевой привод предплечья',21],['5 — стабилизатор единого запястья',21],
 ['FUT1–3 / FLT1–3 — 3 сухожилия',21,'#f1eddd'],['Запястье → 3 пальца → основы когтей',20,'#c5cdd6'],
 ['RU / RL — задняя верхняя / нижняя',22,'#bf9ee9',700],['1 — опорный разгибатель бедра',21],['2 — сгибатель бедра',21],['3 — привод голени / скакательного участка',20],['4 — стабилизатор стопы: 3+1 пальца',21],
 ['T1/T2 — продольные спина / шея',21,'#b9c88c'],['T3/T4 — плечево-боковые стабилизаторы',20,'#d1ab87'],
 ['Итого: 5×2 + 4×2 + 4 = 22 агрегата',22,'#fff',700],['6 передних сухожильных маршрутов',21,'#fff'],
 ['ID = функциональный блок,',21,'#f3d39b',700],['а не отдельная доказанная мышца.',21,'#f3d39b'],
 ['Скрытые длины / крепления неизвестны.',20,'#c5cdd6'],['Нагрузка / риг / анимация не проверены.',20,'#c5cdd6'],
 ['v5: пальцы влево, число 3+1 читается.',21,'#ffb5a6',700],['Задний сгиб / опора смещены от плана.',19,'#ffb5a6'],['Передние: 3 прямых когтя, единый карпус.',20,'#b5e5ed'],
 ['Схему НЕ подгоняли под лапы изображения.',20,'#fff',700]
 ];
 let y=140;for(const [s,n,col,w]of rows){b+=text(1228,y,s,n,col||'#e9eef4',w||400);y+=38;}
 await sharp(svg(1800,1200,b)).png().toFile(path.join(d,'body-map-raised-v2.png'));
 console.log(JSON.stringify({files:['raised-v5-muscles-check.png','validation-v5.json','body-map-raised-v2.png'],status:validation.overall}));
}
run().catch(e=>{console.error(e);process.exit(1)});
