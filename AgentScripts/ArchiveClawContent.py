"""Hash exact imports and preserve the isolated scene delta, without editing assets."""
from pathlib import Path
import hashlib,json,subprocess,difflib
ROOT=Path(__file__).resolve().parents[1]
F17=ROOT/'Concepts/Unused/Visual/FastGroupEnemy17SimplifiedMotion'
F18=ROOT/'Concepts/Unused/Visual/FastGroupEnemy18Remains'
F19=ROOT/'Concepts/Unused/Visual/FastGroupEnemy19SpiderEyes'
ART=ROOT/'Assets/EmergencyShift/Art/Enemies/ClawPrototype'
USED=ROOT/'Concepts/Used/Visual/ClawPrototype01'
VALID=ROOT/'Docs/Validation/ClawSandbox01'
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,value):p.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
imports=[]
for dest in sorted(ART.rglob('*.png')):
    part=dest.relative_to(ART)
    if part.parts[0]=='Remains':source=F18/part.name
    elif part.parts[0]=='Sweep':source=F17/'alternatives/heavy/sweep/frames'/part.name
    else:source=F17/part.parts[0]/'frames'/part.name
    assert digest(source)==digest(dest),(source,dest)
    imports.append({'source':source.relative_to(ROOT).as_posix(),'asset':dest.relative_to(ROOT).as_posix(),'sha256':digest(dest)})
assert len(imports)==53
write(USED/'import-manifest.json',{'task':'TASK-005','copiedWithoutRasterChanges':True,'files':imports})
write(F18/'art-sha256.json',{p.name:digest(p) for p in sorted(F18.glob('*.png'))})
write(F19/'selection.json',{'task':'TASK-005','mainDesign':'J-closed-mandibles.png','sha256':digest(F19/'J-closed-mandibles.png'),'decision':'Owner explicitly selected this as main design, 2026-10-26.','eyes':8,'mandiblePairs':1,'animationTransferComplete':False,'currentRunArtisticallyAccepted':False})
scene='Assets/EmergencyShift/Scenes/MechanicsSandbox.unity'
before=(VALID/'scene-before.unity.txt').read_text(encoding='utf-8-sig').splitlines(True)
after=(ROOT/scene).read_text(encoding='utf-8-sig').splitlines(True)
patch=''.join(difflib.unified_diff(before,after,fromfile='a/'+scene,tofile='b/'+scene))
assert not [line for line in patch.splitlines() if line.startswith('-') and not line.startswith('---')], 'Unexpected old scene mutation'
(VALID/'own-scene.patch').write_text(patch,encoding='utf-8',newline='\n')
print(json.dumps({'exactImports':len(imports),'sceneDeltaOnlyAdds':True,'mainDesign':'J'},ensure_ascii=False))
