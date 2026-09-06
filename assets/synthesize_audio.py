"""Generate all Strike Ledger audio with Python's standard library.

No recordings, samples, external MIDI, commercial music or copied material.
Deterministic procedural synthesis, authored for this project, September 2026.
Run from any directory; output is ../game/Presentation/Audio/*.wav.
"""
from pathlib import Path
import math
import random
import struct
import wave

RATE = 22050
TAU = math.tau
OUT = Path(__file__).resolve().parent.parent / "game" / "Presentation" / "Audio"
OUT.mkdir(parents=True, exist_ok=True)


def save(name, seconds, synth):
    rng = random.Random(4183)
    samples = []
    for i in range(round(seconds * RATE)):
        t = i / RATE
        edge = min(1, t / .003, (seconds - t) / .008)
        s = max(-.98, min(.98, synth(t, rng) * edge))
        samples.append(round(s * 32767))
    with wave.open(str(OUT / (name + ".wav")), "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(RATE)
        wav.writeframes(struct.pack("<" + "h" * len(samples), *samples))


def tone(t, hz, decay=12):
    return math.sin(TAU * hz * t) * math.exp(-t * decay)


def noise(rng):
    return rng.uniform(-1, 1)


save("hit", .20, lambda t, r: .52 * math.sin(TAU * (135 * t - 210 * t*t)) * math.exp(-t*28) + .42 * noise(r) * math.exp(-t*55))
save("heavy", .36, lambda t, r: .70 * math.sin(TAU * (105*t - 80*t*t)) * math.exp(-t*14) + .39 * noise(r) * math.exp(-t*43))
save("block", .19, lambda t, r: .23*tone(t, 460, 33) + .21*tone(t, 693, 37) + .31*noise(r)*math.exp(-t*46))
save("parry", .48, lambda t, r: .30*tone(t, 1244, 12) + .22*tone(t, 1866, 14) + .15*tone(t, 2488, 19) + .13*noise(r)*math.exp(-t*90))
save("swing", .16, lambda t, r: .30*noise(r)*math.sin(min(1, t/.16)*math.pi)**1.4*math.exp(-t*7))
save("throw", .34, lambda t, r: .53*tone(t, 67, 12)+.31*noise(r)*math.exp(-t*24)+.15*tone(t, 141, 23))
save("land", .20, lambda t, r: .25*tone(t, 75, 27)+.24*noise(r)*math.exp(-t*35))
save("ex", .42, lambda t, r: .27*math.sin(TAU*(250*t+720*t*t))*math.exp(-t*12)+.14*tone(t, 747, 14))
save("super", .92, lambda t, r: .27*math.sin(TAU*(80*t+240*t*t))*math.exp(-t*4)+.16*tone(t, 311, 6)+.15*tone(t, 466, 5)+.1*noise(r)*math.exp(-t*10))
save("round", .64, lambda t, r: .24*tone(t, 311, 6)+.20*tone(t, 466, 7)+.18*tone(t, 622, 9))
save("ko", 1.0, lambda t, r: .3*tone(t, 78, 4)+.18*tone(t, 155, 3)+.13*tone(t, 233, 4)+.12*noise(r)*math.exp(-t*16))
save("select", .08, lambda t, r: .20*tone(t, 830, 46)+.13*tone(t, 1244, 48))
save("confirm", .22, lambda t, r: .22*tone(t, 622, 17)+.18*math.sin(TAU*932*max(0,t-.055))*math.exp(-max(0,t-.055)*22))
save("cancel", .15, lambda t, r: .17*math.sin(TAU*(415*t-410*t*t))*math.exp(-t*24))
save("denied", .18, lambda t, r: .15*tone(t, 156, 18)+.12*tone(t, 171, 20))

# Thirty-two beats at 96 BPM: quiet, original industrial-electronic accompaniment.
# Sparse tuned percussion and bass leave impact/parry transients audible.
BEAT = 60 / 96
LENGTH = BEAT * 32
roots = [77.7817, 92.4986, 69.2957, 61.7354]
melody = [2, 0, 7, 5, 0, 3, 2, -2]


def music(t, rng):
    beat = t / BEAT
    bar = int(beat) // 8
    local = beat % 1 * BEAT
    root = roots[bar % 4]
    half = beat % .5 * BEAT
    s = .10 * math.sin(TAU*root*t) * math.exp(-local*5)
    s += .025 * math.sin(TAU*root*2*t) * math.exp(-local*7)
    if int(beat) % 4 in (0, 2):
        s += .22 * math.sin(TAU*(48*local+1.5*(1-math.exp(-local*45)))) * math.exp(-local*19)
    if int(beat) % 4 == 3:
        s += .048 * noise(rng)*math.exp(-local*31)
        s += .026 * tone(local, 310, 35)
    s += .018*noise(rng)*math.exp(-half*70)
    phrase = beat % 2 * BEAT
    if int(beat) % 2 == 0:
        note = root*4*2**(melody[(int(beat)//2) % 8]/12)
        s += .036 * tone(phrase, note, 6)
        s += .015 * tone(phrase, note*2.003, 8)
    # Low air movement is filtered by amplitude; it is nonintrusive ambience.
    s += .003 * noise(rng)
    return s


save("foundry_loop", LENGTH, music)
# Added original, deliberately synthetic exertion/impact variations. These are
# noise/formant designs, not recordings, impersonations or claimed human vocals.
for identity,base in (("rook",130),("vale",205)):
    save(identity+"_breath",.19,lambda t,r,b=base: (.12*noise(r)+.055*math.sin(TAU*b*t)+.035*math.sin(TAU*b*2.4*t))*math.sin(math.pi*min(1,t/.19))**1.5*math.exp(-t*8))
for variant in (1,2):
    save("hit_"+str(variant),.20,lambda t,r,v=variant: .47*math.sin(TAU*((125+v*21)*t-(190+v*35)*t*t))*math.exp(-t*(26+v*3))+.34*noise(r)*math.exp(-t*(45+v*8)))
    save("heavy_"+str(variant),.34,lambda t,r,v=variant: .61*math.sin(TAU*((83+v*13)*t-75*t*t))*math.exp(-t*(13+v))+.31*noise(r)*math.exp(-t*(31+v*6)))

def campus(t,r,blue=False):
    beat=t/BEAT;local=beat%2*BEAT;note=[155.5635,184.9972,138.5913,123.4708][int(beat)//8%4]
    s=.035*math.sin(TAU*note*t)*(.7+.3*math.sin(TAU*t/LENGTH))
    s+=.045*tone(local,note*(3 if blue else 2),blue and 3.7 or 5.2)
    s+=.012*tone(local,note*4.002,6)
    s+=.0015*noise(r)
    return s
save("marist_green_loop",LENGTH,lambda t,r:campus(t,r,False))
save("marist_gates_loop",LENGTH,lambda t,r:campus(t,r,True))
print(f"Wrote 24 original mono PCM sound assets to {OUT}")
