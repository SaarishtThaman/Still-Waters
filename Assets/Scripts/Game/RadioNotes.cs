using System.Collections.Generic;
using UnityEngine;

namespace SW
{
    /// <summary>
    /// While a radio station is playing a song, faint pixel music notes drift up off the boat.
    /// The notes are drawn in code (two tiny shapes), so there's no new art.
    /// </summary>
    public class RadioNotes : MonoBehaviour
    {
        class Note { public SpriteRenderer sr; public Vector3 origin; public float age, life, sway, phase; }

        static Sprite[] shapes;
        readonly List<Note> notes = new List<Note>();
        SpriteRenderer boat;
        float next;
        static readonly Color Tint = new Color(1f, 0.95f, 0.82f);

        static readonly string[][] Shapes =
        {
            new[] { "..##.", "..#.#", "..#..", "..#..", "###..", "###..", ".#..." },   // eighth note
            new[] { "..#", "..#", "..#", "..#", "###", "###", ".#." },                  // quarter note
        };

        public static RadioNotes Attach(SpriteRenderer boatSr)
        {
            var n = boatSr.gameObject.AddComponent<RadioNotes>();
            n.boat = boatSr;
            return n;
        }

        static Sprite Make(string[] rows)
        {
            int h = rows.Length, w = rows[0].Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, h - 1 - y, rows[y][x] == '#' ? Color.white : Color.clear);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0), 16f);
        }

        void Update()
        {
            if (shapes == null) shapes = new[] { Make(Shapes[0]), Make(Shapes[1]) };
            float dt = Time.deltaTime;
            bool playing = Radio.I != null && Radio.I.Playing && Game.I != null && !Game.I.Blocked;

            next -= dt;
            if (playing && next <= 0)
            {
                next = Random.Range(1.4f, 2.6f);
                Spawn();
            }

            for (int i = notes.Count - 1; i >= 0; i--)
            {
                var n = notes[i];
                n.age += dt;
                float t = n.age / n.life;
                if (t >= 1f) { Destroy(n.sr.gameObject); notes.RemoveAt(i); continue; }
                n.sr.transform.position = n.origin + new Vector3(Mathf.Sin(n.phase + n.age * 1.6f) * n.sway, t * 0.9f, 0);
                // fade in, hold, fade out; faint the whole time
                float a = Mathf.Min(t / 0.15f, 1f, (1f - t) / 0.4f) * 0.55f;
                n.sr.color = new Color(Tint.r, Tint.g, Tint.b, a);
            }
        }

        void Spawn()
        {
            var go = new GameObject("note");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = shapes[Random.Range(0, shapes.Length)];
            sr.sortingLayerID = boat.sortingLayerID;
            sr.sortingOrder = boat.sortingOrder + 2;
            sr.color = Color.clear;
            var origin = boat.transform.position + new Vector3(Random.Range(-0.5f, 0.3f), 0.75f, 0);
            go.transform.position = origin;
            notes.Add(new Note { sr = sr, origin = origin, life = Random.Range(2.6f, 3.4f), sway = Random.Range(0.06f, 0.14f), phase = Random.value * 6.28f });
        }

        void OnDisable()
        {
            foreach (var n in notes) if (n.sr != null) Destroy(n.sr.gameObject);
            notes.Clear();
        }
    }
}
