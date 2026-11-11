using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SW
{
    /// <summary>
    /// The jam's reel minigame, made busier: a fast bar sweeps across the fish and you press on
    /// the green. Every press redraws the zones, each hit shrinks the next green band, red bands
    /// cost two line marks, and the panel trembles harder as the fish fights.
    /// </summary>
    public class Minigame : MonoBehaviour
    {
        RectTransform root, fish, cursor;
        Text label, strikesText;
        Button reelBtn;
        readonly List<RectTransform> bands = new List<RectTransform>();
        const float ArtW = 90f, Scale = 6f; // fish art is 90x34 px, drawn at 6x
        public static bool autoWin;          // headless playtest

        public static Minigame Create(Transform canvas)
        {
            var rt = UI.Rect("Minigame", canvas).Stretch();
            var m = rt.gameObject.AddComponent<Minigame>();
            m.root = rt;
            m.Build();
            return m;
        }

        void Build()
        {
            var panel = UI.Panel(root, new Color(0.3f, 0.3f, 0.31f, 0.88f), 6);
            panel.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(160 * Scale, 72 * Scale));
            panel.raycastTarget = false;
            var fishImg = UI.Sprite(panel.transform, Art.Get("minigamefish"), null, false);
            fishImg.color = new Color(0.42f, 0.4f, 0.44f); // dull the red fill so the green and red zones read clearly
            fish = fishImg.rectTransform;
            fish.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(ArtW * Scale, 34 * Scale));
            var mask = fishImg.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            cursor = UI.Solid(fish, Color.white, false, "Cursor").rectTransform;
            cursor.Place(new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3 * Scale, 40 * Scale));
            var outline = UI.Sprite(panel.transform, Art.Get("minigamefishoutline"), null, false);
            outline.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(ArtW * Scale, 34 * Scale));
            label = UI.Txt(panel.transform, "SPACE", 50, UI.TextCol);
            label.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(920, 60));
            strikesText = UI.Txt(panel.transform, "", 44, UI.TextCol, TextAnchor.UpperRight);
            strikesText.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -14), new Vector2(500, 60));
            // touch: same spot as the CAST button, so the thumb doesn't move
            reelBtn = UI.Btn(root, "REEL", null, 54, new Color(0, 0, 0, 0.45f), true);
            reelBtn.onClick.RemoveAllListeners();
            reelBtn.gameObject.AddComponent<PressHandler>().onPress = Ctl.PressAction;
            ((RectTransform)reelBtn.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 40), new Vector2(230, 230));
            root.gameObject.SetActive(false);
        }

        enum Zone { Green, Red }
        readonly List<Zone> kinds = new List<Zone>();
        RectTransform panelRt;
        Image cursorImg;
        float shakeAmt;
        Vector2 panelBase;

        /// <summary>Plays one round. result(true) if landed.</summary>
        public IEnumerator Run(int difficulty, bool assist, System.Action<bool> result)
        {
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            bool touchUI = Ctl.usingTouch || Application.isMobilePlatform;
            reelBtn.gameObject.SetActive(touchUI);
            label.text = touchUI ? "REEL on the green. Avoid the red." : "SPACE on the green. Avoid the red.";
            if (panelRt == null) { panelRt = (RectTransform)fish.parent; panelBase = panelRt.anchoredPosition; cursorImg = cursor.GetComponent<Image>(); }

            int d = Mathf.Clamp(difficulty, 1, 3);
            int need = d;                                            // 1, 2 or 3 hits
            float green = new[] { 16f, 13f, 11f }[d - 1] * (assist ? 1.4f : 1f);
            float redW = new[] { 10f, 12f, 14f }[d - 1];
            int reds = d == 1 ? 1 : 2;
            float speed = new[] { 95f, 115f, 135f }[d - 1] * (assist ? 0.75f : 1f);
            int line = assist ? 4 : 3;                               // a miss costs 1, red costs 2
            float pos = Random.value < 0.5f ? 2f : ArtW - 2f;
            float dir = pos < ArtW / 2 ? 1 : -1;
            int hits = 0, lost = 0;
            Regen(pos, green, redW, reds);
            UpdateStrikes(hits, need, lost, line);
            yield return null;
            Ctl.Consume();
            float grace = 0.2f;
            float fight = 0;   // the fish jerks the cursor now and then
            while (true)
            {
                float dt = Time.deltaTime;
                grace -= dt;
                // the fish fights: brief speed surges (the bar only ever turns at the ends)
                fight -= dt;
                float surge = 1f;
                if (fight < 0) { fight = Random.Range(0.6f, 1.4f); if (Random.value < 0.25f + hits * 0.15f) Shake(5f + d * 2f); }
                if (fight > 0 && fight < 0.18f) surge = 1.6f;
                pos += dir * speed * surge * dt;
                if (pos >= ArtW - 1) { pos = ArtW - 1; dir = -1; }
                if (pos <= 1) { pos = 1; dir = 1; }
                cursor.anchoredPosition = new Vector2(pos * Scale, 0);
                var under = ZoneAt(pos);
                cursorImg.color = under == 1 ? new Color(1f, 0.55f, 0.55f) : under == 0 ? new Color(0.75f, 1f, 0.75f) : Color.white;

                // constant tremble on the line, stronger as the fish tires you out
                shakeAmt = Mathf.Max(shakeAmt * Mathf.Exp(-dt * 10f), 1f + d * 0.6f + hits * 0.8f);
                panelRt.anchoredPosition = panelBase + Random.insideUnitCircle * shakeAmt;

                if (autoWin && grace < -0.5f)
                {
                    Close();
                    result(true);
                    yield break;
                }
                if (grace <= 0 && Ctl.Action)
                {
                    if (under == 0)
                    {
                        hits++;
                        Audio.Play("reel_in", 0.5f, 1.2f + hits * 0.1f);
                        Haptics.Buzz(Haptics.Kind.Hit);
                        Game.I.world.Shake(0.08f + hits * 0.04f);
                        Shake(14f);
                        StartCoroutine(Pulse(fish, 1.08f));
                        speed *= 1.12f;
                        green *= 0.72f;                              // every hit shrinks the next green
                        if (hits >= need)
                        {
                            UpdateStrikes(hits, need, lost, line);
                            yield return new WaitForSeconds(0.2f);
                            break;
                        }
                    }
                    else
                    {
                        bool red = under == 1;
                        lost += red ? 2 : 1;
                        Audio.Play("ui_close", 0.9f, red ? 0.45f : 0.6f);
                        Haptics.Buzz(red ? Haptics.Kind.Escape : Haptics.Kind.Miss);
                        Game.I.world.Shake(red ? 0.3f : 0.15f);
                        Shake(red ? 34f : 20f);
                        StartCoroutine(Pulse(fish, red ? 0.88f : 0.94f));
                        if (lost >= line)
                        {
                            UpdateStrikes(hits, need, lost, line);
                            yield return new WaitForSeconds(0.25f);
                            Close();
                            result(false);
                            yield break;
                        }
                    }
                    Regen(pos, green, redW, reds);                  // every press redraws the zones
                    UpdateStrikes(hits, need, lost, line);
                }
                yield return null;
            }
            Close();
            result(true);
        }

        void Close()
        {
            if (panelRt != null) panelRt.anchoredPosition = panelBase;
            root.gameObject.SetActive(false);
        }

        void Shake(float px) => shakeAmt = Mathf.Max(shakeAmt, px);

        /// <summary>-1 none, 0 green, 1 red.</summary>
        int ZoneAt(float pos)
        {
            for (int i = 0; i < bands.Count; i++)
                if (Mathf.Abs(bands[i].anchoredPosition.x / Scale - pos) <= bands[i].sizeDelta.x / Scale / 2 + 1f) return (int)kinds[i];
            return -1;
        }

        /// <summary>New green band (away from the cursor) and red bands that don't touch it.</summary>
        void Regen(float cursorPos, float green, float redW, int reds)
        {
            foreach (var b in bands) Destroy(b.gameObject);
            bands.Clear();
            kinds.Clear();
            float margin = 6f;
            float gx = 0;
            for (int tries = 0; tries < 30; tries++)
            {
                gx = Random.Range(margin + green / 2, ArtW - margin - green / 2);
                if (Mathf.Abs(gx - cursorPos) > 18f) break;
            }
            Add(gx, green, Zone.Green);
            for (int r = 0; r < reds; r++)
            {
                for (int tries = 0; tries < 30; tries++)
                {
                    float rx = Random.Range(margin + redW / 2, ArtW - margin - redW / 2);
                    bool ok = Mathf.Abs(rx - cursorPos) > 6f;
                    foreach (var b in bands) if (Mathf.Abs(b.anchoredPosition.x / Scale - rx) < (b.sizeDelta.x / Scale + redW) / 2 + 3f) ok = false;
                    if (ok) { Add(rx, redW, Zone.Red); break; }
                }
            }
            cursor.SetAsLastSibling();
        }

        void Add(float x, float w, Zone z)
        {
            var c = z == Zone.Green ? new Color(0.35f, 0.85f, 0.42f) : new Color(1f, 0.16f, 0.14f);
            var band = UI.Solid(fish, c, false, z + " band").rectTransform;
            band.Place(new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x * Scale, 0), new Vector2(w * Scale, 40 * Scale));
            bands.Add(band);
            kinds.Add(z);
        }

        void UpdateStrikes(int hits, int need, int lost, int line)
        {
            string s = "";
            for (int i = 0; i < line; i++) s += i < line - lost ? "I " : "<color=#cc3333>X</color> ";
            strikesText.text = $"LINE {s}  {hits}/{need}";
        }

        IEnumerator Pulse(RectTransform t, float s)
        {
            t.localScale = Vector3.one * s;
            for (float a = 0; a < 1; a += Time.deltaTime * 8)
            {
                t.localScale = Vector3.one * Mathf.Lerp(s, 1, a);
                yield return null;
            }
            t.localScale = Vector3.one;
        }
    }
}
