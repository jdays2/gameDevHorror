"""Production DSP primitives for the approved membrane; no audition generator or service.

Python/NumPy 2.3.5, offline only. Keep this file with generate_stalker_membrane_v04.py.
"""
import numpy as np
SR=48000

def envelope(x, attack=.0006, release=.006):
    x=x.copy()
    a=min(int(attack*SR),len(x)//2);r=min(int(release*SR),len(x)//2)
    if a:x[:a]*=np.sin(np.linspace(0,np.pi/2,a))**2
    if r:x[-r:]*=np.cos(np.linspace(0,np.pi/2,r))**2
    x[0]=x[-1]=0
    return x


def add(dst,src,at,gain=1):
    start=int(at*SR);end=min(len(dst),start+len(src))
    if end>start:dst[start:end]+=src[:end-start]*gain


def scale(x,level):
    return x*level/max(float(np.max(np.abs(x))),1e-10)


def band(rng,n,low,high):
    f=np.fft.rfftfreq(n,1/SR)
    response=(1-np.exp(-(f/low)**4))/np.sqrt(1+(f/high)**8)
    x=np.fft.irfft(np.fft.rfft(rng.normal(size=n))*response,n=n)
    return x/max(float(np.sqrt(np.mean(x*x))),1e-8)


def snap(rng,soft=False):
    """Tiny moist joint release, not an exhale."""
    seconds=rng.uniform(.028,.047);t=np.arange(int(seconds*SR))/SR
    x=band(rng,len(t),180,1900 if soft else 3200)*np.exp(-t/.0017)
    # Low, heavily damped, aperiodic cavity response under the contact.
    phase=2*np.pi*np.cumsum(rng.uniform(230,350)*(1+.5*np.exp(-t/.002)))/SR
    x+=.45*np.sin(phase)*np.exp(-t/.003)
    return scale(envelope(x,.00015,.008),.11 if soft else .2)


def membrane(rng,seconds=.16,distressed=False):
    """Stick/slip creak in a strained membrane: irregular little bursts, no air layer."""
    n=int(seconds*SR);t=np.arange(n)/SR
    f0=rng.uniform(560,720) if distressed else rng.uniform(230,340)
    knots=rng.uniform(-1,1,max(7,int(seconds*95)))
    jitter=np.interp(np.linspace(0,1,n),np.linspace(0,1,len(knots)),knots)
    frequency=f0*(1-.40*t/seconds+.16*jitter)
    phase=np.cumsum(frequency)*2*np.pi/SR
    # Inharmonic, softly excited modes; unlike the first pack's harmonic stack.
    body=np.sin(phase)+.25*np.sin(phase*1.46+.5)+.11*np.sin(phase*2.18)
    gates=np.zeros(n);at=.002
    while at<seconds-.01:
        length=min(rng.uniform(.012,.033),seconds-at)
        bump=np.sin(np.linspace(0,np.pi,max(2,int(length*SR))))**2
        add(gates,bump,at,rng.uniform(.3,1))
        at+=length*rng.uniform(.6,1.1)
    body*=gates*np.sin(np.linspace(0,np.pi,n))**.7
    # Small pressure releases on the edges; no continuous noise.
    for at in [.005,seconds*.6]:add(body,snap(rng,True),at,.20)
    return scale(envelope(body,.002,.018),.30 if distressed else .09)


def small_squeal(rng,seconds=.17,level=.32):
    """A short broken high pain sound, followed by silence rather than a drone."""
    n=int(seconds*SR);t=np.arange(n)/SR
    jitter=np.interp(np.linspace(0,1,n),np.linspace(0,1,13),rng.uniform(-1,1,13))
    frequency=(1180-490*(t/seconds)**.55)*(1+.07*jitter)
    phase=np.cumsum(frequency)*2*np.pi/SR
    x=np.sin(phase)+.18*np.sin(phase*2.03)+.08*np.sin(phase*3.17)
    x*=np.interp(t/seconds,[0,.035,.25,.40,.47,.64,.78,1],[0,1,.73,.23,.58,.55,.12,0])
    # Brief noisy onset, not breath throughout the event.
    onset_n=int(.009*SR)
    x[:onset_n]+=.20*band(rng,onset_n,1000,4500)*np.exp(-np.arange(onset_n)/SR/.0015)
    return scale(envelope(x,.001,.012),level)


def foot(rng):
    """Dry neutral bone contact. No tile modes or room tail."""
    seconds=.085;n=int(seconds*SR);t=np.arange(n)/SR
    x=band(rng,n,130,2900)*np.exp(-t/rng.uniform(.0012,.0020))
    # Bone-tip mass: very short, low resonance, no bright ringing.
    x+=.35*np.sin(2*np.pi*rng.uniform(420,610)*t)*np.exp(-t/.0019)
    for _ in range(5):
        grain_n=int(rng.uniform(.001,.0025)*SR)
        grain=band(rng,grain_n,700,2600)*np.exp(-np.arange(grain_n)/SR/.00045)
        add(x,envelope(grain,.00012,.0003),rng.uniform(.01,.058),rng.uniform(.015,.045))
    return scale(envelope(x,.00015,.008),rng.uniform(.20,.27))
