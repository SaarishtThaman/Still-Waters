using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SW
{
    /// <summary>
    /// Typed dialogue. Wes speaks in plain white, Dan has a name tag, the Other is red and won't
    /// sit still, and "static" lines are stage directions. Full mode is the black story screen.
    /// </summary>
    public class Dialog : MonoBehaviour
    {
        RectTransform root, box, choiceRow;
        Image backdrop, boxImg;
        Text nameText, body, arrow;
        AudioSource talk;
        bool full;
        Speaker who;
        string shown = "";
        float glitchT;
        Vector2 bodyBase;
        public bool Busy { get; private set; }

        public static Dialog Create(Transform canvas)
        {
            var rt = UI.Rect("Dialog", canvas).Stretch();
            var d = rt.gameObject.AddComponent<Dialog>();
            d.root = rt;
            d.Build();
            return d;
        }

        void Build()
        {
            backdrop = UI.Solid(root, Color.black, true, "Backdrop");
            backdrop.rectTransform.Stretch();
            box = UI.Rect("Box", root);
            boxImg = UI.Panel(box, new Color(0.16f, 0.16f, 0.17f, 0.9f), 5);
            boxImg.rectTransform.Stretch();
            boxImg.raycastTarget = false;
            nameText = UI.Txt(box, "", 64, UI.DanCol, TextAnchor.LowerLeft);
            nameText.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 0), new Vector2(18, 10), new Vector2(600, 70));
            body = UI.Txt(box, "", 84, UI.TextCol, TextAnchor.UpperLeft);
            body.rectTransform.Stretch(40);
            body.lineSpacing = 1.0f;
            arrow = UI.Txt(box, ">>", 56, UI.Dim, TextAnchor.LowerRight);
            arrow.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-22, 14), new Vector2(120, 50));
            choiceRow = UI.Rect("Choices", root);
            var v = choiceRow.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 18;
            v.childControlHeight = v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            talk = gameObject.AddComponent<AudioSource>();
            talk.clip = Resources.Load<AudioClip>("Audio/talk");
            talk.loop = true;
            talk.playOnAwake = false;
            root.gameObject.SetActive(false);
        }

        void Layout()
        {
            if (full)
            {
                backdrop.color = Color.black;
                backdrop.enabled = true;
                box.anchorMin = new Vector2(0.1f, 0.25f);
                box.anchorMax = new Vector2(0.9f, 0.75f);
                box.offsetMin = box.offsetMax = Vector2.zero;
                boxImg.enabled = false;
                foreach (var t in boxImg.GetComponentsInChildren<Image>()) t.enabled = false;
                body.fontSize = 84;
                body.alignment = TextAnchor.MiddleCenter;
                choiceRow.anchorMin = new Vector2(0.25f, 0.08f);
                choiceRow.anchorMax = new Vector2(0.75f, 0.3f);
            }
            else
            {
                backdrop.color = new Color(0, 0, 0, 0.01f); // catches taps
                backdrop.enabled = true;
                box.anchorMin = new Vector2(0.05f, 0.04f);
                box.anchorMax = new Vector2(0.95f, 0.31f);
                box.offsetMin = box.offsetMax = Vector2.zero;
                boxImg.enabled = true;
                foreach (var t in boxImg.GetComponentsInChildren<Image>()) t.enabled = true;
                body.fontSize = 84;
                body.alignment = TextAnchor.UpperLeft;
                choiceRow.anchorMin = new Vector2(0.2f, 0.36f);
                choiceRow.anchorMax = new Vector2(0.8f, 0.72f);
            }
            choiceRow.offsetMin = choiceRow.offsetMax = Vector2.zero;
        }

        public IEnumerator Play(IList<Line> lines, bool fullScreen)
        {
            Busy = true;
            full = fullScreen;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            Layout();
            foreach (var l in lines)
                yield return Say(l);
            root.gameObject.SetActive(false);
            Busy = false;
        }

        IEnumerator Say(Line l)
        {
            who = l.Who;
            nameText.text = who == Speaker.Dan ? "DAN" : who == Speaker.Other ? "???" : "";
            nameText.color = who == Speaker.Other ? UI.OtherRed : UI.DanCol;
            body.color = who == Speaker.Other ? UI.OtherRed : who == Speaker.Static ? UI.Dim : UI.TextCol;
            body.fontStyle = who == Speaker.Static ? FontStyle.Italic : FontStyle.Normal;
            if (who == Speaker.Static && !full) body.alignment = TextAnchor.MiddleCenter;
            else if (!full) body.alignment = TextAnchor.UpperLeft;
            arrow.enabled = false;
            bodyBase = body.rectTransform.anchoredPosition;
            string text = l.Text;
            shown = "";
            body.text = "";
            yield return null;
            Ctl.Consume();
            if (who != Speaker.Static && talk.clip != null)
            {
                talk.pitch = who == Speaker.Dan ? 0.82f : who == Speaker.Other ? 0.55f : 1f;
                talk.volume = Audio.SfxVol * (who == Speaker.Other ? 0.8f : 0.55f);
                talk.time = UnityEngine.Random.Range(0f, 3f);
                talk.Play();
            }
            float delay = Game.I.settings.CharDelay * (who == Speaker.Other ? 1.4f : 1f);
            float t = 0;
            int i = 0;
            while (i < text.Length)
            {
                if (Ctl.Any) { i = text.Length; break; }
                t += Time.unscaledDeltaTime;
                while (t >= delay && i < text.Length)
                {
                    t -= delay;
                    i++;
                    if (i < text.Length && (text[i - 1] == '.' || text[i - 1] == ',' || text[i - 1] == '?')) t -= delay * 5;
                }
                shown = text.Substring(0, i);
                body.text = shown;
                yield return null;
            }
            shown = text;
            body.text = shown;
            talk.Stop();
            arrow.enabled = true;
            yield return null;
            while (!Ctl.Any) yield return null;
            Ctl.Consume();
            body.rectTransform.anchoredPosition = bodyBase;
        }

        /// <summary>Shows the last line with options; returns the chosen index via callback.</summary>
        public IEnumerator Choose(string[] options, bool[] enabled, Action<int> picked)
        {
            Busy = true;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            arrow.enabled = false;
            int choice = -1;
            var made = new List<GameObject>();
            for (int i = 0; i < options.Length; i++)
            {
                int k = i;
                var b = UI.Btn(choiceRow, options[i], () => choice = k, 48, null, true);
                b.interactable = enabled == null || enabled[i];
                var le = b.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 110;
                made.Add(b.gameObject);
            }
            while (choice < 0) yield return null;
            foreach (var g in made) Destroy(g);
            root.gameObject.SetActive(false);
            Busy = false;
            picked(choice);
        }

        static readonly string Glyphs = "#@%&*!?$^~";

        void Update()
        {
            if (!root.gameObject.activeSelf || who != Speaker.Other) return;
            glitchT -= Time.unscaledDeltaTime;
            if (glitchT <= 0)
            {
                glitchT = 0.05f;
                body.rectTransform.anchoredPosition = bodyBase + new Vector2(UnityEngine.Random.Range(-3, 4), UnityEngine.Random.Range(-2, 3));
                if (shown.Length > 3 && UnityEngine.Random.value < 0.25f)
                {
                    var c = shown.ToCharArray();
                    int n = UnityEngine.Random.Range(1, 3);
                    for (int k = 0; k < n; k++)
                    {
                        int p = UnityEngine.Random.Range(0, c.Length);
                        if (c[p] != ' ') c[p] = Glyphs[UnityEngine.Random.Range(0, Glyphs.Length)];
                    }
                    body.text = new string(c);
                }
                else body.text = shown;
            }
        }
    }
}
