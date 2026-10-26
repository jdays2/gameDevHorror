"""Collect only this sprite revision, preserving other chats' work and Git index."""
from pathlib import Path
import subprocess,json,hashlib

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[3]
folders=['Concepts/Used/Stalker/Locomotion03Sprites/',
 'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion03Sprites/',
 'Assets/EmergencyShift/Editor/Authoring/StalkerSprites/']
exact=['AgentScripts/BakeStalkerSprites.cs','AgentScripts/InspectStalkerSpriteStates.cs',
 'AgentScripts/InstallStalkerSprites.cs','AgentScripts/PreviewStalkerSprites.cs','AgentScripts/ReviewStalkerSprites.cs',
 'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion03Sprites.meta',
 'Assets/EmergencyShift/Editor/Authoring/StalkerSprites.meta',
 'Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab',
 'Assets/EmergencyShift/Runtime/Definitions/StalkerLocomotionProfile.cs',
 'Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerSpriteAnimator.cs',
 'Assets/EmergencyShift/Runtime/Presentation/Visuals/CreatureSpriteAnimator2D.cs',
 'Assets/EmergencyShift/Tests/PlayMode/StalkerRigSceneTests.cs',
 'Docs/Enemies/STALKER.md','Docs/Features/Enemies.md','Docs/Guides/MONSTER_CONCEPT_WORKFLOW.md',
 'Concepts/Used/Stalker/Locomotion02/README.md']
for path in ['Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset',
 'Assets/EmergencyShift/Data/Enemies/StalkerSpriteLocomotion.asset',
 'Assets/EmergencyShift/Runtime/Features/Enemies/Config/StalkerSpriteSet.cs']:
    exact.extend([path,path+'.meta'])
for path in ['Config/StalkerRigProfile.cs','Logic/StalkerGaitModel.cs','View/StalkerSkinMesh.cs',
 'View/StalkerLimbRig2D.cs','View/StalkerRigShadows2D.cs']:
    full='Assets/EmergencyShift/Runtime/Features/Enemies/'+path
    exact.extend([full,full+'.meta'])
def git(*args):
    return subprocess.check_output(['git',*args],cwd=ROOT).decode('utf-8').split('\0')
modified=git('diff','--name-only','HEAD','-z')
untracked=git('ls-files','--others','--exclude-standard','-z')
paths=sorted({p for p in modified+untracked if p and (p in exact or any(p.startswith(f) for f in folders))})
manifest='Concepts/Used/Stalker/Locomotion03Sprites/changed-files.json'
if manifest not in paths:
    paths.append(manifest)
paths.sort()
rows=[]
for path in paths:
    file=ROOT/path
    rows.append({'path':path,'deleted':not file.exists(),
        'sha256':hashlib.sha256(file.read_bytes()).hexdigest() if file.exists() and path not in (manifest,'Docs/Features/Enemies.md') else None,
        'sharedFileScope':'Stalker sprite ownership row only; other chats retain their unstaged documentation.' if path=='Docs/Features/Enemies.md' else None})
(OUT/'changed-files.json').write_text(json.dumps({'files':rows,'count':len(paths),'scope':'Stalker whole sprites only; no authored scene saves; no external publication'},indent=2))
(ROOT/'Temp/stalker03-paths.txt').write_bytes(('\0'.join(paths)+'\0').encode('utf-8'))
print(f'{len(paths)} scoped paths; no changes staged by this collector')
