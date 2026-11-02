const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');

const folder = 'Docs/Validation/DeathAudio53';
const summary = [];
for (const label of ['before', 'after', 'morgue-after']) {
  const rawPath = `${folder}/${label}-world.f32`;
  const wavPath = `${folder}/${label}-world.wav`;
  const samples = fs.existsSync(rawPath) ? fs.readFileSync(rawPath) : fs.readFileSync(wavPath).subarray(44);
  const header = Buffer.alloc(44);
  header.write('RIFF');
  header.writeUInt32LE(samples.length + 36, 4);
  header.write('WAVEfmt ', 8);
  header.writeUInt32LE(16, 16);
  header.writeUInt16LE(3, 20);
  header.writeUInt16LE(2, 22);
  header.writeUInt32LE(48000, 24);
  header.writeUInt32LE(48000 * 8, 28);
  header.writeUInt16LE(8, 32);
  header.writeUInt16LE(32, 34);
  header.write('data', 36);
  header.writeUInt32LE(samples.length, 40);
  fs.writeFileSync(wavPath, Buffer.concat([header, samples]));
  function window(start, end) {
    let square = 0, peak = 0, count = 0;
    for (let index = Math.floor(start * 48000) * 2; index < Math.min(samples.length / 4, Math.floor(end * 48000) * 2); index++) {
      const value = samples.readFloatLE(index * 4);
      if (!Number.isFinite(value)) throw new Error(`${label}: nonfinite sample`);
      square += value * value;
      peak = Math.max(peak, Math.abs(value));
      count++;
    }
    return { rms: Math.sqrt(square / count), peak, samples: count };
  }
  const reportPath = `${folder}/${label}-reproduction.json`;
  const report = JSON.parse(fs.readFileSync(reportPath, 'utf8'));
  report.file = wavPath;
  fs.writeFileSync(reportPath, JSON.stringify(report, null, 2) + '\n');
  const record = {
    label, wavPath, seconds: samples.length / (4 * 2 * 48000),
    sha256: crypto.createHash('sha256').update(fs.readFileSync(wavPath)).digest('hex'),
    alive: window(.1, .8), deadWorld: window(2.8, 3.9), ui: label === 'before' ? null : window(4.1, 4.9),
    routedWorldVoices: report.routes?.filter(source => source.output !== null).length,
    independentUiVoices: report.routes?.filter(source => source.ui).length,
    unroutedWorldVoices: report.routes?.filter(source => !source.ui && source.output === null).map(source => source.path)
  };
  summary.push(record);
}
if (summary[0].deadWorld.rms <= .0005) throw new Error('Original audible world defect was not captured');
for (const record of summary.slice(1)) {
  if (record.deadWorld.peak > .000001 || record.ui.rms < .001 || record.unroutedWorldVoices.length !== 0) {
    throw new Error(`${record.label}: world silence, UI independence or source coverage failed`);
  }
}
fs.writeFileSync(`${folder}/mix-analysis.json`, JSON.stringify({ passed: true, format: 'unaltered Unity AudioRenderer float32 stereo / 48 kHz', summary }, null, 2) + '\n');
console.log(JSON.stringify(summary, null, 2));
