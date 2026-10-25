const fs=require('fs');
const path=require('path');
const sharp=require('C:/Users/Mark/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const root=__dirname;
async function main(){
  const guide=JSON.parse(fs.readFileSync(path.join(root,'standing-projection-v1.json'),'utf8'));
  const lines=guide.edges.map(([a,b])=>`<path d="M${guide.points[a]} L${guide.points[b]}"/>`).join('');
  const joints=Object.values(guide.points).map(([x,y])=>`<circle cx="${x}" cy="${y}" r="9"/>`).join('');
  const skeleton=Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="1536" height="1024"><g fill="none" stroke="#20d6f0" stroke-width="7" stroke-linecap="round">${lines}</g><g fill="#ffad22">${joints}</g></svg>`);
  await sharp(skeleton).png().toFile(path.join(root,'standing-bones-layer.png'));
  await sharp(path.join(root,'standing-v1.png')).flatten({background:'#27292d'}).composite([{input:skeleton}]).png().toFile(path.join(root,'standing-v1-bone-check.png'));
  const target=Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="1536" height="1024"><rect width="1536" height="1024" fill="#303238"/><path d="M410 512 C470 330 650 270 890 295 C1120 270 1300 360 1400 510 C1280 680 1090 740 880 720 C650 760 460 685 410 512Z" fill="#767980" opacity=".55"/><ellipse cx="350" cy="512" rx="205" ry="185" fill="#4d4f56"/><g fill="#ff4a4a" fill-opacity=".22" stroke="#ff7777" stroke-width="5"><ellipse cx="945" cy="265" rx="100" ry="75"/><ellipse cx="945" cy="759" rx="100" ry="75"/></g><g fill="none" stroke="#20d6f0" stroke-width="7" stroke-linecap="round">${lines}</g><g fill="#ffad22">${joints}</g></svg>`);
  await sharp(target).png().toFile(path.join(root,'standing-projection-guide.png'));
}
main().catch(error=>{console.error(error);process.exit(1);});
