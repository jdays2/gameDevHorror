"""Content identities and local-link checks for this scoped TASK-005 revision."""
from pathlib import Path
import hashlib, json, re

PROJECT = Path(__file__).resolve().parents[1]
FOLDER = PROJECT / 'Docs/Validation/ScorpionR9'

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    catalog_path = PROJECT / 'Concepts/catalog.json'
    catalog = json.loads(catalog_path.read_text(encoding='utf-8'))
    for folder, original, status in [
        ('Used/Audio/ScorpionDiscovery01', 'Originals/insect-screech.mp3', 'Current original insect alarm; reference descriptions only, no reference samples copied.'),
        ('Unused/Audio/ScorpionPressureAccent01', 'discovery.mp3', 'Unused pressure accent, superseded by owner request for insect squeal; never imported.')
    ]:
        base = PROJECT / 'Concepts' / folder
        provenance = json.loads((base / 'provenance.json').read_text(encoding='utf-8'))
        target = base / original
        relative = target.relative_to(PROJECT).as_posix()
        catalog = [item for item in catalog if item.get('to') != relative]
        catalog.append(dict(source='Runway Sound Effects', taskId=provenance['taskId'], to=relative, reason=status, sha256=digest(target)))
    catalog_path.write_text(json.dumps(catalog, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    docs = [PROJECT/'Docs/Enemies/SCORPION.md', PROJECT/'Concepts/Used/Audio/Scorpion01/README.md',
            PROJECT/'Concepts/Used/Audio/ScorpionDiscovery01/README.md', PROJECT/'Concepts/Used/Audio/CreatureRupture06/README.md']
    docs += sorted(FOLDER.glob('*.md'))
    missing = []
    for path in docs:
        for link in re.findall(r'\]\(([^)]+)\)', path.read_text(encoding='utf-8')):
            if link.startswith(('http:', 'https:', '#')):
                continue
            target = path.parent / link.split('#')[0]
            if not target.exists():
                missing.append(dict(file=path.relative_to(PROJECT).as_posix(), link=link))
    fingerprints = []
    for name in ['MuscleWad', 'DenseClot', 'TissueStrip']:
        for ext in ['.asset', '.asset.meta', '.png', '.png.meta']:
            path = PROJECT / 'Assets/EmergencyShift/Art/Effects/CreatureFlesh04' / (name + ext)
            fingerprints.append(dict(path=path.relative_to(PROJECT).as_posix(), sha256=digest(path)))
    result = dict(task='TASK-005', revision='r9-discovery-death-dim-eyes', missingLocalLinks=missing,
                  sharedFlesh=fingerprints, visualMovementFramesChanged=False,
                  newAudio='Concepts/Used/Audio/ScorpionDiscovery01/processing.json',
                  sharedDeathBank='Assets/EmergencyShift/Data/Audio/CreatureRupture06.asset',
                  originalSharedAssetsModified=False, artisticApproval=False, stressTest=False)
    (FOLDER/'content-checks.json').write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(dict(missingLocalLinks=missing, sharedFiles=len(fingerprints))))

if __name__ == '__main__':
    main()
