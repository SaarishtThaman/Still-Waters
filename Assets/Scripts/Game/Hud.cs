using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SW
{
    /// <summary>Day and clock, money, the fish box, evidence count, thoughts over the boat, touch controls.</summary>
    public class Hud : MonoBehaviour
    {
        RectTransform root, touch, thoughtRt;
        Text day, clock, zone, cash, boxText, evidence, thought, prompt, toast;
        Image coin;
        Button actionBtn, tentBtn, pauseBtn, radioBtn;
        Coroutine thinking, toasting;
        CanvasGroup group;

        public static Hud Create(Transform parent)
        {
            var rt = UI.Rect("Hud", parent).Stretch();
            var h = rt.gameObject.AddComponent<Hud>();
            h.root = rt;
            h.Build();
            return h;
        }

        void Build()
        {
            group = root.gameObject.AddComponent<CanvasGroup>();
            day = UI.Txt(root, "DAY 1", 62, UI.TextCol, TextAnchor.UpperLeft);
            day.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -26), new Vector2(500, 70));
            clock = UI.Txt(root, "6:00 AM", 46, UI.TextCol, TextAnchor.UpperLeft);
            clock.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(42, -100), new Vector2(500, 60));
            zone = UI.Txt(root, "", 38, UI.Dim, TextAnchor.UpperLeft);
            zone.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(42, -156), new Vector2(600, 50));

            pauseBtn = UI.Btn(root, "II", () => Game.I.TogglePause(), 40, new Color(0, 0, 0, 0.3f), true);
            ((RectTransform)pauseBtn.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-26, -24), new Vector2(84, 84));
            radioBtn = UI.Btn(root, "FM", () => { if (!Game.I.Blocked) Panels.Radio(null); }, 34, new Color(0, 0, 0, 0.3f), true);
            ((RectTransform)radioBtn.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-26, -124), new Vector2(84, 84));
            cash = UI.Txt(root, "5", 58, UI.TextCol, TextAnchor.UpperRight);
            cash.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-196, -28), new Vector2(300, 64));
            coin = UI.Sprite(root, Art.Get("coin"));
            coin.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-132, -34), new Vector2(46, 46));
            boxText = UI.Txt(root, "", 44, UI.TextCol, TextAnchor.UpperRight);
            boxText.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-132, -104), new Vector2(500, 60));
            evidence = UI.Txt(root, "", 44, UI.OtherRed, TextAnchor.UpperRight);
            evidence.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-132, -160), new Vector2(500, 60));

            thought = UI.Txt(root, "", 46, UI.TextCol);
            thoughtRt = thought.rectTransform;
            thoughtRt.Place(new Vector2(0, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(1200, 160));
            thought.alignment = TextAnchor.LowerCenter;

            prompt = UI.Txt(root, "", 40, UI.TextCol);
            prompt.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1200, 50));
            toast = UI.Txt(root, "", 52, UI.Gold);
            toast.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(1400, 70));

            // touch controls
            touch = UI.Rect("Touch", root).Stretch();
            var left = UI.HoldButton(touch, "<", on => Ctl.touchLeft = on);
            left.rectTransform.Place(Vector2.zero, Vector2.zero, new Vector2(40, 40), new Vector2(190, 190));
            var right = UI.HoldButton(touch, ">", on => Ctl.touchRight = on);
            right.rectTransform.Place(Vector2.zero, Vector2.zero, new Vector2(250, 40), new Vector2(190, 190));
            actionBtn = UI.Btn(touch, "CAST", null, 50, new Color(0, 0, 0, 0.4f), true);
            actionBtn.onClick.RemoveAllListeners();
            actionBtn.gameObject.AddComponent<PressHandler>().onPress = Ctl.PressAction;
            ((RectTransform)actionBtn.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 40), new Vector2(230, 230));
            tentBtn = UI.Btn(touch, "TENT", null, 46, new Color(0.2f, 0.15f, 0.05f, 0.6f), true);
            tentBtn.onClick.RemoveAllListeners();
            tentBtn.gameObject.AddComponent<PressHandler>().onPress = Ctl.PressTent;
            ((RectTransform)tentBtn.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-290, 40), new Vector2(170, 130));
        }

        public void SetVisible(bool on)
        {
            group.alpha = on ? 1 : 0;
            group.blocksRaycasts = on;
        }

        public void Think(string text, bool other = false)
        {
            if (thinking != null) StopCoroutine(thinking);
            thinking = StartCoroutine(ThinkCo(text, other));
        }

        IEnumerator ThinkCo(string s, bool other)
        {
            thought.color = other ? UI.OtherRed : UI.TextCol;
            thought.text = "";
            for (int i = 1; i <= s.Length; i++)
            {
                thought.text = s.Substring(0, i);
                yield return new WaitForSeconds(0.035f);
            }
            yield return new WaitForSeconds(1.6f + s.Length * 0.02f);
            while (thought.text.Length > 0)
            {
                thought.text = thought.text.Substring(0, thought.text.Length - 1);
                yield return new WaitForSeconds(0.015f);
            }
            thinking = null;
        }

        public void Toast(string s, Color? c = null)
        {
            if (toasting != null) StopCoroutine(toasting);
            toasting = StartCoroutine(ToastCo(s, c ?? UI.Gold));
        }

        IEnumerator ToastCo(string s, Color c)
        {
            toast.text = s;
            for (float t = 0; t < 3f; t += Time.deltaTime)
            {
                float a = Mathf.Min(1, t * 4, (3f - t) * 2);
                toast.color = new Color(c.r, c.g, c.b, a);
                yield return null;
            }
            toast.text = "";
        }

        void LateUpdate()
        {
            var g = Game.I;
            if (g == null || g.save == null) return;
            var s = g.save;
            day.text = "DAY " + s.day;
            clock.text = UI.Clock(g.world.hour);
            clock.color = g.world.hour >= 20 ? new Color(0.7f, 0.75f, 1f) : g.world.hour >= 18 ? new Color(1f, 0.8f, 0.6f) : UI.TextCol;
            zone.text = g.boat != null ? GameData.ZoneNames[Loot.ZoneAt(g.boat.x)] : "";
            cash.text = s.cash.ToString();
            boxText.text = $"BOX {s.box.Count}/{s.BoxSize}";
            boxText.color = s.box.Count >= s.BoxSize ? new Color(1f, 0.6f, 0.4f) : UI.TextCol;
            evidence.text = s.evidence.Count > 0 && s.storyCaught.Contains("hand") ? $"{CountRunEvidence(s)}/6" : "";

            // thought follows the boat
            if (g.boat != null)
            {
                var sp = g.world.cam.WorldToScreenPoint(g.boat.transform.position + new Vector3(0, 1.2f, 0));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, sp, g.UICam, out var local);
                thoughtRt.anchoredPosition = local + new Vector2(root.rect.width / 2, root.rect.height / 2);
            }

            bool free = g.boat != null && g.boat.state == Boat.State.Free && !g.Blocked;
            bool touchUI = Ctl.usingTouch || Application.isMobilePlatform;
            touch.gameObject.SetActive(touchUI && !g.Blocked);
            bool near = free && g.NearTent;
            g.world.SetTentHighlight(near);
            tentBtn.gameObject.SetActive(near);
            if (g.boat != null) actionBtn.SetLabel(g.boat.phase == "bite" ? "STRIKE!" : g.boat.phase == "wait" ? "REEL" : "CAST");
            prompt.text = touchUI ? "" : near ? "E  open tent        SPACE  cast" : (g.boat != null && g.boat.phase == "bite" ? "SPACE!" : "");
        }

        static int CountRunEvidence(SaveData s)
        {
            int n = 0;
            foreach (var e in GameData.EvidenceOrder) if (s.storyCaught.Contains(e)) n++;
            return n;
        }
    }
}
