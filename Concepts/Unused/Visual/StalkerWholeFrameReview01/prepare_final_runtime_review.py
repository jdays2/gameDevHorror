"""Read-only final runtime provenance and exact prior-run comparison."""
from pathlib import Path
from collections import defaultdict
from concurrent.futures import ThreadPoolExecutor
import hashlib,json
from PIL import Image

HERE=Path(__file__).resolve().parent
BASE=HERE.parents[3]/'Concepts/Used/Stalker/Postures05/Scenarios'
ROOT=BASE/'stalker-final'
BEFORE=BASE/'stalker-interruption-before'
OUT=HERE/'RuntimeFullFinal01'

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def inspect(args):
    key,frame=args
    p=ROOT/key/f"frame-{frame['frame']:05d}.png"
    old=BEFORE/key/p.name
    file_sha=sha(p)
    rgb_sha=hashlib.sha256(Image.open(p).convert('RGB').tobytes()).hexdigest()
    old_sha=sha(old)
    old_rgb=rgb_sha if old_sha==file_sha else hashlib.sha256(Image.open(old).convert('RGB').tobytes()).hexdigest()
    return {'frame':frame['frame'],'sha256':file_sha,'decodedRgbSha256':rgb_sha,'beforeSha256':old_sha,'exactRgbBefore':old_rgb==rgb_sha,'facts':frame}

def main():
    OUT.mkdir(exist_ok=True)
    run=json.loads((ROOT/'run.json').read_text(encoding='utf-8-sig'))
    assert run['completedAll'] and not run['captureErrors']
    profiles=[]; scenario_groups=defaultdict(list)
    for entry in run['completed']:
        assert entry['species']=='stalker'
        key=entry['key'];p=ROOT/key/'manifest.json'
        m=json.loads(p.read_text(encoding='utf-8-sig'))
        assert len(m['frames'])==entry['frames']==1540
        with ThreadPoolExecutor(max_workers=4) as pool:
            records=list(pool.map(inspect,[(key,f) for f in m['frames']]))
        scenarios=[]
        for scenario in dict.fromkeys(f['scenario'] for f in m['frames']):
            rows=[r for r in records if r['facts']['scenario']==scenario]
            seq=hashlib.sha256(''.join(r['decodedRgbSha256'] for r in rows).encode()).hexdigest()
            exact=sum(r['exactRgbBefore'] for r in rows)
            info={'scenario':scenario,'first':rows[0]['frame'],'last':rows[-1]['frame'],'count':len(rows),'exactRgbBefore':exact,'differentFromBefore':[r['frame'] for r in rows if not r['exactRgbBefore']],'sequenceRgbSha256':seq}
            scenarios.append(info)
            scenario_groups[(scenario,seq)].append({'key':key,'first':info['first'],'last':info['last']})
        profiles.append({'key':key,'manifestSha256':sha(p),'sourceVariantConfiguration':m['sourceVariantConfiguration'],'speed':m['speed'],'sampleHz':m['sampleHz'],'frames':records,'scenarios':scenarios})
        print(json.dumps({'profile':key,'exactRgbBefore':sum(r['exactRgbBefore'] for r in records),'differentByScenario':{s['scenario']:s['count']-s['exactRgbBefore'] for s in scenarios if s['count']!=s['exactRgbBefore']}}),flush=True)
    groups=[{'scenario':s,'sequenceRgbSha256':h,'members':members} for (s,h),members in scenario_groups.items()]
    out={'status':'PREPARED; visual review pending','runSha256':sha(ROOT/'run.json'),'runCompleted':run['completed'],'profiles':profiles,'scenarioGroups':groups,'actualFrameCount':sum(len(p['frames']) for p in profiles),'uniqueScenarioSequences':len(groups)}
    (OUT/'review-coverage.json').write_text(json.dumps(out,indent=2))
    print(json.dumps({'frames':out['actualFrameCount'],'uniqueScenarioSequences':len(groups),'path':str(OUT/'review-coverage.json')}))

if __name__=='__main__':main()
