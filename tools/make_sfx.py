# Synthesises the pop sounds: a short upward "plop" with a crisp click on top, like a finger popping out of a cheek.
# Usage: python3 tools/make_sfx.py <out_dir>   (writes sfx_pop_1..3.wav, 44.1 kHz mono 16-bit)
import sys, wave
import numpy as np

SR = 44100
OUT = sys.argv[1]

def pop(f_start, f_end, seed, length=0.14):
    t = np.arange(int(SR * length)) / SR
    rng = np.random.default_rng(seed)
    # pitch glides up fast, then settles
    freq = f_end + (f_start - f_end) * np.exp(-t / 0.014)
    phase = 2 * np.pi * np.cumsum(freq) / SR
    body = np.sin(phase) + 0.22 * np.sin(2 * phase + 0.4)
    body *= (1 - np.exp(-t / 0.0012)) * np.exp(-t / 0.026)
    # click: a few milliseconds of brightened noise at the very start
    noise = np.diff(rng.standard_normal(len(t)), prepend=0.0)
    click = 0.35 * noise * np.exp(-t / 0.0022)
    x = body + click
    fade = np.ones_like(x); n = int(SR * 0.012); fade[-n:] = np.linspace(1, 0, n)
    x *= fade
    return x / np.max(np.abs(x)) * 0.7

for i, (a, b) in enumerate([(380, 1000), (430, 1120), (340, 920)], start=1):
    x = pop(a, b, seed=i)
    data = (x * 32767).astype(np.int16)
    with wave.open(f"{OUT}/sfx_pop_{i}.wav", "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(data.tobytes())
    print(f"sfx_pop_{i}.wav", f"{len(x) / SR * 1000:.0f} ms", f"peak {np.max(np.abs(data))}")
