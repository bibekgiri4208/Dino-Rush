using System;
using UnityEngine;

namespace DinoRush
{
    [Serializable]
    public sealed class RunnerStoryLine
    {
        public string Speaker, Text, Shot;
        public RunnerStoryLine(string speaker, string text, string shot = "")
        { Speaker = speaker; Text = text; Shot = shot; }
    }

    [Serializable]
    public sealed class RunnerStoryScene
    {
        public string Title, Shot, Companion;
        public RunnerStoryLine[] Lines;
        public RunnerStoryScene(string title, string shot, string companion, params RunnerStoryLine[] lines)
        { Title = title; Shot = shot; Companion = companion; Lines = lines; }
    }

    [Serializable]
    public sealed class RunnerStoryLevel
    {
        public int Index;
        public string Title, Objective;
        public float Duration, DifficultyStart, DifficultyEnd;
        public RunnerStoryScene Opening, Ending;
        public int Chapter => Index / 3;
        public string Number => $"{Chapter + 1}-{Index % 3 + 1}";
        public string MapId => RunnerStoryCampaign.MapIds[Chapter];
        public int Reward => 75 + Chapter * 25;
        public int Seed => 47021 + Index * 7919;
        public float CourseTime(float elapsed) => Mathf.Lerp(DifficultyStart, DifficultyEnd, Mathf.Clamp01(elapsed / Duration));
        // Integrate the same speed curve used by the real simulation, including stage boundaries.
        public float DistanceAt(float time)
        {
            float total = 0, end = Mathf.Clamp(time, 0, Duration);
            for (float t = 0; t < end; t += 0.1f)
            {
                float dt = Mathf.Min(0.1f, end - t);
                total += RunnerRules.Speed(CourseTime(t + dt * 0.5f)) * dt;
            }
            return total;
        }
    }

    /// <summary>Campaign content lives here, independently of UI and the runner simulation.</summary>
    public static class RunnerStoryCampaign
    {
        public const int LevelCount = 15;
        public static readonly string[] MapIds = { "forest", "desert", "jungle", "rural", "snow" };
        public static readonly string[] ChapterTitles =
        { "The Broken Nest", "Footprints in the Sand", "Secrets of the Ancients", "The Last Gathering", "Beyond the Frozen Peak" };
        private static RunnerStoryLine L(string who, string text, string shot = "") => new RunnerStoryLine(who, text, shot);
        private static RunnerStoryScene S(string title, string shot, string friend, params RunnerStoryLine[] lines) =>
            new RunnerStoryScene(title, shot, friend, lines);
        private static RunnerStoryLevel Level(int index, string title, string objective, float seconds, float start, float end,
            RunnerStoryScene opening, RunnerStoryScene ending) => new RunnerStoryLevel
            { Index = index, Title = title, Objective = objective, Duration = seconds, DifficultyStart = start,
                DifficultyEnd = end, Opening = opening, Ending = ending };

        public static readonly RunnerStoryLevel[] Levels =
        {
            Level(0, "When the Ground Woke", "Reach the safe clearing. Jump over fallen debris.", 60, 0, 11,
                S("A morning like any other", "nest", "Kira",
                    L("Blue", "One, two, three... All the eggs are here. A perfect morning."),
                    L("Kira", "You said that yesterday. Then you sat on a pinecone."),
                    L("Narrator", "Far beneath the roots, something stirred.", "quake"),
                    L("Blue", "The nests! Everyone, away from the trees!", "escape"),
                    L("Kira", "The clearing, Blue! Keep moving!"),
                    L("Blue", "Jump the fallen branches. I'll find you there!")),
                S("An empty clearing", "empty", "Kira",
                    L("Blue", "Kira? Cole? Can anybody hear me?"),
                    L("Narrator", "The shaking faded. The silence felt even bigger."),
                    L("Blue", "There should be more footprints here."),
                    L("Blue", "Hold on, everyone. I'm coming."))),
            Level(1, "A Voice in the Trees", "Find Kira. Hold Down / S, or swipe down and hold, to duck.", 65, 20, 40,
                S("Someone answers", "blocked", "Kira",
                    L("Kira", "Blue! Over here! The path's blocked!"),
                    L("Blue", "I can hear you. Are you hurt?"),
                    L("Kira", "Only my pride. A branch has outsmarted me."),
                    L("Blue", "Stay there. I'll go around."),
                    L("Kira", "Keep low under the thorns. They don't negotiate.")),
                S("Two sets of footprints", "friend", "Kira",
                    L("Blue", "There you are! I thought I'd lost everyone."),
                    L("Kira", "Not everyone. You're stuck with me."),
                    L("Blue", "But where do we go? The nests are gone."),
                    L("Kira", "I saw an old trail marker. Let's start with a direction."))),
            Level(2, "The Ancient Trail", "Reach the old trail marker at the forest boundary.", 70, 28, 47,
                S("A mark in the moss", "trail", "Kira",
                    L("Kira", "That shape on the stone. Doesn't it look like a valley?"),
                    L("Blue", "Or a very lumpy footprint."),
                    L("Kira", "Let's hope the ancient artists were better than you."),
                    L("Blue", "The rest of the carving is past the ridge. Come on.")),
                S("A journey home", "map", "Kira",
                    L("Narrator", "Five lands. One winding trail. A green valley beyond a white peak."),
                    L("Blue", "Hidden Valley. The elders said it was only a story."),
                    L("Kira", "Stories usually start somewhere."),
                    L("Blue", "What if I lead everyone the wrong way?"),
                    L("Kira", "Then we turn around. Together."),
                    L("Blue", "First we find our friends. No one gets left behind."))),
            Level(3, "Following the Scout", "Follow the footprints across the dunes and find Cole.", 70, 28, 47,
                S("Tracks in the sand", "tracks", "Kira",
                    L("Blue", "Those prints are fresh. Someone made it out!"),
                    L("Kira", "Big feet. Heavy steps. Definitely Cole."),
                    L("Blue", "How can you tell?"),
                    L("Kira", "He kicked every stone along the way.")),
                S("A stubborn survivor", "scout", "Cole",
                    L("Cole", "About time. I was beginning to enjoy the quiet."),
                    L("Blue", "We're finding a new home. Will you come?"),
                    L("Cole", "Across that desert? With the ground still shaking?"),
                    L("Blue", "Not alone. With us."),
                    L("Cole", "...All right. But I'm choosing the next rest stop."))),
            Level(4, "Through the Sandstorm", "Reach shelter before the dunes swallow the trail.", 75, 36, 60,
                S("The wind turns", "storm", "Cole",
                    L("Cole", "See that wall of sand? We need shelter."),
                    L("Blue", "There's a rocky hollow beyond the dunes."),
                    L("Cole", "And a lot of trouble between here and there."),
                    L("Blue", "Then we take it one jump at a time.")),
                S("A voice beyond the wind", "shelter", "Cole",
                    L("Kira", "Sand in my eyes. Sand in my ears. How is there sand in my tail?"),
                    L("Narrator", "A low call rolled across the desert. The stone beneath them trembled."),
                    L("Cole", "That wasn't thunder."),
                    L("Blue", "It sounded... lonely."),
                    L("Cole", "It sounded enormous. Let's not meet it today."))),
            Level(5, "The Buried Warning", "Reach the stone uncovered by the storm.", 75, 40, 64,
                S("What the wind uncovered", "uncover", "Kira",
                    L("Kira", "The storm moved half the desert. Look what it left behind."),
                    L("Blue", "The same symbols as the forest marker!"),
                    L("Cole", "Ancient stones can wait. We need water."),
                    L("Blue", "The carving might show us where to find it.")),
                S("An unfinished warning", "warning", "Cole",
                    L("Blue", "A creature as tall as the mountain... What is it guarding?"),
                    L("Kira", "There's another shape beside it. Most of that part is missing."),
                    L("Cole", "A warning. That's what it is."),
                    L("Blue", "Or a piece of the way home."),
                    L("Kira", "Sena knows these symbols. If she reached the jungle, she can help."))),
            Level(6, "Under the Green Roof", "Follow Sena's markers through the low jungle canopy.", 75, 44, 68,
                S("A familiar ribbon", "trail", "Kira",
                    L("Blue", "Someone tied leaves around those branches."),
                    L("Kira", "Sena's trail signs. She always was tidier than us."),
                    L("Blue", "Then she's alive!"),
                    L("Kira", "Alive, and probably correcting an ancient inscription.")),
                S("The reader of old stones", "ruins", "Sena",
                    L("Sena", "Blue! Kira! I hoped someone would see my markers."),
                    L("Blue", "We found a carving. A mountain, a giant creature..."),
                    L("Sena", "The guardian. There are more pictures deeper in these ruins."),
                    L("Blue", "Then let's read the rest of the story."))),
            Level(7, "The Guardian's Story", "Reach the inner ruins. Use fast drops to prepare for low branches.", 80, 48, 73,
                S("Words without voices", "ruins", "Sena",
                    L("Sena", "These marks aren't a warning. They're a thank-you."),
                    L("Cole", "To the thing shaking the ground?"),
                    L("Sena", "To the one who kept this trail open, long ago."),
                    L("Blue", "What happened to it?"),
                    L("Sena", "The last wall might tell us. Mind the hanging vines.")),
                S("The keeper of the pass", "history", "Sena",
                    L("Sena", "The guardian guided the first herds through the mountain."),
                    L("Cole", "Then why is it destroying everything now?"),
                    L("Blue", "Maybe something changed."),
                    L("Sena", "There's one more carving. A smaller set of footprints leads to it."))),
            Level(8, "Not a Monster", "Find the final carving and the tiny footprints beyond it.", 80, 50, 75,
                S("The missing picture", "tracks", "Sena",
                    L("Kira", "Those aren't any of our footprints."),
                    L("Blue", "They're tiny. And whoever made them was alone."),
                    L("Sena", "They pass right by the last wall."),
                    L("Blue", "Let's find out who we've been following.")),
                S("A parent searching", "reveal", "Sena",
                    L("Sena", "Look. The guardian... and its hatchling."),
                    L("Blue", "That's the shape missing from the desert stone!"),
                    L("Narrator", "Another distant call. This time, Blue heard a question in it."),
                    L("Blue", "It's not chasing us. It's looking for its little one.", "search"),
                    L("Cole", "All this time... it was frightened?"),
                    L("Blue", "Just like we were. We need to help them both."))),
            Level(9, "Empty Riverside", "Search the riverside trail for the missing herd.", 80, 48, 73,
                S("Windows without lights", "village", "Cole",
                    L("Cole", "The riverside shelters. We should have found everyone here."),
                    L("Blue", "Fresh tracks. They haven't been gone long."),
                    L("Kira", "I'll check the upper path. You take the river."),
                    L("Blue", "Call if you find anyone. We're done losing each other.")),
                S("A familiar face", "gather", "Cole",
                    L("Narrator", "At Blue's call, familiar faces emerged from the shelters."),
                    L("Cole", "There they are. I told them we'd come."),
                    L("Blue", "Is this everyone?"),
                    L("Cole", "Not yet. The last group went farther along the river."))),
            Level(10, "No One Left Behind", "Reach the last survivors beyond the damaged river trail.", 85, 54, 78,
                S("One more trip", "river", "Cole",
                    L("Cole", "The river trail won't stay open much longer."),
                    L("Kira", "We could reach the mountain before dark if we leave now."),
                    L("Blue", "Not without them."),
                    L("Cole", "You're certain?"),
                    L("Blue", "No. But I know how it feels to wait for someone.")),
                S("Everyone means everyone", "rescue-herd", "Cole",
                    L("Cole", "Counted twice. That's the whole herd."),
                    L("Blue", "Good. Nobody sleeps alone tonight."),
                    L("Kira", "Listen to you. You sound like a leader."),
                    L("Blue", "I still don't know every turn."),
                    L("Cole", "You came back. That's the part that matters."))),
            Level(11, "Tiny Tracks, Big Trouble", "Follow the hatchling's tracks to the mountain foothills.", 85, 56, 80,
                S("The smallest traveler", "tracks", "Sena",
                    L("Sena", "The tiny tracks cross the river here. Then they turn uphill."),
                    L("Blue", "Toward the snow? It'll freeze up there."),
                    L("Cole", "The herd needs a safe place to wait."),
                    L("Blue", "Shelter them at the foothills. We'll open the pass.")),
                S("Before the climb", "camp", "Kira",
                    L("Narrator", "The herd gathered under the last trees. Above them, the mountain answered with a rumble."),
                    L("Blue", "What if I can't do this?"),
                    L("Kira", "Then we take the next step together. Same as the first one."),
                    L("Cole", "I'll keep everyone safe here. Bring that little one home."),
                    L("Blue", "Both little ones. Ours and the guardian's."))),
            Level(12, "The Frozen Pass", "Cross the snowy ridge and listen for the hatchling.", 90, 58, 82,
                S("Above the clouds", "snow", "Kira",
                    L("Kira", "I'd like to formally complain about the weather."),
                    L("Blue", "You can complain all the way to the top."),
                    L("Kira", "Excellent. I have material."),
                    L("Blue", "Wait. Did you hear that?"),
                    L("Narrator", "Beyond the wind: a small, frightened cry.")),
                S("An answer in the snow", "ledge", "Kira",
                    L("Blue", "Little one! We're here!"),
                    L("Hatchling", "...Prr?"),
                    L("Kira", "Beyond the broken ledge. I see it!"),
                    L("Blue", "Don't move. We're coming to you."))),
            Level(13, "A Small Cry", "Reach the hatchling beyond the broken trail.", 95, 62, 87,
                S("Across the broken trail", "rescue", "Kira",
                    L("Kira", "The ledge is narrow. Slow breaths, Blue."),
                    L("Blue", "It's shaking. It must be terrified."),
                    L("Kira", "Then show it someone isn't giving up."),
                    L("Blue", "Stay where you are, little one. Follow my voice.")),
                S("Safe at last", "rescue-complete", "Kira",
                    L("Hatchling", "Prr... prrr!"),
                    L("Blue", "There. Solid ground. You're safe now."),
                    L("Kira", "It likes you. Or it thinks your tail is lunch."),
                    L("Narrator", "A shadow rose beyond the ridge. The guardian had heard.", "guardian"),
                    L("Blue", "Let's take you back to your family."))),
            Level(14, "The Way Home", "Guide the hatchling to the guardian at the ancient gate.", 100, 65, 90,
                S("One last stretch", "guardian", "Cole",
                    L("Narrator", "Cole reached the ridge as the guardian's silhouette filled the sky."),
                    L("Cole", "Blue! Step back!"),
                    L("Blue", "Look at its eyes, Cole. It isn't angry."),
                    L("Hatchling", "Prrr!"),
                    L("Blue", "Your little one is here. Meet us at the gate."),
                    L("Kira", "Last run. Let's bring everyone home.")),
                S("Hidden Valley", "reunion", "Kira",
                    L("Narrator", "The hatchling ran forward. The great guardian lowered its head."),
                    L("Blue", "There you are. You found each other."),
                    L("Narrator", "The mountain grew still. With a gentle push, the guardian opened the ancient gate.", "gate"),
                    L("Cole", "Blue... look beyond it."),
                    L("Narrator", "Green grass. Warm light. Room for every last footprint.", "valley"),
                    L("Sena", "Hidden Valley. The story was real."),
                    L("Kira", "Not bad for someone who couldn't read a lumpy footprint."),
                    L("Blue", "I didn't bring everyone here alone."),
                    L("Cole", "No. You made sure nobody had to walk alone."),
                    L("Narrator", "Seasons would change. New journeys would come. But tonight, the herd was home."),
                    L("Blue", "One, two, three... All the eggs are here.", "egg"),
                    L("Narrator", "A tiny crack. A brand-new beginning.", "hatch"),
                    L("Narrator", "THE END — A NEW BEGINNING", "epilogue")))
        };
    }
}
