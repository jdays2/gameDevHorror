const fs=require('fs');
const path=require('path');
const crypto=require('crypto');
const sharp=require('C:/Users/Mark/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const root=__dirname;
async function main(){
  const version=process.argv[2]||'v4';
  const file=path.join(root,version==='v5'?'standing-v5-wide-feet.png':`standing-${version}.png`);
  const {data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let edge=0,count=0,minX=info.width,minY=info.height,maxX=-1,maxY=-1;
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++){
    const alpha=data[(y*info.width+x)*4+3];
    if(alpha>8){count++;minX=Math.min(minX,x);minY=Math.min(minY,y);maxX=Math.max(maxX,x);maxY=Math.max(maxY,y);if(x===0||y===0||x===info.width-1||y===info.height-1)edge++;}
  }
  const report={canvas:info,alphaThreshold:8,opaqueCount:count,bounds:{minX,minY,maxX,maxY},borderPixels:edge,sha256:crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex')};
  fs.writeFileSync(path.join(root,`technical-validation-${version}.json`),JSON.stringify(report,null,2));
  const preview=await sharp(file).resize(600,400).flatten({background:'#292c32'}).png().toBuffer();
  const light=await sharp(file).resize(600,400).flatten({background:'#e2e0db'}).png().toBuffer();
  const game=await sharp(file).resize(180,120).flatten({background:'#292c32'}).png().toBuffer();
  await sharp({create:{width:1200,height:560,channels:4,background:'#292c32'}})
    .composite([{input:preview,left:0,top:0},{input:light,left:600,top:0},{input:game,left:510,top:430}])
    .png().toFile(path.join(root,`standing-${version}-rgba-review.png`));
  console.log(JSON.stringify({width:info.width,height:info.height,edge,bounds:report.bounds,sha256:report.sha256}));
}
main().catch(error=>{console.error(error);process.exit(1);});
