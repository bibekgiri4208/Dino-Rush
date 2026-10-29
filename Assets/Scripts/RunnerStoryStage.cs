using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DinoRush
{
    /// <summary>Deterministic miniature film sets, not dialogue portraits. No gameplay randomness or physics is used.</summary>
    public sealed class RunnerStoryStage : IDisposable
    {
        private enum Pose { Idle, Run, Jump, Duck, Hurt }
        private sealed class Actor
        {
            public Image Image, Shadow;
            public Sprite[] Frames;
            public int Offset;
        }
        private sealed class Scenery
        {
            public RunnerMapCatalog.MapLayer Spec;
            public Image[] Tiles;
        }
        private readonly ForestRunnerGame game;
        private readonly RunnerStoryArt art;
        private readonly RectTransform viewport, cameraRig, backdrop, sceneryRoot, set, actorsRoot, effects;
        private readonly Image sky, soil, warmth;
        private readonly Dictionary<string, Actor> cast = new Dictionary<string, Actor>();
        private readonly List<Scenery> layers = new List<Scenery>();
        private readonly List<Image> props = new List<Image>();
        private readonly Image tree, fissure, gap, wall, tablet, guardianGlyph, childGlyph, nestGlow;
        private readonly Image[] rocks = new Image[8], nests = new Image[3], eggs = new Image[3], tracks = new Image[8];
        private readonly Image[] markers = new Image[3], route = new Image[5], routeLines = new Image[4], shards = new Image[8];
        private readonly Image[] particles = new Image[56], rays = new Image[7];
        private readonly Image[] sandClouds = new Image[5];
        private readonly RectTransform guardian, neck, head, gate;
        private readonly Image guardianBody, guardianHead, doorLeft, doorRight, gateFrame;
        private readonly Actor hatchling, baby;
        private string companion, speaker;
        private int chapter, currentMap = -1, particleMode;
        private float width = 1080, height = 420, floor, size, pan, zoom, drift, shake;
        public string Shot { get; private set; }
        public float ShotTime { get; private set; }
        public float Duration => DurationFor(Shot);
        public bool ActionFinished => ShotTime >= Duration;
        public Transform Root => cameraRig;
        public float FissureWidth => fissure.rectTransform.sizeDelta.x;
        public float FallingTreeAngle => tree.rectTransform.localEulerAngles.z;
        public Vector2 BluePosition => cast["blue"].Image.rectTransform.anchoredPosition;
        public Vector2 HatchlingPosition => hatchling.Image.rectTransform.anchoredPosition;

        public static float DurationFor(string shot)
        {
            switch (shot)
            {
                case "quake": case "rescue-complete": case "valley": return 6;
                case "map": case "uncover": case "history": case "reveal": case "search": case "river":
                case "rescue-herd": case "camp": case "snow": case "rescue": case "guardian": case "reunion": case "gate": case "hatch": return 5;
                case "egg": case "empty": case "epilogue": return 3;
                default: return 4;
            }
        }
        public RunnerStoryStage(RectTransform parent, ForestRunnerGame owner, RunnerStoryArt assets)
        {
            game = owner; art = assets; viewport = parent;
            sky = RunnerStoryUi.Box("Cinematic sky", parent, 0, 0, 1600, 1200, Color.white);
            backdrop = RunnerStoryUi.Rect("Layered parallax scenery", parent, 0, 0, 1080, 420);
            soil = RunnerStoryUi.Box("Continuous earth below the trail", backdrop, 0, -200, 4000, 400, Color.black);
            sceneryRoot = RunnerStoryUi.Rect("Map layers", backdrop, 0, 0, 1080, 420);
            cameraRig = RunnerStoryUi.Rect("Cinematic camera rig", parent, 0, 0, 1080, 420);
            set = RunnerStoryUi.Rect("Physical story set", cameraRig, 0, 0, 1080, 420);
            tree = Prop("Falling tree", art.Tree); tree.rectTransform.pivot = new Vector2(0.5f, 0.02f);
            fissure = Prop("Opening ground fissure", art.Crack);
            gap = Prop("Broken trail chasm", art.Chasm); gap.preserveAspect = false;
            for (int i = 0; i < rocks.Length; i++) rocks[i] = Prop("Loose rock " + i, game.StoryRockSprite());
            wall = Prop("Illustrated ancient wall", art.Wall);
            tablet = Prop("Discovered trail tablet", art.Ruin);
            guardianGlyph = Prop("Guardian mural", art.Guardian);
            childGlyph = Prop("Hatchling mural", art.HatchlingFrames[0]);
            for (int i = 0; i < route.Length; i++) route[i] = Prop("Migration waypoint " + i, art.Star);
            for (int i = 0; i < routeLines.Length; i++) routeLines[i] = Prop("Migration route " + i, null);
            for (int i = 0; i < 3; i++) { nests[i] = Prop("Nest " + i, art.Nest); eggs[i] = Prop("Nest egg " + i, art.Egg); markers[i] = Prop("Leaf marker " + i, art.Ribbon); }
            for (int i = 0; i < tracks.Length; i++) tracks[i] = Prop("Visible footprint " + i, art.Footprint);
            gate = RunnerStoryUi.Rect("Ancient gate mechanism", set, 0, 0, 300, 340);
            gateFrame = Picture("Stone arch", gate, art.Gate);
            doorLeft = Picture("Sliding left stone", gate, art.Wall); doorRight = Picture("Sliding right stone", gate, art.Wall);
            doorLeft.preserveAspect = doorRight.preserveAspect = false;
            guardian = RunnerStoryUi.Rect("Articulated mountain guardian", set, 0, 0, 320, 320);
            guardianBody = Picture("Armored body and feet", guardian, art.GuardianBody);
            neck = RunnerStoryUi.Rect("Bending neck", guardian, 62, 83, 56, 190); neck.pivot = new Vector2(0.5f, 0);
            Image neckArt = Picture("Neck scales", neck, art.GuardianNeck);
            RunnerShopPresentation.Position(neckArt.transform, 0, 95, 56, 190);
            neckArt.rectTransform.anchorMin = neckArt.rectTransform.anchorMax = new Vector2(0.5f, 0);
            head = RunnerStoryUi.Rect("Moving head", guardian, 80, 260, 128, 80);
            guardianHead = Picture("Face", head, art.GuardianHead);
            RunnerShopPresentation.Position(guardianBody.transform, -14, 67, 260, 150);
            RunnerShopPresentation.Position(guardianHead.transform, 0, 0, 128, 80);
            actorsRoot = RunnerStoryUi.Rect("Animated cast", cameraRig, 0, 0, 1080, 420);
            for (int i = 0; i < game.DinoRoster.Count; i++)
                cast[game.DinoRoster[i].Id] = CreateActor(game.DinoRoster[i].DisplayName, game.StoryDinoFrames(i), i);
            hatchling = CreateActor("Lost hatchling", art.HatchlingFrames, 2);
            baby = CreateActor("New Blue hatchling", game.StoryDinoFrames(0), 3);
            effects = RunnerStoryUi.Rect("Cinematic particles", cameraRig, 0, 0, 1080, 420);
            for (int i = 0; i < shards.Length; i++) shards[i] = Prop("Flying egg shell " + i, art.Egg, effects);
            for (int i = 0; i < particles.Length; i++)
            { particles[i] = Picture("Cinematic particle " + i, effects, art.Dust); particles[i].preserveAspect = false; }
            nestGlow = Picture("Warm nest light", set, art.Dust); nestGlow.transform.SetAsFirstSibling();
            for (int i = 0; i < rays.Length; i++)
            { rays[i] = Picture("Valley sunlight " + i, set, art.Dust); rays[i].preserveAspect = false; }
            for (int i = 0; i < sandClouds.Length; i++)
            {
                sandClouds[i] = Picture("Approaching sand wall " + i, set, art.Dust);
                sandClouds[i].preserveAspect = false; sandClouds[i].transform.SetAsFirstSibling();
            }
            warmth = RunnerStoryUi.Box("Cinematic color grade", parent, 0, 0, 1600, 1200, Color.clear);
            ResetObjects();
        }
        private Image Prop(string name, Sprite sprite, Transform parent = null)
        { Image image = Picture(name, parent != null ? parent : set, sprite); props.Add(image); return image; }
        private static Image Picture(string name, Transform parent, Sprite sprite)
        {
            Image image = RunnerStoryUi.Box(name, parent, 0, 0, 100, 100, Color.white);
            image.sprite = sprite; image.preserveAspect = sprite != null; return image;
        }
        private Actor CreateActor(string name, Sprite[] frames, int offset)
        {
            var actor = new Actor { Frames = frames, Offset = offset };
            actor.Shadow = Picture(name + " shadow", actorsRoot, art.Dust); actor.Shadow.color = new Color(0, 0, 0, 0.28f);
            actor.Image = Picture(name + " animated actor", actorsRoot, frames[0]); actor.Image.rectTransform.pivot = new Vector2(0.5f, 0);
            return actor;
        }
        public void Play(RunnerStoryScene scene, int chapterIndex)
        { chapter = chapterIndex; companion = scene.Companion.ToLowerInvariant(); speaker = ""; Shot = null; BeginShot(scene.Shot); }
        public void SetSpeaker(string name) { speaker = name.ToLowerInvariant(); }
        public void BeginShot(string shot)
        {
            if (string.IsNullOrEmpty(shot) || Shot == shot) return;
            Shot = shot; ShotTime = 0;
            int map = shot == "valley" || shot == "egg" || shot == "hatch" || shot == "epilogue" ? 0 : shot == "search" ? 4 : chapter;
            SetMap(map); ResetObjects(); Render();
        }
        private void SetMap(int chapterIndex)
        {
            int index = game.MapCatalog.Find(RunnerStoryCampaign.MapIds[chapterIndex]);
            if (index == currentMap) return;
            currentMap = index;
            // Bounded per-stage layer pool: reuse tiles rather than rebuilding a scene on every line.
            var specs = game.MapCatalog[index].layers;
            for (int i = 0; i < specs.Length; i++)
            {
                if (i == layers.Count)
                {
                    var layer = new Scenery { Tiles = new Image[5] };
                    for (int tile = 0; tile < 5; tile++) layer.Tiles[tile] = Picture("Live map layer " + i + " tile " + tile, sceneryRoot, specs[i].sprite);
                    layers.Add(layer);
                }
                layers[i].Spec = specs[i];
                foreach (Image image in layers[i].Tiles) { image.sprite = specs[i].sprite; image.preserveAspect = false; }
            }
            for (int i = 0; i < layers.Count; i++)
                foreach (Image image in layers[i].Tiles) image.gameObject.SetActive(i < specs.Length);
            // Earth must cover the opaque sky below the trail, but stay underneath the grass tiles.
            soil.transform.SetParent(sceneryRoot, false); soil.transform.SetAsLastSibling();
            soil.transform.SetSiblingIndex(layers[specs.Length - 1].Tiles[0].transform.GetSiblingIndex());
            sky.color = game.MapCatalog[index].skyColor;
            soil.color = game.MapCatalog[index].soilColor;
        }
        public void Layout()
        {
            width = viewport.rect.width; height = viewport.rect.height;
            floor = -height * 0.32f; size = Mathf.Min(width * 0.135f, height * 0.36f);
            backdrop.sizeDelta = cameraRig.sizeDelta = new Vector2(width, height);
            Render();
        }
        public void Tick(float dt) { ShotTime += Mathf.Max(0, dt); Render(); }
        private static float Ease(float t, float start, float end) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(start, end, t));
        private static float Hop(float t, float start, float end) => t < start || t > end ? 0 : Mathf.Sin(Mathf.InverseLerp(start, end, t) * Mathf.PI);
        private void Show(Image image, float x, float y, float w, float h, float angle = 0)
        {
            image.gameObject.SetActive(true);
            RunnerShopPresentation.Position(image.transform, x * width, floor + y * height, w, h);
            if (image == tree) image.rectTransform.pivot = new Vector2(0.5f, 0.02f);
            image.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
        }
        private void Put(Actor actor, float x, float y = 0, Pose pose = Pose.Idle, bool right = true, float scale = 1)
        {
            actor.Image.gameObject.SetActive(true); actor.Shadow.gameObject.SetActive(true);
            float bodySize = size * scale;
            int frame;
            if (actor.Frames.Length == 4) frame = pose == Pose.Run ? (int)(ShotTime * 9 + actor.Offset) % 4 : 0;
            else frame = pose == Pose.Run ? 4 + (int)(ShotTime * 11 + actor.Offset) % 6 : pose == Pose.Jump ? 11 :
                pose == Pose.Duck ? 18 + (int)(ShotTime * 9) % 6 : pose == Pose.Hurt ? 14 : (int)(ShotTime * 4 + actor.Offset) % 4;
            actor.Image.sprite = actor.Frames[frame];
            bool speaking = cast.TryGetValue(speaker ?? "", out Actor talker) && talker == actor;
            float breathe = pose == Pose.Idle ? Mathf.Sin(ShotTime * 3 + actor.Offset) * (speaking ? 0.018f : 0.012f) : 0;
            float stride = pose == Pose.Run ? Mathf.Sin(ShotTime * 19 + actor.Offset) * 0.025f : 0;
            RunnerShopPresentation.Position(actor.Image.transform, x * width, floor + y * height - bodySize / 12f, bodySize, bodySize);
            actor.Image.rectTransform.pivot = new Vector2(0.5f, 0);
            actor.Image.rectTransform.localScale = new Vector3((right ? 1 : -1) * (1 - breathe + stride), 1 + breathe - stride, 1);
            actor.Image.rectTransform.localRotation = Quaternion.identity;
            actor.Image.color = Color.white;
            RunnerShopPresentation.Position(actor.Shadow.transform, x * width, floor + 3, bodySize * 0.7f * (1 - Mathf.Min(y, 0.5f)), bodySize * 0.16f);
        }
        private void Dino(string id, float x, float y = 0, Pose pose = Pose.Idle, bool right = true, float scale = 1)
        { if (cast.TryGetValue(id, out Actor actor)) Put(actor, x, y, pose, right, scale); }
        private void Hide(Actor actor) { actor.Image.gameObject.SetActive(false); actor.Shadow.gameObject.SetActive(false); }
        private void ResetObjects()
        {
            foreach (Image image in props) image.gameObject.SetActive(false);
            foreach (Image rock in rocks) rock.sprite = game.StoryRockSprite();
            tree.preserveAspect = true;
            foreach (Actor actor in cast.Values) Hide(actor);
            Hide(hatchling); Hide(baby); guardian.gameObject.SetActive(false); gate.gameObject.SetActive(false); nestGlow.gameObject.SetActive(false);
            foreach (Image ray in rays) ray.gameObject.SetActive(false);
            foreach (Image cloud in sandClouds) cloud.gameObject.SetActive(false);
            particleMode = 0; pan = drift = shake = 0; zoom = 1;
        }
        private void Render()
        {
            if (Shot == null || width <= 0 || height <= 0) return;
            // Visibility is reset on cuts, not every frame: avoid rebuilding the whole Canvas/Graphic registry at 60 Hz.
            particleMode = 0; pan = drift = shake = 0; zoom = 1;
            float t = ShotTime;
            switch (Shot)
            {
                case "nest": Morning(t); break;
                case "quake": Earthquake(t); break;
                case "escape": Escape(t); break;
                case "empty": EmptyClearing(t); break;
                case "blocked": Blocked(t); break;
                case "friend": FoundFriend(t); break;
                case "tracks": FollowTrail(t, true); break;
                case "scout": FindScout(t); break;
                case "storm": Sandstorm(t, false); break;
                case "shelter": Sandstorm(t, true); break;
                case "uncover": Mural(t, true); break;
                case "map": case "warning": case "history": case "reveal": case "carving": Mural(t, false); break;
                case "search": SearchingGuardian(t); break;
                case "ruins": Ruins(t); break;
                case "village": Village(t); break;
                case "gather": Gathering(t); break;
                case "river": case "rescue-herd": River(t); break;
                case "camp": Camp(t); break;
                case "snow": SnowPass(t); break;
                case "ledge": case "rescue": case "rescue-complete": Rescue(t); break;
                case "guardian": ApproachingGuardian(t); break;
                case "reunion": Reunion(t); break;
                case "gate": OpeningGate(t); break;
                case "valley": Migration(t); break;
                case "egg": case "hatch": case "epilogue": NewBeginning(t); break;
                default: FollowTrail(t, false); break;
            }
            RenderScenery(); RenderParticles();
            float jolt = Mathf.Sin(t * 48) * shake;
            cameraRig.anchoredPosition = new Vector2(pan * width + jolt, Mathf.Cos(t * 37) * shake * 0.5f);
            cameraRig.localScale = backdrop.localScale = Vector3.one * zoom;
            backdrop.anchoredPosition = new Vector2(jolt * 0.6f, Mathf.Cos(t * 37) * shake * 0.35f);
            warmth.color = Shot == "storm" || Shot == "shelter" ? new Color(0.88f, 0.65f, 0.36f, 0.12f) :
                new Color(1, 0.77f, 0.36f, Shot == "valley" || Shot == "egg" || Shot == "hatch" || Shot == "epilogue" ? 0.065f : 0);
        }
        private void Morning(float t)
        {
            NestSet(false); float arrival = Ease(t, 0, 2.8f);
            Dino("blue", Mathf.Lerp(-0.43f, -0.19f, arrival), 0, arrival < 1 ? Pose.Run : Pose.Idle);
            Dino("kira", Mathf.Lerp(-0.6f, -0.37f, Ease(t, 0.5f, 3.5f)), 0, t < 3.5f ? Pose.Run : Pose.Idle);
            Dino("red", 0.34f, 0, Pose.Idle, false, 0.65f); Dino("yellow", 0.43f, 0, Pose.Idle, false, 0.6f);
            Sunlight(0.12f); pan = -Ease(t, 0, 4) * 0.025f;
        }
        private void NestSet(bool broken)
        {
            for (int i = 0; i < 3; i++)
            {
                float x = -0.06f + i * 0.18f;
                Show(nests[i], x, 0, size * 0.95f, size * 0.35f, broken ? (i - 1) * 14 : 0);
                bool hatched = i == 1 && (Shot == "epilogue" || Shot == "hatch" && ShotTime >= 2.1f);
                if (!hatched) Show(eggs[i], x + (broken ? (i - 1) * Ease(ShotTime, 1.6f, 4) * 0.09f : 0), 0.06f,
                    size * 0.35f, size * 0.42f, broken ? (i - 1) * Ease(ShotTime, 1.6f, 4) * 70 : 0);
            }
        }
        private void Earthquake(float t)
        {
            NestSet(true); particleMode = 3;
            float split = Ease(t, 0.5f, 3.3f), fall = Ease(t, 1.3f, 3.5f);
            Show(fissure, -0.04f, -0.025f, width * (0.02f + split * 0.83f), height * 0.09f);
            Show(tree, -0.26f, 0, size * 1.1f, height * 0.88f, -86 * fall);
            float flight = Ease(t, 2.5f, 5.8f);
            Dino("blue", Mathf.Lerp(-0.19f, 0.66f, flight), Hop(t, 2.7f, 3.8f) * 0.24f, t < 2.5f ? Pose.Hurt : Pose.Run);
            Dino("kira", Mathf.Lerp(-0.38f, 0.58f, Ease(t, 2.2f, 5.6f)), Hop(t, 2.5f, 3.5f) * 0.28f, Pose.Run);
            Dino("red", Mathf.Lerp(0.34f, 0.85f, Ease(t, 1.6f, 4)), 0, Pose.Run, true, 0.65f);
            Dino("yellow", Mathf.Lerp(0.43f, 0.94f, Ease(t, 2, 4.5f)), Hop(t, 2.2f, 3.3f) * 0.16f, Pose.Run, true, 0.6f);
            shake = (0.4f + Mathf.Sin(t * 2) * 0.2f) * (1 - Ease(t, 4.8f, 6)) * 13;
            zoom = 1.025f; pan = -flight * 0.08f;
        }
        private void Escape(float t)
        {
            particleMode = 3; drift = t * width * 0.16f; shake = 2;
            Show(tree, 0.03f - Mathf.Repeat(t * 0.14f, 0.85f), 0, size * 0.85f, height * 0.7f, -86);
            Show(fissure, 0.28f - Mathf.Repeat(t * 0.1f, 0.8f), -0.02f, width * 0.35f, height * 0.08f);
            Dino("blue", -0.1f, Hop(Mathf.Repeat(t, 3), 0.5f, 1.6f) * 0.26f, Pose.Run);
            Dino("kira", -0.3f, Hop(Mathf.Repeat(t, 3), 0.9f, 2) * 0.24f, Pose.Run);
        }
        private void EmptyClearing(float t)
        {
            float walk = Ease(t, 0, 2.3f);
            Dino("blue", Mathf.Lerp(-0.3f, 0.03f, walk), 0, walk < 1 ? Pose.Run : Pose.Idle, t < 3.3f || (int)t % 6 > 2);
            for (int i = 0; i < 5; i++)
            { Show(tracks[i], -0.3f + i * 0.09f, 0.005f, size * 0.15f, size * 0.12f, i % 2 == 0 ? 30 : -15); tracks[i].color = new Color(1, 1, 1, 0.3f); }
            pan = -walk * 0.04f;
        }
        private void Blocked(float t)
        {
            Show(tree, -0.06f, 0, size, height * 0.65f, -76);
            Dino("kira", 0.27f, 0, Pose.Idle, false);
            float move = Ease(t, 0, 3);
            Dino("blue", Mathf.Lerp(-0.5f, -0.22f, move), 0, move < 1 ? Pose.Run : Pose.Idle);
            for (int i = 0; i < 3; i++) Show(rocks[i], -0.07f + i * 0.07f, 0.025f, size * 0.45f, size * 0.5f);
        }
        private void FoundFriend(float t)
        {
            Show(rocks[0], -0.11f, 0.03f, size * 0.7f, size * 0.5f);
            float cross = Ease(t, 0.5f, 2.5f), leave = Ease(t, 3.2f, 5.5f);
            Dino("blue", Mathf.Lerp(-0.34f, 0.04f, cross) + leave * 0.26f, Hop(t, 0.5f, 2.5f) * 0.28f, cross < 1 || leave > 0 ? Pose.Run : Pose.Idle);
            Dino("kira", 0.2f + leave * 0.3f, 0, leave > 0 ? Pose.Run : Pose.Idle, leave > 0);
        }
        private void FollowTrail(float t, bool footprints)
        {
            float move = Ease(t, 0, 3.5f);
            Dino("blue", Mathf.Lerp(-0.38f, -0.04f, move), 0, move < 1 ? Pose.Run : Pose.Idle);
            Dino(companion, Mathf.Lerp(-0.55f, -0.22f, move), 0, move < 1 ? Pose.Run : Pose.Idle);
            if (footprints)
                for (int i = 0; i < tracks.Length; i++)
                { Show(tracks[i], -0.34f + i * 0.105f, 0.008f, size * (chapter >= 2 ? 0.15f : 0.22f), size * 0.18f, i % 2 == 0 ? 25 : -15); tracks[i].color = new Color(1, 1, 1, 0.75f); }
            else
            {
                Show(tablet, 0.29f, 0.14f, size * 1.35f, size * 1.55f);
                if (chapter == 2) for (int i = 0; i < markers.Length; i++) Show(markers[i], -0.25f + i * 0.3f, 0.48f + i % 2 * 0.08f,
                    size * 0.25f, size * 0.4f, Mathf.Sin(t * 2 + i) * 13);
            }
            pan = -move * 0.05f;
        }
        private void FindScout(float t)
        {
            rocks[0].sprite = game.StoryRockSprite(true);
            Show(rocks[0], 0.36f, 0.15f, size * 1.5f, size * 1.8f);
            Dino("cole", 0.26f, 0, t < 2 ? Pose.Duck : Pose.Idle, false);
            float move = Ease(t, 0, 3);
            Dino("blue", Mathf.Lerp(-0.4f, -0.02f, move), 0, move < 1 ? Pose.Run : Pose.Idle);
            Dino("kira", Mathf.Lerp(-0.58f, -0.2f, move), 0, move < 1 ? Pose.Run : Pose.Idle);
        }
        private void Sandstorm(float t, bool sheltered)
        {
            particleMode = 1; drift = sheltered ? 0 : t * width * 0.1f;
            for (int i = 0; i < sandClouds.Length; i++)
            {
                float wind = Ease(t, 0, 4.5f);
                Show(sandClouds[i], Mathf.Lerp(0.62f, 0.08f, wind) + Mathf.Sin(t * 0.6f + i) * 0.12f + i * 0.06f,
                    0.15f + i * 0.12f, width * (0.65f + i * 0.08f), height * 1.1f);
                sandClouds[i].color = new Color(0.85f, 0.66f, 0.36f, sheltered ? 0.15f : 0.19f + wind * 0.1f);
            }
            rocks[0].sprite = rocks[2].sprite = game.StoryRockSprite(true);
            for (int i = 0; i < 3; i++) Show(rocks[i], (sheltered ? -0.04f : 0.37f) + (i - 1) * 0.1f, i == 1 ? 0.46f : 0.1f,
                size * (i == 1 ? 2 : 1.1f), size * (i == 1 ? 0.85f : 1.8f), i == 1 ? 8 : 0);
            float move = Ease(t, 0, 5);
            Dino("blue", sheltered ? -0.12f : Mathf.Lerp(-0.26f, 0.16f, move), 0, sheltered ? Pose.Duck : Pose.Run);
            Dino("cole", sheltered ? 0.04f : Mathf.Lerp(-0.43f, -0.01f, move), 0, sheltered ? Pose.Duck : Pose.Run);
            Dino("kira", sheltered ? -0.29f : Mathf.Lerp(-0.6f, -0.18f, move), 0, sheltered ? Pose.Duck : Pose.Run);
            shake = sheltered ? Mathf.Sin(t * 0.7f) > 0.8f ? 4 : 0 : 2;
        }
        private void Mural(float t, bool emerging)
        {
            float reveal = Ease(t, 0.5f, 3.5f);
            Show(wall, 0.12f, emerging ? Mathf.Lerp(-0.32f, 0.3f, reveal) : 0.3f, width * 0.49f, height * 0.74f);
            float wallY = emerging ? Mathf.Lerp(-0.32f, 0.3f, reveal) : 0.3f;
            Dino("blue", -0.31f, 0, Pose.Idle, true, 0.78f); Dino(companion, -0.44f, 0, Pose.Idle, true, 0.74f);
            if (Shot == "map" || Shot == "carving" || Shot == "uncover")
            {
                for (int i = 0; i < 5; i++)
                {
                    float x = -0.055f + i * 0.087f, y = wallY + Mathf.Sin(i * 1.7f) * 0.12f;
                    route[i].sprite = art.Star;
                    Show(route[i], x, y, size * 0.25f, size * 0.25f);
                    route[i].color = new Color(1, 1, 1, Mathf.Clamp01((t - i * 0.4f) * 2));
                    if (i < 4) { Show(routeLines[i], x + 0.043f, y, width * 0.06f, 3, i % 2 == 0 ? 20 : -20); routeLines[i].color = RunnerStoryUi.Gold; }
                }
            }
            else
            {
                Show(guardianGlyph, 0.12f, wallY + 0.06f, size * 1.7f, size * 1.7f);
                guardianGlyph.color = new Color(0.82f, 0.86f, 0.6f, 0.9f);
                if (Shot == "warning")
                {
                    Show(childGlyph, 0.29f, wallY - 0.15f, size * 0.55f, size * 0.55f);
                    childGlyph.color = new Color(0.9f, 0.85f, 0.6f, 0.25f);
                    Show(fissure, 0.27f, wallY - 0.14f, size * 0.8f, size * 0.11f, -30);
                }
                if (Shot == "reveal")
                {
                    Show(childGlyph, 0.29f, wallY - 0.15f, size * 0.6f, size * 0.6f);
                    childGlyph.color = new Color(1, 1, 0.7f, reveal);
                    Show(routeLines[0], 0.23f, wallY - 0.09f, size * 0.45f, 4, -32); routeLines[0].color = RunnerStoryUi.Gold;
                }
                if (Shot == "history")
                    for (int i = 0; i < 3; i++)
                    {
                        route[i].sprite = game.StoryDinoFrames(i)[15];
                        Show(route[i], -0.04f + i * 0.12f, wallY - 0.2f, size * 0.32f, size * 0.32f);
                        route[i].color = RunnerStoryUi.Gold;
                    }
            }
            if (emerging) particleMode = 1;
            zoom = 1 + reveal * 0.07f; pan = -reveal * 0.025f;
        }
        private void Ruins(float t)
        {
            Show(wall, 0.29f, 0.28f, width * 0.35f, height * 0.7f);
            Show(tablet, 0.05f, 0.11f, size, size * 1.4f);
            float arrive = Ease(t, 0, 3.2f);
            Dino("sena", 0.1f, 0, Pose.Idle, false);
            Dino("blue", Mathf.Lerp(-0.5f, -0.13f, arrive), 0, arrive < 1 ? Pose.Run : Pose.Idle);
            Dino("kira", Mathf.Lerp(-0.65f, -0.3f, arrive), 0, arrive < 1 ? Pose.Run : Pose.Idle);
        }
        private void SearchingGuardian(float t)
        {
            float pace = Mathf.Sin(t * 0.65f) * 0.1f;
            RenderGuardian(0.11f + pace, 0.5f, 0.8f);
            for (int i = 0; i < 6; i++) { Show(tracks[i], -0.4f + i * 0.14f, 0, size * 0.13f, size * 0.14f); tracks[i].color = new Color(1, 1, 1, 0.5f); }
            particleMode = 2; pan = -pace * 0.2f; zoom = 1.04f;
        }
        private void Village(float t)
        {
            float walk = Ease(t, 0, 3.5f); pan = -walk * 0.07f;
            Dino("blue", Mathf.Lerp(-0.4f, 0.02f, walk), 0, walk < 1 ? Pose.Run : Pose.Idle);
            Dino("cole", Mathf.Lerp(-0.59f, -0.18f, walk), 0, walk < 1 ? Pose.Run : Pose.Idle);
            Dino("kira", Mathf.Lerp(-0.2f, 0.7f, Ease(t, 0.3f, 4)), 0.03f, Pose.Run, true, 0.8f);
        }
        private void Gathering(float t)
        {
            string[] ids = { "cole", "red", "yellow", "green", "kira", "sena" };
            Dino("blue", -0.04f);
            for (int i = 0; i < ids.Length; i++)
            {
                float end = i < 3 ? -0.45f + i * 0.13f : 0.14f + (i - 3) * 0.13f;
                float move = Ease(t, i * 0.35f, 2.7f + i * 0.35f);
                Dino(ids[i], Mathf.Lerp(i < 3 ? -0.7f : 0.7f, end, move), 0, move < 1 ? Pose.Run : Pose.Idle, i < 3, 0.76f);
            }
            zoom = 0.97f;
        }
        private void River(float t)
        {
            bool saving = Shot == "rescue-herd";
            Show(gap, 0.08f, -0.18f, width * 0.24f, height * 0.4f);
            for (int i = 0; i < 4; i++) Show(rocks[i], i < 2 ? -0.07f - i * 0.07f : 0.23f + (i - 2) * 0.07f, -0.015f, size * 0.7f, size * 0.65f);
            if (saving)
            {
                tree.preserveAspect = false; // A long fallen trunk must span both banks, not shrink to the sprite's short side.
                Show(tree, -0.09f, 0.015f, size * 0.35f, width * 0.34f, -90);
            }
            Dino("blue", -0.28f, 0, Pose.Idle, true);
            string[] ids = { "red", "yellow", "green", "cole" };
            for (int i = 0; i < ids.Length; i++)
            {
                float move = saving ? Ease(t, i * 0.6f, 2.8f + i * 0.6f) : 0;
                Dino(ids[i], Mathf.Lerp(0.28f + i * 0.06f, -0.43f + i * 0.1f, move), saving ? Hop(t, i * 0.6f + 0.4f, i * 0.6f + 2.2f) * 0.12f : 0,
                    saving && move < 1 ? Pose.Run : Pose.Idle, false, 0.7f);
            }
            particleMode = 4;
        }
        private void Camp(float t)
        {
            Show(rocks[0], -0.3f, 0.1f, size * 2, size * 1.5f);
            Dino("cole", -0.16f, 0, Pose.Idle, true, 0.85f); Dino("red", -0.42f, 0, Pose.Idle, true, 0.6f);
            Dino("yellow", -0.32f, 0, Pose.Idle, true, 0.55f); Dino("green", -0.05f, 0, Pose.Idle, false, 0.6f);
            float leave = Ease(t, 0.6f, 5);
            Dino("blue", Mathf.Lerp(0.04f, 0.52f, leave), leave * 0.13f, Pose.Run);
            Dino("kira", Mathf.Lerp(-0.08f, 0.36f, leave), leave * 0.1f, Pose.Run);
            particleMode = 5;
        }
        private void SnowPass(float t)
        {
            float walk = Ease(t, 0, 5); particleMode = 2; drift = t * width * 0.05f;
            Dino("blue", Mathf.Lerp(-0.3f, 0.12f, walk), walk * 0.07f, Pose.Run);
            Dino("kira", Mathf.Lerp(-0.48f, -0.06f, walk), walk * 0.05f, Pose.Run);
            for (int i = 0; i < 3; i++) Show(rocks[i], 0.12f + i * 0.14f, 0.005f + i * 0.04f, size * 0.8f, size * 0.7f);
            pan = -walk * 0.05f;
        }
        private void Rescue(float t)
        {
            bool saved = Shot == "rescue-complete"; particleMode = 2;
            Show(gap, 0.04f, -0.14f, width * 0.26f, height * 0.42f);
            Show(rocks[0], -0.15f, 0.01f, size, size * 0.65f); Show(rocks[1], 0.25f, 0.015f, size * 1.5f, size * 0.8f);
            float outward = Ease(t, 0.3f, 2.8f), home = Ease(t, 3, 5.8f);
            float blueX = saved ? Mathf.Lerp(-0.29f, 0.26f, outward) - home * 0.5f : Mathf.Lerp(-0.4f, -0.22f, Ease(t, 0, 2.8f));
            float blueY = saved ? (Hop(t, 0.3f, 2.8f) + Hop(t, 3, 5.8f)) * 0.33f : 0;
            Dino("blue", blueX, blueY, saved ? t < 5.8f ? Pose.Jump : Pose.Idle : t < 2.8f ? Pose.Run : Pose.Idle, !saved || t < 3);
            Dino("kira", -0.42f, 0, Pose.Idle, true, 0.85f);
            float babyX = saved ? 0.31f - home * 0.5f : 0.31f + Mathf.Sin(t * 16) * 0.002f;
            Put(hatchling, babyX, saved ? Hop(t, 3, 5.8f) * 0.3f : 0.015f, saved && t > 3 && home < 1 ? Pose.Run : Pose.Idle, !saved || t < 3, 0.57f);
            pan = -Ease(t, 0, 2) * 0.025f; zoom = 1.03f;
        }
        private void RenderGuardian(float x, float bow, float scale = 1)
        {
            guardian.gameObject.SetActive(true);
            float k = height * 0.83f / 320 * scale;
            guardian.anchoredPosition = new Vector2(x * width, floor - 7);
            guardian.localScale = new Vector3(-k, k, 1);
            float angle = bow * 66;
            neck.localRotation = Quaternion.Euler(0, 0, -angle);
            float radians = angle * Mathf.Deg2Rad;
            head.anchoredPosition = new Vector2(62 + Mathf.Sin(radians) * 184 + 23, 83 + Mathf.Cos(radians) * 184);
            head.localRotation = Quaternion.Euler(0, 0, angle * 0.18f);
            guardianBody.rectTransform.anchoredPosition = new Vector2(-14, 67 + Mathf.Sin(ShotTime * 2) * 2);
            guardianHead.color = guardianBody.color = Color.white;
        }
        private void ApproachingGuardian(float t)
        {
            float approach = Ease(t, 0, 4); RenderGuardian(Mathf.Lerp(0.67f, 0.31f, approach), 0.1f + approach * 0.22f);
            Dino("blue", -0.16f); Dino("cole", Mathf.Lerp(-0.65f, -0.36f, Ease(t, 0.6f, 3.2f)), 0, t < 3.2f ? Pose.Run : Pose.Idle);
            Put(hatchling, -0.015f, 0, Pose.Idle, true, 0.55f);
            particleMode = 3; shake = (1 - approach) * Mathf.Max(0, Mathf.Sin(t * 9)) * 8; zoom = 1.02f;
        }
        private void Reunion(float t)
        {
            float run = Ease(t, 0.3f, 3.5f); RenderGuardian(0.33f, Ease(t, 1, 4) * 0.94f);
            Dino("blue", -0.29f); Dino("kira", -0.44f, 0, Pose.Idle, true, 0.85f);
            Put(hatchling, Mathf.Lerp(-0.12f, 0.16f, run), Hop(t, 1.2f, 2) * 0.05f, run < 1 ? Pose.Run : Pose.Idle, true, 0.58f);
            particleMode = 4; zoom = 0.96f;
        }
        private void Gate(float t, bool open)
        {
            gate.gameObject.SetActive(true); gate.anchoredPosition = new Vector2(width * 0.29f, floor + height * 0.37f);
            float k = height * 0.87f / 340; gate.localScale = Vector3.one * k;
            RunnerShopPresentation.Position(gateFrame.transform, 0, 0, 300, 340);
            float separation = open ? Ease(t, 1.2f, 4.2f) : 0;
            RunnerShopPresentation.Position(doorLeft.transform, -47 - separation * 140, -28, 94, 260);
            RunnerShopPresentation.Position(doorRight.transform, 47 + separation * 140, -28, 94, 260);
            doorLeft.color = doorRight.color = new Color(1, 1, 1, 1 - separation);
        }
        private void OpeningGate(float t)
        {
            Gate(t, true); RenderGuardian(0.12f + Ease(t, 0, 1.6f) * 0.06f, (1 - Ease(t, 0.4f, 2)) * 0.6f, 0.87f);
            Dino("blue", -0.25f); Dino("cole", -0.43f, 0, Pose.Idle, true, 0.8f);
            Put(hatchling, 0.05f, 0, Pose.Idle, true, 0.5f);
            particleMode = 3; Sunlight(Ease(t, 1.8f, 4.7f) * 0.32f); shake = Hop(t, 1, 3.5f) * 4;
        }
        private void Migration(float t)
        {
            Gate(8, true); Sunlight(0.3f); particleMode = 4;
            string[] ids = { "blue", "kira", "cole", "sena", "red", "yellow", "green", "mono", "loki", "kuro" };
            for (int i = 0; i < ids.Length; i++)
            {
                float move = Ease(t, i * 0.22f, 5 + i * 0.15f);
                Dino(ids[i], Mathf.Lerp(-0.62f - i * 0.06f, 0.38f - i * 0.085f, move), 0, move < 1 ? Pose.Run : Pose.Idle, true, i == 0 ? 0.92f : 0.65f);
            }
            zoom = 0.94f; pan = -Ease(t, 0, 6) * 0.04f;
        }
        private void NewBeginning(float t)
        {
            NestSet(false); Sunlight(0.16f);
            // Close framing on the central nest; the egg visibly splits before the baby appears.
            float crack = Shot == "hatch" ? Ease(t, 0.4f, 2.4f) : Shot == "epilogue" ? 1 : 0;
            if (Shot != "epilogue" && (Shot != "hatch" || t < 2.1f))
                Show(fissure, 0.12f, 0.1f, size * 0.3f * crack, size * 0.12f, 85);
            eggs[1].rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * (Shot == "hatch" ? 18 : 7)) * (Shot == "egg" ? 7 : 15) * (1 - crack));
            if (Shot == "hatch" || Shot == "epilogue")
            {
                bool hatched = t >= 2.1f || Shot == "epilogue";
                eggs[1].gameObject.SetActive(!hatched); fissure.gameObject.SetActive(!hatched);
                if (hatched)
                {
                    float emerge = Shot == "epilogue" ? 1 : Ease(t, 2.1f, 3.6f);
                    Put(baby, 0.12f - emerge * 0.12f, Hop(t, 2.1f, 3.6f) * 0.18f, emerge < 1 ? Pose.Jump : Pose.Idle, false, 0.43f);
                    for (int i = 0; i < shards.Length && Shot == "hatch"; i++)
                    {
                        float flight = Mathf.Clamp01((t - 2.1f) / 1.3f);
                        Show(shards[i], 0.12f + (i - 3.5f) * 0.022f * flight, 0.12f + Mathf.Sin(flight * Mathf.PI) * 0.14f - flight * 0.13f,
                            size * 0.1f, size * 0.12f, i * 40 + flight * 100);
                        shards[i].color = new Color(1, 1, 1, 1 - flight);
                    }
                }
            }
            Dino("blue", -0.16f, 0, Pose.Idle, true, 0.95f);
            if (Shot != "egg") Dino("kira", 0.38f, Hop(t, 3, 4.2f) * 0.12f, t > 3 && t < 4.2f ? Pose.Jump : Pose.Idle, false, 0.9f);
            zoom = 1.08f; pan = -0.04f; particleMode = 4;
        }
        private void Sunlight(float opacity)
        {
            for (int i = 0; i < rays.Length; i++)
            {
                Image ray = rays[i]; ray.gameObject.SetActive(true);
                RunnerShopPresentation.Position(ray.transform, width * (0.2f + i * 0.07f), height * 0.1f, width * 0.12f, height * 1.7f);
                ray.rectTransform.localRotation = Quaternion.Euler(0, 0, -28 + i * 3);
                ray.color = new Color(1, 0.89f, 0.54f, opacity * (0.7f + Mathf.Sin(ShotTime + i) * 0.1f));
            }
        }
        private void RenderScenery()
        {
            float ppu = height / 11.25f;
            RunnerShopPresentation.Position(soil.transform, pan * width, floor - height * 0.3f, width * 4, height * 0.6f);
            foreach (Scenery layer in layers)
            {
                if (!layer.Tiles[0].gameObject.activeSelf) continue;
                var spec = layer.Spec;
                float h = spec.sprite.rect.height / spec.sprite.pixelsPerUnit * spec.height * ppu;
                float w = (spec.sprite.rect.width - 1) / spec.sprite.pixelsPerUnit * spec.height * ppu;
                float offset = pan * width * spec.speed - drift * spec.speed;
                for (int i = 0; i < layer.Tiles.Length; i++)
                {
                    layer.Tiles[i].gameObject.SetActive(!spec.stationary || i == 0);
                    float x = spec.stationary ? spec.x * ppu : (i - 2) * w + Mathf.Repeat(offset, w);
                    RunnerShopPresentation.Position(layer.Tiles[i].transform, x, floor + (spec.bottom - RunnerRules.GroundY) * ppu + h * 0.5f, w + 2, h);
                }
            }
        }
        private void RenderParticles()
        {
            float t = ShotTime;
            for (int i = 0; i < particles.Length; i++)
            {
                Image image = particles[i]; float seed = Mathf.Repeat(i * 0.618034f, 1);
                float x, y, w, h, angle; Color tint;
                image.sprite = particleMode == 0 ? art.Ribbon : art.Dust;
                switch (particleMode)
                {
                    case 1:
                        x = (0.7f - Mathf.Repeat(t * (0.3f + seed * 0.5f) + seed, 1.4f)) * width;
                        y = (Mathf.Repeat(i * 0.317f + t * 0.03f, 1) - 0.5f) * height;
                        w = 28 + seed * 150; h = 2 + i % 3; angle = -12;
                        tint = new Color(0.93f, 0.77f, 0.4f, 0.25f + seed * 0.2f); break;
                    case 2:
                        x = (0.65f - Mathf.Repeat(t * (0.1f + seed * 0.3f) + seed, 1.3f)) * width;
                        y = (0.55f - Mathf.Repeat(t * (0.08f + seed * 0.12f) + i * 0.21f, 1.1f)) * height;
                        w = h = 3 + seed * 8; angle = t * 20; tint = new Color(0.9f, 0.97f, 1, 0.6f); break;
                    case 3:
                        float flight = t - i * 0.045f;
                        x = (seed - 0.5f) * width + Mathf.Sin(i * 2) * flight * 18;
                        y = floor + 15 + flight * (80 + seed * 100) - flight * flight * 90;
                        w = h = 5 + seed * 14; angle = flight * 150;
                        tint = new Color(0.64f, 0.62f, 0.43f, flight > 0 && flight < 2.7f ? 0.8f : 0); break;
                    case 4:
                        x = (seed - 0.5f) * width; y = floor + Mathf.Repeat(t * 12 + i * 13, height * 0.9f);
                        w = h = 3 + seed * 5; angle = 0; tint = new Color(1, 0.89f, 0.53f, 0.15f + Mathf.Sin(t + i) * 0.12f); break;
                    case 5:
                        x = -width * 0.25f + Mathf.Sin(i * 3 + t) * 18;
                        y = floor + Mathf.Repeat(t * 38 + i * 7, height * 0.25f);
                        w = 9 + seed * 12; h = 15 + seed * 10; angle = 0;
                        tint = new Color(1, 0.58f + seed * 0.3f, 0.24f, 0.5f * (1 - (y - floor) / (height * 0.25f))); break;
                    default:
                        x = (seed - 0.5f) * width + Mathf.Sin(t + i) * 12;
                        y = (0.6f - Mathf.Repeat(t * 0.025f + i * 0.231f, 1.2f)) * height;
                        w = 5 + seed * 6; h = w * 1.5f; angle = t * 18 + i * 32; tint = new Color(1, 1, 1, 0.32f); break;
                }
                image.gameObject.SetActive(tint.a > 0.01f);
                RunnerShopPresentation.Position(image.transform, x, y, w, h);
                image.color = tint; image.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            }
        }
        public void Dispose() { /* Images belong to the cutscene hierarchy; sprites belong to the game/art owners. */ }
    }
}
