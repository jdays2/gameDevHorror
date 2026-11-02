// Select only the four native-authored BUG-051 blocks for Git; never rewrite Assets.
// Before/after scene snapshots must come from VerifySleeperNotes51 and the live Editor.
const fs = require('node:fs');
const cp = require('node:child_process');
const crypto = require('node:crypto');
const path = require('node:path');
process.chdir(path.resolve(__dirname, '..'));

const scene = 'Assets/EmergencyShift/Scenes/MechanicsSandbox.unity';
const output = 'Docs/Validation/SleeperNotes51/';
const allowed = ['3628977', '1061168067', '1349773415', '1451442420'].sort();
const hash = value => crypto.createHash('sha256').update(value).digest('hex');
const git = args => cp.execFileSync('git', args, { encoding: 'utf8', maxBuffer: 32 * 1024 * 1024 });
const requireThat = (condition, message) => { if (!condition) { throw new Error(message); } };
const parse = source => new Map([...source.replace(/\r\n/g, '\n').matchAll(/--- !u!(\d+) &(-?\d+)(?: stripped)?\n([\s\S]*?)(?=--- !u!|$)/g)].map(match => [match[2], match[0]]));

const baseline = JSON.parse(fs.readFileSync('work/bug051/foreign-baseline.json', 'utf8'));
requireThat(git(['rev-parse', 'HEAD']).trim() === baseline.head, 'HEAD changed since the exclusive slot baseline.');
const before = parse(fs.readFileSync('work/bug051/MechanicsSandbox.before.unity', 'utf8'));
const current = parse(fs.readFileSync(scene, 'utf8'));
requireThat(before.size === current.size && [...before.keys()].every(id => current.has(id)), 'Scene objects were added or removed.');
const changed = [...before.keys()].filter(id => before.get(id) !== current.get(id)).sort();
requireThat(JSON.stringify(changed) === JSON.stringify(allowed), 'Unexpected scene block changes.');
const headSource = git(['show', 'HEAD:' + scene]).replace(/\r\n/g, '\n');
const head = parse(headSource);
let payload = headSource;
for (const id of changed) {
    requireThat(head.get(id) === before.get(id), 'Foreign edits share task block ' + id);
    payload = payload.replace(head.get(id), current.get(id));
}
const staged = parse(payload);
requireThat(staged.size === head.size, 'Index payload changed object count.');
requireThat([...head.keys()].every(id => changed.includes(id) || head.get(id) === staged.get(id)), 'Unrelated HEAD blocks changed.');

const foreign = Object.entries(baseline.files).filter(([file]) => file !== scene).map(([file, digest]) => ({ file, unchanged: fs.existsSync(file) && hash(fs.readFileSync(file)) === digest }));
requireThat(foreign.every(item => item.unchanged), 'A foreign dirty file changed during this slot.');
const nativeBefore = JSON.parse(fs.readFileSync(output + 'native-before.json', 'utf8'));
const nativeAfter = JSON.parse(fs.readFileSync(output + 'native-after.json', 'utf8'));
requireThat(JSON.stringify(nativeBefore.actors) === JSON.stringify(nativeAfter.actors), 'Authored actors or overrides changed.');
requireThat(nativeBefore.transformsIncludingInactive === nativeAfter.transformsIncludingInactive && nativeAfter.missingScripts === 0 && nativeAfter.missingReferences === 0 && nativeAfter.duplicatePersistentIds.length === 0, 'Native authoring gate failed.');
for (const note of nativeBefore.notes) {
    const after = nativeAfter.notes.find(value => value.id === note.id);
    const keep = value => ({ ...value, path: undefined, position: undefined });
    requireThat(after && JSON.stringify(keep(note)) === JSON.stringify(keep(after)), 'Note data changed beyond location: ' + note.id);
}

const payloadPath = 'work/bug051/index-task.unity';
fs.writeFileSync(payloadPath, payload);
const result = {
    baseCommit: baseline.head,
    scene,
    nativeBlocksBefore: before.size,
    nativeBlocksAfter: current.size,
    ownChangedBlocks: changed,
    addedBlocks: 0,
    removedBlocks: 0,
    unmodifiedSceneBlocks: before.size - changed.length,
    actorDataAndOverridesIdentical: true,
    noteIdsTextComponentsScaleRotationAndActivityIdentical: true,
    foreignTrackedDirtyFilesUnchanged: foreign,
    headBlocks: head.size,
    indexBlocks: staged.size,
    indexPayloadSha256: hash(payload),
    workingSceneSha256: hash(fs.readFileSync(scene)),
    payloadOnlyForGit: true
};
fs.writeFileSync(output + 'preservation.json', JSON.stringify(result, null, 2) + '\n');
if (process.argv.includes('--stage')) {
    requireThat(git(['diff', '--cached', '--name-only']).trim() === '', 'Refusing to overwrite another staged change.');
    const blob = git(['hash-object', '-w', payloadPath]).trim();
    git(['update-index', '--cacheinfo', '100644', blob, scene]);
}
console.log(JSON.stringify({ changedBlocks: changed, preservedBlocks: result.unmodifiedSceneBlocks, foreignFilesUnchanged: foreign.length, headBlocks: head.size, indexBlocks: staged.size, staged: process.argv.includes('--stage') }));
