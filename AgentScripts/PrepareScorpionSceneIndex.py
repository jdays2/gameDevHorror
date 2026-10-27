"""Stage only the authored Scorpion room assembly and its three explicit shared-parent edits.
The working scene may contain concurrent owner edits; this file never rewrites it.
"""
from pathlib import Path
import re,json,subprocess,hashlib,difflib

PROJECT=Path(__file__).resolve().parents[1]
OUT=PROJECT/'Docs/Validation/ScorpionRoom01'
SCENE='Assets/EmergencyShift/Scenes/MechanicsSandbox.unity'
head=subprocess.check_output(['git','show','HEAD:'+SCENE],cwd=PROJECT).decode('utf-8-sig').replace('\r\n','\n')
current=(PROJECT/SCENE).read_text(encoding='utf-8-sig')

def documents(value):
    parts=re.split(r'(?m)(^--- !u!\d+ &(\d+)[^\n]*\n)',value)
    result={}
    for i in range(1,len(parts),3):
        result[int(parts[i+1])]=parts[i]+parts[i+2]
    return parts[0],result

prefix,baseline=documents(head)
_,live=documents(current)

def names(doc):
    found=re.findall(r'^  m_Name: (.+)$',doc,re.M)
    found+=re.findall(r'propertyPath: m_Name\n      value: (.+)',doc)
    result=[]
    for name in found:
        if name.startswith('"'):
            try:
                name=json.loads(name)
            except ValueError:
                name=name[1:-1].encode('utf-8').decode('unicode_escape')
        result.append(name)
    return result

seeds=['R04_ScorpionPackTest','South boundary east of scorpion entry','04 ↓ СКОРПИОНЫ']
owned=set()
for name in seeds:
    matches=[i for i,doc in live.items() if name in names(doc)]
    if len(matches)!=1:
        raise ValueError((name,matches))
    owned.add(matches[0])

# Follow local references except the owning parent. Include stripped native handles
# pointing back to each owned prefab instance; external GUID references stay external.
while True:
    previous=set(owned)
    for i in list(owned):
        for line in live[i].splitlines():
            if 'm_Father:' in line or 'm_TransformParent:' in line:
                continue
            for value in re.findall(r'\{fileID: (\d+)\}',line):
                target=int(value)
                if target in live:
                    owned.add(target)
    for i,doc in live.items():
        owner=re.search(r'^  m_PrefabInstance: \{fileID: (\d+)\}',doc,re.M)
        if owner and int(owner.group(1)) in owned:
            owned.add(i)
    if owned==previous:
        break

if owned.intersection(baseline):
    raise ValueError('Room closure reached preexisting documents: '+str(owned.intersection(baseline)))
merged=dict(baseline)
parent_edits=[]
pattern=r'(?m)^  m_Children:(?: \[\]\n|\n(?:  - \{fileID: \d+\}\n)*)'
for i,doc in baseline.items():
    if i not in live:
        continue
    block=re.search(pattern,live[i])
    if not block:
        continue
    additions=[int(x) for x in re.findall(r'fileID: (\d+)',block.group()) if int(x) in owned]
    if additions:
        original=re.search(pattern,doc)
        if not original:
            raise ValueError('Missing existing parent child list')
        old_ids=[int(x) for x in re.findall(r'fileID: (\d+)',original.group())]
        ids=old_ids+[x for x in additions if x not in old_ids]
        replacement='  m_Children:\n'+''.join('  - {fileID: '+str(x)+'}\n' for x in ids)
        merged[i]=doc[:original.start()]+replacement+doc[original.end():]
        parent_edits.append(dict(document=i,children=additions))

# The only deliberately resized preexisting assembly is the corridor south wall.
walls=[i for i,doc in baseline.items() if 'South boundary' in names(doc)]
if len(walls)!=1:
    raise ValueError(('South wall',walls))
wall_id=walls[0]
doc=merged[wall_id]
if doc.startswith('--- !u!1001'):
    for property_name,value in [('m_LocalPosition.x','24.15'),('m_LocalScale.x','48.7')]:
        pattern_value=r'(propertyPath: '+re.escape(property_name)+r'\n      value: )[^\n]*'
        doc,count=re.subn(pattern_value,lambda m:m.group(1)+value,doc)
        if count!=1:
            raise ValueError((property_name,count))
    merged[wall_id]=doc
else:
    transform=int(re.search(r'm_Component:\n  - component: \{fileID: (\d+)\}',doc).group(1))
    doc=merged[transform]
    doc,count=re.subn(r'(?m)^  m_LocalPosition:.*$', '  m_LocalPosition: {x: 24.15, y: -6, z: 0}',doc)
    if count!=1:
        raise ValueError('Wall position')
    doc,count=re.subn(r'(?m)^  m_LocalScale:.*$','  m_LocalScale: {x: 48.7, y: 0.4, z: 1}',doc)
    if count!=1:
        raise ValueError('Wall scale')
    merged[transform]=doc
    wall_id=transform

for i in owned:
    merged[i]=live[i]
order=list(baseline)
root_position=next((index for index,i in enumerate(order) if '\nSceneRoots:' in baseline[i]),len(order))
order=order[:root_position]+sorted(owned)+order[root_position:]
candidate=prefix+''.join(merged[i] for i in order)
target=OUT/'scene-index-candidate.unity.txt'
target.write_text(candidate,encoding='utf-8',newline='\n')
delta=''.join(difflib.unified_diff(head.splitlines(True),candidate.splitlines(True),
    fromfile='a/'+SCENE,tofile='b/'+SCENE))
(OUT/'scene-own.patch').write_text(delta,encoding='utf-8',newline='\n')
changed=[i for i in baseline if merged[i]!=baseline[i]]
allowed={wall_id}|{e['document'] for e in parent_edits}
if set(changed)!=allowed or len(parent_edits)!=3:
    raise ValueError(('Unexpected shared document edit',changed,parent_edits))
report=dict(task='TASK-005',ownedNewDocuments=len(owned),sharedParentEdits=parent_edits,
    resizedBoundaryDocument=wall_id,changedExistingDocuments=changed,
    unrelatedDocumentsUnchangedFromHEAD=True,workingSceneUntouched=True,
    candidateSha256=hashlib.sha256(target.read_bytes()).hexdigest())
(OUT/'scene-index-merge.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
