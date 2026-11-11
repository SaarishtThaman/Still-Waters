using System.Collections.Generic;
using UnityEngine;

namespace SW
{
    /// <summary>Sprites from Resources/Art (the jam's pixel art) plus a few generated textures.</summary>
    public static class Art
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static Sprite white, glow, vignette, dot;
        static Font font;

        public static Sprite Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (cache.TryGetValue(name, out var s)) return s;
            s = Resources.Load<Sprite>("Art/" + name);
            if (s == null) Debug.LogWarning("[SW] missing art " + name);
            cache[name] = s;
            return s;
        }

        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    font = Resources.Load<Font>("Fonts/pixel");
                    if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    Font.textureRebuilt += f => { if (f == font && f.material.mainTexture != null) f.material.mainTexture.filterMode = FilterMode.Point; };
                }
                return font;
            }
        }

        /// <summary>1x1 white sprite (1 unit = 1 pixel at PPU 1) for tinted rectangles.</summary>
        public static Sprite White
        {
            get
            {
                if (white == null)
                {
                    var t = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                    var px = new Color32[16];
                    for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
                    t.SetPixels32(px);
                    t.Apply();
                    white = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
                }
                return white;
            }
        }

        /// <summary>Soft radial glow, 1 unit across, white with alpha falloff.</summary>
        public static Sprite Glow
        {
            get
            {
                if (glow == null) glow = Radial(64, (d) => Mathf.Pow(Mathf.Clamp01(1 - d), 2.2f), 64);
                return glow;
            }
        }

        /// <summary>Dark edges, clear middle (for flashbacks and dread).</summary>
        public static Sprite Vignette
        {
            get
            {
                if (vignette == null) vignette = Radial(64, (d) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.45f, 1.05f, d)), 64, true);
                return vignette;
            }
        }

        /// <summary>A small round dot (the bobber).</summary>
        public static Sprite Dot
        {
            get
            {
                if (dot == null) dot = Radial(8, d => d < 0.85f ? 1 : 0, 16);
                return dot;
            }
        }

        static Sprite Radial(int size, System.Func<float, float> alpha, float ppu, bool black = false)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2 - 1, dy = (y + 0.5f) / size * 2 - 1;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    byte a = (byte)(Mathf.Clamp01(alpha(d)) * 255);
                    byte c = black ? (byte)0 : (byte)255;
                    px[y * size + x] = new Color32(c, c, c, a);
                }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
        }
    }
}
