const $ = id => document.getElementById(id);
const canvas = $('screen');
const ctx = canvas.getContext('2d');
const clips = {};
const images = new Map();
let time = 0;
let paused = false;
let previous = performance.now();
let override = null;

function url(path) {
  return '/production/' + path.split('/Art/Enemies/')[1];
}

async function loadImage(path) {
  if (images.has(path)) return images.get(path);
  const image = new Image();
  image.src = path;
  await image.decode();
  images.set(path, image);
  return image;
}

function active() {
  return clips[$('family').value].options.find(clip => clip.key === $('action').value);
}

function duration(clip) {
  return clip.ms.reduce((sum, ms) => sum + ms, 0) / 1000;
}

function options() {
  $('action').replaceChildren();
  for (const clip of clips[$('family').value].options) {
    const option = document.createElement('option');
    option.value = clip.key;
    option.textContent = clip.label;
    $('action').append(option);
  }
  time = 0;
  override = null;
  describe();
}

function describe() {
  const scorpion = $('family').value === 'scorpion';
  $('caption').textContent = scorpion
    ? 'Короткие клешни с массивной ладонью. На беге хват спокойный; при сближении — раскрытие и резкое смыкание. Атака: 0,56 с, лапы продолжают шаг, скорость снижается с 2,4 до 1,90. Мишень показывает только момент захвата, боевого AI и урона здесь нет.'
    : 'Когтистый: выбранный дизайн J, ходьба, бег, размашистый удар и три композиции останков. Молчаливый визуальный префаб.';
}

function indexAt(clip, age) {
  const total = duration(clip);
  age = clip.loop ? ((age % total) + total) % total : Math.max(0, Math.min(age, total - .000001));
  for (let i = 0; i < clip.ms.length; i++) {
    age -= clip.ms[i] / 1000;
    if (age < 0) return i;
  }
  return clip.ms.length - 1;
}

function drawFrame(image, cell, columns, pivot, frame, scale, single = false) {
  if (single) {
    ctx.drawImage(image, -pivot[0] * scale, -pivot[1] * scale, image.width * scale, image.height * scale);
    return;
  }
  ctx.drawImage(image, (frame % columns) * cell, Math.floor(frame / columns) * cell, cell, cell,
    -pivot[0] * scale, -pivot[1] * scale, cell * scale, cell * scale);
}

function drawActor(clip, frame, x, y, scale, night, family) {
  ctx.save();
  ctx.translate(x, y);
  if (family === 'claw') ctx.rotate(Math.PI / 2);
  ctx.filter = night ? 'brightness(15%) saturate(70%)' : clip.dead ? 'brightness(60%) saturate(85%)' : 'none';
  drawFrame(clip.image, 512, clip.columns, clip.pivot, frame, scale, clip.single);
  if (clip.emission) {
    ctx.filter = 'none';
    ctx.globalCompositeOperation = 'lighter';
    const eye = clip.emission;
    drawFrame(eye.image, eye.cell, eye.columns, eye.pivot, frame, scale * 200 / eye.ppu);
  }
  ctx.restore();
}

function drawTarget(x, y, hit, dark) {
  ctx.save();
  ctx.translate(x, y);
  ctx.strokeStyle = hit ? '#ce5149' : dark ? '#73858e' : '#65737a';
  ctx.fillStyle = hit ? '#68332f' : dark ? '#3c4a52' : '#b4bdbd';
  ctx.lineWidth = 3;
  ctx.beginPath();
  ctx.ellipse(0, 0, 44, 34, 0, 0, Math.PI * 2);
  ctx.fill();
  ctx.stroke();
  ctx.beginPath();
  ctx.arc(0, -39, 14, 0, Math.PI * 2);
  ctx.fill();
  ctx.stroke();
  ctx.restore();
}

// A scripted art scenario: distance triggers a clip, no enemy or damage simulation.
function approach(actorIndex, scale) {
  const run = clips.scorpion.options.find(clip => clip.key === 'Run');
  const attack = clips.scorpion.options.find(clip => clip.key === 'Attack');
  const pixels = 200 * scale;
  const runSpeed = 2.4;
  const runPeriod = duration(run) * run.reference / runSpeed;
  const lead = runPeriod * 9; // enter at the shared neutral gait boundary
  const attackTime = duration(attack);
  const targetY = 140;
  const contactY = targetY + (.4 * attackTime * attack.reference + .86) * pixels;
  const startY = contactY + lead * runSpeed * pixels;
  const exitTime = (contactY - attackTime * attack.reference * pixels + 300) / (runSpeed * pixels);
  const period = lead + attackTime + exitTime;
  const offsets = [.32, .50, .70];
  let age = (time + period * offsets[actorIndex]) % period;
  if (override !== null) {
    age = lead + attack.ms.slice(0, override % attack.ms.length).reduce((sum, ms) => sum + ms, 0) / 1000;
  }
  if (age < lead) {
    return { clip: run, age: age * runSpeed / run.reference, y: startY - age * runSpeed * pixels,
      targetY, hit: false, label: 'Сближение' };
  }
  if (age < lead + attackTime) {
    const strikeAge = age - lead;
    return { clip: attack, age: strikeAge, y: contactY - strikeAge * attack.reference * pixels,
      targetY, hit: strikeAge >= attackTime * .38, label: 'Захват на ходу · 79% скорости' };
  }
  const after = age - lead - attackTime;
  return { clip: run, age: after * runSpeed / run.reference,
    y: contactY - attackTime * attack.reference * pixels - after * runSpeed * pixels,
    targetY, hit: true, label: 'Продолжение движения' };
}

function render() {
  if (!clips.scorpion) return;
  const family = $('family').value;
  const selected = active();
  const scenario = selected.key === 'Approach';
  const night = $('background').value === 'night';
  const dark = $('background').value !== 'light';
  const large = $('close').checked;
  const count = large ? 1 : Number($('count').value);
  let scale = .66;
  if (large) {
    scale = family === 'scorpion' ? 1.65 : 1.2;
    if (scenario) scale = .9;
  }
  ctx.fillStyle = night ? '#050708' : dark ? '#262d32' : '#e4e4de';
  ctx.fillRect(0, 0, 1280, 700);
  let speed = 0;
  if (!scenario) {
    if (selected.key === 'Run') speed = family === 'scorpion' ? 2.4 : 1.65;
    if (selected.key === 'Walk') speed = .55;
    if (selected.key === 'Attack') speed = selected.reference;
  }
  const drift = speed * time * 200 * scale;
  ctx.strokeStyle = night ? '#0e1518' : dark ? '#354047' : '#ced0ca';
  ctx.lineWidth = 1;
  for (let x = 0; x < 1280; x += 100) {
    ctx.beginPath(); ctx.moveTo(x, 0); ctx.lineTo(x, 700); ctx.stroke();
  }
  for (let y = -100; y < 800; y += 100) {
    ctx.beginPath(); ctx.moveTo(0, y + drift % 100); ctx.lineTo(1280, y + drift % 100); ctx.stroke();
  }
  let shown = 0;
  let frameCount = selected.ms.length;
  const actors = [];
  for (let i = 0; i < count; i++) {
    const x = count === 1 ? 640 : 260 + i * 380;
    let clip = selected;
    let age = time;
    let y = 350;
    let label = selected.label;
    if (scenario) {
      const state = approach(i, scale);
      clip = state.clip; age = state.age; y = state.y; label = state.label;
      drawTarget(x, state.targetY, state.hit, dark);
    } else {
      const total = duration(clip);
      if (clip.loop && speed) age = time * speed / clip.reference;
      if (!clip.loop && !clip.dead) age = time % (family === 'scorpion' ? total : total + .9);
      if (count === 3) age += total * [0, .31, .67][i];
    }
    const frame = override === null || scenario ? indexAt(clip, age) : override % clip.ms.length;
    if (i === 0) { shown = frame; frameCount = clip.ms.length; }
    drawActor(clip, frame, x, y, scale, night, family);
    ctx.fillStyle = dark ? '#cad4da' : '#465058';
    ctx.font = '16px system-ui'; ctx.textAlign = 'center';
    ctx.fillText(label, x, 665);
    actors.push({ clip: clip.key, frame, x, y });
  }
  $('status').textContent = String(shown + 1).padStart(2, '0') + ' / ' + frameCount;
  if (override === null) $('phase').value = Math.round(shown / Math.max(1, frameCount - 1) * 999);
  window.__reviewAudit = { family, state: selected.key, count, frame: shown, frameCount, actors, loaded: true };
}

async function init() {
  for (const [family, path] of [['scorpion', 'asset-pack.json'], ['claw', '../FastGroupEnemy23AssetPack/asset-pack.json']]) {
    const data = await fetch(path).then(response => {
      if (!response.ok) throw Error(path);
      return response.json();
    });
    const list = [];
    for (const state of data.states) {
      list.push({ key: state.name,
        label: ({ Run: 'Бег', Walk: 'Спокойная ходьба', Sweep: 'Размашистый удар', Hit: 'Попадание', Attack: 'Атака на ходу' })[state.name],
        image: await loadImage(url(state.asset)), columns: state.columns, ms: state.durationsMs,
        loop: state.loop, reference: state.referenceSpeed, pivot: state.pivotPixelsTopLeft,
        emission: state.emission ? { image: await loadImage(url(state.emission.asset)), cell: state.emission.cell,
          columns: state.emission.columns, pivot: state.emission.pivotPixelsTopLeft, ppu: state.emission.pixelsPerUnit } : null });
    }
    for (const [index, item] of (data.remains || []).entries()) {
      const image = await loadImage(url(item.asset));
      const ratio = 200 / item.pixelsPerUnit;
      const scaled = document.createElement('canvas');
      scaled.width = Math.round(image.width * ratio); scaled.height = Math.round(image.height * ratio);
      scaled.getContext('2d').drawImage(image, 0, 0, scaled.width, scaled.height);
      list.push({ key: item.name, label: ['Кровавая туша', 'Оторванная лапа', 'Груда мяса'][index], image: scaled,
        ms: [1000], loop: false, single: true, dead: true, pivot: item.pivotPixelsTopLeft.map(value => value * ratio) });
    }
    if (family === 'scorpion') list.unshift({ key: 'Approach', label: 'Сближение и атака', ms: list.find(c => c.key === 'Attack').ms });
    clips[family] = { data, options: list };
  }
  options();
  $('family').onchange = options;
  $('action').onchange = () => { time = 0; override = null; describe(); };
  $('count').onchange = render; $('close').onchange = render; $('background').onchange = render;
  $('play').onclick = () => { paused = !paused; override = null; $('play').textContent = paused ? 'Пуск' : 'Пауза'; };
  $('next').onclick = () => { paused = true; $('play').textContent = 'Пуск'; override = (window.__reviewAudit?.frame || 0) + 1; render(); };
  $('phase').oninput = () => {
    paused = true; $('play').textContent = 'Пуск';
    override = Math.round(Number($('phase').value) / 999 * (active().ms.length - 1)); render();
  };
  requestAnimationFrame(tick);
}

function tick(now) {
  const delta = Math.min((now - previous) / 1000, .1);
  previous = now;
  if (!paused) time += delta * Number($('tempo').value);
  render();
  requestAnimationFrame(tick);
}

init().catch(error => { $('status').textContent = 'Ошибка загрузки'; $('caption').textContent = String(error); });
