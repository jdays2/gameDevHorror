"""Collect the exact posture revision without staging unrelated work."""
from pathlib import Path
import hashlib
import json
import subprocess

folder = Path(__file__).resolve().parent
root = folder.parents[3]
folders = ['Concepts/Used/Stalker/Postures04/', 'Assets/EmergencyShift/Art/Enemies/Stalker/Postures04/']
exact = [
    'AgentScripts/AuditCreaturePostures.cs',
    'AgentScripts/BakeStalkerPostures.cs',
    'AgentScripts/InstallStalkerPostures.cs',
    'AgentScripts/PreviewStalkerPostures.cs',
    'AgentScripts/CompareCreaturePostures.cs',
    'AgentScripts/ReviewStalkerPostures.cs',
    'Assets/EmergencyShift/Art/Enemies/Stalker/Postures04.meta',
    'Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset',
    'Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab',
    'Assets/EmergencyShift/Runtime/Core/Contracts/EnemyBodyPosture.cs',
    'Assets/EmergencyShift/Runtime/Core/Contracts/EnemyBodyPosture.cs.meta',
    'Assets/EmergencyShift/Runtime/Core/Contracts/EnemyPresentationSource2D.cs',
    'Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerPresentationSource2D.cs',
    'Assets/EmergencyShift/Runtime/Enemies/Sleeper/SleeperPresentationSource2D.cs',
    'Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerSpriteAnimator.cs',
    'Assets/EmergencyShift/Runtime/Presentation/Visuals/CreatureSpriteAnimator2D.cs',
    'Assets/EmergencyShift/Runtime/Features/Enemies/Config/StalkerSpriteSet.cs',
    'Assets/EmergencyShift/Editor/Authoring/StalkerSprites/StalkerPoseComposer.cs',
    'Assets/EmergencyShift/Editor/Authoring/StalkerSprites/StalkerPoseComposer.cs.meta',
    'Assets/EmergencyShift/Tests/EditMode/StalkerPostureTests.cs',
    'Assets/EmergencyShift/Tests/EditMode/StalkerPostureTests.cs.meta',
    'Assets/EmergencyShift/Tests/PlayMode/StalkerRigSceneTests.cs',
    'Assets/EmergencyShift/Tests/PlayMode/SleeperBrainSceneTests.cs',
    'Docs/Enemies/STALKER.md',
    'Docs/Enemies/SLEEPER.md',
    'Docs/Features/Enemies.md',
    'Docs/Guides/MONSTER_CONCEPT_WORKFLOW.md',
    'Concepts/Used/Stalker/Locomotion03Sprites/README.md',
]

def git(*args):
    return subprocess.check_output(['git', *args], cwd=root).decode('utf-8').split('\0')

changed = git('diff', '--name-only', 'HEAD', '-z') + git('ls-files', '--others', '--exclude-standard', '-z')
paths = {p for p in changed if p and (p in exact or any(p.startswith(prefix) for prefix in folders))}
manifest = 'Concepts/Used/Stalker/Postures04/changed-files.json'
paths.add(manifest)
rows = []
for path in sorted(paths):
    file = root / path
    rows.append({'path': path, 'deleted': not file.exists(),
                 'sha256': hashlib.sha256(file.read_bytes()).hexdigest() if file.exists() and path != manifest else None})
(folder / 'changed-files.json').write_text(json.dumps({
    'files': rows, 'count': len(paths),
    'scope': 'Stalker posture correction and explicit Sleeper presentation intent; authored scenes and other creatures preserved.'
}, indent=2), encoding='utf-8')
(root / 'Temp/stalker04-paths.txt').write_bytes(('\0'.join(sorted(paths)) + '\0').encode('utf-8'))
print(f'{len(paths)} scoped paths; no files staged by this collector')
