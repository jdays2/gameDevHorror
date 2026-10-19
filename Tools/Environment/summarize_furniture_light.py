import argparse
import gzip
import json
import statistics as st
from collections import defaultdict
from pathlib import Path

parser = argparse.ArgumentParser(description='Aggregate native furniture benchmark JSON or JSON.gz reports.')
parser.add_argument('reports', nargs='+', type=Path)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
summary = {'runs': []}
metrics = ('frameMs', 'mainMs', 'renderMs', 'gpuMs', 'presentWaitMs', 'gcBytes', 'batches', 'draws')


def percentile(values, p):
    values = sorted(values)
    position = (len(values) - 1) * p
    lower = int(position)
    upper = min(lower + 1, len(values) - 1)
    return values[lower] + (values[upper] - values[lower]) * (position - lower)


for source in args.reports:
    opener = gzip.open if source.suffix == '.gz' else open
    with opener(source, 'rt', encoding='utf-8-sig') as stream:
        report = json.load(stream)
    groups = defaultdict(list)
    for segment in report['segments']:
        key = (segment['scenario'], segment['ceiling'], segment['shadows'], segment['variant'])
        groups[key].append(segment)
    run = {k: v for k, v in report.items() if k != 'segments'}
    run['groups'] = []
    for key, segments in groups.items():
        row = dict(zip(('scenario', 'ceiling', 'shadows', 'variant'), key))
        row['segments'] = []
        row['metrics'] = {}
        for segment in segments:
            values = {}
            for metric in metrics:
                samples = [f[metric] for f in segment['frames'] if f[metric] >= 0 and (metric != 'gpuMs' or f[metric] > 0)]
                values[metric] = st.median(samples) if samples else None
            row['segments'].append({'repetition': segment['repetition'], 'order': segment['order'], 'frames': len(segment['frames']), 'medians': values})
        for metric in metrics:
            medians = [s['medians'][metric] for s in row['segments'] if s['medians'][metric] is not None]
            raw = [f[metric] for s in segments for f in s['frames'] if f[metric] >= 0 and (metric != 'gpuMs' or f[metric] > 0)]
            row['metrics'][metric] = None if not medians else {'medianOfSegments': st.median(medians), 'segmentMin': min(medians), 'segmentMax': max(medians), 'rawP95': percentile(raw, .95), 'samples': len(raw)}
        run['groups'].append(row)
    run['pairedDeltas'] = []
    for scenario, ceiling, shadows in sorted({k[:3] for k in groups}):
        for first, second in ((0, 2), (1, 2)):
            a = next(g for g in run['groups'] if (g['scenario'], g['ceiling'], g['shadows'], g['variant']) == (scenario, ceiling, shadows, first))
            b = next(g for g in run['groups'] if (g['scenario'], g['ceiling'], g['shadows'], g['variant']) == (scenario, ceiling, shadows, second))
            deltas = {}
            for metric in ('frameMs', 'mainMs', 'renderMs', 'gpuMs'):
                pairs = []
                for a_segment in a['segments']:
                    b_segment = next(s for s in b['segments'] if s['repetition'] == a_segment['repetition'])
                    av, bv = a_segment['medians'][metric], b_segment['medians'][metric]
                    if av is not None and bv is not None:
                        pairs.append(bv - av)
                deltas[metric] = None if not pairs else {'median': st.median(pairs), 'min': min(pairs), 'max': max(pairs), 'positivePairs': sum(p > 0 for p in pairs), 'count': len(pairs)}
            run['pairedDeltas'].append({'scenario': scenario, 'ceiling': ceiling, 'shadows': shadows, 'comparison': f'C-minus-{chr(65 + first)}', 'metrics': deltas})
    summary['runs'].append(run)

args.output.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf-8')
for run in summary['runs']:
    print(f"{run['width']}x{run['height']} {run['gpu']} failure={run['failure']!r}")
    for group in run['groups']:
        values = []
        for metric in ('frameMs', 'mainMs', 'renderMs', 'gpuMs', 'draws', 'gcBytes'):
            data = group['metrics'][metric]
            values.append(f'{metric}={data["medianOfSegments"]:.4f}' if data else f'{metric}=N/A')
        print(group['scenario'], 'ceiling='+str(group['ceiling']), 'shadow='+str(group['shadows']), chr(65+group['variant']), *values)
