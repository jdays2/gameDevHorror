// Offline browser verification of design artifacts; does not control Unity.
const fs=require('node:fs'),path=require('node:path');
const {pathToFileURL}=require('node:url');
const {chromium}=require('C:/Users/Mark/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
(async()=>{
 const browser=await chromium.launch({headless:true});const page=await browser.newPage();const errors=[];page.on('pageerror',e=>errors.push(e.message));const results=[];
 for(const [width,height] of [[1280,720],[1920,1080],[2560,1440],[2560,1080],[1024,768]]){
  await page.setViewportSize({width,height});
  for(const variant of ['c','d','e','f'])for(const state of ['fresh','checkpoint','loading','error','exit']){
   await page.goto(pathToFileURL(path.join(__dirname,'mockup.html')).href+`?variant=${variant}&state=${state}`);await page.evaluate(()=>document.fonts.ready);
   results.push({width,height,variant,state,evidence:await page.evaluate(()=>window.evidence())});
   if(state==='fresh'||(width===1280&&height===720&&state==='error'))await page.screenshot({path:path.join(__dirname,`Previews/${variant}-${state}-${width}x${height}.png`)});
  }
 }
 await page.setViewportSize({width:1280,height:720});
 for(const variant of ['c','d','e','f'])for(const state of ['fresh','checkpoint','loading','error','exit']){
  await page.goto(pathToFileURL(path.join(__dirname,'mockup.html')).href+`?variant=${variant}&state=${state}&large=1`);await page.evaluate(()=>document.fonts.ready);
  results.push({width:1280,height:720,variant,state:state+'-125percent',evidence:await page.evaluate(()=>window.evidence())});
  if(state==='fresh')await page.screenshot({path:path.join(__dirname,`Previews/${variant}-text125-1280x720.png`)});
 }
 await page.setViewportSize({width:1600,height:1160});await page.goto(pathToFileURL(path.join(__dirname,'board.html')).href);await Promise.all(page.frames().map(f=>f.evaluate(()=>document.fonts.ready)));await page.screenshot({path:path.join(__dirname,'Previews/four-concepts.png'),fullPage:true});
 await page.setViewportSize({width:1280,height:950});await page.goto(pathToFileURL(path.join(__dirname,'review.html')).href);await page.selectOption('#variant','d');await page.getByText('Появление',{exact:true}).click();await page.waitForTimeout(900);
 let frame=page.frames().find(f=>f.url().includes('mockup.html'));const appearing=await frame.evaluate(()=>getComputedStyle(document.querySelector('#stage')).opacity!=='0');
 await frame.getByText('Повторить попытку',{exact:true}).click();await page.waitForTimeout(80);const intent=await page.locator('#notice').innerText();
 const mediaCounts=await page.evaluate(()=>({audio:document.querySelectorAll('audio').length,video:document.querySelectorAll('video').length}));
 const failures=results.flatMap(r=>r.evidence.filter(e=>e.overflow||!e.inBounds).map(e=>({width:r.width,height:r.height,variant:r.variant,state:r.state,...e})));
 const out={scope:'HTML/CSS concept rendering only; no Unity/GameView claim',music:'none',results,failures,interaction:{jsErrors:errors,appearing,intentReceived:intent.includes('Повторить попытку'),mediaCounts}};
 fs.writeFileSync(path.join(__dirname,'validation.json'),JSON.stringify(out,null,2)+'\n');await browser.close();console.log(JSON.stringify({renders:results.length,failures,interaction:out.interaction}));if(failures.length||errors.length||!appearing||!out.interaction.intentReceived)process.exit(1);
})().catch(e=>{console.error(e);process.exit(1)});
