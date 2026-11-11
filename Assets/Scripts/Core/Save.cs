using System;
using System.Collections.Generic;
using UnityEngine;

namespace SW
{
    [Serializable]
    public class Settings
    {
        public float music = 0.6f;
        public float sfx = 0.85f;
        public int dayLength = 1;      // 0 short (2.5 min), 1 normal (3.5 min), 2 long (5 min). The jam was 2 min.
        public bool assist;            // slower, wider reel zones and a longer bite window
        public int textSpeed = 1;      // 0 slow, 1 normal, 2 fast
        public bool shake = true;
        public bool haptics = true;

        public static readonly float[] DayMinutes = { 2.5f, 3.5f, 5f };
        public static readonly string[] DayLengthNames = { "Short", "Normal", "Long" };
        public static readonly string[] TextSpeedNames = { "Slow", "Normal", "Fast" };
        public float DaySeconds => DayMinutes[Mathf.Clamp(dayLength, 0, 2)] * 60f;
        public float CharDelay => new[] { 0.05f, 0.032f, 0.014f }[Mathf.Clamp(textSpeed, 0, 2)];

        const string Key = "sw_settings_v1";

        public static Settings Load()
        {
            try
            {
                var s = PlayerPrefs.GetString(Key, "");
                if (!string.IsNullOrEmpty(s)) return JsonUtility.FromJson<Settings>(s) ?? new Settings();
            }
            catch (Exception e) { Debug.LogWarning("Settings load failed: " + e.Message); }
            return new Settings();
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }
    }

    [Serializable]
    public class Count { public string id; public int n; }

    [Serializable]
    public class SaveData
    {
        // run
        public bool inRun;
        public int day = 1;              // 1..7
        public float hour = 6f;          // 6:00 .. 21:00
        public float boatX = -1.2f;
        public int cash = 5;
        public int[] levels = new int[4];
        public List<string> box = new List<string>();
        public List<string> storyCaught = new List<string>();
        public int castsToday;
        public int castsSinceStory;
        public bool scared;              // after the hand: no more fishing today
        public bool blackoutDone;
        public bool morningDone;         // today's morning text has been shown

        // forever (survive new runs)
        public List<Count> caught = new List<Count>();
        public List<string> memories = new List<string>();
        public List<string> evidence = new List<string>();
        public List<string> endings = new List<string>();
        public bool warningSeen;

        const string Key = "sw_save_v1";

        public int Level(int i) => levels != null && i < levels.Length ? levels[i] : 0;
        public int BoxSize => (int)GameData.Box.Values[Level(3)];
        public float Speed => GameData.Engine.Values[Level(0)];
        public float CastDist => GameData.Rod.Values[Level(1)];
        public float BiteMean => GameData.Bait.Values[Level(2)];

        public int TimesCaught(string id)
        {
            var c = caught.Find(x => x.id == id);
            return c == null ? 0 : c.n;
        }

        public void AddCaught(string id)
        {
            var c = caught.Find(x => x.id == id);
            if (c == null) caught.Add(new Count { id = id, n = 1 });
            else c.n++;
        }

        public DayDef Today => GameData.Days[Mathf.Clamp(day - 1, 0, GameData.Days.Length - 1)];

        public bool StoryDoneToday()
        {
            foreach (var s in Today.Story) if (!storyCaught.Contains(s.Item)) return false;
            // a day with a blackout isn't over until the Other has had its afternoon
            return string.IsNullOrEmpty(Today.Blackout) || blackoutDone;
        }

        public static SaveData Load()
        {
            try
            {
                var s = PlayerPrefs.GetString(Key, "");
                if (!string.IsNullOrEmpty(s))
                {
                    var d = JsonUtility.FromJson<SaveData>(s);
                    if (d != null) { d.Fix(); return d; }
                }
            }
            catch (Exception e) { Debug.LogWarning("Save load failed: " + e.Message); }
            return new SaveData();
        }

        void Fix()
        {
            if (levels == null || levels.Length != 4) levels = new int[4];
            for (int i = 0; i < 4; i++) levels[i] = Mathf.Clamp(levels[i], 0, 2);
            box ??= new List<string>();
            box.RemoveAll(id => GameData.Item(id) == null);
            storyCaught ??= new List<string>();
            caught ??= new List<Count>();
            memories ??= new List<string>();
            evidence ??= new List<string>();
            endings ??= new List<string>();
            day = Mathf.Clamp(day, 1, GameData.Days.Length);
            hour = Mathf.Clamp(hour, 6f, 21f);
        }

        public void NewRun()
        {
            inRun = true;
            day = 1;
            hour = 6f;
            boatX = -1.2f;
            cash = 5;
            levels = new int[4];
            box.Clear();
            storyCaught.Clear();
            castsToday = 0;
            castsSinceStory = 0;
            scared = false;
            blackoutDone = false;
            morningDone = false;
        }

        public void NextDay()
        {
            day++;
            hour = 6f;
            boatX = -1.2f;
            castsToday = 0;
            castsSinceStory = 0;
            scared = false;
            blackoutDone = false;
            morningDone = false;
        }

        /// <summary>The day ended before its story was done: live it again.</summary>
        public void RepeatDay()
        {
            hour = 6f;
            boatX = -1.2f;
            castsToday = 0;
            castsSinceStory = 0;
            scared = false;
            morningDone = true;
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }
    }
}
