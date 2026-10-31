"""Record R11 source identity, owner feedback and scoped content checks."""
from pathlib import Path
import hashlib
import json
import re
import subprocess

PROJECT = Path(__file__).resolve().parents[1]
FOLDER = PROJECT / 'Docs/Validation/ScorpionPaletteR11'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    catalog_path = PROJECT / 'Concepts/catalog.json'
    catalog = json.loads(catalog_path.read_text(encoding='utf-8'))
    for item in catalog:
        if 'ScorpionChatter01/Originals/' in item.get('to', ''):
            item['reason'] = 'Historical: rejected by owner on 2026-10-31 as monkey-like; replaced in active bank and viewer by the approved Detection.wav.'
    catalog_path.write_text(json.dumps(catalog, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

    source = PROJECT / 'Concepts/Unused/Visual/Scorpion01'
    pack = json.loads((source/'asset-pack.json').read_text(encoding='utf-8'))
    frames = []
    for state in pack['states']:
        paths = [state['asset']]
        if state.get('emission'):
            paths.append(state['emission']['asset'])
        for relative in paths:
            current = subprocess.run(['git', 'hash-object', '--', relative], cwd=PROJECT, check=True, capture_output=True, text=True).stdout.strip()
            original = subprocess.run(['git', 'rev-parse', 'HEAD:' + relative], cwd=PROJECT, check=True, capture_output=True, text=True).stdout.strip()
            frames.append(dict(path=relative, gitObject=current, unchangedFromCommittedSource=current == original))

    docs = [PROJECT/'Docs/Enemies/SCORPION.md', PROJECT/'Concepts/Used/Audio/ScorpionDiscovery01/README.md',
            PROJECT/'Concepts/Used/Audio/ScorpionChatter01/README.md',
            PROJECT/'Concepts/Used/Visual/Scorpion01/README.md', source/'README.md']
    docs += list(FOLDER.glob('*.md'))
    docs += list((PROJECT/'Docs/Validation/ScorpionR10').glob('*.md'))
    missing = []
    for doc in docs:
        for link in re.findall(r'\]\(([^)]+)\)', doc.read_text(encoding='utf-8')):
            if link.startswith(('http:', 'https:', '#')):
                continue
            if not (doc.parent/link.split('#')[0]).exists():
                missing.append(dict(file=doc.relative_to(PROJECT).as_posix(), link=link))

    detection = PROJECT/'Assets/EmergencyShift/Audio/Enemies/Scorpion/Detection.wav'
    result = dict(task='TASK-005', date='2026-10-31', revision='r11-wine-copper-shared-squeak',
        sourceFrames=frames, sourceMovementFramesChanged=any(not item['unchangedFromCommittedSource'] for item in frames),
        missingLocalLinks=missing, detectionSha256=digest(detection),
        ordinarySqueak='Same original Detection.wav; no new generation or pitch alteration.',
        ownerFeedback='Three Chatter candidates rejected; discovery sound selected for periodic use.',
        artisticApproval=False, stressTest=False, authoredSceneSaved=False)
    (FOLDER/'content-checks.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    old_checks = PROJECT/'Docs/Validation/ScorpionR10/content-checks.json'
    if old_checks.exists():
        previous = json.loads(old_checks.read_text(encoding='utf-8'))
        previous['missingLocalLinks'] = missing
        previous['supersededPresentation'] = 'Docs/Validation/ScorpionPaletteR11/README.md'
        old_checks.write_text(json.dumps(previous, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(result, ensure_ascii=False))
    if missing or result['sourceMovementFramesChanged']:
        raise SystemExit('Scorpion R11 content checks failed.')


if __name__ == '__main__':
    main()
