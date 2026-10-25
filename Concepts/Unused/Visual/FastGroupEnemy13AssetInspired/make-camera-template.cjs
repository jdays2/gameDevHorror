const fs=require('fs'),path=require('path');
const sharp=require('C:/Users/Mark/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
(async()=>{
 const d=__dirname, source=path.join(d,'base-v3.png');
 const whole=await sharp(source).resize(401,401,{fit:'fill'}).png().toBuffer();
 const layers=Array.from({length:6},(_,i)=>({input:whole,left:i%3*512+55,top:Math.floor(i/3)*512+55}));
 await sharp({create:{width:1536,height:1024,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite(layers).png().toFile(path.join(d,'neutral-camera-template.png'));
 fs.writeFileSync(path.join(d,'neutral-camera-template.json'),JSON.stringify({purpose:'TECHNICAL CAMERA REFERENCE ONLY. Six unchanged WHOLE source images, no limb compositing, posing or animation manufactured here.',source:'base-v3.png',canvas:[1536,1024],grid:[3,2],cell:[512,512],sourceCanvas:[1254,1254],wholeImageSize:[401,401],wholeImageOffset:[55,55],planNominalScale:0.32,planNominalOffset:55.36},null,2)+'\n');
})();
