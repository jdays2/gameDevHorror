from pathlib import Path
import hashlib,json,re,difflib

PROJECT=Path(__file__).resolve().parents[1]
SOURCE=PROJECT/'Concepts/Unused/Visual/Scorpion01'
inspection=json.loads((SOURCE/'inspection.json').read_text(encoding='utf-8'))
inspection['animationVisualPass']=True
inspection['limitations']=['Prospective functional anatomy of a fictional arthropod, not a medical or dynamic force model.','R7 walking surfaces are independently generated component paint. Earlier Stalker donors remain historical sources only.','Technical QA is not owner artistic approval.']
inspection['checked'].extend([
    'R7 independent projections and native bone overlay reviewed before volume/40-ID attachment and skin passes; eight shorter thicker two-link walking legs use constant transverse widths and short dark feet.',
    'Two independent subagents reviewed all 24 Run/32 Attack source PNG and native/atlas measurements. The Attack06 hinge slit was fixed by a directional overlap sector; a wide round overlap regression was rejected and removed. Final actual PNG reviewed again.',
    'Native Editor Play verified closed-door waiting/eye occlusion, one alarm/all four members charging, moving melee and real damage, sight activation, no flashlight retreat, shadows, death pose preservation and interruption on disable. Scene authoring evidence is separate in ScorpionRoom01.'
])
inspection['checked']=list(dict.fromkeys(inspection['checked']))
(SOURCE/'inspection.json').write_text(json.dumps(inspection,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

# PrepareScorpionSceneIndex writes the exact owned delta against HEAD.
evidence=PROJECT/'Docs/Validation/ScorpionRoom01'

# Local documentation/content links in newly owned canonical docs.
files=['Docs/Enemies/SCORPION.md','Concepts/Used/Visual/Scorpion01/README.md','Concepts/Unused/Visual/Scorpion01/README.md','Concepts/Used/Audio/Scorpion01/README.md','Docs/Validation/ScorpionRoom01/README.md']
bad=[]
for name in files:
    path=PROJECT/name
    for link in re.findall(r'\]\(([^)]+)\)',path.read_text(encoding='utf-8')):
        if '://' in link or link.startswith('#'):
            continue
        target=(path.parent/link.split('#')[0]).resolve()
        if not target.exists():
            bad.append(dict(file=name,target=link))
report=dict(task='TASK-005',documents=len(files),missingLinks=bad,
    sourceFrames=56,animationReview=True,artisticApproval=False,
    beforeSceneSha256=hashlib.sha256((evidence/'scene-before.unity.txt').read_bytes()).hexdigest(),
    ownScenePatchSha256=hashlib.sha256((evidence/'scene-own.patch').read_bytes()).hexdigest())
(evidence/'content-checks.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report))
if bad:
    raise SystemExit(1)
