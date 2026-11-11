using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SW
{
    /// <summary>The menus and cards, each built fresh when opened and destroyed when closed.</summary>
    public static class Panels
    {
        static Game G => Game.I;

        static RectTransform Modal(string name, out RectTransform panel, float w = 1000, float h = 860, bool dim = true)
        {
            var root = UI.Rect(name, G.overlay).Stretch();
            if (dim) UI.Dimmer(root, 0.45f);
            var p = UI.Panel(root);
            panel = p.rectTransform;
            panel.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, h));
            return root;
        }

        static Button CloseX(RectTransform panel, Action onClose)
        {
            var x = UI.Btn(panel, "X", onClose, 52, new Color(0, 0, 0, 0));
            ((RectTransform)x.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -10), new Vector2(100, 100));
            return x;
        }

        static string Scramble(string s)
        {
            const string glyphs = ".@&^%#*!";
            var c = s.ToCharArray();
            for (int i = 0; i < c.Length; i++) if (c[i] != ' ') c[i] = glyphs[(i * 7 + DateTime.Now.Millisecond / 100) % glyphs.Length];
            return new string(c);
        }

        // ---------------------------------------------------------------- catch card
        public static IEnumerator CatchCard(ItemDef item, bool isNew)
        {
            var root = Modal("CatchCard", out var p, 1240, 780);
            var icon = UI.Sprite(p, Art.Get(item.Art));
            icon.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(50, -40), new Vector2(256, 256));
            bool evidence = item.Kind == Kind.Evidence;
            var name = UI.Txt(p, item.GlitchName ? ".@&^%@" : item.Name, 62, evidence ? UI.OtherRed : UI.TextCol, TextAnchor.MiddleLeft);
            name.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(340, -110), new Vector2(840, 110));
            string tag = item.Kind == Kind.Memory ? "MEMORY" : evidence ? "" : isNew ? "NEW!" : "";
            var tagT = UI.Txt(p, tag, 42, item.Kind == Kind.Memory ? UI.DanCol : UI.Green, TextAnchor.MiddleLeft);
            tagT.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(342, -205), new Vector2(400, 50));
            if (item.Price > 0)
            {
                var price = UI.Txt(p, item.Price.ToString(), 54, UI.TextCol, TextAnchor.MiddleRight);
                price.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-96, -30), new Vector2(200, 60));
                var c = UI.Sprite(p, Art.Get("coin"));
                c.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-34, -38), new Vector2(46, 46));
            }
            var desc = UI.Txt(p, item.Desc, 48, evidence && item.GlitchName ? UI.OtherRed : UI.TextCol, TextAnchor.UpperLeft);
            desc.rectTransform.Anchor(0, 0, 1, 1);
            desc.rectTransform.offsetMin = new Vector2(52, 100);
            desc.rectTransform.offsetMax = new Vector2(-52, -320);
            var cont = UI.Txt(p, "CONTINUE>>", 48, UI.TextCol, TextAnchor.LowerRight);
            cont.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 30), new Vector2(400, 60));

            Audio.Play("ui_open", 0.8f);
            yield return null;
            Ctl.Consume();
            float t = 0;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                if (item.GlitchName && UnityEngine.Random.value < 0.1f) name.text = Scramble(".@&^%@");
                cont.color = new Color(1, 1, 1, 0.55f + 0.45f * Mathf.Sin(t * 4));
                if (t > 0.35f && Ctl.Any) break;
                yield return null;
            }
            Ctl.Consume();
            Audio.Play("ui_close", 0.8f);
            UnityEngine.Object.Destroy(root.gameObject);
        }

        // ---------------------------------------------------------------- tent
        public static void Tent(Action onSleep)
        {
            var s = G.save;
            var root = Modal("Tent", out var p, 1400, 960);
            Audio.Play("ui_open");
            Action rebuild = null;
            rebuild = () =>
            {
                for (int i = p.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(p.GetChild(i).gameObject);
                UI.Border(p, 6);
                CloseX(p, () => { Audio.Play("ui_close"); UnityEngine.Object.Destroy(root.gameObject); });
                var title = UI.Txt(p, "CAMP", 60);
                title.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(500, 70));
                var cash = UI.Txt(p, s.cash.ToString(), 56, UI.TextCol, TextAnchor.MiddleRight);
                cash.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-96, -30), new Vector2(300, 60));
                var coin = UI.Sprite(p, Art.Get("coin"));
                coin.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-34, -38), new Vector2(46, 46));

                // sell
                int value = 0;
                foreach (var id in s.box) value += GameData.Item(id).Price;
                var sellInfo = UI.Txt(p, s.box.Count == 0 ? "The fish box is empty." : $"Fish box: {s.box.Count}/{s.BoxSize}", 44, UI.Dim, TextAnchor.MiddleLeft);
                sellInfo.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, -120), new Vector2(600, 60));
                var sell = UI.Btn(p, value > 0 ? $"Sell all  +{value}" : "Sell all", () =>
                {
                    s.cash += value;
                    s.box.Clear();
                    s.Save();
                    Audio.Play("sell_all");
                    rebuild();
                }, 44, null, true);
                sell.interactable = value > 0;
                ((RectTransform)sell.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -110), new Vector2(460, 86));

                // upgrades
                for (int i = 0; i < GameData.Upgrades.Length; i++)
                {
                    int k = i;
                    var u = GameData.Upgrades[i];
                    int lvl = s.Level(i);
                    float y = -230 - i * 130;
                    var nm = UI.Txt(p, $"{u.Name}  (Lvl {(lvl >= 2 ? "MAX" : (lvl + 1).ToString())})", 48, UI.TextCol, TextAnchor.MiddleLeft);
                    nm.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, y), new Vector2(800, 60));
                    var bl = UI.Txt(p, u.Blurb, 38, UI.Dim, TextAnchor.MiddleLeft);
                    bl.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(62, y - 56), new Vector2(800, 48));
                    if (lvl < 2)
                    {
                        int cost = u.Costs[lvl];
                        var b = UI.Btn(p, $"Upgrade   {cost}", () =>
                        {
                            if (s.cash < cost) return;
                            s.cash -= cost;
                            s.levels[k]++;
                            s.Save();
                            Audio.Play("upgrade_purchase");
                            rebuild();
                        }, 42, null, true);
                        b.interactable = s.cash >= cost;
                        ((RectTransform)b.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, y + 4), new Vector2(460, 86));
                    }
                }

                // bottom row: journal, sleep
                var journal = UI.Btn(p, "Journal", () => Journal(null), 44, null, true);
                ((RectTransform)journal.transform).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(60, 36), new Vector2(360, 96));
                bool canSleep = s.scared || s.StoryDoneToday() || G.world.hour >= 20f;
                string sleepLabel = canSleep ? (G.world.hour >= 18f || s.scared ? "Sleep" : "Sleep early") : "Not tired yet";
                var sleep = UI.Btn(p, sleepLabel, () =>
                {
                    UnityEngine.Object.Destroy(root.gameObject);
                    onSleep();
                }, 44, new Color(0.12f, 0.12f, 0.24f, 0.95f), true);
                sleep.interactable = canSleep;
                ((RectTransform)sleep.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-60, 36), new Vector2(460, 96));
                if (!canSleep)
                {
                    var hint = UI.Txt(p, "(you can always sleep after dark)", 34, UI.Dim);
                    hint.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-60, 136), new Vector2(640, 44));
                }
            };
            rebuild();
        }

        // ---------------------------------------------------------------- journal
        // ---------------------------------------------------------------- radio
        /// <summary>Just the dial: drag it to tune by hand. The far left is OFF.</summary>
        public static void Radio(Action onClose)
        {
            var radio = SW.Radio.I;
            if (radio == null) return;
            var root = Modal("Radio", out var p, 1500, 760);
            CloseX(p, () => { G.settings.Save(); UnityEngine.Object.Destroy(root.gameObject); onClose?.Invoke(); });
            var title = UI.Txt(p, "RADIO", 52);
            title.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(600, 70));

            var freq = UI.Txt(p, "", 120, UI.Gold);
            freq.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(900, 140));
            var name = UI.Txt(p, "", 52, UI.TextCol);
            name.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -262), new Vector2(1300, 64));
            var tag = UI.Txt(p, "", 38, UI.Dim);
            tag.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -330), new Vector2(1300, 54));

            // dial: OFF sits in a notch at the far left, then 88 to 108
            const float Lo = 86.6f; // left edge of the dial (the OFF notch)
            float W = 1240f;
            Func<float, float> toX = f => (f - Lo) / (SW.Radio.MaxFreq - Lo) * W - W / 2;
            var dial = UI.Solid(p, new Color(0.12f, 0.11f, 0.1f, 0.95f), true, "Dial");
            var drt = dial.rectTransform;
            drt.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -420), new Vector2(1300, 190));
            UI.Border(drt, 4, new Color(1, 1, 1, 0.6f));
            var off = UI.Txt(drt, "OFF", 32, UI.Dim);
            off.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(toX(SW.Radio.OffFreq), -8), new Vector2(120, 44));
            for (int mhz = 88; mhz <= 108; mhz++)
            {
                float x = toX(mhz);
                bool major = mhz % 4 == 0;
                var tick = UI.Solid(drt, new Color(1, 1, 1, major ? 0.7f : 0.35f));
                tick.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(x, 20), new Vector2(4, major ? 56 : 30));
                if (major)
                {
                    var n = UI.Txt(drt, mhz.ToString(), 32, UI.Dim);
                    n.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(x, -8), new Vector2(120, 44));
                }
            }
            var needle = UI.Solid(drt, new Color(0.9f, 0.2f, 0.18f));
            needle.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8, 170));
            var dh = dial.gameObject.AddComponent<DialHandler>();
            dh.onDrag = local =>
            {
                float f = Lo + Mathf.Clamp01((local.x + W / 2) / W) * (SW.Radio.MaxFreq - Lo);
                radio.Freq = f < SW.Radio.MinFreq ? SW.Radio.OffFreq : Mathf.Round(f * 20f) / 20f;
            };

            var hint = UI.Txt(p, "Drag the dial to tune.", 34, UI.Dim);
            hint.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(1300, 50));
            var credit = UI.Txt(p, SW.Radio.Credit, 26, UI.Dim);
            credit.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(1400, 60));

            var live = root.gameObject.AddComponent<LiveUpdate>();
            live.tick = () =>
            {
                float f = radio.Freq;
                needle.rectTransform.anchoredPosition = new Vector2(toX(f), 0);
                freq.text = radio.On ? f.ToString("0.0") + " FM" : "OFF";
                var lk = radio.On ? radio.Locked : null;
                name.text = !radio.On ? "" : lk != null ? lk.Name : "";
                tag.text = lk != null ? lk.Tag : radio.On ? "Static." : "";
            };
        }

        public static void Journal(Action onClose)
        {
            var s = G.save;
            var root = Modal("Journal", out var p, 1640, 960);
            var tabs = new List<string> { "Fish", "Junk", "Memories" };
            if (s.evidence.Count > 0) tabs.Add("Evidence");
            int tab = 0;
            Action rebuild = null;
            rebuild = () =>
            {
                for (int i = p.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(p.GetChild(i).gameObject);
                UI.Border(p, 6);
                CloseX(p, () => { UnityEngine.Object.Destroy(root.gameObject); onClose?.Invoke(); });
                for (int i = 0; i < tabs.Count; i++)
                {
                    int k = i;
                    var tb = UI.Btn(p, tabs[i], () => { tab = k; rebuild(); }, 42, k == tab ? new Color(0.45f, 0.45f, 0.47f, 1) : UI.ButtonCol);
                    ((RectTransform)tb.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(130 + i * 250, -22), new Vector2(236, 70));
                }
                var kind = tab == 0 ? Kind.Fish : tab == 1 ? Kind.Junk : tab == 2 ? Kind.Memory : Kind.Evidence;
                var list = new List<ItemDef>();
                if (kind == Kind.Memory) foreach (var id in GameData.MemoryOrder) list.Add(GameData.Item(id));
                else if (kind == Kind.Evidence) foreach (var id in GameData.EvidenceOrder) list.Add(GameData.Item(id));
                else foreach (var it in GameData.Items) if (it.Kind == kind) list.Add(it);

                var detail = UI.Txt(p, "", 42, UI.TextCol, TextAnchor.UpperLeft);
                detail.rectTransform.Anchor(0.52f, 0, 1, 1);
                detail.rectTransform.offsetMin = new Vector2(10, 40);
                detail.rectTransform.offsetMax = new Vector2(-50, -130);
                int found = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    var it = list[i];
                    bool known = kind == Kind.Memory ? s.memories.Contains(it.Id) : kind == Kind.Evidence ? s.evidence.Contains(it.Id) : s.TimesCaught(it.Id) > 0;
                    if (known) found++;
                    int col = i % 5, row = i / 5;
                    var cell = UI.Btn(p, "", () =>
                    {
                        if (!known) { detail.text = "<color=#999999>???\n\nNot found yet.</color>"; return; }
                        string text = $"<size=54>{(it.GlitchName ? ".@&^%@" : it.Name)}</size>\n\n{it.Desc}";
                        if (it.Kind == Kind.Fish || it.Kind == Kind.Junk) text += $"\n\n<color=#bbbbbb>Caught: {s.TimesCaught(it.Id)}    Sells for {it.Price}</color>";
                        if (it.Kind == Kind.Fish) text += $"\n<color=#bbbbbb>{GameData.ZoneNames[it.Zone]}{(it.RodReq > 0 ? $", rod lvl {it.RodReq + 1}" : "")}{(it.BaitReq > 0 ? $", bait lvl {it.BaitReq + 1}" : "")}</color>";
                        if (it.Flashback != null)
                        {
                            text += "\n\n<color=#ffc890>";
                            foreach (var l in it.Flashback) text += l.Text + " ";
                            text += "</color>";
                        }
                        detail.text = text;
                    }, 20, new Color(0.18f, 0.18f, 0.19f, 1));
                    ((RectTransform)cell.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(50 + col * 150, -130 - row * 150), new Vector2(136, 136));
                    var ic = UI.Sprite(cell.transform, Art.Get(it.Art), known ? Color.white : new Color(0, 0, 0, 0.55f));
                    ic.rectTransform.Stretch(4);
                }
                var count = UI.Txt(p, $"{found}/{list.Count} found", 38, UI.Dim, TextAnchor.LowerLeft);
                count.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(54, 30), new Vector2(500, 50));
                detail.text = "<color=#999999>Pick an entry.</color>";
            };
            rebuild();
        }

        // ---------------------------------------------------------------- pause and settings
        public static void Pause(Action resume, Action quit)
        {
            var root = Modal("Pause", out var p, 760, 660);
            var t = UI.Txt(p, "PAUSED", 56);
            t.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(600, 80));
            string[] labels = { "Resume", "Journal", "Settings", "Save & Quit to Title" };
            Action[] acts =
            {
                () => { UnityEngine.Object.Destroy(root.gameObject); resume(); },
                () => Journal(null),
                () => Settings(null),
                () => { UnityEngine.Object.Destroy(root.gameObject); quit(); },
            };
            for (int i = 0; i < labels.Length; i++)
            {
                var b = UI.Btn(p, labels[i], acts[i], 46, null, true);
                ((RectTransform)b.transform).Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -160 - i * 116), new Vector2(620, 100));
            }
        }

        public static void Settings(Action onClose)
        {
            var st = G.settings;
            var root = Modal("Settings", out var p, 1240, 960);
            Action rebuild = null;
            rebuild = () =>
            {
                for (int i = p.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(p.GetChild(i).gameObject);
                UI.Border(p, 6);
                CloseX(p, () => { st.Save(); UnityEngine.Object.Destroy(root.gameObject); onClose?.Invoke(); });
                var t = UI.Txt(p, "SETTINGS", 52);
                t.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(600, 70));
                var rows = new List<(string, string, Action)>
                {
                    ("Music", Mathf.RoundToInt(st.music * 10) + "/10", () => st.music = st.music >= 0.99f ? 0 : Mathf.Min(1, st.music + 0.1f)),
                    ("Sound", Mathf.RoundToInt(st.sfx * 10) + "/10", () => st.sfx = st.sfx >= 0.99f ? 0 : Mathf.Min(1, st.sfx + 0.1f)),
                    ("Day length", SW.Settings.DayLengthNames[st.dayLength] + $" ({SW.Settings.DayMinutes[st.dayLength]:0.#} min)", () => st.dayLength = (st.dayLength + 1) % 3),
                    ("Reel assist", st.assist ? "On" : "Off", () => st.assist = !st.assist),
                    ("Text speed", SW.Settings.TextSpeedNames[st.textSpeed], () => st.textSpeed = (st.textSpeed + 1) % 3),
                    ("Screen shake", st.shake ? "On" : "Off", () => st.shake = !st.shake),
                };
                if (Application.isMobilePlatform)
                    rows.Add(("Vibration", st.haptics ? "On" : "Off", () => { st.haptics = !st.haptics; if (st.haptics) Haptics.Buzz(Haptics.Kind.Catch); }));
                if (!Application.isMobilePlatform)
                    rows.Add(("Fullscreen", Screen.fullScreen ? "On" : "Off", () => Screen.fullScreen = !Screen.fullScreen));
                for (int i = 0; i < rows.Count; i++)
                {
                    var (label, value, act) = rows[i];
                    float y = -120 - i * 104;
                    var l = UI.Txt(p, label, 46, UI.TextCol, TextAnchor.MiddleLeft);
                    l.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(80, y), new Vector2(500, 80));
                    var b = UI.Btn(p, value, () => { act(); st.Save(); rebuild(); }, 42, null, true);
                    ((RectTransform)b.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, y), new Vector2(500, 88));
                }
            };
            rebuild();
        }

        // ---------------------------------------------------------------- title
        public static void Title(Action cont, Action newGame)
        {
            var s = G.save;
            var root = UI.Rect("Title", G.overlay).Stretch();
            var bg = UI.Solid(root, new Color(0.18f, 0.04f, 0.06f), true);
            bg.rectTransform.Stretch();
            var art = UI.Sprite(root, Art.Get("coverart"), null, true);
            art.rectTransform.Anchor(0.42f, 0, 1, 1);
                        var title = UI.Txt(root, "STILL WATERS", 110, UI.TextCol, TextAnchor.MiddleLeft);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -110), new Vector2(1000, 160));
            var tag = UI.Txt(root, "Every catch drags up the past.", 44, new Color(1f, 0.75f, 0.7f), TextAnchor.MiddleLeft);
            tag.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(114, -250), new Vector2(1000, 60));

            var items = new List<(string, Action)>();
            if (s.inRun) items.Add(($"Continue  (Day {s.day})", () => { UnityEngine.Object.Destroy(root.gameObject); cont(); }));
            items.Add(("New Game", () =>
            {
                if (s.inRun) Confirm("Start over? Your current run will be lost. (The journal and endings are kept.)", () => { UnityEngine.Object.Destroy(root.gameObject); newGame(); });
                else { UnityEngine.Object.Destroy(root.gameObject); newGame(); }
            }));
            items.Add(("Journal", () => Journal(null)));
            items.Add(("Settings", () => Settings(null)));
            if (!Application.isMobilePlatform && Application.platform != RuntimePlatform.WebGLPlayer)
                items.Add(("Quit", () => Application.Quit()));
            for (int i = 0; i < items.Count; i++)
            {
                var (label, act) = items[i];
                var b = UI.Btn(root, label, act, 50, new Color(0.1f, 0.1f, 0.11f, 0.85f), true);
                ((RectTransform)b.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -350 - i * 124), new Vector2(660, 106));
            }
            int endings = s.endings.Count;
            var foot = UI.Txt(root, (endings > 0 ? $"Endings found: {endings}/3     " : "") + "Remake of the Fishing Horror Jam entry", 32, new Color(1, 1, 1, 0.55f), TextAnchor.LowerLeft);
            foot.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(110, 40), new Vector2(1400, 50));
        }

        public static void Confirm(string text, Action yes)
        {
            var root = Modal("Confirm", out var p, 900, 460);
            var t = UI.Txt(p, text, 46);
            t.rectTransform.Anchor(0, 0.4f, 1, 1);
            t.rectTransform.offsetMin = new Vector2(50, 0);
            t.rectTransform.offsetMax = new Vector2(-50, -30);
            var y = UI.Btn(p, "Yes", () => { UnityEngine.Object.Destroy(root.gameObject); yes(); }, 38, new Color(0.45f, 0.12f, 0.12f, 1), true);
            ((RectTransform)y.transform).Place(new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(-20, 50), new Vector2(300, 90));
            var n = UI.Btn(p, "No", () => UnityEngine.Object.Destroy(root.gameObject), 38, null, true);
            ((RectTransform)n.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(20, 50), new Vector2(300, 90));
        }

        public static IEnumerator Warning()
        {
            var root = UI.Rect("Warning", G.overlay).Stretch();
            UI.Solid(root, Color.black, true).rectTransform.Stretch();
            var t = UI.Txt(root, "<color=#d02020>Warning:</color> this game contains sensitive topics, including violence, gore, mental illness and suicide.\n\nPlay with care.", 46);
            t.rectTransform.Anchor(0.15f, 0.3f, 0.85f, 0.75f);
            var c = UI.Txt(root, "tap or press any key", 40, UI.Dim);
            c.rectTransform.Anchor(0, 0.12f, 1, 0.2f);
            yield return new WaitForSecondsRealtime(0.6f);
            while (!Ctl.Any) yield return null;
            Ctl.Consume();
            UnityEngine.Object.Destroy(root.gameObject);
        }

        /// <summary>Big centred text card (DAY N / ending titles).</summary>
        public static IEnumerator Card(string big, string small, float hold, Color? col = null)
        {
            var root = UI.Rect("Card", G.overlay).Stretch();
            var cg = root.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            var t = UI.Txt(root, big, 96, col ?? UI.TextCol);
            t.rectTransform.Anchor(0, 0.5f, 1, 0.7f);
            var s = UI.Txt(root, small, 50, UI.Dim);
            s.rectTransform.Anchor(0, 0.4f, 1, 0.5f);
            for (float a = 0; a < 1; a += Time.unscaledDeltaTime * 2) { if (cg == null) yield break; cg.alpha = a; yield return null; }
            if (cg == null) yield break;
            cg.alpha = 1;
            for (float h = 0; h < hold; h += Time.unscaledDeltaTime)
            {
                if (h > 0.5f && Ctl.Any) break;
                yield return null;
            }
            for (float a = 1; a > 0; a -= Time.unscaledDeltaTime * 2) { if (cg == null) yield break; cg.alpha = a; yield return null; }
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
        }
    }

    /// <summary>Runs a callback every frame while its panel is open.</summary>
    public class LiveUpdate : MonoBehaviour
    {
        public Action tick;
        void Update() => tick?.Invoke();
    }

    /// <summary>Reports the pointer position (in the dial's local space) while it's pressed or dragged.</summary>
    public class DialHandler : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IDragHandler, UnityEngine.EventSystems.IPointerUpHandler
    {
        public Action<Vector2> onDrag;
        void Report(UnityEngine.EventSystems.PointerEventData e)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out var local))
            {
                onDrag?.Invoke(local);
            }
        }
        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e) => Report(e);
        public void OnDrag(UnityEngine.EventSystems.PointerEventData e) => Report(e);
        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e) { }
    }
}
