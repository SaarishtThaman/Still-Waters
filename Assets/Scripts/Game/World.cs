using System.Collections.Generic;
using UnityEngine;

namespace SW
{
    /// <summary>
    /// The lake: the jam's parallax layers at their original positions and speeds, a live mirrored
    /// reflection, fish shadows under the surface, and full-screen tints for dusk, night,
    /// flashbacks and shocks.
    /// </summary>
    public class World : MonoBehaviour
    {
        public const int ReflectLayer = 8;
        public const float ViewHeight = 11.25f;   // 180 art pixels, as the jam was drawn
        public const float MinViewWidth = 20f;    // 320 art pixels
        public const float CamY = 0f;
        public const float TentBaseX = -3.89f;

        public Camera cam;
        Camera reflCam;
        RenderTexture rt;
        Material waterMat;
        Transform water;

        class Layer { public Transform t; public float baseX, factor; }
        readonly List<Layer> layers = new List<Layer>();
        Transform tentT;
        SpriteRenderer tentSr;
        public float camX = 0.12f;
        float camStartX = 0.12f;
        public float followX;

        SpriteRenderer dusk, night, dread, tint, flash, vignette, black;
        SpriteRenderer boatGlow, tentGlow;
        public Transform boatT;

        class Shadow { public SpriteRenderer sr; public float x0, amp, speed, phase, frame; }
        readonly List<Shadow> shadows = new List<Shadow>();
        Sprite[] fishFrames;

        public float hour = 6f;
        public float calm;        // day 7: the lake goes still
        public float dreadLevel;
        float flashA, flashDecay;
        Color flashCol;
        float tintTarget, tintA;
        Color tintCol = new Color(1f, 0.78f, 0.42f);
        float blackA, blackTarget, blackSpeed = 1.5f;
        float shake;

        public static readonly Color SkyTop = new Color(0.40f, 0.76f, 0.93f);
        static readonly Color NightCol = new Color(9f / 255, 8f / 255, 34f / 255);

        public float HalfWidth => cam.orthographicSize * cam.aspect;
        public float TentX => TentBaseX + (camX - camStartX) * 0.1f;
        public float MinBoatX => Mathf.Max(-6.5f, CamMin - HalfWidth + 1.8f);
        public float MaxBoatX => CamMax + HalfWidth - 2.2f;
        float CamMin => Mathf.Max(camStartX, (HalfWidth - 10.16f) / 0.9f + 0.3f);
        float CamMax => Mathf.Max(CamMin, (30.84f - HalfWidth) / 0.9f - 0.3f);

        public void Build()
        {
            cam = Camera.main;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyTop;
            cam.cullingMask = ~(1 << 5); // everything but UI
            cam.transform.position = new Vector3(camX, CamY, -10);

            // the jam scene, layer for layer (positions, sorting and parallax from DefaultLevel.unity)
            AddLayer("sky", 2.37f, 1.87f, 0.9f, -10);
            AddLayer("sky", 2.37f - 25f, 1.87f, 0.9f, -10, flip: true);
            AddLayer("sky", 2.37f + 25f, 1.87f, 0.9f, -10, flip: true);
            AddLayer("mountains", 0.10f, 1.85f, 0.85f, -9);
            AddLayer("mountains", 0.10f + 38.5f, 1.85f, 0.85f, -9, flip: true);
            AddLayer("bgrocks", 0.08f, 1.88f, 0.7f, -8);
            AddLayer("bgrocks", 0.08f + 25f, 1.88f, 0.7f, -8, flip: true);
            AddLayer("foreground", 10.34f, 1.88f, 0.1f, -7);
            var tent = AddLayer("tent", TentBaseX, -1.4f, 0.1f, -6);
            tentT = tent.t;
            tentSr = tentT.GetComponent<SpriteRenderer>();

            // water + reflection
            reflCam = new GameObject("ReflectionCam").AddComponent<Camera>();
            reflCam.orthographic = true;
            reflCam.cullingMask = 1 << ReflectLayer;
            reflCam.clearFlags = CameraClearFlags.SolidColor;
            reflCam.backgroundColor = SkyTop;
            reflCam.depth = cam.depth - 1;
            reflCam.allowMSAA = false;
            reflCam.allowHDR = false;

            var mesh = new Mesh { name = "Water" };
            mesh.vertices = new[] { new Vector3(-0.5f, -1, 0), new Vector3(0.5f, -1, 0), new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0) };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
            var wgo = new GameObject("Water", typeof(MeshFilter), typeof(MeshRenderer));
            wgo.GetComponent<MeshFilter>().sharedMesh = mesh;
            var mr = wgo.GetComponent<MeshRenderer>();
            var shader = Shader.Find("SW/Water");
            if (shader == null) Debug.LogError("[SW] water shader missing");
            waterMat = new Material(shader);
            mr.sharedMaterial = waterMat;
            mr.sortingOrder = 5;
            water = wgo.transform;

            // buoyancy, as in the jam's WaterReflection prefab (linear and angular drag 5). Density is
            // set so the hull sits a few pixels deep. The hook layer is left out so it sinks.
            var phys = new GameObject("WaterPhysics");
            phys.transform.position = new Vector3(10, GameData.WaterY, 0);
            var box = phys.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.usedByEffector = true;
            box.size = new Vector2(400, 12);
            box.offset = new Vector2(0, -6);
            var fx = phys.AddComponent<BuoyancyEffector2D>();
            fx.surfaceLevel = 0;
            fx.density = 4.3f;
            fx.linearDamping = 5;
            fx.angularDamping = 5;
            fx.useColliderMask = true;
            fx.colliderMask = ~(1 << Boat.HookLayer);

            // fish shadows under the surface (the jam's FishSpawner, slower and dimmer near the top)
            fishFrames = new[] { Art.Get("fishanims_0"), Art.Get("fishanims_1"), Art.Get("fishanims_2"), Art.Get("fishanims_3") };
            var rnd = new System.Random(7);
            for (int i = 0; i < 18; i++)
            {
                var sr = new GameObject("FishShadow").AddComponent<SpriteRenderer>();
                sr.sprite = fishFrames[0];
                sr.sortingOrder = 12;
                float x = -8.6f + (float)rnd.NextDouble() * 36f;
                float y = GameData.WaterY - 1.2f - (float)rnd.NextDouble() * 2.6f;
                float size = 0.3f + (float)rnd.NextDouble() * 0.35f;
                sr.transform.position = new Vector3(x, y, 0);
                sr.transform.localScale = Vector3.one * size * 2f;
                sr.color = new Color(0.05f, 0.1f, 0.18f, 0.10f + (float)rnd.NextDouble() * 0.08f);
                shadows.Add(new Shadow { sr = sr, x0 = x, amp = 2f + (float)rnd.NextDouble() * 1.5f, speed = 0.12f + (float)rnd.NextDouble() * 0.15f, phase = (float)rnd.NextDouble() * 6.28f });
            }

            // screen tints, all following the camera
            boatGlow = Overlay("BoatGlow", 21, new Color(1f, 0.75f, 0.4f, 0), Art.Glow);
            tentGlow = Overlay("TentGlow", 21, new Color(1f, 0.7f, 0.35f, 0), Art.Glow);
            dusk = Overlay("Dusk", 18, new Color(0.95f, 0.45f, 0.2f, 0));
            night = Overlay("Night", 20, new Color(NightCol.r, NightCol.g, NightCol.b, 0));
            dread = Overlay("Dread", 19, new Color(0.25f, 0.02f, 0.04f, 0));
            tint = Overlay("Tint", 22, new Color(tintCol.r, tintCol.g, tintCol.b, 0));
            vignette = Overlay("Vignette", 23, new Color(0, 0, 0, 0), Art.Vignette);
            flash = Overlay("Flash", 24, new Color(0.51f, 0.1f, 0.1f, 0));
            black = Overlay("Black", 40, new Color(0, 0, 0, 0));
        }

        Layer AddLayer(string art, float x, float y, float factor, int order, bool flip = false)
        {
            var sr = new GameObject(art).AddComponent<SpriteRenderer>();
            sr.sprite = Art.Get(art);
            sr.sortingOrder = order;
            sr.flipX = flip;
            sr.gameObject.layer = ReflectLayer;
            sr.transform.position = new Vector3(x, y, 0);
            var l = new Layer { t = sr.transform, baseX = x, factor = factor };
            layers.Add(l);
            return l;
        }

        SpriteRenderer Overlay(string name, int order, Color c, Sprite sprite = null)
        {
            var sr = new GameObject(name).AddComponent<SpriteRenderer>();
            sr.sprite = sprite ?? Art.White;
            sr.sortingOrder = order;
            sr.color = c;
            return sr;
        }

        public SpriteRenderer AddBoat(Sprite s)
        {
            var sr = new GameObject("Boat").AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = 0;
            sr.gameObject.layer = ReflectLayer;
            boatT = sr.transform;
            return sr;
        }

        public void SetTentHighlight(bool on)
        {
            var s = Art.Get(on ? "tent_selected" : "tent");
            if (tentSr.sprite != s) tentSr.sprite = s;
        }

        // ---------------------------------------------------------------- effects
        public void Flash(Color c, float a = 0.8f, float seconds = 1.6f)
        {
            flashCol = c;
            flashA = a;
            flashDecay = a / seconds;
        }

        public void Shake(float amount)
        {
            if (Game.I != null && !Game.I.settings.shake) return;
            shake = Mathf.Max(shake, amount);
        }

        /// <summary>Warm flashback tint (0..1).</summary>
        public void Memory(float a) { tintTarget = a; tintCol = new Color(1f, 0.78f, 0.42f); }
        public void SetBlack(float target, float speed = 1.5f) { blackTarget = target; blackSpeed = speed; }
        public void SnapBlack(float a) { blackA = blackTarget = a; }
        public bool BlackDone => Mathf.Approximately(blackA, blackTarget);

        public void SnapCamera(float x)
        {
            followX = x;
            camX = Mathf.Clamp(x, CamMin, CamMax);
        }

        // ---------------------------------------------------------------- frame
        void LateUpdate()
        {
            if (cam == null) return;
            float dt = Time.deltaTime;
            // keep 180 art pixels of height on wide screens; on narrow ones keep 320 of width
            float aspect = Mathf.Max(0.5f, cam.aspect);
            cam.orthographicSize = aspect >= MinViewWidth / ViewHeight ? ViewHeight / 2 : MinViewWidth / aspect / 2;
            float hw = HalfWidth;

            float target = Mathf.Clamp(followX + 1.5f, CamMin, CamMax);
            camX = Mathf.Lerp(camX, target, 1 - Mathf.Exp(-dt * 3.5f));
            float sx = 0, sy = 0;
            if (shake > 0)
            {
                sx = (Random.value - 0.5f) * shake;
                sy = (Random.value - 0.5f) * shake;
                shake = Mathf.MoveTowards(shake, 0, dt * 1.5f);
            }
            float cx = camX;
            cam.transform.position = new Vector3(cx + sx, CamY + sy, -10);

            float moved = cx - camStartX;
            foreach (var l in layers)
            {
                var p = l.t.position;
                p.x = l.baseX + moved * l.factor;
                l.t.position = p;
            }

            // reflection camera covers the band above the waterline as tall as the visible water
            float bottom = cam.transform.position.y - cam.orthographicSize;
            float reflH = Mathf.Max(1f, GameData.WaterY - bottom + 0.5f);
            reflCam.orthographicSize = reflH / 2;
            reflCam.aspect = (hw * 2) / reflH;
            reflCam.transform.position = new Vector3(cx, GameData.WaterY + reflH / 2, -10);
            int rh = Mathf.Max(16, Mathf.CeilToInt(reflH * 16 * 2));
            int rw = Mathf.Max(16, Mathf.CeilToInt(hw * 2 * 16 * 2));
            if (rt == null || rt.width != rw || rt.height != rh)
            {
                if (rt != null) { reflCam.targetTexture = null; rt.Release(); Destroy(rt); }
                rt = new RenderTexture(rw, rh, 16, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Reflection" };
                reflCam.targetTexture = rt;
                waterMat.SetTexture("_ReflTex", rt);
            }

            water.position = new Vector3(cx, GameData.WaterY, 0);
            water.localScale = new Vector3(hw * 2 + 2, reflH + 2, 1);
            waterMat.SetFloat("_WaterY", GameData.WaterY);
            waterMat.SetFloat("_ReflH", reflH);
            waterMat.SetFloat("_CamLeft", cx - hw);
            waterMat.SetFloat("_CamWidth", hw * 2);
            waterMat.SetFloat("_T", Time.time);
            waterMat.SetFloat("_Calm", calm);
            waterMat.SetFloat("_Dread", dreadLevel * 0.6f);

            // fish shadows
            foreach (var f in shadows)
            {
                f.phase += dt * f.speed;
                float x = f.x0 + Mathf.Sin(f.phase) * f.amp;
                float dir = Mathf.Cos(f.phase);
                var p = f.sr.transform.position;
                p.x = x;
                f.sr.transform.position = p;
                f.sr.flipX = dir > 0;
                f.frame += dt * 6;
                f.sr.sprite = fishFrames[(int)f.frame % 4];
            }

            // time of day: dusk from 17:00, night from 18:30 to the jam's 0.72 darkness
            float duskA = Mathf.Clamp01(Mathf.InverseLerp(16.5f, 18.5f, hour)) * (1 - Mathf.Clamp01(Mathf.InverseLerp(19.5f, 21f, hour)));
            float nightA = Mathf.Lerp(0, 0.72f, Mathf.Clamp01(Mathf.InverseLerp(18.2f, 20.6f, hour)));
            FitScreen(dusk, hw, duskA * 0.16f);
            FitScreen(night, hw, nightA);
            FitScreen(dread, hw, dreadLevel * 0.18f);
            tintA = Mathf.MoveTowards(tintA, tintTarget, dt * 0.8f);
            FitScreen(tint, hw, tintA * 0.32f, tintCol);
            FitScreen(vignette, hw, Mathf.Max(tintA * 0.8f, dreadLevel * 0.5f, nightA * 0.6f), Color.black, 1.25f);
            flashA = Mathf.MoveTowards(flashA, 0, dt * flashDecay);
            FitScreen(flash, hw, flashA, flashCol);
            blackA = Mathf.MoveTowards(blackA, blackTarget, dt * blackSpeed);
            FitScreen(black, hw, blackA);

            // lanterns at night
            float glowA = Mathf.Clamp01(Mathf.InverseLerp(18.8f, 20.5f, hour));
            if (boatT != null)
            {
                boatGlow.transform.position = boatT.position + new Vector3(0, 0.2f, 0);
                boatGlow.transform.localScale = Vector3.one * 6f;
                boatGlow.color = new Color(1f, 0.75f, 0.4f, glowA * 0.22f);
            }
            tentGlow.transform.position = tentT.position + new Vector3(0, 0.3f, 0);
            tentGlow.transform.localScale = Vector3.one * 5f;
            tentGlow.color = new Color(1f, 0.7f, 0.35f, glowA * 0.18f);
        }

        void FitScreen(SpriteRenderer sr, float hw, float a, Color? c = null, float scale = 1f)
        {
            var col = c ?? sr.color;
            col.a = a;
            sr.color = col;
            sr.enabled = a > 0.001f;
            if (!sr.enabled) return;
            var p = cam.transform.position;
            sr.transform.position = new Vector3(p.x, p.y, 0);
            var size = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3((hw * 2 + 1) / size.x * scale, (cam.orthographicSize * 2 + 1) / size.y * scale, 1);
        }
    }
}
