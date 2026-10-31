"""Hash the delivered feature, sounds and authored room evidence without signed URLs."""
from pathlib import Path
import hashlib,json

PROJECT=Path(__file__).resolve().parents[1]

def entry(path):
    return dict(path=path.relative_to(PROJECT).as_posix(),bytes=path.stat().st_size,sha256=hashlib.sha256(path.read_bytes()).hexdigest())

def main():
    paths=[]
    for folder in ['Assets/EmergencyShift/Runtime/Features/Scorpion','Assets/EmergencyShift/Audio/Enemies/Scorpion','Assets/EmergencyShift/Data/Enemies/Scorpion']:
        paths.extend(p for p in (PROJECT/folder).rglob('*') if p.is_file())
    for file in ['Assets/EmergencyShift/Prefabs/Enemies/Scorpion.prefab','Assets/EmergencyShift/Prefabs/Enemies/Scorpion.prefab.meta']:
        paths.append(PROJECT/file)
    for file in ['Assets/EmergencyShift/Prefabs/Enemies/ScorpionToxicPool.prefab','Assets/EmergencyShift/Prefabs/Enemies/ScorpionToxicPool.prefab.meta',
                 'Assets/EmergencyShift/Art/Enemies/Scorpion/RuptureR10.png','Assets/EmergencyShift/Art/Enemies/Scorpion/RuptureR10.png.meta']:
        paths.append(PROJECT/file)
    paths.extend(path for path in (PROJECT/'Assets/EmergencyShift/Materials/Characters/Scorpion').rglob('*') if path.is_file())
    shared=[PROJECT/'Assets/EmergencyShift/Data/Audio/CreatureRupture06.asset']
    shared.extend(PROJECT/f'Assets/EmergencyShift/Art/Effects/CreatureFlesh04/{name}.asset' for name in ['MuscleWad','DenseClot','TissueStrip'])
    historical_names = {'Alarm.wav', 'Presence.wav', 'Squeak1.wav', 'Squeak2.wav', 'Squeak3.wav'}
    historical = [path for path in paths if path.name.removesuffix('.meta') in historical_names]
    paths = [path for path in paths if path not in historical]
    result=dict(task='TASK-005',revision='scorpion-r11-wine-copper-shared-squeak',production=[entry(p) for p in sorted(paths)], historicalAudio=[entry(p) for p in sorted(historical)], sharedDependencies=[entry(p) for p in shared],
        scene='Assets/EmergencyShift/Scenes/MechanicsSandbox.unity',sceneEvidence='Docs/Validation/ScorpionRoom01/authoring.json',
        audioProvenance=['Concepts/Used/Audio/Scorpion01/provenance.json','Concepts/Used/Audio/ScorpionDiscovery01/provenance.json'],
        historicalAudioProvenance=['Concepts/Used/Audio/ScorpionChatter01/provenance.json'],
        artisticApproval=False,stressTest=False)
    (PROJECT/'Concepts/Used/Visual/Scorpion01/runtime-manifest.json').write_text(json.dumps(result,indent=2)+'\n')
    source=PROJECT/'Concepts/Used/Audio/Scorpion01'
    identities=[entry(p) for p in sorted((source/'Sources').glob('*.mp3'))]
    (source/'source-identities.json').write_text(json.dumps(identities,indent=2)+'\n')
    print(json.dumps(dict(production=len(paths),audioSources=len(identities))))

if __name__=='__main__':
    main()
