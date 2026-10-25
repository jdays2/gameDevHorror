const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('C:/Users/Mark/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const d=__dirname,file=process.argv[2]||'sprite-base-v4-raised.png',tag=file.replace(/\.png$/,'');
(async()=>{
 const f=path.join(d,file),m=await sharp(f).metadata(),p=await sharp(f).ensureAlpha().raw().toBuffer();
 let border=0,minX=m.width,minY=m.height,maxX=-1,maxY=-1;
 for(let y=0;y<m.height;y++)for(let x=0;x<m.width;x++)if(p[(y*m.width+x)*4+3]>8){minX=Math.min(minX,x);minY=Math.min(minY,y);maxX=Math.max(maxX,x);maxY=Math.max(maxY,y);if(x===0||y===0||x===m.width-1||y===m.height-1)border++;}
 const comps=[];
 for(let i=0;i<2;i++)comps.push({input:await sharp(f).resize(540,540,{fit:'contain',background:{r:0,g:0,b:0,alpha:0}}).png().toBuffer(),left:i*540,top:0});
 const bg=Buffer.from('<svg xmlns="http://www.w3.org/2000/svg" width="1080" height="760"><rect width="540" height="540" fill="#e8e7e3"/><rect x="540" width="540" height="540" fill="#282b30"/><rect y="540" width="1080" height="220" fill="#282b30"/></svg>');
 comps.push({input:await sharp(f).resize(200,200).png().toBuffer(),left:440,top:550});
 await sharp(bg).composite(comps).png().toFile(path.join(d,tag+'-rgba-review.png'));
 if(m.width>=1200&&m.height>=1100)await sharp(f).extract({left:870,top:280,width:Math.min(380,m.width-870),height:720}).flatten({background:'#e8e7e3'}).png().toFile(path.join(d,tag+'-rear-detail.png'));
 const planVersion=/sprite-base-v[67]-/.test(file)?3:2;
 const report={file,width:m.width,height:m.height,hasAlpha:m.hasAlpha,bounds:[minX,minY,maxX,maxY],borderPixels:border,sha256:crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex'),technicalScope:'RGBA/framing only. Bone/muscle/projection validation is separate. No anatomy truth from alpha coverage.',anatomyPlan:'raised-stance-plan-v'+planVersion+'.json',musclePlan:'raised-stance-muscle-plan-v'+planVersion+'.json',animation:false};
 fs.writeFileSync(path.join(d,tag+'-technical.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
})();
