using UnityEngine;
using UnityEngine.EventSystems;

namespace SW
{
    /// <summary>
    /// One place for input: keyboard (A/D or arrows, Space/Enter, E, Esc, J/Tab), mouse clicks on
    /// the world, and the on-screen touch buttons. Everything is edge-triggered where it matters:
    /// the jam version read Space as "held", so the press that hooked a fish also fired the
    /// minigame a moment later.
    /// </summary>
    public static class Ctl
    {
        // set by the touch buttons
        public static bool touchLeft, touchRight;
        static bool touchAction, touchTent;
        public static bool usingTouch;
        public static bool suppressWorldClick; // a panel consumed this frame's click
        public static bool auto;               // headless playtest: text advances by itself

        static int lastFrame = -1;
        static bool actionDown, tentDown, pauseDown, journalDown, radioDown, anyDown;

        public static void PressAction() => touchAction = true;
        public static void PressTent() => touchTent = true;

        static void Poll()
        {
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;

            bool pointerDown = false, pointerOverUI = false;
            if (Input.touchCount > 0)
            {
                usingTouch = true;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.phase != TouchPhase.Began) continue;
                    pointerDown = true;
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId)) pointerOverUI = true;
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                pointerDown = true;
                pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            }
            if (Input.anyKeyDown && !Input.GetMouseButtonDown(0)) usingTouch = false;

            bool key = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                       || Input.GetKeyDown(KeyCode.JoystickButton0);
            // touch casts only through the on-screen button; a mouse click on the lake still works on desktop
            bool worldClick = pointerDown && !pointerOverUI && !suppressWorldClick && Input.touchCount == 0 && !Application.isMobilePlatform;
            actionDown = key || touchAction || worldClick;
            tentDown = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || touchTent;
            pauseDown = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.JoystickButton7);
            journalDown = Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Tab);
            radioDown = Input.GetKeyDown(KeyCode.R);
            anyDown = key || pointerDown || touchAction;
            touchAction = touchTent = false;
            suppressWorldClick = false;
        }

        public static float Move
        {
            get
            {
                float m = 0;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || touchLeft) m -= 1;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || touchRight) m += 1;
                float axis = Input.GetAxisRaw("Horizontal");
                if (m == 0 && Mathf.Abs(axis) > 0.4f) m = Mathf.Sign(axis);
                return m;
            }
        }

        public static bool Action { get { Poll(); return actionDown; } }
        public static bool Tent { get { Poll(); return tentDown; } }
        public static bool Pause { get { Poll(); return pauseDown; } }
        public static bool Journal { get { Poll(); return journalDown; } }
        public static bool RadioKey { get { Poll(); return radioDown; } }
        /// <summary>Any key, click or tap (for advancing text).</summary>
        public static bool Any { get { Poll(); return anyDown || (auto && Time.frameCount % 2 == 0); } }

        /// <summary>Swallow this frame's presses (after a panel used them).</summary>
        public static void Consume()
        {
            Poll();
            actionDown = tentDown = anyDown = journalDown = radioDown = false;
        }
    }
}
