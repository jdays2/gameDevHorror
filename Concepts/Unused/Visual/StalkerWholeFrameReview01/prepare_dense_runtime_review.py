"""Independent actual-capture provenance. Completed profile manifests only."""
from pathlib import Path
from collections import defaultdict
from concurrent.futures import ThreadPoolExecutor
import hashlib,json
from PIL import Image

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[3]/'Concepts/Used/Stalker/Postures05/Scenarios/stalker-dense-final'
OUT=HERE/'RuntimeDenseFinal01'
OLD=HERE/'RuntimeFullFinal01/review-coverage.json'

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,d):
    t=p.with_suffix(p.suffix+'.tmp')
    t.write_text(json.dumps(d,indent=2),encoding='utf-8');t.replace(p)

def main():
    OUT.mkdir(exist_ok=True)
    old=json.loads(OLD.read_text(encoding='utf-8'))
    oldp={p['key']:p for p in old['profiles']}
    manifests=sorted(ROOT.glob('*/manifest.json'))
    assert manifests,'No completed profile manifests'
    profiles=[];groups=defaultdict(list)
    for mp in manifests:
        key=mp.parent.name;msha=sha(mp)
        m=json.loads(mp.read_text(encoding='utf-8-sig'))
        assert len(m['frames'])==1540
        prior={r['frame']:r for r in oldp[key]['frames']}
        def inspect(f):
            p=mp.parent/f"frame-{f['frame']:05d}.png"
            s=sha(p);rgb=hashlib.sha256(Image.open(p).convert('RGB').tobytes()).hexdigest()
            prev=prior[f['frame']]
            return {'frame':f['frame'],'sha256':s,'decodedRgbSha256':rgb,
                'priorSha256':prev['sha256'],'exactPngPrior':s==prev['sha256'],
                'exactRgbPrior':rgb==prev['decodedRgbSha256'],'facts':f}
        with ThreadPoolExecutor(max_workers=4) as pool: records=list(pool.map(inspect,m['frames']))
        assert sha(mp)==msha,'Completed manifest changed during read'
        scenarios=[]
        for span in m['spans']:
            rows=records[span['first']:span['last']+1]
            assert len(rows)==span['count']
            seq=hashlib.sha256(''.join(r['decodedRgbSha256'] for r in rows).encode()).hexdigest()
            info={**span,'sequenceRgbSha256':seq,
                'exactPngPrior':sum(r['exactPngPrior'] for r in rows),
                'exactRgbPrior':sum(r['exactRgbPrior'] for r in rows),
                'differentFromPrior':[r['frame'] for r in rows if not r['exactRgbPrior']]}
            scenarios.append(info);groups[(span['scenario'],seq)].append(key)
        p={'key':key,'manifestSha256':msha,'sourceVariantConfiguration':m['sourceVariantConfiguration'],
            'speed':m['speed'],'sampleHz':m['sampleHz'],'attackDuration':m['AttackDuration'],
            'strikeAt':m['StrikeAt'],'frames':records,'scenarios':scenarios}
        profiles.append(p)
        write(OUT/(key+'-provenance.json'),p)
        print(json.dumps({'key':key,'exactRgbPrior':sum(r['exactRgbPrior'] for r in records),
            'differentByScenario':{s['scenario']:s['count']-s['exactRgbPrior'] for s in scenarios if s['exactRgbPrior']!=s['count']}}),flush=True)
    out={'status':'PROVENANCE_PREPARED; ACTUAL_VISUAL_REVIEW_AND_GLOBAL_RUN_GATE_PENDING',
        'sourceRoot':str(ROOT),'priorCoverageSha256':sha(OLD),
        'completedProfileManifestCount':len(profiles),'actualFrameCount':sum(len(p['frames']) for p in profiles),
        'profiles':profiles,'scenarioGroups':[{'scenario':s,'sequenceRgbSha256':h,'members':v} for (s,h),v in groups.items()],
        'limits':'A manifest proves an individually completed profile. This report does not assert global run completion or art/tempo PASS. Prior inheritance requires actual current decoded RGB identity and prior visual scope.'}
    write(OUT/'review-coverage.json',out)
    print(json.dumps({'prepared':len(profiles),'frames':out['actualFrameCount'],'uniqueScenarioSequences':len(groups)}),flush=True)

if __name__=='__main__':main()
