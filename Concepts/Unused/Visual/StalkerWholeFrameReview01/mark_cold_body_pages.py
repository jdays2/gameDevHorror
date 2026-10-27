import json
import sys
from pathlib import Path
p=Path(__file__).resolve().parent/'ColdBodyFinal01/review-coverage.json'
d=json.loads(p.read_text())
bg,first,last,note=sys.argv[1],int(sys.argv[2]),int(sys.argv[3]),sys.argv[4]
for q in d['pages']:
    if q['background']==bg and first<=q['page']<=last:
        q['viewed']=True
        q['reviewNote']=note
p.write_text(json.dumps(d,indent=2))
print(sum(q['viewed'] for q in d['pages']), '/', len(d['pages']))
