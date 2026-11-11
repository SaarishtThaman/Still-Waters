# Still Waters radio: three quiet music stations, no talk.
# Render with: .venv/bin/python render.py   (writes Assets/Resources/Radio/*.mp3)
# Station frequencies and names live in Assets/Scripts/Game/Radio.cs; keep the ids in sync.

# clip id -> source file in music_src/
MUSIC = {
    # 90.3 Lakeside Piano
    "p_gymnopedie": "Gymnopedie No 1.mp3",
    "p_meditation1": "Meditation Impromptu 01.mp3",
    "p_meditation2": "Meditation Impromptu 02.mp3",
    "p_relaxing": "Relaxing Piano Music.mp3",
    "p_atrest": "At Rest.mp3",
    # 97.5 Cabin Acoustic
    "a_wholesome": "Wholesome.mp3",
    "a_sincerely": "Sincerely.mp3",
    "a_heartwarming": "Heartwarming.mp3",
    "a_frostwaltz": "Frost Waltz.mp3",
    # 104.1 Night Air
    "n_dreamy": "Dreamy Flashback.mp3",
    "n_bathed": "Bathed in the Light.mp3",
    "n_healing": "Healing.mp3",
    "n_fluidscape": "Fluidscape.mp3",
}

CREDIT = ("Music by Kevin MacLeod (incompetech.com), licensed under Creative Commons: By Attribution 4.0. "
          "Tracks: Gymnopedie No 1, Meditation Impromptu 01 and 02, Relaxing Piano Music, At Rest, Wholesome, "
          "Sincerely, Heartwarming, Frost Waltz, Dreamy Flashback, Bathed in the Light, Healing, Fluidscape.")
