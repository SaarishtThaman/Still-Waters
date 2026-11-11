using System.Collections.Generic;
using UnityEngine;

namespace SW
{
    /// <summary>The jam's sounds: cross-faded music, looping ambience beds and one-shots.</summary>
    public class Audio : MonoBehaviour
    {
        public static Audio I;
        AudioSource musicA, musicB;
        readonly Dictionary<string, AudioSource> beds = new Dictionary<string, AudioSource>();
        readonly Dictionary<string, float> bedTarget = new Dictionary<string, float>();
        readonly List<AudioSource> pool = new List<AudioSource>();
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        string currentMusic;
        float musicFade = 1f, musicTargetVol = 1f, radioDuck = 1f;

        public static float MusicVol => Game.I != null ? Game.I.settings.music : 0.6f;
        public static float SfxVol => Game.I != null ? Game.I.settings.sfx : 0.85f;

        void Awake()
        {
            I = this;
            musicA = gameObject.AddComponent<AudioSource>();
            musicB = gameObject.AddComponent<AudioSource>();
            foreach (var m in new[] { musicA, musicB }) { m.loop = true; m.playOnAwake = false; m.volume = 0; }
            for (int i = 0; i < 10; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                pool.Add(s);
            }
        }

        AudioClip Clip(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (!clips.TryGetValue(name, out var c))
            {
                c = Resources.Load<AudioClip>("Audio/" + name);
                clips[name] = c;
            }
            return c;
        }

        public static void Play(string name, float vol = 1f, float pitch = 1f)
        {
            if (I == null) return;
            var c = I.Clip(name);
            if (c == null) return;
            AudioSource src = null;
            foreach (var s in I.pool) if (!s.isPlaying) { src = s; break; }
            if (src == null) src = I.pool[0];
            src.clip = c;
            src.volume = vol * SfxVol;
            src.pitch = pitch;
            src.loop = false;
            src.Play();
        }

        public static void Music(string name, float vol = 1f)
        {
            if (I == null) return;
            I.musicTargetVol = vol;
            if (name == I.currentMusic) return;
            I.currentMusic = name;
            (I.musicA, I.musicB) = (I.musicB, I.musicA);
            I.musicA.clip = I.Clip(name);
            I.musicA.volume = 0;
            if (I.musicA.clip != null) I.musicA.Play();
            I.musicFade = 0;
        }

        /// <summary>Looping ambience at a target volume (0 fades it out).</summary>
        public static void Bed(string name, float vol)
        {
            if (I == null) return;
            if (!I.beds.TryGetValue(name, out var src))
            {
                if (vol <= 0) return;
                src = I.gameObject.AddComponent<AudioSource>();
                src.clip = I.Clip(name);
                src.loop = true;
                src.volume = 0;
                if (src.clip != null) src.Play();
                I.beds[name] = src;
            }
            I.bedTarget[name] = vol;
        }

        public static void StopBeds()
        {
            if (I == null) return;
            foreach (var k in new List<string>(I.bedTarget.Keys)) I.bedTarget[k] = 0;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            musicFade = Mathf.MoveTowards(musicFade, 1, dt / 1.5f);
            radioDuck = Mathf.MoveTowards(radioDuck, Radio.I != null && Radio.I.Playing ? 0.12f : 1f, dt);
            musicA.volume = musicFade * musicTargetVol * MusicVol * 0.7f * radioDuck;
            musicB.volume = (1 - musicFade) * musicTargetVol * MusicVol * 0.7f * radioDuck;
            if (musicFade >= 1 && musicB.isPlaying) musicB.Stop();
            foreach (var kv in beds)
            {
                float target = (bedTarget.TryGetValue(kv.Key, out var t) ? t : 0) * SfxVol;
                kv.Value.volume = Mathf.MoveTowards(kv.Value.volume, target, dt * 0.6f);
            }
        }
    }
}
