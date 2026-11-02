const fs = require('node:fs');
const { execFileSync } = require('node:child_process');
const git = (...args) => execFileSync('git', args, { encoding: 'utf8' }).trimEnd();
const normalize = text => text.replace(/\r\n/g, '\n');

const verifyOnly = process.argv.includes('--verify');
if (!verifyOnly && git('diff', '--cached', '--name-only')) throw new Error('Refusing to mix pre-existing staging');
if (git('rev-parse', 'HEAD') !== '1fd99053d66741b63cb52124108c3eb7c977d6de') throw new Error('HEAD changed since slot acquisition');

function editLevel(source) {
  return source
    .replace('using UnityEngine;', 'using EmergencyShift.Features.WorldAudio;\nusing EmergencyShift.Features.WorldAudio.View;\nusing UnityEngine;')
    .replace('        private ScenePrefabFactory prefabs;', '        private ScenePrefabFactory prefabs;\n        private WorldAudioPresenter worldAudio;')
    .replace('                prefabs = Container.Resolve<ScenePrefabFactory>();', '                prefabs = Container.Resolve<ScenePrefabFactory>();\n                worldAudio = WorldAudioInstaller.AttachPlayer(Container, player);')
    .replace('            disposed = true;', '            disposed = true;\n            worldAudio?.Dispose();');
}

function indexContent(file, content) {
  const hash = execFileSync('git', ['hash-object', '-w', '--stdin'], { input: content, encoding: 'utf8' }).trim();
  git('update-index', '--cacheinfo', `100644,${hash},${file}`);
}

const level = 'Assets/EmergencyShift/Runtime/Levels/LevelContext2D.cs';
const levelBefore = normalize(fs.readFileSync('../../work/bug053/LevelContext2D-before.cs', 'utf8'));
if (editLevel(levelBefore) !== normalize(fs.readFileSync(level, 'utf8'))) throw new Error('LevelContext contains an unexpected new edit');
if (!verifyOnly) indexContent(level, editLevel(git('show', `HEAD:${level}`) + '\n'));

const guide = 'Docs/Audio/AUDIO_GUIDE.md';
const guideWork = normalize(fs.readFileSync(guide, 'utf8'));
const marker = '## Смерть и восстановление мирового звука';
const markerIndex = guideWork.lastIndexOf(marker);
if (markerIndex < 0) throw new Error('Own guide addition missing');
const guideBefore = normalize(fs.readFileSync('../../work/bug053/AUDIO_GUIDE-before.md', 'utf8')).trimEnd();
if (guideWork.slice(0, markerIndex).trimEnd() !== guideBefore) throw new Error('Earlier guide edits changed');
if (!verifyOnly) indexContent(guide, git('show', `HEAD:${guide}`) + '\n\n' + guideWork.slice(markerIndex).trim() + '\n');

const whole = [
  'AgentScripts/AnalyzeDeathAudio53.cjs', 'AgentScripts/BuildDeathAudio53.cs', 'AgentScripts/InstallDeathAudio53.cs',
  'AgentScripts/VerifyDeathAudio53.cs', 'AgentScripts/StageDeathAudio53.cjs',
  'Assets/EmergencyShift/Data/Audio/WorldAudio.mixer', 'Assets/EmergencyShift/Data/Audio/WorldAudio.mixer.meta',
  'Assets/EmergencyShift/Data/Audio/WorldDeathTransition.asset', 'Assets/EmergencyShift/Data/Audio/WorldDeathTransition.asset.meta',
  'Assets/EmergencyShift/Resources/ProjectContext.prefab',
  'Assets/EmergencyShift/Runtime/Features/WorldAudio', 'Assets/EmergencyShift/Runtime/Features/WorldAudio.meta',
  'Assets/EmergencyShift/Runtime/Features/PauseMenu/View/PauseMenuMusic.cs',
  'Assets/EmergencyShift/Runtime/Enemies/Sleeper/SleeperAudio2D.cs',
  'Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerAudio2D.cs',
  'Assets/EmergencyShift/Runtime/Features/Scorpion/View/ScorpionAudio2D.cs',
  'Assets/EmergencyShift/Runtime/Player/PlayerAudio2D.cs', 'Assets/EmergencyShift/Runtime/Player/PlayerFootsteps2D.cs',
  'Assets/EmergencyShift/Runtime/Player/PlayerUtilityAudio2D.cs',
  'Assets/EmergencyShift/Runtime/Presentation/Audio/LevelAudioDirector2D.cs',
  'Assets/EmergencyShift/Runtime/World/Doors/DoorAudio2D.cs', 'Assets/EmergencyShift/Runtime/World/Doors/DoorAccessFeedback2D.cs',
  'Assets/EmergencyShift/Runtime/World/Lighting/LocalLampAudio2D.cs',
  'Assets/EmergencyShift/Tests/EditMode/WorldAudioTests.cs', 'Assets/EmergencyShift/Tests/EditMode/WorldAudioTests.cs.meta',
  'Assets/EmergencyShift/Tests/PlayMode/WorldAudioBusTests.cs', 'Assets/EmergencyShift/Tests/PlayMode/WorldAudioBusTests.cs.meta',
  'Docs/Features/WorldAudio.md', 'Docs/Features/README.md', 'Docs/Features/PauseMenu.md',
  'Docs/Reports/DeathAudio53.md', 'Docs/Reports/ValidationIndex.md', 'Docs/Validation/DeathAudio53'
];
if (!verifyOnly) git('add', '--', ...whole);
// Unity emits blank-value trailing spaces in native YAML. Preserve serializer output.
git('diff', '--cached', '--check', '--', '.', ':!*.meta', ':!*.mixer', ':!*.prefab');
const staged = git('diff', '--cached', '--name-only').split('\n');
const allowed = [...whole, level, guide];
for (const file of staged) {
  if (!allowed.some(item => file === item || file.startsWith(item + '/'))) throw new Error(`Unexpected staged file ${file}`);
}
fs.writeFileSync('../../work/bug053/staged-files.json', JSON.stringify(staged, null, 2) + '\n');
console.log(JSON.stringify({ stagedFiles: staged.length, sharedFiles: [level, guide], noScenesOrForeignPauseDraft: true }, null, 2));
