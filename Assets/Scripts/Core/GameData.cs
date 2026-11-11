using System;
using System.Collections.Generic;

namespace SW
{
    public enum Kind { Fish, Junk, Memory, Evidence }
    public enum Speaker { Me, Other, Dan, Static }

    public struct Line
    {
        public Speaker Who;
        public string Text;
        public Line(Speaker who, string text) { Who = who; Text = text; }
    }

    public class ItemDef
    {
        public string Id, Name, Art, Desc;
        public Kind Kind;
        public int Price, Difficulty = 1;
        public int Zone;          // 0 shallows, 1 open water, 2 the deep: needs to be at least this far out
        public int RodReq, BaitReq;
        public float Weight = 10;
        public Line[] OnCatch;    // said after closing the catch card (evidence / memories)
        public Line[] Flashback;  // memories: played over a warm tint
        public bool GlitchName;   // name is shown scrambled
    }

    public class StoryCatch
    {
        public string Item;
        public int MinCast, MaxCast; // can come up from MinCast, guaranteed on MaxCast (casts counted per day)
        public StoryCatch(string item, int min, int max) { Item = item; MinCast = min; MaxCast = max; }
    }

    public class DayDef
    {
        public string Subtitle;
        public Line[] Morning;      // before the day starts (dark screen)
        public StoryCatch[] Story = new StoryCatch[0];
        public Line[] Night;        // after sleeping: monologue and Dan on the radio
        public bool LostTime;       // wake up late, out on the water
        public string Blackout;     // item that turns up after a blackout mid-day
        public int BlackoutAfter;   // casts after the day's story is done
        public bool Finale;         // Dan arrives after the story catch
    }

    public class Upgrade
    {
        public string Name, Blurb;
        public float[] Values;
        public int[] Costs; // cost to reach level i+1
    }

    public static class GameData
    {
        static Line M(string s) => new Line(Speaker.Me, s);
        static Line O(string s) => new Line(Speaker.Other, s);
        static Line D(string s) => new Line(Speaker.Dan, s);
        static Line R(string s) => new Line(Speaker.Static, s);

        public const float WaterY = -2.06f;
        public const float ShallowsEnd = 9.6f, OpenEnd = 19f;
        public static readonly string[] ZoneNames = { "The Shallows", "Open Water", "The Deep" };

        // ---------------------------------------------------------------- upgrades
        public static readonly Upgrade Engine = new Upgrade { Name = "Engine", Blurb = "Boat speed", Values = new[] { 2.2f, 3.2f, 4.4f }, Costs = new[] { 70, 160 } };
        public static readonly Upgrade Rod = new Upgrade { Name = "Fishing Rod", Blurb = "Cast further, hook bigger fish", Values = new[] { 1.9f, 2.6f, 3.4f }, Costs = new[] { 80, 180 } };
        public static readonly Upgrade Bait = new Upgrade { Name = "Bait", Blurb = "Faster bites, less junk", Values = new[] { 12f, 9f, 6f }, Costs = new[] { 60, 150 } };
        public static readonly Upgrade Box = new Upgrade { Name = "Fish Box", Blurb = "Carry more", Values = new[] { 4f, 6f, 9f }, Costs = new[] { 40, 120 } };
        public static readonly Upgrade[] Upgrades = { Engine, Rod, Bait, Box };
        public static readonly float[] JunkChance = { 0.33f, 0.22f, 0.12f };

        // ---------------------------------------------------------------- items
        public static readonly ItemDef[] Items =
        {
            // fish: shallows
            new ItemDef { Id = "bluegill", Name = "Blue Gill", Art = "bluegill", Kind = Kind.Fish, Price = 12, Difficulty = 1, Weight = 30,
                Desc = "It's small and stubborn. Dan used to catch these by the dozen and throw every one of them back." },
            new ItemDef { Id = "trout", Name = "Trout", Art = "trout", Kind = Kind.Fish, Price = 15, Difficulty = 1, Weight = 26,
                Desc = "It's a good, honest trout, and it put up a decent fight. That's dinner, if I can be bothered to cook it." },
            new ItemDef { Id = "barb", Name = "Barb", Art = "barb", Kind = Kind.Fish, Price = 12, Difficulty = 1, Weight = 24,
                Desc = "It's a quick little thing. A fish like this is better used as bait than eaten." },
            new ItemDef { Id = "boarfish", Name = "Boar Fish", Art = "boarfish", Kind = Kind.Fish, Price = 14, Difficulty = 1, Weight = 18,
                Desc = "It's flat and red, and it looks like it's frowning at me. I don't remember these living up here." },
            new ItemDef { Id = "catfish", Name = "Catfish", Art = "catfish", Kind = Kind.Fish, Price = 18, Difficulty = 2, Weight = 15,
                Desc = "It has long whiskers and a bad attitude. Catfish live in the mud at the bottom and eat whatever sinks." },
            new ItemDef { Id = "eel", Name = "Eel", Art = "eel", Kind = Kind.Fish, Price = 20, Difficulty = 2, Weight = 10,
                Desc = "It was slippery, and it twisted the whole way up. Dad used to say that eels feed on the drowned." },
            // fish: open water
            new ItemDef { Id = "snook", Name = "Snook", Art = "snook", Kind = Kind.Fish, Price = 22, Difficulty = 2, Zone = 1, RodReq = 1, Weight = 15,
                Desc = "It's sleek and silver. It hit the line hard and jumped twice before I landed it." },
            new ItemDef { Id = "reddrum", Name = "Red Drum", Art = "reddrum", Kind = Kind.Fish, Price = 26, Difficulty = 2, Zone = 1, BaitReq = 1, Weight = 12,
                Desc = "It has a black spot near its tail that looks like an eye that never closes." },
            new ItemDef { Id = "clownfish", Name = "Clown Fish", Art = "clownfish", Kind = Kind.Fish, Price = 20, Difficulty = 2, Zone = 1, Weight = 10,
                Desc = "It's an orange and white clownfish, in a mountain lake. I checked twice." },
            new ItemDef { Id = "angler", Name = "Angler", Art = "angler", Kind = Kind.Fish, Price = 32, Difficulty = 2, Zone = 1, RodReq = 1, BaitReq = 1, Weight = 6,
                Desc = "Its lure was still glowing when I pulled it out of the water. It has so many teeth." },
            // fish: the deep
            new ItemDef { Id = "squid", Name = "Squid", Art = "squid", Kind = Kind.Fish, Price = 36, Difficulty = 3, Zone = 2, RodReq = 1, Weight = 8,
                Desc = "A squid, in fresh water. I put it in the box and decided not to think about it." },
            new ItemDef { Id = "blobfish", Name = "Blob Fish", Art = "blobfish", Kind = Kind.Fish, Price = 48, Difficulty = 3, Zone = 2, RodReq = 2, BaitReq = 1, Weight = 5,
                Desc = "What an ugly looking fish. Something that lives this deep shouldn't look so sad." },

            // junk
            new ItemDef { Id = "can", Name = "Soda Can", Art = "discardedcan", Kind = Kind.Junk, Price = 2, Weight = 22, Desc = "A discarded soda can." },
            new ItemDef { Id = "bottle", Name = "Beer Bottle", Art = "beerbottle", Kind = Kind.Junk, Price = 2, Weight = 20, Desc = "It's a bottle of Dad's brand of beer. Of course it is." },
            new ItemDef { Id = "seaweed", Name = "Lake Weed", Art = "seaweed", Kind = Kind.Junk, Price = 6, Difficulty = 2, Weight = 14, Desc = "It's a clump of weed from the bottom of the lake. It smells like the lake in August." },
            new ItemDef { Id = "boot", Name = "Old Boot", Art = "oldboot", Kind = Kind.Junk, Price = 6, Weight = 12, Desc = "An expensive looking army boot. How did it end up here?" },
            new ItemDef { Id = "sunglasses", Name = "Broken Sunglasses", Art = "brokensunglasses", Kind = Kind.Junk, Price = 6, Weight = 10, Desc = "A nice pair of sunglasses. Unfortunately, they're broken." },
            new ItemDef { Id = "scarf", Name = "Torn Scarf", Art = "scarf", Kind = Kind.Junk, Price = 5, Weight = 10, Desc = "A decent looking scarf, torn in many places. Someone must have been cold out here once." },
            new ItemDef { Id = "umbrella", Name = "Broken Umbrella", Art = "brokenumbrella", Kind = Kind.Junk, Price = 8, Difficulty = 2, Weight = 8, Desc = "A simple umbrella with a snapped shaft." },

            // memories (story catches)
            new ItemDef { Id = "plank", Name = "Carved Plank", Art = "plank", Kind = Kind.Memory, Price = 0,
                Desc = "It's a damp board, cut square. Someone carved letters into it: W + D.",
                Flashback = new[] {
                    M("Dad built a dock here the summer I turned nine."),
                    M("Dan and I carved our initials into the end board while he slept it off in the truck."),
                    M("Dan's D came out backwards. He cried about it, so I fixed it with my pocketknife."),
                    M("We'd sit right there with our feet in the water until Dad yelled for us."),
                } },
            new ItemDef { Id = "bear", Name = "Captain", Art = "discardedbear", Kind = Kind.Memory, Price = 0, Difficulty = 2,
                Desc = "It's a soggy teddy bear with one eye. I know this bear.",
                Flashback = new[] {
                    M("That's Captain. Dan never went anywhere without him."),
                    M("One afternoon he dropped him off the end of the dock, and I went in after him in my clothes."),
                    M("Dad laughed at me the whole way back to the truck. Dan held my hand."),
                    M("I got Captain back that day. I know I did."),
                    M("So what is he doing at the bottom of the lake?"),
                } },
            new ItemDef { Id = "brush", Name = "Hair Brush", Art = "hairbrush", Kind = Kind.Memory, Price = 0,
                Desc = "It's a cheap hair brush with broken bristles. Mary had one just like it.",
                Flashback = new[] {
                    M("Mary brushed her hair a hundred strokes every night. I used to count along to annoy her."),
                    M("The night she left, she didn't take her brush. I remember thinking that was strange."),
                    M("I remember thinking that. And then I don't remember anything until morning."),
                } },
            new ItemDef { Id = "watch", Name = "Dad's Watch", Art = "pocketwatch", Kind = Kind.Memory, Price = 0, Difficulty = 2,
                Desc = "It's a pocket watch with cracked glass, stopped at ten past four. It was Dad's.",
                Flashback = new[] {
                    M("Dad wound this watch every morning, even out here."),
                    M("The last afternoon, he'd been drinking since lunch. Dan knocked the bait bucket over."),
                    M("Dad hit him so hard he went off the dock and into the water."),
                    M("I remember the sound it made. Then I remember it being dark, and my hands being cold."),
                    M("Dan told everyone Dad walked out on us. He told it so many times that I believed him."),
                } },
            new ItemDef { Id = "backpack", Name = "My Backpack", Art = "backpack", Kind = Kind.Memory, Price = 0, Difficulty = 2,
                Desc = "It's a kid's backpack, rotted through. There's a W written on the strap in marker.",
                Flashback = new[] {
                    M("Mom wrote my initial on everything I owned."),
                    M("We drove home without Dad that night. I don't remember the drive. Dan says I did it."),
                    M("When we got home, my backpack was gone. Dan said I must have left it at the lake."),
                    M("It was heavy when we got there. I never asked him what had been inside."),
                } },

            // evidence
            new ItemDef { Id = "hand", Name = "Rotting Hand", Art = "severedhand", Kind = Kind.Evidence, Price = 0, GlitchName = true,
                Desc = "(*#^&*#^*#^#&*^#*^#*(@&(@*@)()@()(@)(!&&!*(@)@)(&^$&@()@^!^*$()#)&@&@)(#)&#&$*($",
                OnCatch = new[] { M("What the... hell is this?"), M("Oh my god... why in God's name did Dan send me here?"), M("I need to leave.") } },
            new ItemDef { Id = "arm", Name = "Arm", Art = "arm", Kind = Kind.Evidence, Price = 0, Difficulty = 2,
                Desc = "A slender arm. It looks much fresher than the other parts I found.",
                OnCatch = new[] { M("It was the arm of a slender woman, and it looked more recent than the others."), M("Why am I doing this to myself?"), O("She scratched you. Do you remember?") } },
            new ItemDef { Id = "leg", Name = "Decaying Leg", Art = "leg", Kind = Kind.Evidence, Price = 0, Difficulty = 2,
                Desc = "It's a rotting leg, broken at the shin.",
                OnCatch = new[] { M("The stench was unbearable."), M("Why did I come back here?"), O("You know why. It's our place.") } },
            new ItemDef { Id = "necklace", Name = "Organ Necklace", Art = "earnecklace", Kind = Kind.Evidence, Price = 0,
                Desc = "It's a necklace strung with human organs. The knots are neat and patient.",
                OnCatch = new[] { M("I don't remember catching this."), O("It was a horrible necklace. But you saw the beauty behind the rot."), O("It took all night. You were very good.") } },
            new ItemDef { Id = "head", Name = "Rotting Head", Art = "malerottinghead", Kind = Kind.Evidence, Price = 0, Difficulty = 2,
                Desc = "The head of a man. The skin has mostly rotted away.",
                OnCatch = new[] { M("The head had decomposed beyond recognition."), M("I felt like throwing up. But I also felt happy."), O("That's Tom, from Mary's work. You never liked the way he laughed at her jokes.") } },
            new ItemDef { Id = "torso", Name = "Female Torso", Art = "wifetorso", Kind = Kind.Evidence, Price = 0,
                Desc = "...",
                OnCatch = new[] { M("It looks familiar..."), M("Is that... is that Mary?"), O("She didn't leave you. We made sure of it.") } },
        };

        public static readonly string[] MemoryOrder = { "plank", "bear", "brush", "watch", "backpack" };
        public static readonly string[] EvidenceOrder = { "hand", "arm", "leg", "necklace", "head", "torso" };

        static Dictionary<string, ItemDef> byId;
        public static ItemDef Item(string id)
        {
            if (byId == null)
            {
                byId = new Dictionary<string, ItemDef>();
                foreach (var i in Items) byId[i.Id] = i;
            }
            return id != null && byId.TryGetValue(id, out var d) ? d : null;
        }

        // ---------------------------------------------------------------- story
        public static readonly Line[] Intro =
        {
            M("Dan drew me a map on the back of a gas station receipt. It was two hours past the last town, then up the old logging road, and the rest of the way on foot."),
            M("I hadn't been up here since we were kids. Part of me thought the lake wasn't real."),
            M("It was real. It was still hidden between the mountains, and it was still as beautiful as I remembered."),
            M("I needed a few days of peace and quiet to come to terms with Mary leaving."),
            M("I'd lugged my old motorboat up with me to keep my hands busy."),
            M("I rested for a while, then pushed off and got started."),
        };

        public static readonly DayDef[] Days =
        {
            new DayDef // 1
            {
                Subtitle = "Arrival",
                Story = new[] { new StoryCatch("plank", 2, 5) },
                Night = new[] {
                    M("My first day back at the lake was wonderful."),
                    M("I understood why Dan never stopped talking about this place."),
                    M("It marked the beginning of my healing journey... or so I thought."),
                    R("The radio crackled."),
                    D("Wes? You there? Over."),
                    M("I'm here."),
                    D("So you found it, then."),
                    M("It's just like we left it. The dock is gone, though. Bits of it keep turning up on my line."),
                    D("Twenty-five years will do that. Get some sleep, okay? I'll call you tomorrow night."),
                },
            },
            new DayDef // 2
            {
                Subtitle = "Old Friends",
                Story = new[] { new StoryCatch("bear", 2, 4), new StoryCatch("brush", 5, 8) },
                Night = new[] {
                    M("Two days, and I'd almost stopped thinking about her."),
                    R("The radio crackled."),
                    D("So how are they biting?"),
                    M("I caught a bear today. A teddy bear."),
                    M("Dan... it was Captain."),
                    D("..."),
                    D("That can't be him. There are lots of bears in the world, Wes."),
                    D("Mary's sister called again. I told her you were away for a while."),
                    M("What did she want?"),
                    D("The same thing she always wants. Get some sleep, Wes."),
                },
            },
            new DayDef // 3
            {
                Subtitle = "Something in the Water",
                Story = new[] { new StoryCatch("hand", 4, 7) },
                Night = new[] {
                    M("I ran away from that place. Going there was a bad idea."),
                    M("A rational person would have reported what I found. Or at least told Dan."),
                    M("But I didn't..."),
                    R("The radio crackled."),
                    D("Wes? You sound off."),
                    M("I'm just tired."),
                    D("...Okay. If you say so."),
                    O("Good. He doesn't need to know."),
                    M("Maybe it was the grief. Maybe it was Mary leaving."),
                    M("I went to sleep that night without much trouble."),
                },
            },
            new DayDef // 4
            {
                Subtitle = "Lost Time",
                LostTime = true,
                Morning = new[] {
                    M("The next day was a daze. When I came to, I was back on the lake, in my boat."),
                    M("It was as if I'd gone out there purely out of habit."),
                    M("I wasn't afraid. I went ahead with renewed energy."),
                },
                Story = new[] { new StoryCatch("watch", 2, 4), new StoryCatch("arm", 6, 8) },
                Night = new[] {
                    R("The radio crackled."),
                    D("Wes. Do you remember the summer Dad left?"),
                    M("Not much. He was drunk, and then he was gone."),
                    D("Right. Good. That's... that's good."),
                    O("He remembers. He was watching."),
                    M("Who-"),
                    D("Wes? You still there?"),
                    M("I'm here. Goodnight, Dan."),
                },
            },
            new DayDef // 5
            {
                Subtitle = "Pride",
                Story = new[] { new StoryCatch("leg", 4, 7) },
                Blackout = "necklace",
                BlackoutAfter = 2,
                Night = new[] {
                    R("The radio crackled."),
                    D("The police came by the house today. They were asking about Mary."),
                    M("What did you tell them?"),
                    D("I told them she went to her sister's. It's the same thing you told me."),
                    O("He's lying for us. He always has."),
                    M("Dan, why did you send me up here?"),
                    D("..."),
                    D("Get some sleep, Wes."),
                },
            },
            new DayDef // 6
            {
                Subtitle = "Recognition",
                Story = new[] { new StoryCatch("backpack", 2, 4), new StoryCatch("head", 6, 8) },
                Night = new[] {
                    M("Over the days, I found more dead people in that lake."),
                    M("With each part I found, I felt a sense of pride growing inside of me."),
                    R("The radio crackled."),
                    D("I'm driving up tomorrow."),
                    M("You don't have to."),
                    D("Stay off the water until I get there. Please."),
                    O("We'll be waiting."),
                },
            },
            new DayDef // 7
            {
                Subtitle = "Still Waters",
                Morning = new[] {
                    M("There was no wind that morning. The lake was so still it looked solid."),
                    M("Dan told me to stay off the water."),
                    O("Since when do we listen to Dan?"),
                },
                Story = new[] { new StoryCatch("torso", 3, 5) },
                Finale = true,
            },
        };

        // ---------------------------------------------------------------- finale and endings
        public static readonly Line[] Finale =
        {
            R("A motor hummed somewhere far off, getting closer."),
            D("Hey, Wes."),
            M("You knew."),
            D("I was eleven. I watched you hold him under until he stopped moving."),
            D("Then you looked at me like you'd never seen me before in your life."),
            D("I told everyone he left. When people started going missing, I told myself it wasn't you."),
            D("Then Mary stopped answering her phone."),
            D("I couldn't say it to your face, so I sent you here. I hoped you'd see it for yourself."),
            O("He's alone out here. Nobody knows he came."),
        };

        public const string ChoiceConfess = "Tell him everything";
        public const string ChoiceOther = "Let the Other answer";
        public const string ChoiceWater = "Walk into the water";

        public static readonly Line[] EndingConfess =
        {
            M("I told him all of it. The ones I remembered, and the ones the Other showed me."),
            M("He listened. When I was done he put a hand on my shoulder, the way he did when we were kids."),
            D("I'll drive you down in the morning."),
            M("We sat on the shore until the sky went grey. The Other didn't say a word."),
            M("The water was still. For the first time in twenty-five years, so was I."),
        };

        public static readonly Line[] EndingOther =
        {
            O("Dan. You always were good at keeping secrets."),
            D("Wes? Wes, don't-"),
            O("The water closed over him without a sound."),
            O("Next summer we'll come back. We always come back."),
        };

        public static readonly Line[] EndingOtherCoda =
        {
            M("I arrived at the lake that Dan had told me about. It was just as hidden and just as beautiful as he had described."),
            M("A few days of peace and quiet. That was all I needed."),
        };

        public static readonly Line[] EndingWater =
        {
            M("I walked into the lake with my boots on. It was colder than I remembered."),
            D("Wes! Wes!"),
            M("Dan was shouting my name, and then he wasn't."),
            M("Under the surface it was quiet. For once, nobody else was talking."),
        };

        public static readonly string[] EndingIds = { "confess", "other", "water" };
        public static readonly string[] EndingTitles = { "Confession", "The Other", "Still Waters" };

        // ---------------------------------------------------------------- thoughts
        public static readonly string[] LostFish = { "Damn, it got away.", "I almost had it.", "Damn it...", "I was too slow." };
        public static readonly string[] BoxFull = { "There's no more space on the boat...", "I should sell what I've got first.", "The box is full. Back to the tent." };
        public static readonly string[] TooDark = { "It's getting late. I should call it a day.", "I can't fish in the dark.", "I can't see a thing. It's time to sleep." };
        public static readonly string[] GoodCatch = { "That's a good catch.", "That's not bad at all.", "That one will do." };
        public static readonly string[] JunkCatch = { "Well, at least I caught something...", "It's just junk.", "Who throws this stuff in a lake?" };
        public static readonly string[] OtherWhispers = { "Go out deeper.", "You know what's down there.", "Keep going, Wes.", "Don't stop now.", "We put them here." };
    }
}
