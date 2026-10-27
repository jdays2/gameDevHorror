"""Read-only endpoint sampling study; validates its XY formula against C# JSON."""
from pathlib import Path
import json
import numpy as np

HERE = Path(__file__).resolve().parent
FAMILY = HERE.parent
PPU = 640/3.3


def smooth(x):
    x = np.clip(x, 0, 1)
    return x*x*(3-2*x)


def main():
    poses = {p['name']: p for p in json.loads((FAMILY/'attack-poses.json').read_text())}
    def feet(group, phase, progress):
        start = np.asarray(poses[f'{group}-{phase:02d}-00']['feet'])[:, :2]
        end = np.asarray(poses[f'{group}-{phase:02d}-07']['feet'])[:, :2]
        weight = smooth((progress-.3)/.7) if group == 'entry' else smooth(progress/.7)
        return start+(end-start)*weight
    max_error = 0.
    for group in ['entry', 'return']:
        for phase in range(48):
            for sample in range(8):
                observed = np.asarray(poses[f'{group}-{phase:02d}-{sample:02d}']['feet'])[:, :2]
                max_error = max(max_error, float(np.max(np.abs(feet(group, phase, sample/7)-observed))))
    assert max_error < 2e-7, max_error
    sampling = []
    for count in [8, 15, 23]:
        adjacent = []
        for group in ['entry', 'return']:
            for phase in range(48):
                for sample in range(count-1):
                    delta = np.linalg.norm(feet(group, phase, (sample+1)/(count-1))-feet(group, phase, sample/(count-1)), axis=-1)*PPU
                    adjacent.append({'group': group, 'phase': phase, 'sampleFrom': sample, 'sampleTo': sample+1,
                        'maximumToePixels': float(delta.max()), 'leg': int(np.argmax(delta))})
        sampling.append({'bridgeFrames': count, 'maximumAdjacentAllPhases': max(adjacent, key=lambda d: d['maximumToePixels']),
            'phase25Return': [v for v in adjacent if v['group']=='return' and v['phase']==25]})
    scheduling = []
    for count, entry, returned, end in [(8, .15, .85, .97), (8, .15, .67, .97), (15, .15, .67, .97), (15, .25, .67, .97), (15, .30, .67, .97)]:
        peaks = {}
        for group in ['entry', 'return']:
            worst = None
            for phase in range(48):
                old = None
                for tick in range(49):
                    progress = tick/48
                    if group=='entry' and progress<=entry:
                        u = progress/entry
                    elif group=='return' and progress>=returned:
                        u = np.clip((progress-returned)/(end-returned), 0, 1)
                    else:
                        continue
                    sample = int(np.rint(u*(count-1)))
                    current = feet(group, phase, sample/(count-1))
                    if old is not None:
                        delta = np.linalg.norm(current-old[1], axis=-1)*PPU
                        candidate = {'phase': phase, 'tickFrom': old[0], 'tickTo': tick,
                            'sampleFrom': old[2], 'sampleTo': sample, 'maximumToePixels': float(delta.max()), 'leg': int(np.argmax(delta))}
                        if worst is None or candidate['maximumToePixels']>worst['maximumToePixels']:
                            worst = candidate
                    old = (tick, current, sample)
            peaks[group] = worst
        scheduling.append({'bridgeFrames': count, 'entryFraction': entry, 'returnStart': returned,
            'returnComplete': end, 'actualDuration': .8, 'sampleHz': 60, 'worst': peaks})
    # Explicit interruption bridge timing; its authored path already eases.
    interruptions = []
    for duration in [.18, .22, .24]:
        for eased_indices in [False, True]:
            worst = None
            for phase in range(48):
                previous = None
                for tick in range(int(np.ceil(duration*60))+1):
                    u = min(1, tick/(duration*60))
                    index_progress = smooth(u) if eased_indices else u
                    sample = int(np.rint(index_progress*14))
                    current = feet('return', phase, sample/14)
                    if previous is not None:
                        delta = np.linalg.norm(current-previous[1], axis=-1)*PPU
                        candidate = {'phase': phase, 'tickFrom': previous[0], 'tickTo': tick,
                            'sampleFrom': previous[2], 'sampleTo': sample, 'maximumToePixels': float(delta.max())}
                        if worst is None or candidate['maximumToePixels']>worst['maximumToePixels']:
                            worst = candidate
                    previous = (tick, current, sample)
            interruptions.append({'bridgeFrames': 15, 'bridgeSeconds': duration,
                'smoothstepIndexProgress': eased_indices, 'worst': worst})
    out = HERE/'RuntimeAttackCuts03'
    result = {'xyFormulaMaximumErrorAgainstActual8SampleCSharpJson': max_error,
        'pixelsPerUnitAtActual640Camera': PPU, 'sampling': sampling, 'normalSchedule': scheduling,
        'interruptionBridgeSchedule': interruptions,
        'limits': 'Endpoint XY only, fixed gait phase. Dense knees/length/floor/skin must be exported from actual C# and reviewed separately. No source or runtime file changed.'}
    (out/'timing-sampling-study.json').write_text(json.dumps(result, indent=2))
    print(json.dumps({'formulaError': max_error, 'adjacent': [{'frames': v['bridgeFrames'], 'max': v['maximumAdjacentAllPhases']} for v in sampling],
        'normal': scheduling, 'interruptions': interruptions}, indent=2))


if __name__ == '__main__':
    main()
