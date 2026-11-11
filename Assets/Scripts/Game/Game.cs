using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SW
{
    /// <summary>Boots the whole game from code (one empty scene) and runs the story.</summary>
    public class Game : MonoBehaviour
    {
        public static Game I;
        public Settings settings;
        public SaveData save;
        public World world;
        public Boat boat;
        public Hud hud;
        public Dialog dialog;
        public Minigame minigame;
        public RectTransform overlay, safe;
        public Canvas canvas;
        public Camera UICam => canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        public bool cutscene = true;
        public bool inDay;
        bool paused;
        float whisperT = 40f;

        public bool Blocked => cutscene || paused || overlay.childCount > 0 || dialog.Busy;
        public bool NearTent => boat != null && Mathf.Abs(boat.x - world.TentX) < 2.2f;
        public bool BlackoutDue
        {
            get
            {
                var d = save.Today;
                if (string.IsNullOrEmpty(d.Blackout) || save.blackoutDone) return false;
                foreach (var sc in d.Story) if (!save.storyCaught.Contains(sc.Item)) return false;
                return save.castsSinceStory >= d.BlackoutAfter;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            new GameObject("Game").AddComponent<Game>();
        }

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            settings = Settings.Load();
            save = SaveData.Load();
            gameObject.AddComponent<Audio>();

            if (Camera.main == null)
            {
                var c = new GameObject("Main Camera").AddComponent<Camera>();
                c.tag = "MainCamera";
            }
            if (FindAnyObjectByType<AudioListener>() == null) Camera.main.gameObject.AddComponent<AudioListener>();
            world = new GameObject("World").AddComponent<World>();
            world.Build();
            boat = Boat.Create(world);
            boat.Place(save.boatX);

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(es);
            canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            safe = UI.Rect("Safe", canvas.transform).Stretch();
            hud = Hud.Create(safe);
            minigame = Minigame.Create(safe);
            dialog = Dialog.Create(canvas.transform);
            Radio.Create(transform);
            overlay = UI.Rect("Overlay", canvas.transform).Stretch();
            ApplySafeArea();
        }

        void ApplySafeArea()
        {
            var sa = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            safe.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            safe.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            safe.offsetMin = safe.offsetMax = Vector2.zero;
        }

        void Start() => StartCoroutine(Main());

        IEnumerator Main()
        {
            hud.SetVisible(false);
            world.hour = 7.5f;
            if (!save.warningSeen)
            {
                yield return Panels.Warning();
                save.warningSeen = true;
                save.Save();
            }
            GoTitle();
        }

        public void GoTitle()
        {
            StopAllCoroutines();
            Time.timeScale = 1;
            paused = false;
            inDay = false;
            cutscene = true;
            boat.state = Boat.State.Locked;
            hud.SetVisible(false);
            Audio.StopBeds();
            Audio.Music("calm_bgm", 0.8f);
            world.SetBlack(0);
            world.calm = 0;
            world.dreadLevel = 0;
            for (int i = overlay.childCount - 1; i >= 0; i--) Destroy(overlay.GetChild(i).gameObject);
            Panels.Title(() => StartCoroutine(RunDay()), () =>
            {
                save.NewRun();
                save.Save();
                StartCoroutine(Intro());
            });
        }

        IEnumerator Intro()
        {
            world.SnapBlack(1);
            Audio.Music(null);
            yield return dialog.Play(GameData.Intro, true);
            yield return RunDay();
        }

        // ---------------------------------------------------------------- day
        IEnumerator RunDay()
        {
            var s = save;
            cutscene = true;
            inDay = false;
            boat.state = Boat.State.Locked;
            hud.SetVisible(false);
            world.SnapBlack(1);
            Audio.StopBeds();
            if (!s.morningDone)
            {
                Audio.Music(null);
                yield return Panels.Card("DAY " + s.day, s.Today.Subtitle, 2.6f);
                if (s.Today.Morning != null) yield return dialog.Play(s.Today.Morning, true);
                if (s.Today.LostTime) { s.hour = 10.5f; s.boatX = 23f; }
                s.morningDone = true;
                s.Save();
            }
            world.hour = s.hour;
            boat.Place(s.boatX);
            world.calm = s.day == 7 ? 1 : 0;
            world.dreadLevel = s.day >= 6 ? 0.55f : s.day >= 4 ? 0.25f : 0f;
            DayAudio();
            world.SetBlack(0, 1.2f);
            hud.SetVisible(true);
            yield return new WaitForSeconds(0.6f);
            cutscene = false;
            inDay = true;
            boat.state = Boat.State.Free;
            if (s.day == 1 && s.castsToday == 0)
            {
                hud.Toast(Ctl.usingTouch || Application.isMobilePlatform ? "Arrows to move.  CAST to fish." : "A / D to move.   SPACE to cast.", UI.TextCol);
                yield return new WaitForSeconds(4f);
                hud.Toast(Ctl.usingTouch || Application.isMobilePlatform ? "Strike when you see the  !" : "Strike with SPACE when you see the  !", UI.TextCol);
            }
        }

        void DayAudio()
        {
            var s = save;
            Audio.Bed("lake_sound", s.day == 7 ? 0.25f : 0.45f);
            bool horror = s.storyCaught.Contains("hand");
            Audio.Music(horror || s.day >= 4 ? null : "calm_bgm", 0.75f);
            Audio.Bed("seagullaudio", (s.day == 3 && !s.storyCaught.Contains("hand")) ? 0.35f : 0f);
        }

        void Update()
        {
            if (Ctl.Pause && !cutscene && inDay) TogglePause();
            if (Ctl.Journal && inDay && !Blocked) Panels.Journal(null);
            if (Ctl.RadioKey && inDay && !Blocked) Panels.Radio(null);
            if (!inDay || Blocked) return;

            var s = save;
            float before = world.hour;
            world.hour = Mathf.Min(21f, world.hour + 15f / settings.DaySeconds * Time.deltaTime);
            s.hour = world.hour;
            s.boatX = boat.x;
            Audio.Bed("night_time", Mathf.Clamp01(Mathf.InverseLerp(18.5f, 20.5f, world.hour)) * 0.5f);
            if (before < 18f && world.hour >= 18f) hud.Think("The light is going. I don't have long left.");
            if (before < 20f && world.hour >= 20f) hud.Think("It's too dark to fish now. I should head back to the tent.");

            // the Other starts talking on day 4
            if (s.day >= 4 && boat.state == Boat.State.Free)
            {
                whisperT -= Time.deltaTime;
                if (whisperT <= 0)
                {
                    whisperT = Random.Range(45f, 90f) / (s.day >= 6 ? 1.6f : 1f);
                    hud.Think(GameData.OtherWhispers[Random.Range(0, GameData.OtherWhispers.Length)], true);
                }
            }
        }

        void OnApplicationPause(bool p) { if (p && save != null && inDay) save.Save(); }
        void OnApplicationQuit() { if (save != null && inDay) save.Save(); }

        public void TogglePause()
        {
            if (paused) return; // closing happens from the menu
            if (cutscene || overlay.childCount > 0 || dialog.Busy) return;
            paused = true;
            Time.timeScale = 0;
            save.Save();
            Panels.Pause(() => { paused = false; Time.timeScale = 1; }, () => { save.Save(); GoTitle(); });
        }

        public void OpenTent()
        {
            if (Blocked) return;
            Panels.Tent(() => StartCoroutine(Night()));
        }

        // ---------------------------------------------------------------- night
        IEnumerator Night()
        {
            var s = save;
            cutscene = true;
            inDay = false;
            boat.state = Boat.State.Locked;
            hud.SetVisible(false);
            bool done = s.StoryDoneToday();
            Audio.Music(null);
            world.SetBlack(1, 1.5f);
            yield return new WaitForSeconds(0.8f);
            Audio.Bed("seagullaudio", 0);
            Audio.Bed("night_time", 0.5f);
            world.hour = 22f;
            boat.Place(world.TentX + 1.5f);
            world.SetBlack(0.25f, 1f);
            yield return new WaitForSeconds(1.2f);
            if (done && s.Today.Night != null) yield return dialog.Play(s.Today.Night, false);
            else if (!done) yield return dialog.Play(new[] { new Line(Speaker.Me, "I slept badly. It felt like I'd left something unfinished out there.") }, false);
            world.SetBlack(1, 1f);
            yield return new WaitForSeconds(1.2f);
            Audio.Bed("night_time", 0);
            if (done) s.NextDay();
            else s.RepeatDay();
            s.Save();
            yield return RunDay();
        }

        // ---------------------------------------------------------------- catches
        public IEnumerator Caught(string id)
        {
            var s = save;
            var item = GameData.Item(id);
            bool isNew = s.TimesCaught(id) == 0;
            s.AddCaught(id);
            if (item.Kind == Kind.Evidence)
            {
                Audio.Play("horrocaughtaudio");
                world.Flash(new Color(0.51f, 0.1f, 0.1f), 0.8f, 2.2f);
                world.Shake(0.3f);
                Haptics.Buzz(Haptics.Kind.Dread);
            }
            else { Audio.Play("fish_caught", 0.8f); Haptics.Buzz(Haptics.Kind.Catch); }
            yield return Panels.CatchCard(item, isNew);
            yield return AfterCatch(item, isNew);
        }

        IEnumerator AfterCatch(ItemDef item, bool isNew)
        {
            var s = save;
            switch (item.Kind)
            {
                case Kind.Fish:
                case Kind.Junk:
                    s.box.Add(item.Id);
                    bool other = s.day >= 5 && Random.value < 0.3f;
                    if (other) hud.Think(GameData.OtherWhispers[Random.Range(0, GameData.OtherWhispers.Length)], true);
                    else hud.Think(item.Kind == Kind.Fish ? GameData.GoodCatch[Random.Range(0, 3)] : GameData.JunkCatch[Random.Range(0, 3)]);
                    if (isNew && item.Kind == Kind.Fish) hud.Toast("New species: " + item.Name);
                    break;
                case Kind.Memory:
                    StoryDone(item.Id);
                    if (!s.memories.Contains(item.Id)) s.memories.Add(item.Id);
                    world.Memory(1);
                    Audio.Music("calm_bgm", 0.5f);
                    yield return new WaitForSeconds(0.6f);
                    yield return dialog.Play(item.Flashback, false);
                    world.Memory(0);
                    DayAudio();
                    hud.Toast($"Memory {CountMemories()}/5");
                    break;
                case Kind.Evidence:
                    StoryDone(item.Id);
                    if (!s.evidence.Contains(item.Id)) s.evidence.Add(item.Id);
                    if (item.OnCatch != null) yield return dialog.Play(item.OnCatch, false);
                    if (item.Id == "hand")
                    {
                        s.scared = true;
                        DayAudio();
                        Audio.Bed("seagullaudio", 0);
                        hud.Think("I need to get back to the tent. Now.");
                    }
                    if (s.Today.Finale && s.StoryDoneToday())
                    {
                        s.Save();
                        yield return Finale();
                        yield break;
                    }
                    break;
            }
            s.Save();
        }

        void StoryDone(string id)
        {
            if (!save.storyCaught.Contains(id)) save.storyCaught.Add(id);
            save.castsSinceStory = 0;
        }

        int CountMemories()
        {
            int n = 0;
            foreach (var m in GameData.MemoryOrder) if (save.storyCaught.Contains(m) || save.memories.Contains(m)) n++;
            return n;
        }

        /// <summary>Day 5: the Other takes the afternoon.</summary>
        public IEnumerator Blackout()
        {
            var s = save;
            cutscene = true;
            boat.state = Boat.State.Locked;
            hud.SetVisible(false);
            world.SetBlack(1, 0.8f);
            yield return new WaitForSeconds(1.4f);
            yield return dialog.Play(new[] { new Line(Speaker.Other, "Let me take it from here."), new Line(Speaker.Static, "...") }, true);
            s.blackoutDone = true;
            world.hour = s.hour = Mathf.Min(s.hour + 4f, 18.7f);
            boat.Place(Mathf.Max(boat.x, 23f));
            yield return new WaitForSeconds(0.6f);
            world.SetBlack(0, 0.6f);
            yield return new WaitForSeconds(1.2f);
            hud.SetVisible(true);
            var item = GameData.Item(s.Today.Blackout);
            s.AddCaught(item.Id);
            Audio.Play("horrocaughtaudio");
            world.Flash(new Color(0.51f, 0.1f, 0.1f), 0.7f, 2f);
            yield return Panels.CatchCard(item, true);
            yield return AfterCatch(item, true);
            s.Save();
            cutscene = false;
            boat.state = Boat.State.Free;
        }

        // ---------------------------------------------------------------- finale
        IEnumerator Finale()
        {
            var s = save;
            cutscene = true;
            inDay = false;
            boat.state = Boat.State.Locked;
            hud.SetVisible(false);
            world.SetBlack(1, 0.7f);
            yield return new WaitForSeconds(1.6f);
            world.hour = 19.6f;
            world.calm = 1;
            world.dreadLevel = 0.8f;
            boat.Place(world.TentX + 2.4f);
            Audio.StopBeds();
            Audio.Bed("night_time", 0.4f);
            world.SetBlack(0, 0.5f);
            yield return new WaitForSeconds(1.5f);
            Audio.Play("motorboat", 0.7f);
            yield return dialog.Play(GameData.Finale, false);

            int mem = CountMemories();
            bool canConfess = mem >= 5;
            int choice = -1;
            yield return dialog.Choose(
                new[] { canConfess ? GameData.ChoiceConfess : GameData.ChoiceConfess + "  (I don't remember enough)", GameData.ChoiceOther, GameData.ChoiceWater },
                new[] { canConfess, true, true }, c => choice = c);
            yield return Ending(choice);
        }

        IEnumerator Ending(int idx)
        {
            var s = save;
            Audio.StopBeds();
            if (idx == 0)
            {
                world.SetBlack(1, 0.4f);
                yield return dialog.Play(GameData.EndingConfess, true);
            }
            else if (idx == 1)
            {
                world.Flash(new Color(0.6f, 0.05f, 0.05f), 0.9f, 3f);
                Audio.Play("horrocaughtaudio");
                yield return dialog.Play(GameData.EndingOther, true);
                yield return Panels.Card("DAY 1", "Arrival", 2.2f);
                yield return dialog.Play(GameData.EndingOtherCoda, true);
            }
            else
            {
                world.SetBlack(1, 0.3f);
                yield return dialog.Play(GameData.EndingWater, true);
            }
            string id = GameData.EndingIds[idx];
            if (!s.endings.Contains(id)) s.endings.Add(id);
            s.inRun = false;
            s.Save();
            var black = UI.Solid(overlay, Color.black, true);
            black.rectTransform.Stretch();
            yield return Panels.Card("ENDING: " + GameData.EndingTitles[idx].ToUpper(), $"Endings found: {s.endings.Count}/3", 4f, idx == 1 ? UI.OtherRed : UI.TextCol);
            yield return Panels.Card("THANK YOU FOR PLAYING", idx == 0 ? "" : "There's another way this could end.", 3.5f);
            Destroy(black.gameObject);
            GoTitle();
        }
    }
}
