// Offline layout renders; browser only, no Unity or external requests.
const fs=require('node:fs'),path=require('node:path');
const {chromium}=require('C:/Users/Mark/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const {pathToFileURL}=require('node:url');
(async()=>{
 const browser=await chromium.launch({headless:true});const page=await browser.newPage();const results=[];
 for(const [width,height] of [[1280,720],[1920,1080],[2560,1440],[2560,1080],[1024,768]]){
  await page.setViewportSize({width,height});
  for(const variant of ['a','b'])for(const state of ['fresh','checkpoint','loading','error','exit']){
   await page.goto(pathToFileURL(path.join(__dirname,'mockup.html')).href+`?variant=${variant}&state=${state}`);await page.evaluate(()=>document.fonts.ready);
   const evidence=await page.evaluate(()=>window.layoutEvidence());results.push({width,height,variant,state,evidence});
   if(state==='fresh'||(width===1280&&height===720))await page.screenshot({path:path.join(__dirname,`Previews/${variant}-${state}-${width}x${height}.png`)});
  }
 }
 await page.setViewportSize({width:1280,height:720});
 for(const variant of ['a','b'])for(const state of ['fresh','checkpoint','loading','error','exit']){await page.goto(pathToFileURL(path.join(__dirname,'mockup.html')).href+`?variant=${variant}&state=${state}&large=1`);await page.evaluate(()=>document.fonts.ready);results.push({width:1280,height:720,variant,state:state+'-125percent',evidence:await page.evaluate(()=>window.layoutEvidence())});if(state==='fresh')await page.screenshot({path:path.join(__dirname,`Previews/${variant}-text125-1280x720.png`)})}
 await page.goto(pathToFileURL(path.join(__dirname,'mockup.html')).href+'?variant=a&focus=exit');await page.evaluate(()=>document.fonts.ready);await page.screenshot({path:path.join(__dirname,'Previews/a-focus-exit-1280x720.png')});
 fs.writeFileSync(path.join(__dirname,'layout-validation.json'),JSON.stringify({scope:'Offline HTML mockup; not Unity rendering, runtime or game support certification',results},null,2)+'\n');
 const jsErrors=[];page.on('pageerror',e=>jsErrors.push(e.message));
 await page.goto(pathToFileURL(path.join(__dirname,'review.html')).href);await page.getByText('Повторить появление',{exact:true}).click();await page.waitForTimeout(1000);
 const child=page.frames().find(f=>f.url().includes('mockup.html'));
 const animationVisible=await child.evaluate(()=>getComputedStyle(document.querySelector('#stage')).opacity!=='0');
 await page.selectOption('#variant','b');await page.selectOption('#state','error');await page.waitForTimeout(150);
 await page.frames().find(f=>f.url().includes('mockup.html')).getByText('С начала сцены',{exact:true}).click();await page.waitForTimeout(100);
 const intent=await page.locator('#notice').innerText();
 await page.goto(pathToFileURL(path.join(__dirname,'mockup.html')).href);await page.keyboard.press('ArrowDown');const active=await page.locator('button.active').innerText();
 const linear=v=>{v/=255;return v<=.04045?v/12.92:((v+.055)/1.055)**2.4};const lum=rgb=>rgb.map(linear).reduce((s,x,i)=>s+x*[.2126,.7152,.0722][i],0);const contrast=(a,b)=>(lum(a)+.05)/(lum(b)+.05);
 const conservativeB=[8,10,12].map(v=>.65*v+.35*(.55*255+.45*13));
 const interaction={scope:'Local review HTML only; no gameplay actions',jsErrors,animationVisible,freshIntentSingle: intent.includes('fresh-confirm'),keyboardDownSelectsExit:active==='Выйти из игры',contrast:{method:'Relative sRGB luminance; B uses conservative all-white source with minimum veil65% and image55%',secondaryOnA:contrast([167,171,170],[13,13,12]),secondaryOnWorstB:contrast([167,171,170],conservativeB),activeOnWorstB:contrast([245,245,242],conservativeB)}};
 fs.writeFileSync(path.join(__dirname,'interaction-validation.json'),JSON.stringify(interaction,null,2)+'\n');
 if(jsErrors.length||!animationVisible||!interaction.freshIntentSingle||!interaction.keyboardDownSelectsExit)throw new Error(JSON.stringify(interaction));
 console.log(JSON.stringify({renders:results.length,failures:results.flatMap(r=>r.evidence.filter(e=>!e.visible||e.overflow).map(e=>({width:r.width,height:r.height,variant:r.variant,state:r.state,...e})))}));await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
