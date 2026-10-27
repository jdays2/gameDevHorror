"""Record only page files actually returned at original detail and inspected."""
from pathlib import Path
import hashlib,json,sys
HERE=Path(__file__).resolve().parent/'RuntimeDenseFinal01'
p=HERE/('unique-pending-coverage.json' if '--unique' in sys.argv else 'native-page-coverage.json')
d=json.loads(p.read_text(encoding='utf-8'))
names=set(x for x in sys.argv[1:] if x!='--unique');found=set()
for pg in d['pages']:
 name=Path(pg['path']).name
 if name not in names:continue
 assert hashlib.sha256(Path(pg['path']).read_bytes()).hexdigest()==pg['sha256'],name
 pg['reviewed']=True;pg['reviewStatus']='ACTUALLY_VIEWED_ORIGINAL_1X_NO_RESIZE'
 found.add(name)
assert found==names,(found,names)
d['actuallyViewedPages']=sum(pg['reviewed'] for pg in d['pages'])
d['actuallyViewedFrameInstances']=sum(len(pg['frames']) for pg in d['pages'] if pg['reviewed'])
t=p.with_suffix('.json.tmp');t.write_text(json.dumps(d,indent=2),encoding='utf-8');t.replace(p)
print(json.dumps({'viewedPages':d['actuallyViewedPages'],'viewedFrameInstances':d['actuallyViewedFrameInstances']}))
