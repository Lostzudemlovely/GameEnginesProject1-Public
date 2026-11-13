# Game Engines – Projekt 1
# Erzeugt Musik und vier Soundeffekte als WAV-Dateien.
"""Original 32-second loop and four short effects using the Python standard library."""
from array import array
from pathlib import Path
import math
import random
import wave

RATE = 22050
OUT = Path(__file__).resolve().parents[1] / 'Assets' / 'Audio'
OUT.mkdir(parents=True, exist_ok=True)


def frequency(note):
    return 440 * 2 ** ((note - 69) / 12)


def write(name, samples, peak=0.75):
    gain = peak / max(max(abs(x) for x in samples), 0.001)
    pcm = array('h', (round(max(-1, min(1, x * gain)) * 32767) for x in samples))
    with wave.open(str(OUT / name), 'wb') as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(RATE)
        wav.writeframes(pcm.tobytes())
    print(name, f'{len(samples) / RATE:.2f}s')


def add_note(track, start, duration, note, volume, mellow=False):
    hz = frequency(note)
    for i in range(int(duration * RATE)):
        t = i / RATE
        envelope = min(t / 0.012, 1) * min((duration - t) / 0.08, 1)
        tone = math.sin(2 * math.pi * hz * t)
        if not mellow:
            tone += 0.22 * math.sin(4 * math.pi * hz * t)
            envelope *= math.exp(-t * 4)
        index = int(start * RATE) + i
        if index < len(track):
            track[index] += tone * envelope * volume


# Vier Akkorde und eine achttaktige Melodie werden in zwei Durchläufen arrangiert.
music = [0.0] * (32 * RATE)
chords = [(57, 60, 64), (53, 57, 60), (48, 52, 55), (55, 59, 62)]
melody = [
    [76, 72, 69, None, 72, 74, 76, 79],
    [76, None, 72, 69, 67, 69, 72, None],
    [72, 69, 65, None, 69, 72, 74, 72],
    [69, None, 67, 65, 64, 65, 69, None],
    [67, 72, 76, None, 74, 72, 67, 64],
    [67, None, 69, 72, 76, 74, 72, None],
    [71, 74, 79, None, 77, 74, 71, 67],
    [69, 71, 74, None, 72, 71, 67, None],
]
noise = random.Random(21731)
for bar in range(16):
    start = bar * 2.0
    chord = chords[(bar // 2) % 4]
    for note in chord:
        add_note(music, start, 1.95, note, 0.065, mellow=True)
    for beat in range(4):
        add_note(music, start + beat * 0.5, 0.42, chord[0] - 24, 0.20)
        for i in range(int(0.13 * RATE)):
            t = i / RATE
            kick = math.sin(2 * math.pi * (58 * t - 18 * t * t)) * math.exp(-t * 32)
            music[int((start + beat * 0.5) * RATE) + i] += kick * 0.16
    for eighth, note in enumerate(melody[bar % 8]):
        if note is not None:
            add_note(music, start + eighth * 0.25, 0.23, note + (0 if bar < 8 else -12), 0.12)
        for i in range(int(0.035 * RATE)):
            t = i / RATE
            music[int((start + eighth * 0.25) * RATE) + i] += noise.uniform(-1, 1) * math.exp(-t * 140) * 0.025
write('CloudCircuit.wav', music, 0.65)

jump = []
phase = 0.0
for i in range(int(0.16 * RATE)):
    t = i / RATE
    phase += 2 * math.pi * (380 + 1600 * t) / RATE
    envelope = min(t / 0.008, 1) * (1 - t / 0.16) ** 2
    jump.append(math.sin(phase) * envelope)
write('Jump.wav', jump)

goal = [0.0] * int(0.65 * RATE)
for start, note in [(0, 72), (0.10, 76), (0.20, 79), (0.30, 84)]:
    add_note(goal, start, 0.35, note, 0.5)
write('Goal.wav', goal)

death = []
phase = 0.0
for i in range(int(0.42 * RATE)):
    t = i / RATE
    phase += 2 * math.pi * (300 - 230 * t / 0.42) / RATE
    envelope = min(t / 0.01, 1) * (1 - t / 0.42) ** 1.5
    death.append((math.sin(phase) + 0.15 * math.sin(2 * phase)) * envelope)
write('Fall.wav', death)

kickoff = []
impact_noise = random.Random(27)
for i in range(int(0.18 * RATE)):
    t = i / RATE
    attack = min(t / 0.002, 1)
    thump = math.sin(2 * math.pi * (138 * t - 260 * t * t)) * math.exp(-t * 29)
    contact = impact_noise.uniform(-1, 1) * math.exp(-t * 125) * 0.42
    kickoff.append(attack * (thump + contact))
write('Kickoff.wav', kickoff)
