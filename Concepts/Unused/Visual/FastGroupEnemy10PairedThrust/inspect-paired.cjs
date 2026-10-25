const fs=require('fs'),path=require('path');
const sharp=require('C:/Users/Mark/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const d=__dirname,version=process.argv[2]||'v1';
(async()=>{
 const p=path.join(d,`paired-${version}-source.png`),m=await sharp(p).metadata();
 const cw=Math.floor(m.width/2),ch=Math.floor(m.height/2),scale=Math.min(cw/768,ch/512);
 const gw=Math.round(768*scale),gh=Math.round(512*scale),ox=Math.floor((cw-gw)/2),oy=Math.floor((ch-gh)/2);
 const inputs=[];
 for(let i=0;i<4;i++){
  const input=await sharp(path.join(d,'paired-bones-layer.png')).extract({left:i%2*768,top:Math.floor(i/2)*512,width:768,height:512}).resize(gw,gh).png().toBuffer();
  inputs.push({input,left:i%2*cw+ox,top:Math.floor(i/2)*ch+oy});
 }
 const over=path.join(d,`paired-${version}-01-bones.png`);
 await sharp(p).composite(inputs).png().toFile(over);
 const frames=[];
 for(let i=0;i<4;i++){
  const rect={left:i%2*cw,top:Math.floor(i/2)*ch,width:cw,height:ch};
  const fp=path.join(d,`paired-${version}-frame-${i}.png`);
  await sharp(p).extract(rect).png().toFile(fp);
  await sharp(over).extract(rect).png().toFile(path.join(d,`paired-${version}-frame-${i}-bones.png`));
  const {data}=await sharp(fp).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let minX=cw,minY=ch,maxX=-1,maxY=-1,nonzero=0,border=0;
  for(let y=0;y<ch;y++)for(let x=0;x<cw;x++){if(data[(y*cw+x)*4+3]>8){nonzero++;minX=Math.min(x,minX);minY=Math.min(y,minY);maxX=Math.max(x,maxX);maxY=Math.max(y,maxY);if(x===0||y===0||x===cw-1||y===ch-1)border++;}}
  frames.push({id:i,rect,bounds:[minX,minY,maxX,maxY],nonzero,borderPixels: border});
 }
 const r={version,dimensions:[m.width,m.height],cell:[cw,ch],hasAlpha:m.hasAlpha,guideUniformScale:scale,guideCenterOffset:[ox,oy],anatomyGuideRefit:false,frames,note:'RGBA and registration diagnostics only; no claim that alpha containment proves anatomical correctness.'};
 fs.writeFileSync(path.join(d,`paired-${version}-technical.json`),JSON.stringify(r,null,2));console.log(JSON.stringify(r));
})();
