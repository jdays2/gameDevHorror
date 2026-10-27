from pathlib import Path
import json
import sys

p = Path(__file__).resolve().parent / 'ColdAttackAdjacent01/review-coverage.json'
d = json.loads(p.read_text())
background, first, last, note = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
count = 0
for page in d['pages']:
    if page['background'] == background and first <= page['page'] <= last:
        page['viewed'] = True
        page['reviewNote'] = note
        count += 1
p.write_text(json.dumps(d, indent=2))
print(json.dumps({'marked': count, 'viewed': sum(p['viewed'] for p in d['pages']), 'total': len(d['pages'])}))
