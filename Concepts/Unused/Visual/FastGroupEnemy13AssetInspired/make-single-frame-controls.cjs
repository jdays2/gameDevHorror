const fs=require('fs'),path=require('path');
const sharp=require('C:/Users/Mark/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
(async()=>{
 const d=__dirname,out=path.join(d,'FrameControls');fs.mkdirSync(out,{recursive:true});
 const art=await sharp(path.join(d,'base-v3.png')).resize(802,802).png().toBuffer();
 await sharp({create:{width:1024,height:1024,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite([{input:art,left:110,top:110}]).png().toFile(path.join(out,'neutral-1024.png'));
 for(const kind of ['run','attack'])for(let i=0;i<6;i++)await sharp(path.join(d,kind+'-phase-guide-v4.png')).extract({left:i%3*512,top:Math.floor(i/3)*512,width:512,height:512}).resize(1024,1024).png().toFile(path.join(out,`${kind}-${i.toString().padStart(2,'0')}-guide.png`));
 fs.writeFileSync(path.join(out,'method.json'),JSON.stringify({purpose:'Technical reference framing only; poses are not made by transforming artwork.',wholeSource:'base-v3.png',sourceCanvas:[1254,1254],canvas:[1024,1024],resize:[802,802],offset:[110,110],nominalPlanTransform:{scale:.64,translate:[110.72,110.72]},controls:'Whole diagram cell extraction only',artOutput:'Direct image_gen per complete frame; whole-frame equal packing only'},null,2)+'\n');
})();
