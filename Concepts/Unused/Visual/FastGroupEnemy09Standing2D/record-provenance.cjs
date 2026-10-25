const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const root = __dirname;
const hash = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const outputIds = [
  'exec-87736571-f776-4db0-9b43-425489b82933.png',
  'exec-4a195b94-5ddf-4c1d-8dbd-41a2c7fc7213.png',
  'exec-de29255e-bffa-4e69-a844-f222523b1a8f.png',
  'exec-30fb482d-33f6-4d00-b4e0-9288faee74ab.png',
  'exec-6eaec03a-9929-40e1-80c9-b4637bc4e5db.png'
];
const inputs = [
  ['../FastGroupEnemy04/sprite-v25-painted.png', '../FastGroupEnemy07Model3D/standing-reference-owner.png'],
  ['standing-v1.png', 'standing-projection-guide.png'],
  ['standing-v2.png', 'standing-projection-guide.png'],
  ['standing-v3.png'],
  ['standing-v4.png']
];
const results = [
  'Rejected: forward-reaching exposed limbs; independent support projection failed.',
  'Rejected: external muscular loops still read as crawling arms.',
  'Standing projection plausible; only two claw tips visible per foot, count failed.',
  'Three visible short claws per compact foot; superseded by owner request for broader feet.',
  'Current candidate: broad feet, wider stance, three short claws each; owner acceptance pending.'
];
const generations = outputIds.map((outputId, index) => {
  const version = index + 1;
  const file = version === 5 ? 'standing-v5-wide-feet.png' : `standing-v${version}.png`;
  return {
    version, file, sha256: hash(path.join(root, file)),
    generatedOutput: outputId,
    inputReferences: inputs[index].map(file => ({ file, sha256: hash(path.resolve(root, file)) })),
    promptFile: `standing-v${version}-prompt.txt`,
    promptSha256: hash(path.join(root, `standing-v${version}-prompt.txt`)),
    result: results[index]
  };
});
const provenance = {
  task: 'TASK-005', date: '2026-10-25', tool: 'built-in image_gen',
  transparentBackground: true, orientation: 'strict perpendicular overhead, front left',
  sourcePolicy: 'Direct raster generation/editing. No assembled limbs, raster warping or 3D rendering. Code only produces independent technical guides and RGBA review.',
  currentCandidate: 'standing-v5-wide-feet.png', ownerApproval: 'pending',
  ownerGaitDecision: 'Почти парные толчки, как рывки',
  validationLimits: 'Visible support positions, parts and RGBA inspected. Hidden joints, actual 3D bone lengths and hidden muscle attachments are not established by the overhead drawing.',
  unityIntegration: false, notionUpload: false,
  generations
};
fs.writeFileSync(path.join(root, 'provenance.json'), JSON.stringify(provenance, null, 2) + '\n');
const hashes = {};
for (const file of fs.readdirSync(root).sort()) {
  if (file === 'sha256.json' || !fs.statSync(path.join(root, file)).isFile()) continue;
  hashes[file] = hash(path.join(root, file));
}
fs.writeFileSync(path.join(root, 'sha256.json'), JSON.stringify(hashes, null, 2) + '\n');
console.log(JSON.stringify({ generations: generations.length, filesHashed: Object.keys(hashes).length, currentSha256: generations[4].sha256 }));
