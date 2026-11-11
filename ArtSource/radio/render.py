"""Renders the radio's music and tuning hiss to Assets/Resources/Radio/*.mp3.

Music gets only a gentle touch: a little warmth (top end rolled off), soft compression, and
every track levelled to the same quiet loudness so nothing jumps out. The between-stations
sound is plain FM hiss; the game adds the drift and muffling live (Radio.cs), which is what
makes a real radio sound real.

Usage: python3 render.py [clip ids...]   (no ids = everything; only needs ffmpeg)
"""
import os, subprocess, sys
from script import MUSIC

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "../../Assets/Resources/Radio"))
os.makedirs(OUT, exist_ok=True)

MUSIC_FX = ("highpass=f=70,lowpass=f=9000,"
            "acompressor=threshold=-26dB:ratio=2:attack=30:release=400,"
            "loudnorm=I=-22:TP=-4:LRA=9")


MAX_LEN = 330  # seconds


def ff(*args):
    subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", *args], check=True)


def fetch(f):
    """music_src isn't in git: grab the original from incompetech when it's missing."""
    path = os.path.join(HERE, "music_src", f)
    if not os.path.exists(path):
        import urllib.parse, urllib.request
        os.makedirs(os.path.dirname(path), exist_ok=True)
        url = "https://incompetech.com/music/royalty-free/mp3-royaltyfree/" + urllib.parse.quote(f)
        print("downloading", f)
        urllib.request.urlretrieve(url, path)
    return path


def main(only):
    for mid, f in MUSIC.items():
        if only and mid not in only: continue
        dst = os.path.join(OUT, mid + ".mp3")
        src = fetch(f)
        length = float(subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", src],
                                      capture_output=True, text=True).stdout)
        fx = MUSIC_FX
        if length > MAX_LEN:  # long pieces are cut to keep the build small, with a slow fade
            fx = f"atrim=0:{MAX_LEN},afade=t=out:st={MAX_LEN - 8}:d=8," + fx
        ff("-i", src, "-af", fx, "-ac", "1", "-ar", "32000",
           "-c:a", "libmp3lame", "-q:a", "3", dst)
        print("music", mid)
    if not only or "static" in only:
        # FM inter-station hiss: bright, smooth, steady. No crackle, no hum.
        ff("-f", "lavfi", "-t", "12", "-i", "anoisesrc=c=white:r=32000:a=0.3:s=7",
           "-f", "lavfi", "-t", "12", "-i", "anoisesrc=c=pink:r=32000:a=0.3:s=11",
           "-filter_complex",
           "[0:a]highpass=f=400,lowpass=f=7000[w];[1:a]lowpass=f=3000[p];"
           "[w][p]amix=inputs=2:weights=0.7 0.3:normalize=0,loudnorm=I=-28:TP=-8[o]",
           "-map", "[o]", "-ac", "1", "-c:a", "libmp3lame", "-q:a", "4", os.path.join(OUT, "static.mp3"))
        print("static")


if __name__ == "__main__":
    main(set(sys.argv[1:]))
