"""Collect only this correction; preserve the earlier implementation manifest."""
from pathlib import Path
import hashlib
import importlib.util
import json
import subprocess

ROOT=Path(__file__).resolve().parent
FAMILY=ROOT.parent
PROJECT=FAMILY.parents[3]
spec=importlib.util.spec_from_file_location('stalker_previous_scope',FAMILY/'collect_changes.py')
previous=importlib.util.module_from_spec(spec)
spec.loader.exec_module(previous)
SCOPE=previous.SCOPE+[
    'AgentScripts/AuditStalkerMotion.cs',
    'Assets/EmergencyShift/Runtime/Features/Enemies/View/StalkerSkinMesh.cs',
    'Assets/EmergencyShift/Runtime/Features/Enemies/View/StalkerSkinMesh.cs.meta',
    'Docs/Guides/MONSTER_CONCEPT_WORKFLOW.md',
]

if __name__=='__main__':
    manifest=ROOT/'changed-files.json'
    if not manifest.exists():
        manifest.write_text('{}')
    result=subprocess.run(['git','status','--porcelain','-z','--untracked-files=all','--']+SCOPE,
        cwd=PROJECT,check=True,capture_output=True).stdout
    files=sorted(record[3:].decode('utf-8') for record in result.split(b'\0') if record)
    data=dict(baseImplementation='f426876',files=[dict(path=path,
        workingFileSha256=None if PROJECT/path==manifest else hashlib.sha256((PROJECT/path).read_bytes()).hexdigest()) for path in files],
        scope=SCOPE,excluded='Authored scenes, unrelated pause/input/Claw work, build binaries and derived frame caches.',
        note='Working-file SHA-256; self hash omitted. Local commit only, no upload or push.')
    manifest.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
    (PROJECT/'Temp/stalker-motion-revision-paths.txt').write_bytes(b'\0'.join(path.encode('utf-8') for path in files)+b'\0')
    print(len(files),'scoped files')
