const $ = id => document.getElementById(id);
const canvas = $('screen');
const ctx = canvas.getContext('2d');
const clips = {};
const images = new Map();
let rupture;
let ruptureImage;
let time = 0;
let paused = false;
let previous = performance.now();
let override = null;
const attackAudio = new Audio();
const cueAudio = new Audio();
const presenceAudio = new Audio();
let nextSqueak = 3 + Math.random() * 2;
let previousAttackPhase = -1;
let playedAudio = 0;

function stopAudio() {
  attackAudio.pause();
  attackAudio.currentTime = 0;
  cueAudio.pause();
  cueAudio.currentTime = 0;
  presenceAudio.pause();
  presenceAudio.currentTime = 0;
}

function auditionCue(name, quiet = false) {
  stopAudio();
  cueAudio.src = '/scorpion-audio/' + name + '.wav';
  cueAudio.volume = name === 'Death' ? .48 : quiet ? .22 : .6;
  cueAudio.play().then(() => { $('audioStatus').textContent = name === 'Death' ? 'Играет смерть' : 'Играет писк'; })
    .catch(() => { $('audioStatus').textContent = 'Нажмите ещё раз для разрешения звука'; });
}
cueAudio.onended = () => { $('audioStatus').textContent = 'Проба завершена'; };

function playGrasp() {
  attackAudio.pause();
  attackAudio.currentTime = 0;
  attackAudio.src = '/scorpion-audio/HeavyShell' + $('soundVariant').value + '.wav';
  attackAudio.volume = .6;
  attackAudio.playbackRate = Number($('tempo').value);
  attackAudio.play().then(() => {
    playedAudio++;
    $('audioStatus').textContent = 'Играет ' + $('soundVariant').value;
  }).catch(() => {
    $('audioStatus').textContent = 'Звук заблокирован браузером';
    $('caption').textContent = 'Нажмите «Послушать удар», чтобы разрешить звук в браузере.';
  });
}
attackAudio.onended = () => { $('audioStatus').textContent = $('soundVariant').value + ' · проба завершена'; };

function url(path) {
  return '/production/' + path.split('/Art/Enemies/')[1];
}

function burgundy(image) {
  const tinted = document.createElement('canvas');
  tinted.width = image.width; tinted.height = image.height;
  const paint = tinted.getContext('2d', { willReadFrequently: true });
  paint.drawImage(image, 0, 0);
  const pixels = paint.getImageData(0, 0, tinted.width, tinted.height);
  const smooth = (a, b, value) => { const t = Math.max(0, Math.min(1, (value - a) / (b - a))); return t * t * (3 - 2 * t); };
  const dark = [.12, .04, .055], middle = [.42, .115, .14], light = [.64, .32, .27];
  for (let p = 0; p < pixels.data.length; p += 4) {
    if (!pixels.data[p + 3]) continue;
    const lum = (pixels.data[p] * .25 + pixels.data[p + 1] * .60 + pixels.data[p + 2] * .15) / 255;
    const low = smooth(0, .5, lum), high = smooth(.45, 1, lum);
    for (let channel = 0; channel < 3; channel++) {
      const base = dark[channel] + (middle[channel] - dark[channel]) * low;
      pixels.data[p + channel] = Math.round((base + (light[channel] - base) * high) * 255);
    }
  }
  paint.putImageData(pixels, 0, 0);
  return tinted;
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
  for (const id of ['sound', 'soundVariant', 'audition', 'discoveryAudition', 'deathAudition', 'squeakAudition', 'deathPose']) $(id).disabled = !scorpion;
  $('caption').textContent = scorpion
    ? 'Три направления разлёта, 12 частей выбранной фазы. Панцирь бордовый, глаза едва светятся, кровь жёлтая. Это художественная демонстрация: повороты, стены и дистанция между врагами проверяются отдельно в Unity. При включённом звуке первая особь иногда пищит.'
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
  ctx.save();
  ctx.translate(6 * scale, 9 * scale);
  ctx.filter = 'brightness(0) opacity(38%)';
  drawFrame(clip.image, 512, clip.columns, clip.pivot, frame, scale * 1.035, clip.single);
  ctx.restore();
  ctx.filter = night ? 'brightness(15%) saturate(70%)' : clip.dead ? 'brightness(60%) saturate(85%)' : 'none';
  drawFrame(clip.image, 512, clip.columns, clip.pivot, frame, scale, clip.single);
  if (clip.emission) {
    ctx.filter = 'none';
    ctx.globalCompositeOperation = 'lighter';
    if (family === 'scorpion') ctx.globalAlpha = .12;
    const eye = clip.emission;
    drawFrame(eye.image, eye.cell, eye.columns, eye.pivot, frame, scale * 200 / eye.ppu);
  }
  ctx.restore();
}

function drawDeath(clip, x, y, scale, night, variant) {
  const age = override === null ? time % 3.2 : override / 31 * 3.2;
  const [state, frame] = $('deathPose').value.split('-');
  const pose = rupture.poses.find(item => item.state === state && item.frame === Number(frame));
  ctx.save();
  ctx.translate(x, y);
  ctx.fillStyle = night ? '#171d08' : '#a7ad20';
  ctx.globalAlpha = Math.min(age / .24, 1) * .75;
  ctx.beginPath();
  for (let point = 0; point < 20; point++) {
    const angle = point * Math.PI * 2 / 20;
    const radius = .8 + .17 * Math.sin(point * 2.7 + variant);
    const px = Math.cos(angle) * 100 * scale * radius;
    const py = Math.sin(angle) * 65 * scale * radius;
    if (!point) ctx.moveTo(px, py); else ctx.lineTo(px, py);
  }
  ctx.closePath(); ctx.fill();
  for (let drop = 0; drop < 9; drop++) {
    const angle = drop * 2.4 + variant;
    ctx.beginPath();
    ctx.ellipse(Math.cos(angle) * (80 + drop * 4) * scale, Math.sin(angle) * (55 + drop * 3) * scale,
      (2 + drop % 3) * scale, (3 + drop % 4) * scale, angle, 0, Math.PI * 2);
    ctx.fill();
  }
  ctx.restore();
  const seconds = Math.max(0, Math.min(age - .035, .18));
  const lift = Math.max(0, 2 * seconds - 13 * seconds * seconds);
  for (let i = 0; i < pose.parts.length; i++) {
    const item = pose.parts[i];
    const rect = item.rectTopLeft;
    const angle = i * Math.PI * 2 / 12 + (variant - 1) * 43 * Math.PI / 180 + .1;
    const power = (i === 11 ? .4 : 1) * (i % 3 === 0 ? .65 : 1) * (.8 + (variant - 1) * .2);
    const distance = seconds * 3.5 * 200 * scale * power;
    ctx.save();
    ctx.translate(x + item.offset[0] * 200 * scale + Math.cos(angle) * distance,
      y - item.offset[1] * 200 * scale + Math.sin(angle) * distance);
    ctx.fillStyle = '#101214'; ctx.globalAlpha = .3;
    ctx.beginPath(); ctx.ellipse(0, 0, 11 * scale, 5 * scale, 0, 0, Math.PI * 2); ctx.fill();
    ctx.globalAlpha = 1;
    ctx.rotate(i === 11 ? 0 : seconds * (i % 2 ? 5 : -5));
    ctx.filter = night ? 'brightness(15%)' : 'brightness(74%)';
    const ratio = scale * (1 + lift * .5);
    ctx.drawImage(ruptureImage, ...rect, -rect[2] * ratio / 2, -rect[3] * ratio / 2, rect[2] * ratio, rect[3] * ratio);
    ctx.restore();
  }
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
  const contactY = targetY + (.4 * attackTime * attack.reference + .80) * pixels;
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
    if (selected.key === 'Death') scale = 1.05;
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
  if (!paused && override === null && family === 'scorpion' && selected.key !== 'Death' && $('sound').checked && time >= nextSqueak && presenceAudio.paused) {
    presenceAudio.src = '/scorpion-audio/Detection.wav';
    presenceAudio.volume = .22;
    presenceAudio.playbackRate = 1;
    presenceAudio.play().catch(() => {});
    nextSqueak = time + 3 + Math.random() * 2;
  }
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
    if (i === 0) {
      const attackPhase = family === 'scorpion' && clip.key === 'Attack' ? (age % duration(clip)) / duration(clip) : -1;
      const crossed = attackPhase >= .32 && (previousAttackPhase < .32 || attackPhase < previousAttackPhase);
      if (!paused && override === null && $('sound').checked && crossed && attackPhase < .65) playGrasp();
      previousAttackPhase = attackPhase;
    }
    if (i === 0) { shown = frame; frameCount = clip.ms.length; }
    if (clip.key === 'Death') drawDeath(clip, x, y, scale, night, i + 1);
    else drawActor(clip, frame, x, y, scale, night, family);
    ctx.fillStyle = dark ? '#cad4da' : '#465058';
    ctx.font = '16px system-ui'; ctx.textAlign = 'center';
    ctx.fillText(label, x, 665);
    actors.push({ clip: clip.key, frame, x, y });
  }
  $('status').textContent = String(shown + 1).padStart(2, '0') + ' / ' + frameCount;
  if (override === null) $('phase').value = Math.round(shown / Math.max(1, frameCount - 1) * 999);
  window.__reviewAudit = { family, state: selected.key, count, frame: shown, frameCount, actors, loaded: true, playedAudio, sound: $('sound').checked, soundVariant: $('soundVariant').value };
}

async function init() {
  for (const [family, path] of [['scorpion', 'asset-pack.json'], ['claw', '../FastGroupEnemy23AssetPack/asset-pack.json']]) {
    const data = await fetch(path).then(response => {
      if (!response.ok) throw Error(path);
      return response.json();
    });
    const list = [];
    for (const state of data.states) {
      const bodyImage = await loadImage(url(state.asset));
      list.push({ key: state.name,
        label: ({ Run: 'Бег', Walk: 'Спокойная ходьба', Sweep: 'Размашистый удар', Hit: 'Попадание', Attack: 'Атака на ходу' })[state.name],
        image: family === 'scorpion' ? burgundy(bodyImage) : bodyImage, columns: state.columns, ms: state.durationsMs,
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
    if (family === 'scorpion') {
      const run = list.find(clip => clip.key === 'Run');
      list.push({ ...run, key: 'Death', label: 'Смерть и разлёт', ms: Array(32).fill(100), loop: false, dead: true, emission: null });
      list.unshift({ key: 'Approach', label: 'Сближение и атака', ms: list.find(c => c.key === 'Attack').ms });
    }
    clips[family] = { data, options: list };
  }
  rupture = await fetch('rupture-r10.json').then(response => response.json());
  ruptureImage = burgundy(await loadImage(url(rupture.atlas)));
  options();
  $('family').onchange = () => { stopAudio(); previousAttackPhase = -1; options(); };
  $('action').onchange = () => { stopAudio(); nextSqueak = 3 + Math.random() * 2; previousAttackPhase = -1; time = 0; override = null; describe(); };
  $('count').onchange = render; $('close').onchange = render; $('background').onchange = render;
  $('play').onclick = () => { paused = !paused; override = null; if (paused) stopAudio(); $('play').textContent = paused ? 'Пуск' : 'Пауза'; };
  $('next').onclick = () => { stopAudio(); paused = true; $('play').textContent = 'Пуск'; override = (window.__reviewAudit?.frame || 0) + 1; render(); };
  $('audition').onclick = playGrasp;
  $('discoveryAudition').onclick = () => auditionCue('Detection');
  $('deathAudition').onclick = () => auditionCue('Death');
  $('squeakAudition').onclick = () => auditionCue('Detection', true);
  $('deathPose').onchange = () => { time = 0; override = null; render(); };
  $('sound').onchange = () => { stopAudio(); previousAttackPhase = -1; };
  $('soundVariant').onchange = () => { stopAudio(); previousAttackPhase = -1; };
  $('phase').oninput = () => {
    stopAudio(); paused = true; $('play').textContent = 'Пуск';
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
