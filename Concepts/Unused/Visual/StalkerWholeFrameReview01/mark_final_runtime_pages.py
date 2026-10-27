from pathlib import Path
import json,sys
p=Path(__file__).resolve().parent/'RuntimeFullFinal01/native-page-coverage.json'
d=json.loads(p.read_text())
profile=int(sys.argv[1]);lo=int(sys.argv[2]);hi=int(sys.argv[3]);note=sys.argv[4]
count=0
for page in d['pages']:
 if page['profileIndex']==profile and ((isinstance(page['page'],int) and lo<=page['page']<=hi) or (lo==-1 and isinstance(page['page'],str))):
  page['reviewed']=True;page['reviewNote']=note;count+=1
p.write_text(json.dumps(d,indent=2))
print(json.dumps({'marked':count,'reviewed':sum(x['reviewed'] for x in d['pages']),'total':len(d['pages'])}))
