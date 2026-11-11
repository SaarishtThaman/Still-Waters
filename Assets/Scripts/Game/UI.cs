using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SW
{
    /// <summary>uGUI built from code, in the jam's look: grey panels with a white border and the pixel font.</summary>
    public static class UI
    {
        /// <summary>The pixel font draws small for its size, so everything gets a boost.</summary>
        public const float TextScale = 1.15f;

        public static readonly Color PanelCol = new Color(0.30f, 0.30f, 0.31f, 0.93f);
        public static readonly Color ButtonCol = new Color(0.19f, 0.19f, 0.2f, 0.95f);
        public static readonly Color TextCol = new Color(0.97f, 0.96f, 0.93f);
        public static readonly Color Dim = new Color(0.68f, 0.68f, 0.7f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.25f);
        public static readonly Color OtherRed = new Color(0.86f, 0.12f, 0.14f);
        public static readonly Color DanCol = new Color(1f, 0.74f, 0.38f);
        public static readonly Color Green = new Color(0.45f, 0.82f, 0.5f);

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float inset = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static RectTransform Anchor(this RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Solid(Transform parent, Color c, bool raycast = false, string name = "solid")
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Sprite(Transform parent, Sprite s, Color? c = null, bool preserve = true)
        {
            var rt = Rect(s != null ? s.name : "sprite", parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = s;
            img.color = c ?? Color.white;
            img.preserveAspect = preserve;
            img.raycastTarget = false;
            return img;
        }

        public static Text Txt(Transform parent, string text, int size, Color? color = null, TextAnchor align = TextAnchor.MiddleCenter, bool shadow = true)
        {
            var rt = Rect("text", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Art.Font;
            t.text = text;
            t.fontSize = Mathf.RoundToInt(size * TextScale);
            t.color = color ?? TextCol;
            t.alignment = align;
            t.raycastTarget = false;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.25f;
            if (shadow)
            {
                var sh = rt.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0, 0, 0, 0.6f);
                sh.effectDistance = new Vector2(3, -3);
            }
            return t;
        }

        /// <summary>White frame drawn as four edges (so translucent panels stay clean).</summary>
        public static void Border(Transform parent, float w, Color? c = null)
        {
            var col = c ?? Color.white;
            Solid(parent, col, false, "edgeT").rectTransform.Edge(0, 1, 1, 1, w, true);
            Solid(parent, col, false, "edgeB").rectTransform.Edge(0, 0, 1, 0, w, true);
            Solid(parent, col, false, "edgeL").rectTransform.Edge(0, 0, 0, 1, w, false);
            Solid(parent, col, false, "edgeR").rectTransform.Edge(1, 0, 1, 1, w, false);
        }

        static void Edge(this RectTransform rt, float x0, float y0, float x1, float y1, float w, bool horizontal)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.pivot = new Vector2(x0, y0);
            rt.sizeDelta = horizontal ? new Vector2(0, w) : new Vector2(w, 0);
            rt.anchoredPosition = Vector2.zero;
            if (horizontal) rt.pivot = new Vector2(0.5f, y0);
            else rt.pivot = new Vector2(x0, 0.5f);
        }

        public static Image Panel(Transform parent, Color? c = null, float border = 6)
        {
            var img = Solid(parent, c ?? PanelCol, true, "Panel");
            if (border > 0) Border(img.transform, border);
            return img;
        }

        public static Button Btn(Transform parent, string label, Action onClick, int fontSize = 38, Color? bg = null, bool border = false)
        {
            var img = Solid(parent, bg ?? ButtonCol, true, "Button " + label);
            if (border) Border(img.transform, 4, new Color(1, 1, 1, 0.8f));
            var b = img.gameObject.AddComponent<Button>();
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
            cb.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
            cb.fadeDuration = 0.06f;
            cb.colorMultiplier = 1.5f;
            b.colors = cb;
            var nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            b.onClick.AddListener(() =>
            {
                Ctl.Consume();
                Audio.Play("ui_open", 0.6f, 1.25f);
                onClick?.Invoke();
            });
            var t = Txt(img.transform, label, fontSize);
            t.rectTransform.Stretch(8);
            t.name = "Label";
            return b;
        }

        public static void SetLabel(this Button b, string s)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = s;
        }

        public static Image Dimmer(Transform parent, float alpha = 0.6f)
        {
            var img = Solid(parent, new Color(0, 0, 0, alpha), true, "Dimmer");
            img.rectTransform.Stretch();
            return img;
        }

        /// <summary>A button that reports press and release (touch movement).</summary>
        public static Image HoldButton(Transform parent, string label, Action<bool> onHold, int fontSize = 56)
        {
            var img = Solid(parent, new Color(0, 0, 0, 0.35f), true, "Hold " + label);
            Border(img.transform, 4, new Color(1, 1, 1, 0.5f));
            var t = Txt(img.transform, label, fontSize, new Color(1, 1, 1, 0.85f));
            t.rectTransform.Stretch();
            var h = img.gameObject.AddComponent<HoldHandler>();
            h.onHold = onHold;
            return img;
        }

        public static string Clock(float hour)
        {
            int h = Mathf.FloorToInt(hour);
            int m = Mathf.FloorToInt((hour - h) * 60 / 10) * 10;
            string ampm = h >= 12 ? "PM" : "AM";
            int h12 = h % 12 == 0 ? 12 : h % 12;
            return $"{h12}:{m:00} {ampm}";
        }
    }

    public class HoldHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action<bool> onHold;
        bool down;
        public void OnPointerDown(PointerEventData e) { down = true; Ctl.usingTouch = true; onHold?.Invoke(true); }
        public void OnPointerUp(PointerEventData e) { if (down) { down = false; onHold?.Invoke(false); } }
        public void OnPointerExit(PointerEventData e) { if (down) { down = false; onHold?.Invoke(false); } }
        void OnDisable() { if (down) { down = false; onHold?.Invoke(false); } }
    }

    /// <summary>Raises a callback on pointer down (not click), for instant touch actions.</summary>
    public class PressHandler : MonoBehaviour, IPointerDownHandler
    {
        public Action onPress;
        public void OnPointerDown(PointerEventData e) { Ctl.usingTouch = true; onPress?.Invoke(); }
    }
}
