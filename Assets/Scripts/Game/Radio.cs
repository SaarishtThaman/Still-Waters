using System.Collections.Generic;
using UnityEngine;

namespace SW
{
    /// <summary>
    /// The boat's FM radio: three quiet music stations, tuned by hand. Stations play whether
    /// you're listening or not (each keeps a playhead on real time), so you tune into the middle
    /// of a song. Near a station the signal drifts in and out and sounds muffled, and hiss fills
    /// the gaps. The far left of the dial is OFF. Clips live in Resources/Radio
    /// (rendered by ArtSource/radio/render.py).
    /// </summary>
    public class Radio : MonoBehaviour
    {
        public const float OffFreq = 87.5f, MinFreq = 88f, MaxFreq = 108f;
        const float Width = 0.6f; // MHz either side of a station where you can hear it

        public class Station
        {
            public float Freq;
            public string Name, Tag;
            public string[] Tracks;
            public AudioSource Src;
            public AudioLowPassFilter Filter;
            public AudioClip Clip;
            public float Start, Length;
            public int Index;
            public float Signal, Seed;
        }

        public static Radio I;
        public readonly List<Station> Stations = new List<Station>();

        /// <summary>Dial position. Below MinFreq the radio is off. Every day starts with it off.</summary>
        public float Freq { get => dial; set => dial = Mathf.Clamp(value, OffFreq, MaxFreq); }
        float dial = OffFreq;
        int lastDay = -1;
        public bool On => Freq >= MinFreq;

        AudioSource hissSrc;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        float duck = 1f;

        public static Radio Create(Transform parent)
        {
            var go = new GameObject("Radio");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<Radio>();
            I = r;
            r.Build();
            return r;
        }

        void Build()
        {
            hissSrc = gameObject.AddComponent<AudioSource>();
            hissSrc.clip = Load("static");
            hissSrc.loop = true;
            hissSrc.playOnAwake = false;
            hissSrc.volume = 0;

            Add(90.3f, "Lakeside Piano", "Soft piano", "p_gymnopedie", "p_meditation1", "p_relaxing", "p_meditation2", "p_atrest");
            Add(97.5f, "Cabin Acoustic", "Warm and slow", "a_wholesome", "a_sincerely", "a_frostwaltz", "a_heartwarming");
            Add(104.1f, "Night Air", "Ambient, for the late hours", "n_dreamy", "n_bathed", "n_healing", "n_fluidscape");

            // every station starts somewhere in the middle of its playlist
            float now = Time.unscaledTime;
            foreach (var s in Stations)
            {
                s.Index = Random.Range(0, s.Tracks.Length) - 1;
                Advance(s, now);
                s.Start = now - Random.Range(0f, s.Length * 0.8f);
            }
        }

        void Add(float freq, string name, string tag, params string[] tracks)
        {
            // each station gets its own child so it can carry its own low-pass filter
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var src = child.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.volume = 0;
            var lp = child.AddComponent<AudioLowPassFilter>();
            lp.cutoffFrequency = 22000;
            Stations.Add(new Station { Freq = freq, Name = name, Tag = tag, Tracks = tracks, Src = src, Filter = lp, Seed = Random.value * 100f });
        }

        AudioClip Load(string id)
        {
            if (!clips.TryGetValue(id, out var c))
            {
                c = Resources.Load<AudioClip>("Radio/" + id);
                if (c == null) Debug.LogWarning("Radio clip missing: " + id);
                clips[id] = c;
            }
            return c;
        }

        void Advance(Station s, float start)
        {
            s.Index = (s.Index + 1) % s.Tracks.Length;
            s.Clip = Load(s.Tracks[s.Index]);
            s.Start = start;
            s.Length = (s.Clip != null ? s.Clip.length : 5f) + 2.5f; // a breath of quiet between songs
        }

        /// <summary>The station you're tuned to well enough to hear clearly (or null).</summary>
        public Station Locked
        {
            get
            {
                Station best = null;
                foreach (var s in Stations) if (s.Signal > 0.6f && (best == null || s.Signal > best.Signal)) best = s;
                return best;
            }
        }

        /// <summary>True while a song is actually coming through (for the music notes and ducking).</summary>
        public bool Playing
        {
            get
            {
                var g = Game.I;
                var lk = Locked;
                return On && g != null && g.inDay && !g.cutscene && lk != null && lk.Src.isPlaying;
            }
        }

        void Update()
        {
            var g = Game.I;
            if (g == null || g.settings == null || g.save == null) return;
            float now = Time.unscaledTime;
            if (g.save.day != lastDay) { lastDay = g.save.day; dial = OffFreq; } // optional: you turn it on yourself

            bool live = On && g.inDay;
            float target = !live || g.cutscene ? 0 : g.dialog != null && g.dialog.Busy ? 0.3f : 1f;
            duck = Mathf.MoveTowards(duck, target, Time.unscaledDeltaTime * 1.5f);
            float vol = Audio.MusicVol * 0.5f * duck;

            float best = 0;
            foreach (var s in Stations)
            {
                while (now - s.Start >= s.Length) Advance(s, s.Start + s.Length > now - 30f ? s.Start + s.Length : now);
                float raw = Mathf.Clamp01(1f - Mathf.Abs(Freq - s.Freq) / Width);
                // a weak signal drifts: slow, irregular fades, like the mountains are in the way
                float drift = Mathf.PerlinNoise(s.Seed, now * 0.7f);
                s.Signal = raw * Mathf.Lerp(1f, 0.35f + 0.65f * drift, 1f - raw);
                best = Mathf.Max(best, s.Signal);

                float pos = now - s.Start;
                bool inSong = s.Clip != null && pos < s.Clip.length - 0.05f;
                if (live && raw > 0.01f && inSong)
                {
                    if (s.Src.clip != s.Clip || !s.Src.isPlaying) { s.Src.clip = s.Clip; s.Src.time = Mathf.Max(0, pos); s.Src.Play(); }
                    s.Src.volume = vol * Mathf.SmoothStep(0, 1, s.Signal);
                    s.Filter.cutoffFrequency = Mathf.Lerp(900f, 22000f, s.Signal * s.Signal); // muffled until it's tuned in
                }
                else if (s.Src.isPlaying) { s.Src.Stop(); s.Src.clip = null; }
            }

            // hiss: strong between stations, gone when you're right on one, breathing a little
            float breathe = 0.85f + 0.15f * Mathf.PerlinNoise(7.3f, now * 0.4f);
            float hiss = live ? Mathf.Lerp(0.22f, 0f, Mathf.SmoothStep(0, 1, best)) * breathe : 0;
            hissSrc.volume = vol * hiss;
            if (live && !hissSrc.isPlaying && hissSrc.clip != null) hissSrc.Play();
            if (!live && hissSrc.isPlaying) hissSrc.Stop();
        }

        public static string Credit => "Music by Kevin MacLeod (incompetech.com), CC BY 4.0.";
    }
}
