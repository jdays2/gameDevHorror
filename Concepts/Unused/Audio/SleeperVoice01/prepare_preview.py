"""Prepare local Sleeper auditions from four authorized generated MP3 originals.

No Unity writes, source replacement, extra service calls or Stalker samples.
Requires NumPy and an explicit FFmpeg executable.
"""
import argparse
import hashlib
import json
import subprocess
import wave
from pathlib import Path

import numpy as np

BASE = Path(__file__).resolve().parent
RATE = 48000
PEAK_TARGET = {'sleep': 0.035, 'wake': 0.68, 'pain': 0.38, 'rupture': 0.72}


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read_json(path):
    return json.loads(path.read_text(encoding='utf-8'))


def measurements(samples):
    peak = float(np.max(np.abs(samples)))
    rms = float(np.sqrt(np.mean(samples * samples)))
    return {
        'frames': len(samples),
        'durationSeconds': len(samples) / RATE,
        'sampleRate': RATE,
        'channels': 1,
        'peak': peak,
        'peakDbFS': float(20 * np.log10(max(peak, 1e-12))),
        'rms': rms,
        'rmsDbFS': float(20 * np.log10(max(rms, 1e-12))),
        'samplesAtOrAboveFullScale': int(np.count_nonzero(np.abs(samples) >= 1)),
        'finite': bool(np.isfinite(samples).all()),
        'dcOffset': float(np.mean(samples)),
        'boundaryJump': float(abs(samples[0] - samples[-1])),
    }


def write_wav(path, samples):
    assert np.isfinite(samples).all()
    assert np.max(np.abs(samples)) < 0.98
    pcm = np.rint(samples * 32767).astype('<i2')
    with wave.open(str(path), 'wb') as stream:
        stream.setnchannels(1)
        stream.setsampwidth(2)
        stream.setframerate(RATE)
        stream.writeframes(pcm.tobytes())
    quantized = pcm.astype(float) / 32768
    return dict(file=path.relative_to(BASE).as_posix(), sha256=sha(path), bytes=path.stat().st_size, bits=16, **measurements(quantized))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ffmpeg', required=True)
    args = parser.parse_args()
    requests = read_json(BASE / 'requests.json')['requests']
    tool_metadata = {row['id']: row for row in read_json(BASE / 'source-metadata.json')}
    clips = {}
    report = []
    for request in requests:
        identity = request['id']
        source = BASE / f'Originals/sleeper_{identity}_01.mp3'
        raw = subprocess.check_output([args.ffmpeg, '-v', 'error', '-i', str(source), '-f', 'f32le', '-ar', str(RATE), '-ac', '1', '-'])
        decoded = np.frombuffer(raw, '<f4').astype(float)
        assert len(decoded) > 0 and np.isfinite(decoded).all()
        source_metrics = measurements(decoded)
        samples = decoded - decoded.mean()
        if request['loop']:
            overlap = round(0.06 * RATE)
            fade = np.linspace(0, 1, overlap)
            samples[:overlap] = samples[-overlap:] * (1 - fade) + samples[:overlap] * fade
            samples = samples[:-overlap]
            edge_processing = '60ms wrap crossfade; output shortened by 60ms, no start/end silence fades'
        else:
            attack = min(round(0.0005 * RATE), len(samples) // 4)
            release = min(round(0.008 * RATE), len(samples) // 4)
            samples[:attack] *= np.linspace(0, 1, attack)
            samples[-release:] *= np.linspace(1, 0, release)
            edge_processing = '0.5ms entry and 8ms exit fades'
        gain = PEAK_TARGET[identity] / max(float(np.max(np.abs(samples))), 1e-12)
        samples *= gain
        path = BASE / f'WAV/sleeper_{identity}_01.wav'
        output = write_wav(path, samples)
        clips[identity] = samples
        report.append({
            'id': identity,
            'prompt': request['promptText'],
            'taskId': request['taskId'],
            'provider': 'Runway connected workspace',
            'taskType': request['taskType'],
            'requestedDurationSeconds': request['duration'],
            'loopRequested': request['loop'],
            'toolMetadata': tool_metadata[identity]['meta'],
            'nativeOriginal': {'file': source.relative_to(BASE).as_posix(), 'sha256': sha(source), 'bytes': source.stat().st_size, 'format': 'MP3, native generated download; preserved unchanged', 'decodedMono48k': source_metrics},
            'processing': {'dcRemoval': True, 'edgeProcessing': edge_processing, 'peakTarget': PEAK_TARGET[identity], 'gain': gain, 'note': 'Explicit linear gain to separate per-cue peak targets; no compression, Stalker samples, pitch shifting, reverb or added vocal layers'},
            'wav': output,
        })
    montage = np.zeros(round(15 * RATE))
    timeline = []
    cursor = 0.25
    for identity in ('sleep', 'wake', 'pain', 'rupture'):
        samples = clips[identity]
        start = round(cursor * RATE)
        montage[start:start + len(samples)] += samples
        timeline.append({'id': identity, 'atSeconds': start / RATE, 'endSeconds': (start + len(samples)) / RATE})
        cursor += len(samples) / RATE + 0.85
    montage = montage[:round((cursor + 0.15) * RATE)]
    preview = write_wav(BASE / 'Previews/sleeper-voice-catalogue.wav', montage)
    report_data = {
        'date': '2026-10-17',
        'family': 'SleeperVoice01',
        'status': 'Four new voice auditions; artistic evaluation pending; no Unity integration',
        'method': 'Four text-to-sound-effect generations through built-in Runway connector, followed by local decode and restrained DSP',
        'creditCostPerToolSchema': 11,
        'creditsRemainingAfterSubmissions': 408,
        'rights': {'source': 'Generated outputs from the connected Runway workspace; source prompts and task IDs retained', 'terms': 'Use is governed by the current Runway service/account terms. No CC0 or third-party stock license is asserted; this record is provenance, not an independent rights determination.', 'termsReference': 'https://runwayml.com/terms-of-use/', 'signedUrlsOrTokensStored': False},
        'stalkerReference': {'family': 'Concepts/Used/Audio/StalkerMembrane04', 'role': 'Read-only reference for the shared organic sonic palette; no audio copied, overwritten or layered'},
        'listening': {'performed': False, 'reason': 'No audio-listening tool is available to this agent; generated clips are provided for real playback and owner evaluation. Measurements do not establish timbre or emotional quality.'},
        'outputs': report,
        'preview': preview,
        'previewTimeline': timeline,
        'knownLimitations': ['One audition per cue, not a finished variation bank.', 'MP3 source remains lossy; WAV decode does not recover a lossless master.', 'Loop seam and output headroom are measured; natural phrasing and distinction from the Stalker still need listening.', 'No AI state binding, event latch, cooldown or death playback behavior has been implemented.'],
    }
    (BASE / 'provenance.json').write_text(json.dumps(report_data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'outputs': [{**row['wav'], 'id': row['id']} for row in report], 'previewTimeline': timeline}, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    main()
