"""Record exact source/export identities for the two visual-only prefab families."""
from pathlib import Path
import hashlib
import json

PROJECT=Path(__file__).resolve().parents[1]


def record(path):
    return {'path':path.relative_to(PROJECT).as_posix(),
            'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}


def main():
    for folder,used,art,animation in [
        ('FastGroupEnemy23AssetPack','ClawVisual01','ClawVisual','ClawVisual'),
        ('Scorpion01','Scorpion01','Scorpion','ScorpionVisual')]:
        source=PROJECT/'Concepts/Unused/Visual'/folder
        manifest=json.loads((source/'asset-pack.json').read_text(encoding='utf-8'))
        paths=set()
        for state in manifest['states']:
            paths.update(PROJECT/p for p in state.get('sources',[]))
        paths.update(source/p['file'] for p in json.loads((source/'technical-validation.json').read_text(encoding='utf-8')).get('sourceFrames',[]) if 'file' in p)
        paths.update(PROJECT/r['source'] for r in manifest.get('remains',[]))
        paths.update(p for p in source.glob('body-source-*.png'))
        paths.update(p for p in (source/'sources').glob('*.png'))
        assets=list((PROJECT/'Assets/EmergencyShift/Art/Enemies'/art).rglob('*'))
        assets+=list((PROJECT/'Assets/EmergencyShift/Animations/Enemies'/animation).rglob('*'))
        prefab=PROJECT/'Assets/EmergencyShift/Prefabs/Enemies'/(animation+'.prefab')
        assets.extend([prefab,prefab.with_suffix('.prefab.meta')])
        result={'revision':manifest['revision'],'task':'TASK-005','audio':False,'gameplayBrain':False,
                'sources':[record(p) for p in sorted(paths)],
                'production':[record(p) for p in sorted(assets) if p.is_file()],
                'generationProvenance':record(source/('image-generation.json' if art=='ClawVisual' else 'provenance.json')),
                'unityEvidence':'Docs/Validation/CreatureVisuals01/prefabs.json',
                'artisticApproval':'Not inferred from import or technical checks.'}
        destination=PROJECT/'Concepts/Used/Visual'/used/'import-manifest.json'
        destination.parent.mkdir(parents=True,exist_ok=True)
        destination.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        print(json.dumps({'family':animation,'sources':len(paths),'production':len(result['production'])}))


if __name__=='__main__':
    main()
