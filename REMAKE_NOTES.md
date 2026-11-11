# Still Waters — Remake Notes

Unity 6000.4, built-in render pipeline. Targets: **Web** and **Android** (a Mac build also works from the menu).
This remake lives on the `remake` branch of SaarishtThaman/Still-Waters. `main` keeps the original jam project untouched.

## How it's put together

- **One scene** (`Assets/Scenes/Main.unity`) with just a camera. `Game.Boot` builds everything at runtime, so there's no scene or prefab wiring to break.
- **Content and story:** every item, upgrade, day, line of dialogue and ending is in `Assets/Scripts/Core/GameData.cs`.
- **What's on the hook:** `Core/Loot.cs` (plain C#).
- **Saves:** `Core/Save.cs` (PlayerPrefs JSON). The journal and endings survive a new run.
- **The lake:** `Game/World.cs` places the jam's layers at their original positions and parallax speeds (read from `DefaultLevel.unity`). A second camera renders everything above the waterline, and `Resources/Shaders/Water.shader` mirrors it with row-by-row ripples, like the jam's 2DWater reflection.
- **Art:** the jam's Aseprite files, exported to `Assets/Resources/Art/*.png` (PPU 16, point filtering). No new pixel art.
  - To re-export: `ArtSource/export_art.sh <path to Still-Waters checkout>`. It uses Aseprite from Steam.
  - Catchable items go through `ArtSource/ase2png.py --no-bg`, which strips the grey background the body parts were drawn on.
- **Menu:** `Still Waters > Setup Project`, `Build Web`, `Build Android (dev APK)`, `Build Mac`.

## Bugs fixed from the jam version

| Jam behaviour | Now |
|---|---|
| `PickFish` added the fish list 3× then called `.Union(junk)`, which removes duplicates. The extra fish vanished, so ~62% of early catches were junk | Fish and junk are weighted properly. The junk chance is 33% / 22% / 12% by bait level |
| The Female Torso could be caught any time after the hand. Its "final scene" dialogue reloaded the same day | Every story item is scheduled for a specific day |
| The glitched hand / body parts could repeat, sold for 0 and filled boat slots | Evidence and memories never repeat and don't use the fish box |
| Story parts were pure RNG with no guarantee (the head needed a max rod) | Each story item can turn up from cast N and is guaranteed by cast M that day |
| Space was read as *held*: the press that hooked a fish could fail the minigame 0.15 s later | All input is edge-triggered. The minigame ignores presses for 0.2 s after opening |
| Green zones 9 px (6 px on hard) at 90–120 px/s, one press decides | A fast bar (95/115/135 px/s, faster after each hit). Every press redraws the zones. Each hit shrinks the next green by 28%. Red zones cost two line marks, a plain miss costs one (3 marks, 4 with Reel assist). Harder fish need 2–3 hits. The panel trembles harder as the fish fights, with speed surges. The bar only turns at the ends |
| A day lasted 120 s (alpha +0.006/s) | 2.5 / 3.5 / 5 minutes (Settings; was 4/6/9, which felt too long). The clock runs 6 AM–9 PM and pauses during dialogue and menus. Too dark to fish after 8 PM |
| Bites took 14–20 s at the start (mean 17/13/8 ±3 s) | Mean 12 / 9 / 6 s by bait level, ±25% (9–15 s at the start). The first remake pass used 7/5/3.4, which felt too quick |
| No save | Autosaves on every catch, sale, upgrade and night. "Continue" resumes mid-day |
| Keyboard only | Touch buttons (move, cast/strike, tent, pause, and REEL in the minigame), a safe-area layout, and the camera keeps 320 px of width on narrow screens. On touch, only the buttons cast (tapping the lake does nothing) |
| Scene flow used magic build indices | One data-driven day list |
| Typos ("stentch", "repoted", "motoboat", "I few days", "THANK YOUR") | Fixed |

## Feel (matches the jam's physics)

- The boat is a Rigidbody2D floating on a BuoyancyEffector2D (linear and angular drag 5, as in the jam's WaterReflection prefab). It rocks on a gentle swell, leans into acceleration, and eases in and out of motion.
- The hook hangs off the rod on a SpringJoint2D (frequency 3, damping 1). It has gravity ×3 and water drag 4.09, the jam's values. Casting flings it out and pays the line out. Bites jerk it side to side every 0.25 s. Reeling runs at the jam's speeds (2.5 empty, 1.5 with a fish).
- The camera isn't snapped to the pixel grid any more, so slow movement stays smooth.
- Haptics (`Game/Haptics.cs`): shaped vibrations on Android (VibrationEffect) and mobile web (`Plugins/WebGL/Haptics.jslib`) for splash-down, bite, each tug, strike, minigame hit/miss, catch, escape, and a slow heavy pulse for body parts. Settings > Vibration (mobile only).
- Text: the pixel font draws small for its size, so `UI.TextScale` boosts all text by 15%. Dialogue is 84.

## Radio

Optional and fully manual. Every day starts with the radio off. Tap FM on the HUD (or press R) and drag the dial; the far left notch is OFF. There are no seek or preset buttons.

| Freq | Station | Music |
|---|---|---|
| 90.3 | Lakeside Piano | Gymnopedie No 1, Meditation Impromptu 01 and 02, Relaxing Piano Music, At Rest |
| 97.5 | Cabin Acoustic | Wholesome, Sincerely, Frost Waltz, Heartwarming |
| 104.1 | Night Air | Dreamy Flashback, Bathed in the Light, Healing, Fluidscape |

- Stations keep playing when you're not tuned in, so you land in the middle of a song.
- Near a station, the signal drifts in and out and sounds muffled (a live low-pass filter). Hiss fills the gaps between stations.
- Game music fades down while a song is coming through.
- While a song plays, faint pixel music notes drift up off the boat (`RadioNotes.cs`, drawn in code, no new art).
- All music is by Kevin MacLeod (incompetech.com), CC BY 4.0. The credit is in the radio panel and must stay. The originals go in `ArtSource/radio/music_src`, which is not in git; `render.py` downloads any that are missing.
- To render: `python3 ArtSource/radio/render.py`. It only needs ffmpeg. It levels every track to the same quiet loudness, trims anything over 5.5 minutes with a fade, and writes `Assets/Resources/Radio/*.mp3`.
- Import: songs stream, as Vorbis q0.35 mono at 22 kHz.

## Story (expanded)

Wes (the name is easy to change in `GameData.cs`) returns to the hidden lake between the mountains where he and his brother Dan fished as kids. Seven days, then the finale:

1. **Arrival**: the carved plank "W + D" from the dock their father built.
2. **Old Friends**: Captain, Dan's teddy bear (Wes rescued it as a kid, so why is it in the lake?), then Mary's hair brush.
3. **Something in the Water**: the severed hand. Wes doesn't tell Dan. The Other speaks for the first time.
4. **Lost Time**: Wes wakes up out on the water at 10:30. Dad's pocket watch (the day Dad "left"), then the Arm late in the day.
5. **Pride**: the Leg, then a blackout: the Other takes the afternoon and leaves the Organ Necklace.
6. **Recognition**: Wes's childhood backpack, then the Rotting Head (Tom, from Mary's work). Dan says he's driving up.
7. **Still Waters**: the lake goes still. Mary's torso. Dan arrives at dusk and tells what he saw when he was eleven.

Body parts are the progression, so they're spaced out: one per day from day 3, always after the day's memory, and never sooner than 3 casts after the previous story catch.

**Endings:** *Confession* (needs all 5 memories), *The Other* (it loops back to Day 1), and *Still Waters* (walk into the lake). The title screen shows how many endings you've found.

**The Other:** red, jittering text that sometimes glitches. It whispers while you fish from Day 4, more often from Day 6, and sometimes replaces Wes's thoughts after a catch.

All the jam's dialogue lines are kept. Fish descriptions are rewritten in Wes's voice, and the impossible ones (squid, clownfish in a mountain lake) are played as unease.

## New fishing areas

These use no new art: they are distance bands across the jam's level.
- **The Shallows**: from the tent out to x 9.6 (the jam's FAR_X). Bluegill, trout, barb, boarfish, catfish, eel.
- **Open Water**: snook (rod 2), red drum (bait 2), clownfish, angler (rod 2 + bait 2).
- **The Deep**: past x 19. Squid (rod 2) and blobfish (rod 3 + bait 2).

## Upgrades

| | Lvl 1 | Lvl 2 | Lvl 3 | Costs |
|---|---|---|---|---|
| Engine (speed) | 2.2 | 3.2 | 4.4 | 70, 160 |
| Fishing Rod (cast) | 1.9 | 2.6 | 3.4 | 80, 180 |
| Bait (mean bite s) | 12 | 9 | 6 | 60, 150 |
| Fish Box | 4 | 6 | 9 | 40, 120 |

## Testing (headless, nothing opens on screen, audio muted)

```
Unity -batchmode -projectPath . -executeMethod SW.EditorTools.Capture.Run -swOut Captures/X [-swQuick]
```
A bot plays all 7 days to the Confession ending, then checks the menus and several screen shapes (phone, 4:3, portrait). It writes PNGs plus report.txt.

## Not done / open

- iOS and Windows were skipped on purpose.
- No new music. The jam's calm BGM plays on days 1–3. From day 4 it's ambience only.
