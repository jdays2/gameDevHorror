const fs=require('node:fs'),path=require('node:path'),sharp=require('C:/Users/Mark/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const d=__dirname;
(async()=>{
 const files=['selected-03-reference.png','selected-03-long-claws.png'],comp=[],metadata=[];
 for(let i=0;i<files.length;i++){
  const f=path.join(d,files[i]),m=await sharp(f).metadata(),p=await sharp(f).ensureAlpha().raw().toBuffer();
  let border=0,minX=m.width,minY=m.height,maxX=-1,maxY=-1;
  for(let y=0;y<m.height;y++)for(let x=0;x<m.width;x++)if(p[(y*m.width+x)*4+3]>8){minX=Math.min(minX,x);minY=Math.min(minY,y);maxX=Math.max(maxX,x);maxY=Math.max(maxY,y);if(x===0||y===0||x===m.width-1||y===m.height-1)border++;}
  metadata.push({file:files[i],width:m.width,height:m.height,hasAlpha:m.hasAlpha,bounds:[minX,minY,maxX,maxY],borderPixels:border});
  for(let bg=0;bg<2;bg++)comp.push({input:await sharp(f).resize({width:360,height:600,fit:'contain',background:{r:0,g:0,b:0,alpha:0}}).png().toBuffer(),left:(i+bg*2)*360,top:0});
 }
 const b=Buffer.from('<svg xmlns="http://www.w3.org/2000/svg" width="1440" height="600"><rect width="720" height="600" fill="#e8e7e3"/><rect x="720" width="720" height="600" fill="#282b30"/></svg>');
 await sharp(b).composite(comp).png().toFile(path.join(d,'selected-03-rgba-review.png'));
 fs.writeFileSync(path.join(d,'selected-03-technical.json'),JSON.stringify({metadata,reviewOrder:'reference/final on light; reference/final on dark',visualScope:'Two front paws each have3claws; inspect actual compositing and relative extension, no exact15percent measurement or full-anatomy claim.'},null,2)+'\n');
 console.log(JSON.stringify(metadata));
})();
