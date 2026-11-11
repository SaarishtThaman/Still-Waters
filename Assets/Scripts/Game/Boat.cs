using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SW
{
    /// <summary>
    /// The boat, on real 2D physics like the jam: it floats on a buoyancy effector (World), rocks,
    /// leans into acceleration, and the hook hangs off the rod on a spring joint, sinks with
    /// water drag and gets jerked about by bites.
    /// </summary>
    public class Boat : MonoBehaviour
    {
        public enum State { Free, Fishing, Locked }
        public State state = State.Locked;

        World world;
        SpriteRenderer sr, hookSr, exclaim;
        Rigidbody2D rb, hookRb;
        BoxCollider2D hull;
        SpringJoint2D joint;
        LineRenderer line;
        float facing = 1, motorVol, vx, waveT, move;
        bool lineOn, reeling;
        float prevHookY;
        public bool hookBiting;
        public string phase = "";  // for HUD prompts: "", "wait", "bite"

        public const int HookLayer = 9;
        static readonly Vector2 RodTip = new Vector2(2.5f / 16f, 10.5f / 16f);
        public const float RestY = -1.62f;

        class Bit { public SpriteRenderer sr; public Vector2 v; public float life; }
        readonly List<Bit> bits = new List<Bit>();

        public float x => rb != null ? rb.position.x : transform.position.x;

        public static Boat Create(World w)
        {
            var sr = w.AddBoat(Art.Get("finalboat"));
            var b = sr.gameObject.AddComponent<Boat>();
            b.world = w;
            b.sr = sr;
            b.Build();
            RadioNotes.Attach(sr);
            return b;
        }

        void Build()
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.angularDamping = 0.6f;
            rb.linearDamping = 0.1f;
            rb.useAutoMass = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            hull = gameObject.AddComponent<BoxCollider2D>();
            hull.size = new Vector2(46f / 16f, 13f / 16f);
            hull.offset = new Vector2(-2f / 16f, -3.5f / 16f);
            hull.density = 1f;

            var hg = new GameObject("Hook");
            hg.layer = HookLayer;
            hookSr = hg.AddComponent<SpriteRenderer>();
            hookSr.sprite = Art.Get("hook");
            hookSr.sortingOrder = 6;
            hookRb = hg.AddComponent<Rigidbody2D>();
            hookRb.gravityScale = 3;            // the jam's hook: gravity 3, drag 4.09 in water
            hookRb.mass = 0.25f;
            hookRb.interpolation = RigidbodyInterpolation2D.Interpolate;
            hookRb.angularDamping = 1;
            hookRb.freezeRotation = true;
            var hc = hg.AddComponent<CircleCollider2D>();
            hc.radius = 0.08f;
            Physics2D.IgnoreCollision(hc, hull);
            hg.SetActive(false);

            joint = gameObject.AddComponent<SpringJoint2D>();
            joint.enableCollision = false;
            joint.autoConfigureConnectedAnchor = false;
            joint.autoConfigureDistance = false;
            joint.connectedAnchor = Vector2.zero;
            joint.frequency = 3f;
            joint.dampingRatio = 1f;
            joint.enabled = false;

            exclaim = new GameObject("Exclaim").AddComponent<SpriteRenderer>();
            exclaim.sprite = Art.Get("exclamation");
            exclaim.sortingOrder = 30;
            exclaim.enabled = false;
            line = new GameObject("Line").AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startWidth = line.endWidth = 0.04f;
            line.positionCount = 16;
            line.sortingOrder = 6;
            line.startColor = new Color(1, 1, 1, 0.9f);
            line.endColor = new Color(0.85f, 0.92f, 1f, 0.6f);
            line.numCapVertices = 0;
            line.enabled = false;
        }

        public void Place(float px)
        {
            px = Mathf.Clamp(px, world.MinBoatX, world.MaxBoatX);
            rb.position = new Vector2(px, RestY);
            rb.rotation = 0;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;
            transform.position = new Vector3(px, RestY, 0);
            transform.rotation = Quaternion.identity;
            vx = 0;
            world.SnapCamera(px);
        }

        Vector3 Tip => transform.TransformPoint(new Vector3(RodTip.x * facing, RodTip.y, 0));

        void Update()
        {
            var g = Game.I;
            if (g == null) return;
            move = 0;
            if (state == State.Free && !g.Blocked)
            {
                move = Ctl.Move;
                if (move != 0) facing = move;
                if (Ctl.Tent && g.NearTent) g.OpenTent();
                else if (Ctl.Action) TryCast();
            }
            float speedFrac = Mathf.Abs(vx) / Mathf.Max(0.1f, g.save.Speed);
            motorVol = Mathf.MoveTowards(motorVol, move != 0 ? 0.35f : speedFrac * 0.2f, Time.deltaTime * 1.5f);
            Audio.Bed("motorboat", motorVol);
            sr.flipX = facing < 0;
            hull.offset = new Vector2(-2f / 16f * facing, -3.5f / 16f);
            joint.anchor = new Vector2(RodTip.x * facing, RodTip.y);
            world.followX = transform.position.x;
            exclaim.transform.position = transform.position + new Vector3(0.1f * facing, 1.35f + Mathf.Abs(Mathf.Sin(Time.time * 10)) * 0.08f, 0);
            UpdateLine();
            UpdateBits();
        }

        void FixedUpdate()
        {
            var g = Game.I;
            if (g == null) return;
            float dt = Time.fixedDeltaTime;

            // ease into and out of motion; the hull leans back when accelerating
            float target = move * g.save.Speed;
            float accel = g.save.Speed * (move != 0 ? 2.2f : 1.6f);
            float before = vx;
            vx = Mathf.MoveTowards(vx, target, accel * dt);
            float p = rb.position.x;
            if ((p <= world.MinBoatX && vx < 0) || (p >= world.MaxBoatX && vx > 0)) vx = 0;
            rb.linearVelocityX = vx;
            if (p < world.MinBoatX - 0.05f || p > world.MaxBoatX + 0.05f)
                rb.position = new Vector2(Mathf.Clamp(p, world.MinBoatX, world.MaxBoatX), rb.position.y);
            rb.AddTorque((vx - before) / dt * -0.12f * rb.mass);

            // the lake's swell: a gentle heave and roll (gone on the still last day)
            waveT += dt;
            float swell = 1f - world.calm * 0.9f;
            rb.AddForceAtPosition(Vector2.up * Mathf.Sin(waveT * 1.4f) * 0.9f * rb.mass * swell, rb.worldCenterOfMass + (Vector2)transform.right * 1.1f);
            rb.AddForce(Vector2.up * Mathf.Sin(waveT * 0.9f + 1f) * 0.5f * rb.mass * swell);
            // keep it upright: a soft righting torque on top of buoyancy
            rb.AddTorque(-Mathf.DeltaAngle(0, rb.rotation) * 0.08f * rb.mass - rb.angularVelocity * 0.01f * rb.mass);

            // hook: water drag below the surface, splash when it goes in
            if (hookRb.gameObject.activeSelf && !reeling)
            {
                bool under = hookRb.position.y < GameData.WaterY;
                hookRb.linearDamping = under ? 4.09f : 0.15f;
                if (prevHookY >= GameData.WaterY && under && hookRb.linearVelocity.y < -0.5f) { Splash(new Vector3(hookRb.position.x, GameData.WaterY, 0), 7); Haptics.Buzz(Haptics.Kind.Tick); }
                prevHookY = hookRb.position.y;
            }
        }

        void TryCast()
        {
            var g = Game.I;
            var s = g.save;
            if (s.scared) { g.hud.Think("I need to leave. I should get back to the tent."); return; }
            if (g.world.hour >= 20f) { g.hud.Think(Pick(GameData.TooDark)); return; }
            if (s.box.Count >= s.BoxSize) { g.hud.Think(Pick(GameData.BoxFull)); return; }
            if (g.BlackoutDue) { g.StartCoroutine(g.Blackout()); return; }
            StartCoroutine(Fish());
        }

        static string Pick(string[] a) => a[Random.Range(0, a.Length)];

        IEnumerator Fish()
        {
            var g = Game.I;
            var s = g.save;
            state = State.Fishing;
            s.castsToday++;
            s.castsSinceStory++;

            // cast: fling the hook from the rod tip and pay the line out
            Audio.Play("fishing_string_throw", 0.9f);
            phase = "wait";
            reeling = false;
            float reach = s.CastDist;
            var tip = Tip;
            hookRb.gameObject.SetActive(true);
            hookRb.simulated = true;
            hookRb.bodyType = RigidbodyType2D.Dynamic;
            hookRb.position = tip;
            hookRb.transform.position = tip;
            float k = Mathf.Sqrt(reach / 1.9f);
            hookRb.linearVelocity = rb.linearVelocity + new Vector2(facing * 3.4f * k, 2.4f * k);
            prevHookY = tip.y;
            joint.connectedBody = hookRb;
            joint.distance = 0.3f;
            joint.enabled = true;
            line.enabled = lineOn = true;
            for (float t = 0; t < 0.7f; t += Time.deltaTime)
            {
                joint.distance = Mathf.Lerp(0.3f, reach, t / 0.7f);
                yield return null;
            }
            joint.distance = reach;

            // wait for a bite (you can reel in early)
            float wait = s.BiteMean * Random.Range(0.75f, 1.25f); // jam: mean ±3 s
            float waited = 0;
            bool cancelled = false;
            while (waited < wait)
            {
                waited += Time.deltaTime;
                if (!g.Blocked && waited > 0.3f && Ctl.Action) { cancelled = true; break; }
                yield return null;
            }

            string caught = null;
            if (!cancelled)
            {
                // bite: the fish yanks the hook side to side (the jam's FishBitCoroutine)
                hookBiting = true;
                phase = "bite";
                exclaim.enabled = true;
                Audio.Play("fish_bit_exclamation");
                Haptics.Buzz(Haptics.Kind.Bite);
                Splash(new Vector3(hookRb.position.x, GameData.WaterY, 0), 4);
                float window = g.settings.assist ? 2.6f : 1.6f;
                bool struck = false;
                float tug = reach / 1.25f, nextTug = 0;
                int dir = 1;
                for (float t = 0; t < window; t += Time.deltaTime)
                {
                    if (t >= nextTug)
                    {
                        hookRb.linearVelocity += new Vector2(dir * tug, -0.6f);
                        if (t > 0) Haptics.Buzz(Haptics.Kind.Tug);
                        dir = -dir;
                        nextTug += 0.25f;
                    }
                    if (!g.Blocked && Ctl.Action) { struck = true; Haptics.Buzz(Haptics.Kind.Strike); break; }
                    yield return null;
                }
                exclaim.enabled = false;
                hookBiting = false;
                phase = "";
                if (struck)
                {
                    string id = Loot.Pick(s, x, () => Random.value);
                    var item = GameData.Item(id);
                    bool landed = false;
                    g.hud.SetVisible(false);
                    yield return g.minigame.Run(item.Difficulty, g.settings.assist, ok => landed = ok);
                    g.hud.SetVisible(true);
                    if (landed) caught = id;
                    else { g.hud.Think(Pick(GameData.LostFish)); Haptics.Buzz(Haptics.Kind.Escape); }
                }
                else { g.hud.Think("The fish got away."); Haptics.Buzz(Haptics.Kind.Escape); }
            }

            // reel in at the jam's speeds (2.5 empty, 1.5 with a fish on)
            phase = "";
            reeling = true;
            joint.enabled = false;
            hookRb.linearVelocity = Vector2.zero;
            hookRb.simulated = false;              // reel by hand: move the hook straight to the rod tip
            Audio.Bed("reel_in", 0.6f);
            float speed = caught != null ? 1.5f : 2.5f;
            var ht = hookRb.transform;
            bool wasUnder = ht.position.y < GameData.WaterY;
            for (float t = 0; t < 4f && Vector2.Distance(ht.position, Tip) > 0.08f; t += Time.deltaTime)
            {
                speed += Time.deltaTime * 1.2f; // ease in so it doesn't snap
                var next = Vector2.MoveTowards(ht.position, Tip, speed * Time.deltaTime);
                ht.position = next;
                if (wasUnder && next.y >= GameData.WaterY) { Splash(new Vector3(next.x, GameData.WaterY, 0), caught != null ? 9 : 5); wasUnder = false; }
                yield return null;
            }
            Audio.Bed("reel_in", 0f);
            hookRb.gameObject.SetActive(false);
            line.enabled = lineOn = false;
            reeling = false;

            if (caught != null) yield return g.Caught(caught);
            state = State.Free;
            g.save.Save();
        }

        void UpdateLine()
        {
            if (!lineOn) return;
            var hp = hookRb.transform.position;
            bool under = hp.y < GameData.WaterY;
            hookSr.color = under ? new Color(0.75f, 0.88f, 1f, 0.8f) : Color.white;
            Vector3 a = Tip, b = hp;
            float dist = Vector3.Distance(a, b);
            float slack = joint.enabled ? Mathf.Max(0, joint.distance - dist) : 0;
            float sag = Mathf.Clamp(slack * 0.6f + dist * 0.04f, 0.02f, 0.7f);
            for (int i = 0; i < line.positionCount; i++)
            {
                float t = i / (float)(line.positionCount - 1);
                var p = Vector3.Lerp(a, b, t);
                p.y -= Mathf.Sin(t * Mathf.PI) * sag;
                if (under && p.y < GameData.WaterY && Vector3.Lerp(a, b, t).y > GameData.WaterY) p.y = Mathf.Max(p.y, GameData.WaterY - 0.02f);
                line.SetPosition(i, p);
            }
        }

        // ---------------------------------------------------------------- splash bits
        public void Splash(Vector3 at, int n)
        {
            for (int i = 0; i < n; i++)
            {
                Bit b = bits.Find(q => q.life <= 0);
                if (b == null)
                {
                    b = new Bit { sr = new GameObject("Splash").AddComponent<SpriteRenderer>() };
                    b.sr.sprite = Art.White;
                    b.sr.sortingOrder = 7;
                    b.sr.transform.localScale = Vector3.one / 16f; // one art pixel
                    bits.Add(b);
                }
                b.sr.enabled = true;
                b.sr.transform.position = at;
                b.v = new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(1.5f, 3.2f));
                b.life = Random.Range(0.35f, 0.6f);
            }
        }

        void UpdateBits()
        {
            foreach (var b in bits)
            {
                if (b.life <= 0) continue;
                b.life -= Time.deltaTime;
                b.v.y -= 12f * Time.deltaTime;
                b.sr.transform.position += (Vector3)(b.v * Time.deltaTime);
                b.sr.color = new Color(0.9f, 0.96f, 1f, Mathf.Clamp01(b.life * 3));
                if (b.life <= 0 || b.sr.transform.position.y < GameData.WaterY - 0.1f) { b.life = 0; b.sr.enabled = false; }
            }
        }
    }
}
