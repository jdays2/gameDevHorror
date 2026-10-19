"""Signal measurements, not perceptual listening or instrument identification."""
from pathlib import Path
import sys
import json

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "runtime"))
import numpy as np
import scipy.signal as sig
import librosa

NOTES = ['C', 'C#', 'D', 'Eb', 'E', 'F', 'F#', 'G', 'Ab', 'A', 'Bb', 'B']
MAJOR = np.array([6.35,2.23,3.48,2.33,4.38,4.09,2.52,5.19,2.39,3.66,2.29,2.88])
MINOR = np.array([6.33,2.68,3.52,5.38,2.60,3.53,2.54,4.75,3.98,2.69,3.34,3.17])

def candidates(c):
    scores = []
    for root in range(12):
        for mode, profile in [('major', MAJOR), ('minor', MINOR)]:
            score = np.corrcoef(c, np.roll(profile, root))[0,1]
            scores.append({'key': f'{NOTES[root]} {mode}', 'profile_correlation': round(float(score), 3)})
    return sorted(scores, key=lambda x:x['profile_correlation'], reverse=True)[:5]

def tempo_candidates(envelope, sr, hop, n=7):
    ac=librosa.autocorrelate(envelope-envelope.mean(),max_size=int(sr/hop*4))
    peaks,_=sig.find_peaks(ac)
    peaks=[p for p in peaks if 40 <=60*sr/(hop*p)<=200]
    result=[]
    for p in sorted(peaks,key=lambda p:ac[p],reverse=True)[:n]:
        denom=ac[p-1]-2*ac[p]+ac[p+1]
        offset=.5*(ac[p-1]-ac[p+1])/denom if denom else 0
        lag=p+offset
        result.append({'bpm':round(float(60*sr/(hop*lag)),2),'relative_peak':round(float(ac[p]/max(ac[0],1e-9)),3)})
    return result

def analyze(filename):
    y, sr = librosa.load(ROOT / filename, sr=22050, mono=True)
    hop = 512
    duration = len(y)/sr
    rms = librosa.feature.rms(y=y, hop_length=hop)[0]
    times = librosa.frames_to_time(np.arange(len(rms)), sr=sr, hop_length=hop)
    onset = librosa.onset.onset_strength(y=y, sr=sr, hop_length=hop)
    ac = librosa.autocorrelate(onset - onset.mean(), max_size=int(sr/hop*4))
    lags = np.arange(len(ac))
    bpms = 60 * sr / (hop * np.maximum(lags,1))
    peaks, _ = sig.find_peaks(ac)
    peaks = [p for p in peaks if 40 <= bpms[p] <= 200]
    peaks = sorted(peaks,key=lambda p: ac[p],reverse=True)[:8]
    ac_norm = max(float(ac[0]), 1e-9)
    tempo_peaks = [{'bpm': round(float(bpms[p]),2), 'onset_ac_relative':round(float(ac[p]/ac_norm),3)} for p in peaks]
    tempo_default = float(np.asarray(librosa.feature.tempo(onset_envelope=onset,sr=sr,hop_length=hop,aggregate=np.median)).reshape(-1)[0])
    D = librosa.stft(y, n_fft=2048, hop_length=hop)
    H, P = librosa.decompose.hpss(D)
    # CQT resolves semitones in the low register better than a 2048-point STFT.
    harmonic_y = librosa.istft(H,hop_length=hop,length=len(y))
    chroma = librosa.feature.chroma_cqt(y=harmonic_y,sr=sr,hop_length=hop,
        fmin=librosa.note_to_hz('C2'),n_octaves=6,bins_per_octave=36)
    register_profiles={}
    for name,lowest,octaves in [('low_register_C1_B2','C1',2),('middle_register_C3_B4','C3',2),('upper_register_C5_B7','C5',3)]:
        rc=librosa.feature.chroma_cqt(y=harmonic_y,sr=sr,hop_length=hop,
            fmin=librosa.note_to_hz(lowest),n_octaves=octaves,bins_per_octave=36)
        rm=rc.mean(axis=1)
        register_profiles[name]={'top_pitch_classes':[{NOTES[i]:round(float(rm[i]/rm.sum()),3)} for i in np.argsort(rm)[::-1][:5]],'key_profile_candidates':candidates(rm)[:3]}
    weights = np.minimum(rms[:chroma.shape[1]],np.percentile(rms,90))
    chroma_mean = (chroma * weights[None,:]).sum(axis=1) / max(weights.sum(),1e-9)
    chroma_share = chroma_mean/chroma_mean.sum()
    centroid = librosa.feature.spectral_centroid(S=np.abs(D),sr=sr)[0]
    power = np.abs(D)**2
    freqs = librosa.fft_frequencies(sr=sr,n_fft=2048)
    total = power.sum()
    bands = {}
    for name,lo,hi in [('sub_20_120',20,120),('low_120_500',120,500),('mid_500_2000',500,2000),('high_2000_8000',2000,8000)]:
        bands[name] = round(float(power[(freqs>=lo)&(freqs<hi)].sum()/total),3)
    perc_y=librosa.istft(P,hop_length=hop,length=len(y))
    perc_onset=librosa.onset.onset_strength(y=perc_y,sr=sr,hop_length=hop)
    low_mel=librosa.feature.melspectrogram(y=y,sr=sr,hop_length=hop,n_mels=24,fmin=20,fmax=250)
    low_onset=librosa.onset.onset_strength(S=librosa.power_to_db(low_mel),sr=sr,hop_length=hop)
    low_power=power[(freqs>=20)&(freqs<180)].sum(axis=0)
    low_perc_power=(np.abs(P[(freqs>=20)&(freqs<180)])**2).sum(axis=0)
    percussion_windows=[]
    for start in np.arange(0,duration,5):
        mask=(times>=start)&(times<start+5)
        percussion_windows.append({'start_s':float(start),'low_20_180_power_db_relative':round(float(10*np.log10(np.mean(low_power[mask])/max(np.mean(low_power),1e-9))),1),
            'low_percussive_share':round(float(np.sum(low_perc_power[mask])/max(np.sum(low_power[mask]),1e-9)),3),
            'onset_activity_mean':round(float(np.mean(perc_onset[mask])),3)})
    summary=[]
    for start in np.arange(0,duration,10):
        mask=(times>=start)&(times<start+10)
        if not mask.any(): continue
        cols=np.flatnonzero(mask)
        summary.append({'start_s':round(float(start),1),'end_s':round(float(min(start+10,duration)),1),
            'rms_dbfs':round(float(20*np.log10(np.sqrt(np.mean(rms[mask]**2))+1e-10)),1),
            'centroid_hz_median':round(float(np.median(centroid[cols[cols<len(centroid)]]))),
            'top_pitch_classes':[NOTES[i] for i in np.argsort(chroma[:,cols[cols<chroma.shape[1]]].mean(axis=1))[::-1][:3]]})
    # Compare eight seconds before and after each candidate boundary.
    features = np.vstack([chroma, librosa.feature.mfcc(S=librosa.power_to_db(power),sr=sr,n_mfcc=6)])
    scale = features.std(axis=1,keepdims=True)+1e-6
    z=(features-features.mean(axis=1,keepdims=True))/scale
    novelty=[]
    for t in np.arange(8,duration-8,1):
        left=(times>=t-8)&(times<t)
        right=(times>=t)&(times<t+8)
        a=z[:,left].mean(axis=1); b=z[:,right].mean(axis=1)
        novelty.append((float(np.linalg.norm(a-b)),float(t)))
    chosen=[]
    for val,t in sorted(novelty,reverse=True):
        if all(abs(t-old['time_s'])>=9 for old in chosen):
            chosen.append({'time_s':t,'feature_change_score':round(val,2)})
        if len(chosen)==7:break
    # Lagged chroma similarity: high baseline can reflect static harmony, not a melody repeat.
    sim=[]
    chroma_z=chroma-chroma.mean(axis=0,keepdims=True)
    chroma_z/=np.linalg.norm(chroma_z,axis=0,keepdims=True)+1e-9
    for lag_s in np.arange(4,min(80,duration/1.5),0.5):
        lag=int(round(lag_s*sr/hop))
        sim.append(float(np.mean(np.sum(chroma_z[:,:-lag]*chroma_z[:,lag:],axis=0))))
    sp,_=sig.find_peaks(sim,distance=6)
    lags_s=np.arange(4,min(80,duration/1.5),0.5)
    repetition=[{'lag_s':round(float(lags_s[i]),1),'pitch_class_cosine':round(sim[i],3)} for i in sorted(sp,key=lambda i:sim[i],reverse=True)[:6]]
    # Refine strongest periodic lags to the frame grid, excluding static pedal bias.
    dynamic=chroma-chroma.mean(axis=1,keepdims=True)
    dynamic/=np.linalg.norm(dynamic,axis=0,keepdims=True)+1e-9
    refined=[]
    for target in repetition[:4]:
        best=None
        for lag in range(max(1,int((target['lag_s']-.6)*sr/hop)),int((target['lag_s']+.6)*sr/hop)):
            val=float(np.mean(np.sum(dynamic[:,:-lag]*dynamic[:,lag:],axis=0)))
            if best is None or val>best[0]:best=(val,lag)
        lag_s=best[1]*hop/sr
        refined.append({'lag_s':round(lag_s,3),'dynamic_chroma_cosine':round(best[0],3),'implied_bpm_if_four_bars_of_four_beats':round(16*60/lag_s,2)})
    return {'file':filename,'duration_s':round(duration,3),'sr_analyzed':sr,
        'tempo_default_bpm':round(tempo_default,2),'tempo_onset_candidates':tempo_peaks,
        'tempo_note':'Autocorrelation peaks are candidate periodicities; half/double-time and meter require listening.',
        'key_candidates':candidates(chroma_mean),
        'register_pitch_profiles':register_profiles,
        'pitch_class_share':{NOTES[i]:round(float(chroma_share[i]),3) for i in range(12)},
        'signal_summary':{'peak_dbfs':round(float(20*np.log10(np.max(np.abs(y))+1e-10)),2),
            'integrated_rms_dbfs':round(float(20*np.log10(np.sqrt(np.mean(y*y))+1e-10)),2),
            'spectral_centroid_median_hz':round(float(np.median(centroid))),
            'spectral_centroid_p10_p90_hz':[round(float(v)) for v in np.percentile(centroid,[10,90])],
            'hpss_harmonic_energy_fraction':round(float(np.sum(np.abs(H)**2)/(np.sum(np.abs(H)**2)+np.sum(np.abs(P)**2))),3),
            'frequency_energy_share':bands},
        'potential_section_boundaries':sorted(chosen,key=lambda x:x['time_s']),
        'repetition_lag_candidates':repetition,
        'refined_dynamic_pitch_repetition':refined,
        'tempo_percussion_onset_candidates':tempo_candidates(perc_onset,sr,hop),
        'tempo_low_band_20_250_onset_candidates':tempo_candidates(low_onset,sr,hop),
        'percussion_five_second_windows':percussion_windows,
        'ten_second_windows':summary,
        'limitations':'No instrument identification or listening claim. Key scores are fit to average pitch-class energy, not a verified transcription. Novelty boundaries are signal changes, not named musical sections.'}

if __name__=='__main__':
    data=[analyze(f) for f in ['alien_shooter_menu.mp3','alien_shooter_2_theme.mp3']]
    out=ROOT/'reference_measurements.json'
    out.write_text(json.dumps(data,indent=2,ensure_ascii=False),encoding='utf-8')
    print(json.dumps(data,indent=2,ensure_ascii=False))
