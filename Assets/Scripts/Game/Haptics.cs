using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace SW
{
    /// <summary>
    /// Short, shaped vibrations on Android (VibrationEffect with amplitude) and mobile web
    /// (navigator.vibrate). Silent everywhere else, and when Settings > Vibration is off.
    /// </summary>
    public static class Haptics
    {
        public enum Kind { Tick, Tug, Bite, Strike, Hit, Miss, Catch, Escape, Dread }

        // (on ms, amplitude 1-255) pairs separated by off gaps: on, off, on, off...
        static (long[] t, int[] a) Shape(Kind k) => k switch
        {
            Kind.Tick => (new long[] { 0, 12 }, new[] { 0, 60 }),
            Kind.Tug => (new long[] { 0, 28 }, new[] { 0, 140 }),
            Kind.Bite => (new long[] { 0, 45, 50, 45 }, new[] { 0, 220, 0, 220 }),
            Kind.Strike => (new long[] { 0, 70 }, new[] { 0, 255 }),
            Kind.Hit => (new long[] { 0, 35 }, new[] { 0, 200 }),
            Kind.Miss => (new long[] { 0, 30, 40, 90 }, new[] { 0, 120, 0, 90 }),
            Kind.Catch => (new long[] { 0, 40, 60, 40, 60, 120 }, new[] { 0, 160, 0, 200, 0, 255 }),
            Kind.Escape => (new long[] { 0, 220 }, new[] { 0, 70 }),
            Kind.Dread => (new long[] { 0, 400, 150, 600 }, new[] { 0, 90, 0, 140 }),
            _ => (new long[] { 0, 20 }, new[] { 0, 100 }),
        };

        public static void Buzz(Kind k)
        {
            var g = Game.I;
            if (g != null && g.settings != null && !g.settings.haptics) return;
            if (Ctl.auto) return;
            var (t, a) = Shape(k);
#if UNITY_ANDROID && !UNITY_EDITOR
            Android(t, a);
#elif UNITY_WEBGL && !UNITY_EDITOR
            var parts = new System.Collections.Generic.List<string>();
            for (int i = 1; i < t.Length; i++) parts.Add(t[i].ToString());
            SW_Vibrate(string.Join(",", parts));
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void SW_Vibrate(string pattern);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
        static int sdk;
        static bool ready, failed;

        static void Android(long[] t, int[] a)
        {
            if (failed) return;
            try
            {
                if (!ready)
                {
                    ready = true;
                    using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var act = up.GetStatic<AndroidJavaObject>("currentActivity"))
                        vibrator = act.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    using (var v = new AndroidJavaClass("android.os.Build$VERSION")) sdk = v.GetStatic<int>("SDK_INT");
                }
                if (vibrator == null) { failed = true; return; }
                if (sdk >= 26)
                {
                    using (var ve = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = ve.CallStatic<AndroidJavaObject>("createWaveform", t, a, -1))
                        vibrator.Call("vibrate", effect);
                }
                else
                {
                    long total = 0;
                    for (int i = 1; i < t.Length; i += 2) total += t[i];
                    vibrator.Call("vibrate", total);
                }
            }
            catch (System.Exception e) { failed = true; Debug.LogWarning("Haptics off: " + e.Message); }
        }

        // Referencing Handheld.Vibrate makes Unity add the VIBRATE permission to the manifest.
        public static void NeverCalled() => Handheld.Vibrate();
#endif
    }
}
