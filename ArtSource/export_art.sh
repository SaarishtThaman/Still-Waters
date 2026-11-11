#!/bin/zsh
# Re-export the jam's .aseprite art into Assets/Resources/Art as PNGs (Aseprite from Steam).
# Usage: ArtSource/export_art.sh <path to original Still-Waters checkout>
# Single-frame files become name.png; animations become name_0.png, name_1.png, ...
set -e
SRC="$1/Assets/Sprites"
OUT="${0:A:h}/../Assets/Resources/Art"
ASE="$HOME/Library/Application Support/Steam/steamapps/common/Aseprite/Aseprite.app/Contents/MacOS/aseprite"
mkdir -p "$OUT"
find "$SRC" -name "*.aseprite" ! -name "ReferenceScene*" ! -name "CatchUI.aseprite" | while read f; do
  n=$(basename "$f" .aseprite | tr -d ' ' | tr '[:upper:]' '[:lower:]' | tr '-' '_')
  "$ASE" -b "$f" --save-as "$OUT/${n}_{frame}.png"
  [ -f "$OUT/${n}_1.png" ] || mv "$OUT/${n}_0.png" "$OUT/$n.png"
done
cp "$SRC/Icon.png" "${0:A:h}/../Assets/Branding/app_icon.png"
# catchable items: drop solid background layers (the body parts were drawn on a grey fill)
find "$SRC/Fishable" -name "*.aseprite" | while read f; do
  n=$(basename "$f" .aseprite | tr -d ' ' | tr '[:upper:]' '[:lower:]' | tr '-' '_')
  python3 -I "${0:A:h}/ase2png.py" "$f" "$OUT/$n" --no-bg >/dev/null
done
