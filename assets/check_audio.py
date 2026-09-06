"""Inspect the actual shipped PCM assets; does not claim speakers/headphone validation."""
from pathlib import Path
import hashlib
import math
import struct
import wave

root = Path(__file__).resolve().parent.parent
paths = sorted((root / "game/Presentation/Audio").glob("*.wav"))
assert len(paths) == 24, f"Expected 24 sound assets, found {len(paths)}"
hashes = set()
for path in paths:
    with wave.open(str(path), "rb") as wav:
        assert wav.getnchannels() == 1 and wav.getsampwidth() == 2 and wav.getframerate() == 22050, path
        duration = wav.getnframes() / wav.getframerate()
        assert .05 <= duration <= 21, path
        raw = wav.readframes(wav.getnframes())
    samples = struct.unpack("<" + "h" * (len(raw) // 2), raw)
    rms = math.sqrt(sum(s*s for s in samples) / len(samples)) / 32767
    peak = max(abs(s) for s in samples) / 32767
    assert .005 < rms < .5, (path, rms)
    assert peak < .99 and samples[0] == 0 and abs(samples[-1]) < 100, (path, peak)
    digest = hashlib.sha256(raw).hexdigest()
    assert digest not in hashes, f"Duplicated cue {path}"
    hashes.add(digest)
    print(f"PASS {path.name:19} {duration:5.2f}s RMS {rms:.3f} peak {peak:.3f} sha256 {digest}")
print("PASS 24 distinct, bounded, non-silent PCM assets. Device audio playback is a separate check.")
