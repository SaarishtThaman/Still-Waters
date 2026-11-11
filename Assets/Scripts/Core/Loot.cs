using System;
using System.Collections.Generic;

namespace SW
{
    /// <summary>
    /// What's on the hook. Plain C# so it can be simulated outside Unity.
    /// Fixes from the jam version: fish are properly weighted (the jam's Union() dropped the
    /// extra fish copies, so junk outnumbered fish), story items come up on a schedule with a
    /// guaranteed cast instead of pure chance, and evidence never repeats.
    /// </summary>
    public static class Loot
    {
        public const float StoryChance = 0.35f;

        public static int ZoneAt(float x) => x >= GameData.OpenEnd ? 2 : x >= GameData.ShallowsEnd ? 1 : 0;

        /// <summary>The story item due on this cast, if any. cast is 1-based (casts today).</summary>
        public static string StoryFor(SaveData s, int cast, Func<double> rnd)
        {
            foreach (var sc in s.Today.Story)
            {
                if (s.storyCaught.Contains(sc.Item)) continue;
                // one story beat at a time, in order: the next one waits a few casts after the last
                int since = s.castsSinceStory;
                int min = sc.MinCast, max = sc.MaxCast;
                // body parts are the real progression: give them room after the last story catch
                var def = GameData.Item(sc.Item);
                int gap = def != null && def.Kind == Kind.Evidence ? 3 : 2;
                bool late = s.hour >= 17.5f; // short days: never let the light run out on a story catch
                if (cast >= max || late || (cast >= min && since >= gap && rnd() < StoryChance)) return sc.Item;
                return null;
            }
            return null;
        }

        public static string Pick(SaveData s, float boatX, Func<double> rnd)
        {
            int cast = s.castsToday;
            var story = StoryFor(s, cast, rnd);
            if (story != null) return story;

            int zone = ZoneAt(boatX);
            int rod = s.Level(1), bait = s.Level(2);
            var pool = new List<ItemDef>();
            var weights = new List<double>();
            bool junk = rnd() < GameData.JunkChance[bait];
            foreach (var it in GameData.Items)
            {
                if (junk ? it.Kind != Kind.Junk : it.Kind != Kind.Fish) continue;
                if (it.Zone > zone || it.RodReq > rod || it.BaitReq > bait) continue;
                double w = it.Weight;
                if (it.Kind == Kind.Fish && s.TimesCaught(it.Id) == 0) w *= 1.6; // new species turn up a bit more
                if (it.Kind == Kind.Fish && it.Zone == zone && zone > 0) w *= 1.5; // reward going further out
                pool.Add(it);
                weights.Add(w);
            }
            if (pool.Count == 0) return "can";
            double total = 0;
            foreach (var w in weights) total += w;
            double r = rnd() * total;
            for (int i = 0; i < pool.Count; i++)
            {
                r -= weights[i];
                if (r <= 0) return pool[i].Id;
            }
            return pool[pool.Count - 1].Id;
        }
    }
}
