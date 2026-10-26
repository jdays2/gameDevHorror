"""List only this task's paths; never stage the shared repository wholesale."""
from pathlib import Path
import hashlib
import json
import subprocess

ROOT=Path(__file__).resolve().parent
PROJECT=ROOT.parents[3]
SCOPE=[
 'AgentScripts/InspectStalkerLocomotion.cs','AgentScripts/InstallStalkerRig.cs',
 'AgentScripts/PreviewStalkerRig.cs','AgentScripts/ReviewStalkerRig.cs',
 'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02',
 'Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02.meta',
 'Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset',
 'Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset.meta',
 'Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab',
 'Assets/EmergencyShift/Runtime/Enemies/Stalker/StalkerSpriteAnimator.cs',
 'Assets/EmergencyShift/Runtime/Presentation/Visuals/CreatureSpriteAnimator2D.cs',
 'Docs/Enemies/STALKER.md','Docs/Features/Enemies.md',
 'Concepts/Used/Stalker/Locomotion02',
]
for stem in ['StalkerRigSurface.shader','StalkerRigSurface.mat','StalkerRigShadow.shader','StalkerRigShadow.mat']:
    for suffix in ['', '.meta']:SCOPE.append('Assets/EmergencyShift/Materials/Characters/'+stem+suffix)
for path in ['Runtime/Features/Enemies/Config/StalkerRigProfile.cs',
             'Runtime/Features/Enemies/Logic/StalkerGaitModel.cs',
             'Runtime/Features/Enemies/View/StalkerLimbRig2D.cs',
             'Runtime/Features/Enemies/View/StalkerRigShadows2D.cs',
             'Tests/EditMode/StalkerGaitTests.cs','Tests/PlayMode/StalkerRigSceneTests.cs']:
    for suffix in ['', '.meta']:SCOPE.append('Assets/EmergencyShift/'+path+suffix)

if __name__=='__main__':
    manifest=ROOT/'changed-files.json'
    if not manifest.exists():manifest.write_text('{}')
    result=subprocess.run(['git','status','--porcelain','-z','--untracked-files=all','--']+SCOPE,cwd=PROJECT,check=True,capture_output=True).stdout
    files=sorted(item[3:].decode('utf-8') for item in result.split(b'\0') if item)
    data={'scope':SCOPE,'files':[{'path':p,'sha256':None if PROJECT/p==manifest else hashlib.sha256((PROJECT/p).read_bytes()).hexdigest()} for p in files],
          'excluded':'Unrelated dirty files, authored scenes, Claw/F17–F21 work, menu/pause/input changes.',
          'note':'Manifest excludes its own hash. Media never uploaded. Local commit only.'}
    manifest.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
    print(f'{len(files)} scoped changed files; manifest written.')
