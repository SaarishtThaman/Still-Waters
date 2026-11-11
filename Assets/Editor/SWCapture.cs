using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SW.EditorTools
{
    /// <summary>
    /// Headless playtest: plays the whole story with a bot (no window, no sound) and saves
    /// screenshots plus report.txt. Run:
    ///   Unity -batchmode -projectPath . -executeMethod SW.EditorTools.Capture.Run [-swOut Captures/X] [-swQuick]
    /// -swQuick stops after day 2.
    /// </summary>
    public static class Capture
    {
        static string outDir;
        static bool quick;
        static IEnumerator script;
        static double waitUntil, started;
        static RenderTexture rt;
        static readonly List<string> errors = new List<string>();
        static readonly List<string> log = new List<string>();
        static readonly HashSet<string> shots = new HashSet<string>();

        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            string Arg(string n, string d) { int i = Array.IndexOf(args, n); return i >= 0 && i + 1 < args.Length ? args[i + 1] : d; }
            outDir = Path.GetFullPath(Arg("-swOut", "Captures/latest"));
            quick = args.Contains("-swQuick");
            Directory.CreateDirectory(outDir);
            foreach (var f in Directory.GetFiles(outDir, "*.png")) File.Delete(f);
            Application.logMessageReceived += (msg, stack, type) =>
            {
                if ((type == LogType.Exception || type == LogType.Error || type == LogType.Assert) && EditorApplication.isPlaying && !stack.Contains("UnityEditor"))
                    errors.Add($"{type}: {msg}\n{stack}");
            };
            EditorUtility.audioMasterMute = true;
            // keep this driver alive across play mode (no domain reload)
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene(Setup.ScenePath);
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetString("sw_settings_v1", JsonUtility.ToJson(new Settings { music = 0, sfx = 0, dayLength = 0, textSpeed = 2 }));
            started = EditorApplication.timeSinceStartup;
            script = Script();
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup - started > 1500) { Note("TIMEOUT"); Finish(2); return; }
            if (EditorApplication.timeSinceStartup < waitUntil) return;
            try
            {
                if (!script.MoveNext()) Finish(errors.Count == 0 ? 0 : 1);
                else if (script.Current is float f) waitUntil = EditorApplication.timeSinceStartup + f;
            }
            catch (Exception e) { errors.Add("Driver: " + e); Finish(3); }
        }

        static void Finish(int code)
        {
            EditorApplication.update -= Tick;
            Time.timeScale = 1;
            File.WriteAllText(Path.Combine(outDir, "report.txt"), $"errors: {errors.Count}\n" + string.Join("\n", log) + "\n\n" + string.Join("\n\n", errors.Distinct().Take(30)));
            Debug.Log($"[SW] Capture done, {errors.Count} errors -> {outDir}");
            EditorApplication.Exit(code);
        }

        static void Note(string s)
        {
            var l = $"[{EditorApplication.timeSinceStartup - started:F1}s] {s}";
            log.Add(l);
            Debug.Log("[SW] " + l);
        }

        static void SetSize(int w, int h)
        {
            var cam = Camera.main;
            if (rt != null) { cam.targetTexture = null; rt.Release(); }
            rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            var c = Game.I.canvas;
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = 1;
            c.sortingOrder = 1000; // above every sprite, like the real overlay canvas
            Game.I.safe.anchorMin = Vector2.zero;
            Game.I.safe.anchorMax = Vector2.one;
        }

        static void Shot(string name, bool once = true)
        {
            if (once && !shots.Add(name)) return;
            var cam = Camera.main;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            Note("shot " + name);
        }

        static Game G => Game.I;

        static bool Click(string label)
        {
            var b = UnityEngine.Object.FindObjectsByType<Button>()
                .FirstOrDefault(x => x.isActiveAndEnabled && x.interactable && x.GetComponentInChildren<Text>() != null && x.GetComponentInChildren<Text>().text.StartsWith(label));
            if (b == null) return false;
            b.onClick.Invoke();
            return true;
        }

        static bool HasOverlay(string name) => G.overlay.Cast<Transform>().Any(t => t.name == name);

        static IEnumerator Script()
        {
            while (!EditorApplication.isPlaying || Game.I == null) yield return 0.2f;
            Note("playing");
            SetSize(1920, 1080);
            yield return 0.5f;
            Shot("00_warning");
            Ctl.auto = true;
            yield return 0.5f;
            Ctl.auto = false;
            yield return 1f;
            Shot("01_title");
            Click("New Game");
            yield return 0.8f;
            Shot("02_intro");
            Ctl.auto = true;
            Minigame.autoWin = true;

            // ------------------------------------------------ play every day with a bot
            int lastDay = 0, guard = 0;
            float moveTarget = 4f;
            var cardsSeen = new HashSet<int>();
            while (guard++ < 200000)
            {
                yield return 0.02f;
                if (G == null) yield break;
                var s = G.save;
                if (!s.inRun && s.endings.Count > 0) break;
                Time.timeScale = G.cutscene || G.dialog.Busy ? 1 : 4;

                if (s.day != lastDay && !G.cutscene)
                {
                    lastDay = s.day;
                    Note($"day {s.day} start, hour {G.world.hour:F1}, x {G.boat.x:F1}, cash {s.cash}, levels {string.Join(",", s.levels)}");
                    Ctl.auto = false;
                    yield return 0.3f;
                    Shot($"d{s.day}_10_start");
                    Ctl.auto = true;
                    if (quick && s.day >= 3) break;
                }

                // story beats worth a picture
                if (G.dialog.Busy && UnityEngine.Object.FindObjectsByType<Button>().Any(x => x.isActiveAndEnabled && x.name.StartsWith("Button Tell him")))
                {
                    Ctl.auto = false;
                    yield return 0.6f;
                    Shot("d7_choice");
                    if (Click("Tell him everything")) Note("chose confess");
                    else { Click("Let the Other"); Note("confess locked, chose other"); }
                    Ctl.auto = true;
                    continue;
                }
                if (G.dialog.Busy)
                {
                    string key = G.world.hour >= 21.9f ? "night" : HasOverlay("Card") ? "card" : "talk";
                    if (!shots.Contains($"d{s.day}_{key}"))
                    {
                        Ctl.auto = false;
                        yield return 0.9f;
                        Shot($"d{s.day}_{key}");
                        Ctl.auto = true;
                    }
                    continue;
                }
                if (HasOverlay("CatchCard"))
                {
                    int n = s.caught.Sum(c => c.n);
                    if (cardsSeen.Add(n) && (n <= 2 || s.storyCaught.Count > shots.Count(x => x.Contains("_card_"))))
                    {
                        Ctl.auto = false;
                        yield return 0.4f;
                        Shot($"d{s.day}_card_{n}");
                        Ctl.auto = true;
                    }
                    continue;
                }
                if (G.cutscene || HasOverlay("Tent") || HasOverlay("Card")) continue;

                var b = G.boat;
                if (b.phase == "bite") { Shot($"d{s.day}_bite"); Ctl.PressAction(); continue; }
                if (G.minigame.isActiveAndEnabled && G.minigame.transform.gameObject.activeSelf) { Shot("minigame"); continue; }
                if (b.state != Boat.State.Free) { if (b.phase == "wait") Shot($"d{s.day}_wait"); continue; }

                bool wantSleep = s.scared || G.world.hour >= 20f || (s.StoryDoneToday() && !G.BlackoutDue && !s.Today.Finale);
                bool wantSell = s.box.Count >= s.BoxSize;
                Ctl.touchLeft = Ctl.touchRight = false;
                if (wantSleep || wantSell)
                {
                    if (!G.NearTent) { Ctl.touchLeft = true; continue; }
                    G.OpenTent();
                    yield return 0.2f;
                    Shot($"d{s.day}_tent");
                    Click("Sell all");
                    yield return 0.1f;
                    for (int k = 0; k < 4; k++) if (Click("Upgrade")) yield return 0.1f;
                    if (wantSleep) { if (!Click("Sleep")) Click("X"); }
                    else Click("X");
                    yield return 0.3f;
                    moveTarget = UnityEngine.Random.Range(-1f, s.levels[0] > 0 ? 26f : 14f);
                    continue;
                }
                if (Mathf.Abs(b.x - moveTarget) > 0.4f && b.x < G.world.MaxBoatX - 0.5f && b.x > G.world.MinBoatX + 0.5f)
                {
                    if (b.x < moveTarget) Ctl.touchRight = true; else Ctl.touchLeft = true;
                    continue;
                }
                Ctl.PressAction();
                yield return 0.1f;
            }
            Ctl.touchLeft = Ctl.touchRight = false;
            Note($"run over: endings {string.Join(",", G.save.endings)}, memories {G.save.memories.Count}, evidence {G.save.evidence.Count}");

            // ------------------------------------------------ menus and layouts
            Ctl.auto = false;
            Minigame.autoWin = false;
            Time.timeScale = 1;
            for (int w = 0; w < 200 && !HasOverlay("Title"); w++) yield return 0.1f;
            yield return 0.6f;
            Shot("90_title_after", false);
            Click("Journal");
            yield return 0.3f;
            Click("Memories");
            yield return 0.2f;
            Shot("91_journal", false);
            Click("X");
            Click("Settings");
            yield return 0.3f;
            Shot("92_settings", false);
            Click("X");

            // a fresh day for layout checks
            G.save.NewRun();
            G.save.morningDone = true;
            G.save.hour = 9;
            G.save.boatX = 2;
            G.save.Save();
            G.GoTitle();
            yield return 0.3f;
            Click("Continue");
            yield return 2.5f;
            Shot("93_lake_1080p", false);
            // radio: tune to the news and check something is actually playing
            Radio.I.Freq = 90.3f;
            Panels.Radio(null);
            yield return 1.5f;
            Shot("89_radio", false);
            foreach (var st in Radio.I.Stations)
                Note($"radio {st.Freq} {st.Name}: clip {st.Tracks[st.Index]} signal {st.Signal:0.00} playing {st.Src.isPlaying}");
            Note($"radio playing {Radio.I.Playing}, notes {GameObject.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Count(r => r.name == "note")}");
            Radio.I.Freq = 95.5f;
            yield return 0.5f;
            Shot("89_radio_static", false);
            foreach (Transform t in G.overlay) if (t.name == "Radio") UnityEngine.Object.Destroy(t.gameObject);
            Radio.I.Freq = 90.3f;
            yield return 4.5f;
            Note($"notes with panel closed: {GameObject.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Count(r => r.name == "note")}");
            Shot("89_notes", false);
            Radio.I.Freq = Radio.OffFreq;
            yield return 0.3f;
            G.world.hour = 19.2f;
            yield return 0.5f;
            Shot("94_dusk", false);
            G.world.hour = 20.4f;
            yield return 0.5f;
            Shot("95_night", false);
            G.world.hour = 9f;
            Ctl.usingTouch = true;
            SetSize(2400, 1080);
            yield return 0.6f;
            Shot("96_phone_touch", false);
            SetSize(1600, 1200);
            yield return 0.6f;
            Shot("97_tablet_4x3", false);
            SetSize(1080, 1920);
            yield return 0.6f;
            Shot("98_portrait", false);
            SetSize(1920, 1080);
            Ctl.usingTouch = false;
            G.TogglePause();
            yield return 0.4f;
            Shot("99_pause", false);
        }
    }
}
