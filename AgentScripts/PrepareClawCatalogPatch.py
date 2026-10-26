from pathlib import Path
import subprocess,difflib,json
root=Path(__file__).resolve().parents[1]
folder=root/'Docs/Validation/ClawSandbox01'
catalog='Concepts/README.md'
head=subprocess.check_output(['git','show','HEAD:'+catalog],cwd=root).decode('utf-8')
old='- [TASK-005 · упрощённый враг: ходьба, атака и бег](Unused/Visual/FastGroupEnemy17SimplifiedMotion/README.md) — единый рисунок и фактура, стадия оценки; в Unity не импортировано.'
working=(root/catalog).read_text(encoding='utf-8-sig')
lines=[line for line in working.splitlines() if line.startswith('- [TASK-005') and ('ClawPrototype01' in line or 'FastGroupEnemy18' in line or 'FastGroupEnemy19' in line)]
assert len(lines)==3 and old in head
intended=head.replace(old,'\n'.join(lines))
patch=''.join(difflib.unified_diff(head.splitlines(True),intended.splitlines(True),fromfile='a/'+catalog,tofile='b/'+catalog))
(folder/'own-catalog.patch').write_text(patch,encoding='utf-8',newline='\n')
initial=folder/'initial-editor.json'
data=json.loads(initial.read_text(encoding='utf-8-sig'))
if 'roots' in data['scenes'][0]:
    def count(node):return 1+sum(count(c) for c in node['children'])
    for scene in data['scenes']:
        scene['objectCount']=sum(count(r) for r in scene.pop('roots'))
    initial.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Catalog patch contains only the three TASK-005 entries; original editor snapshot reduced to scene state/count.')
