using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DinoRush
{
    [DisallowMultipleComponent]
    public sealed class ForestRunnerGame : MonoBehaviour
    {
        public enum RunState { Ready, Running, Paused, GameOver, AwaitingStart }
        private const string HighScoreKey = "DinoRush.Forest.HighScore";

        [Header("Player and original forest artwork")]
        [SerializeField] private Texture2D dinoSheet;
        [SerializeField] private RunnerDinoRoster dinoRoster;
        [SerializeField] private RunnerMapCatalog mapCatalog;
        [SerializeField] private Texture2D sky;
        [SerializeField] private Texture2D mountains;
        [SerializeField] private Texture2D distantForest;
        [SerializeField] private Texture2D middleForest;
        [SerializeField] private Texture2D tallTrees;
        [SerializeField] private Texture2D nearForest;
        [SerializeField] private Texture2D ground;
        [Tooltip("Map catalog order: seamless subsoil below each running surface.")]
        [SerializeField] private Sprite[] soilSprites;
        [SerializeField] private Texture2D[] obstacleTextures;
        [SerializeField] private Material spriteMaterial;

        [System.Serializable]
        private sealed class Layer
        {
            public Transform[] tiles;
            public float width; // Repeat stride; render quads overlap by one texture pixel.
            public float speed;
            public float offset;
            public bool stationary;
        }

        [System.Serializable]
        private sealed class Obstacle
        {
            public SpriteRenderer renderer;
            public float width;
            public float height;
            public bool active;
            public bool passed;
            public bool hitPlayer;
            public bool overhead;
        }

        [System.Serializable]
        private sealed class Dust
        {
            public SpriteRenderer renderer;
            public Vector2 velocity;
            public float life;
            public float duration;
            public Color startColor;
            public float size;
            public float spin;
            public float gravity;
            public bool puff;
        }

        private enum DirtEmission { Running, Takeoff, Landing, Crash }

        private readonly List<Sprite> generatedSprites = new List<Sprite>();
        private readonly List<Texture2D> generatedTextures = new List<Texture2D>();
        private readonly Dictionary<int, Sprite[]> dinoFrameCache = new Dictionary<int, Sprite[]>();
        private readonly Dictionary<int, List<Layer>> mapLayerCache = new Dictionary<int, List<Layer>>();
        private readonly Dictionary<Transform, Vector3> skyBaseScales = new Dictionary<Transform, Vector3>();
        private Transform trailSoil;
        private readonly Dictionary<int, Sprite> soilSpriteCache = new Dictionary<int, Sprite>();
        private float soilOffset;
        private List<Layer> forestLayers;
        private int activeMapIndex;
        private Color mapDustTint = Color.white;
        [Header("Saved scene objects")]
        [SerializeField] private Camera gameCamera;
        [SerializeField] private SpriteRenderer dinoRenderer;
        [SerializeField] private Transform playerShadow;
        [SerializeField] private Sprite[] dinoFrames;
        [SerializeField] private List<Layer> layers = new List<Layer>();
        [SerializeField] private List<Obstacle> obstacles = new List<Obstacle>();
        [SerializeField] private List<Dust> dust = new List<Dust>();
        [SerializeField] private List<SpriteRenderer> motes = new List<SpriteRenderer>();
        [SerializeField] private List<Sprite> obstacleSprites = new List<Sprite>();
        [SerializeField] private RunnerHud hud;
        [Header("Pixel dirt effects")]
        [SerializeField] private Sprite[] dirtSprites;
        [SerializeField, Range(0.04f, 0.25f)] private float runningDirtInterval = 0.10f;
        [SerializeField, Range(1, 6)] private int runningDirtCount = 3;
        [SerializeField, Range(8, 24)] private int landingDirtCount = 18;
        private RunnerPlayer player;
        private RunnerPlayerTrail playerTrail;
        private readonly RunnerHealth health = new RunnerHealth();
        private RunnerAudio audioCues;
        private RunnerEggCollectibles eggs;
        private RunnerEncounterDirector encounterDirector;
        private Sprite canopySprite;
        private float nextEncounterDistance;
        private int announcedLevel = 1;
        private int announcedLap;
        private float stageBannerTime;
        private Sprite pixel;
        private Transform environmentGroup;
        private Transform playerGroup;
        private Transform obstacleGroup;
        private Transform dustGroup;
        private Transform motesGroup;
        private float elapsed;
        private float distance;
        private float spawnTimer;
        private float dustTimer;
        private float accumulator;
        private float timeSinceCrash;
        private float shake;
        private int best;
        private int bestAtRunStart;
        private int cleared;
        private int milestone;
        private int lastHudScore = -1;
        private int screenWidth;
        private int screenHeight;
        private Rect screenSafeArea;
        private bool initialized;
        private int preparedInputFrame;
        private bool keyboardDuckHeld;
        private bool pointerDuckHeld;
        private bool touchGestureActive, touchGestureDucking;
        private Vector2 touchGestureStart;
        private int nextDirtParticle;
        private static readonly Color[] DirtColors =
        {
            new Color32(100, 76, 48, 255), new Color32(137, 105, 64, 255),
            new Color32(173, 142, 91, 255), new Color32(192, 163, 111, 255)
        };

        public RunState State { get; private set; }
        public int Score => Mathf.FloorToInt(distance);
        public int HighScore => best;
        public int ObstaclesCleared => cleared;
        public int EggsCollected => eggs != null ? eggs.Collected : 0;
        public RunnerShopProgress Shop { get; private set; }
        public int Lives => health.Lives;
        public bool IsInvulnerable => health.IsInvulnerable;
        public int Level => RunnerRules.Level(elapsed);
        public float CurrentSpeed => RunnerRules.Speed(elapsed);
        public RunnerDinoRoster DinoRoster => dinoRoster;
        public RunnerMapCatalog MapCatalog => mapCatalog;
        public int SelectedMapIndex { get; private set; }
        public string SelectedMapName => mapCatalog != null && mapCatalog.Count > 0
            ? mapCatalog[SelectedMapIndex].displayName : "Forest";
        public int SelectedDinoIndex { get; private set; }
        public string SelectedDinoName => dinoRoster != null && dinoRoster.Count > 0
            ? dinoRoster[SelectedDinoIndex].DisplayName : "Blue";
        public bool HasAuthoredScene => gameCamera != null && dinoRenderer != null &&
            playerShadow != null && dinoFrames != null && dinoFrames.Length == 24 && hud != null && hud.IsBuilt;

#if UNITY_EDITOR
        public void AssignMainMenu(ForestMainMenu menu)
        {
            hud.SetMainMenu(menu);
            hud.ShowReady(0);
            gameCamera.transform.position = new Vector3(0, 1.4f, -10);
            SetSkyFraming(true);
            dinoRenderer.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
            foreach (Obstacle obstacle in obstacles) obstacle.renderer.gameObject.SetActive(false);
        }
#endif

        private void Awake()
        {
            if (!AssetsAssigned())
            {
                Debug.LogError("Forest runner is missing art references. Open Assets/Scenes/ForestRun.unity.", this);
                enabled = false;
                return;
            }

            best = Mathf.Max(0, PlayerPrefs.GetInt(HighScoreKey, 0));
            // Older, unbaked scenes still work; the saved ForestRun scene takes this path zero times.
            if (!HasAuthoredScene) CreateSceneObjects();
            canopySprite = RunnerCanopyArt.Create(generatedSprites, generatedTextures);
            forestLayers = layers;
            if (mapCatalog != null && mapCatalog.Count > 0)
                SelectedMapIndex = mapCatalog.Find(PlayerPrefs.GetString(RunnerMapCatalog.SelectionKey, "forest"));
            ApplyPixelFiltering();
            player = new RunnerPlayer(dinoRenderer, dinoFrames, playerShadow);
            var trailObject = new GameObject("Equipped dino trail");
            trailObject.transform.SetParent(transform, false);
            playerTrail = trailObject.AddComponent<RunnerPlayerTrail>();
            playerTrail.Initialize(dinoRenderer);
            Shop = new RunnerShopProgress(dinoRoster);
            if (dinoRoster != null && dinoRoster.Count > 0)
            {
                SelectedDinoIndex = dinoRoster.Find(PlayerPrefs.GetString(RunnerDinoRoster.SelectionKey, "blue"));
                if (!Shop.IsUnlocked(SelectedDinoIndex))
                {
                    SelectedDinoIndex = 0;
                    PlayerPrefs.SetString(RunnerDinoRoster.SelectionKey, dinoRoster[0].Id);
                    PlayerPrefs.Save();
                }
                player.SetFrames(DinoFrames(SelectedDinoIndex));
            }
            foreach (Obstacle obstacle in obstacles)
            {
                obstacle.active = false;
                obstacle.renderer.gameObject.SetActive(false);
            }
            foreach (Dust particle in dust)
            {
                particle.life = 0f;
                particle.renderer.gameObject.SetActive(false);
            }
            audioCues = new RunnerAudio(gameObject);
            eggs = new RunnerEggCollectibles(transform, spriteMaterial, position =>
            {
                Shop.CollectEgg();
                audioCues.Pickup();
                hud.CollectEgg(eggs.Collected, position, gameCamera);
            });
            hud.InitializeShop(Shop, audioCues);
            hud.Initialize(PrimaryAction, TogglePause, Jump, ToggleMute, RestartRun, ReturnToMenu, SetDuckHeld, eggs.EggSprite);
            hud.UpdateLives(Lives);
            hud.InitializeDinos(dinoRoster, SelectedDinoIndex, SelectDino);
            hud.InitializeMaps(mapCatalog, SelectedMapIndex, SelectMap, PrepareRun);
            hud.SetSound(audioCues.Muted);
            hud.UpdateScore(0, best, 0, RunnerRules.Speed(0));
            hud.ShowReady(best);
            State = RunState.Ready;
            SetSoil(0);
            UpdateViewport();
            initialized = true;
        }

        private void CreateSceneObjects()
        {
            CreateCamera();
            environmentGroup = Group("Forest environment", transform);
            playerGroup = Group("Player", transform);
            obstacleGroup = Group("Obstacle pool", transform);
            Transform effects = Group("Forest effects", transform);
            dustGroup = Group("Trail dust pool", effects);
            motesGroup = Group("Floating motes", effects);
            BuildWorld();
            hud = new RunnerHud(transform);
        }

        private static Transform Group(string groupName, Transform parent)
        {
            Transform group = new GameObject(groupName).transform;
            group.SetParent(parent, false);
            return group;
        }

        private void ApplyPixelFiltering()
        {
            dinoSheet.filterMode = sky.filterMode = mountains.filterMode = FilterMode.Point;
            distantForest.filterMode = middleForest.filterMode = tallTrees.filterMode = FilterMode.Point;
            nearForest.filterMode = ground.filterMode = FilterMode.Point;
            foreach (Texture2D texture in obstacleTextures) texture.filterMode = FilterMode.Point;
            if (dinoRoster != null)
                for (int i = 0; i < dinoRoster.Count; i++)
                {
                    dinoRoster[i].Sheet.filterMode = FilterMode.Point;
                    if (dinoRoster[i].Portrait != null) dinoRoster[i].Portrait.texture.filterMode = FilterMode.Point;
                }
        }

        public void SelectDino(int index)
        {
            if (!initialized || State != RunState.Ready || dinoRoster == null || index < 0 || index >= dinoRoster.Count) return;
            if (!Shop.IsUnlocked(index)) return;
            player.SetFrames(DinoFrames(index));
            SelectedDinoIndex = index;
            hud.SetDinoSelection(index);
            if (PlayerPrefs.GetString(RunnerDinoRoster.SelectionKey, "") != dinoRoster[index].Id)
            {
                PlayerPrefs.SetString(RunnerDinoRoster.SelectionKey, dinoRoster[index].Id);
                PlayerPrefs.Save();
            }
        }

        private Sprite[] DinoFrames(int index)
        {
            if (dinoFrameCache.TryGetValue(index, out Sprite[] cached)) return cached;
            Texture2D sheet = dinoRoster[index].Sheet;
            if (sheet == dinoSheet) return dinoFrameCache[index] = dinoFrames;
            float cell = sheet.width / 24f;
            var frames = new Sprite[24];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = Sprite.Create(sheet, new Rect(i * cell, 0, cell, sheet.height),
                    new Vector2(0.5f, 2f / 24f), cell, 0, SpriteMeshType.FullRect);
                frames[i].name = dinoRoster[index].DisplayName + " dino frame " + i;
                generatedSprites.Add(frames[i]);
            }
            dinoFrameCache[index] = frames;
            return frames;
        }

        public void SelectMap(int index)
        {
            if (!initialized || State != RunState.Ready || mapCatalog == null || index < 0 || index >= mapCatalog.Count) return;
            SelectedMapIndex = index;
            hud.SetMapSelection(index);
            PlayerPrefs.SetString(RunnerMapCatalog.SelectionKey, mapCatalog[index].id);
            PlayerPrefs.Save();
        }

        private void ApplyMap(int index)
        {
            if (mapCatalog == null || mapCatalog.Count == 0) return;
            foreach (Layer layer in layers)
                foreach (Transform tile in layer.tiles) tile.gameObject.SetActive(false);
            if (index == 0) layers = forestLayers;
            else if (!mapLayerCache.TryGetValue(index, out List<Layer> cached))
            {
                layers = new List<Layer>();
                Transform parent = Group(mapCatalog[index].displayName + " map", transform.Find("Forest environment"));
                foreach (RunnerMapCatalog.MapLayer spec in mapCatalog[index].layers)
                {
                    float width = (spec.sprite.rect.width - 1) / spec.sprite.pixelsPerUnit * spec.height;
                    var layer = new Layer { tiles = new Transform[spec.stationary ? 1 : 3], width = width,
                        speed = spec.speed, stationary = spec.stationary, offset = width * 0.25f };
                    for (int i = 0; i < layer.tiles.Length; i++)
                    {
                        SpriteRenderer renderer = Renderer(mapCatalog[index].displayName + " / " + spec.sprite.name + " tile " + i,
                            spec.sprite, spec.order, parent);
                        renderer.transform.localScale = new Vector3(spec.height, spec.height, 1f);
                        renderer.transform.position = new Vector3(spec.stationary ? spec.x : (i - 1) * width - layer.offset, spec.bottom, 0f);
                        layer.tiles[i] = renderer.transform;
                    }
                    layers.Add(layer);
                }
                mapLayerCache[index] = layers;
            }
            else layers = cached;
            foreach (Layer layer in layers)
                foreach (Transform tile in layer.tiles) tile.gameObject.SetActive(true);
            activeMapIndex = index;
            RunnerMapCatalog.Map map = mapCatalog[index];
            mapDustTint = map.dustTint;
            gameCamera.backgroundColor = map.skyColor;
            SetSoil(index);
            foreach (SpriteRenderer mote in motes)
            {
                Color color = map.moteColor;
                color.a = mote.color.a;
                mote.color = color;
            }
            Scroll(0f);
            FitSceneryToViewport();
        }

        private void SetSkyFraming(bool mainMenu)
        {
            if (activeMapIndex != 0) return;
            foreach (Transform tile in layers[0].tiles)
            {
                Vector3 position = tile.position;
                position.y = -8.775f + (mainMenu ? 1.4f : 0f);
                tile.position = position;
            }
        }

#if UNITY_EDITOR
        // Called explicitly by the editor authoring tool, never on edit-mode updates or reloads.
        public IReadOnlyList<Object> CreateEditorScene()
        {
            if (Application.isPlaying || HasAuthoredScene || transform.childCount > 0)
                throw new System.InvalidOperationException("Scene objects already exist; they will not be overwritten.");
            if (!AssetsAssigned()) throw new System.InvalidOperationException("Assign the forest art before baking the scene.");
            CreateSceneObjects();
            gameCamera.rect = new Rect(0, 0, 1, 1);
            gameCamera.aspect = 16f / 9f;
            gameCamera.orthographicSize = 5.625f;
            hud.UpdateScore(0, 0, 0, RunnerRules.Speed(0));
            hud.ShowReady(0);

            // Stage actual pool members on the trail so obstacle art is visible in edit mode.
            // Awake hides these until their first runtime spawn.
            for (int i = 0; i < 3; i++)
            {
                SpriteRenderer obstacle = obstacles[i].renderer;
                obstacle.sprite = obstacleSprites[i];
                obstacle.transform.position = new Vector3(1.5f + i * 3.4f, RunnerRules.GroundY, 0);
                obstacle.transform.localScale = Vector3.one * 1.05f;
                obstacle.gameObject.SetActive(true);
            }
            var assets = new List<Object>();
            assets.AddRange(generatedTextures);
            assets.AddRange(generatedSprites);
            return assets;
        }
#endif

        private bool AssetsAssigned()
        {
            if (dinoSheet == null || sky == null || mountains == null || distantForest == null ||
                middleForest == null || tallTrees == null || nearForest == null || ground == null ||
                spriteMaterial == null || obstacleTextures == null || obstacleTextures.Length == 0)
                return false;
            foreach (Texture2D texture in obstacleTextures)
                if (texture == null) return false;
            return true;
        }

        private void CreateCamera()
        {
            gameCamera = new GameObject("Forest camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            gameCamera.transform.SetParent(transform, false);
            gameCamera.tag = "MainCamera";
            gameCamera.transform.position = new Vector3(0, 0, -10);
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = 5.625f;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = new Color32(118, 184, 201, 255);
            gameCamera.nearClipPlane = 0.1f;
            gameCamera.farClipPlane = 100f;
            gameCamera.allowHDR = false;
            UpdateViewport();
        }

        private void UpdateViewport()
        {
            screenWidth = Screen.width;
            screenHeight = Screen.height;
            screenSafeArea = Screen.safeArea;
            RunnerScreenLayout layout = RunnerScreenLayout.Calculate(screenWidth, screenHeight, screenSafeArea);
            ApplyScreenLayout(layout);
        }

        public void ApplyScreenLayout(RunnerScreenLayout layout)
        {
            gameCamera.rect = layout.CameraRect;
            gameCamera.orthographicSize = layout.OrthographicSize;
            FitSceneryToViewport();
            hud?.ApplyScreenLayout(layout);
        }

        private void FitSceneryToViewport()
        {
            if (layers.Count == 0) return;
            float extraSky = Mathf.Max(0f, gameCamera.orthographicSize - 5.625f);
            foreach (Transform tile in layers[0].tiles)
            {
                if (!skyBaseScales.TryGetValue(tile, out Vector3 original))
                    skyBaseScales[tile] = original = tile.localScale;
                float spriteHeight = tile.GetComponent<SpriteRenderer>().sprite.bounds.size.y;
                tile.localScale = new Vector3(original.x, original.y + extraSky / spriteHeight, original.z);
            }
            FitSoil();
        }

        private void SetSoil(int index)
        {
            if (trailSoil == null) trailSoil = transform.Find("Forest environment/Earth below the trail");
            if (trailSoil == null) return;
            Sprite sprite = soilSprites != null && index < soilSprites.Length ? soilSprites[index] : null;
            if (sprite == null && !soilSpriteCache.TryGetValue(index, out sprite))
            {
                Color color = mapCatalog != null && index < mapCatalog.Count ? mapCatalog[index].soilColor : new Color32(19, 48, 49, 255);
                string id = mapCatalog != null && index < mapCatalog.Count ? mapCatalog[index].id : "forest";
                Texture2D texture = RunnerSoilArt.CreateTexture(color, id);
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 1f), RunnerSoilArt.PixelsPerUnit, 0, SpriteMeshType.FullRect);
                generatedTextures.Add(texture);
                generatedSprites.Add(sprite);
                soilSpriteCache[index] = sprite;
            }
            var renderer = trailSoil.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            trailSoil.localScale = Vector3.one;
            soilOffset = 0f;
            FitSoil();
        }

        private void FitSoil()
        {
            if (trailSoil == null) return;
            var renderer = trailSoil.GetComponent<SpriteRenderer>();
            float width = renderer.sprite.bounds.size.x;
            // Extend the canvas, not the pixels. Overscan keeps both edges covered while scrolling/shaking.
            renderer.size = new Vector2(24f + width * 2f,
                Mathf.Max(6f, RunnerSoilArt.Top + gameCamera.orthographicSize + 1f));
            trailSoil.localPosition = new Vector3(-soilOffset, RunnerSoilArt.Top, 0f);
        }

        private void BuildWorld()
        {
            var white = new Texture2D(1, 1) { name = "Generated dust pixel", filterMode = FilterMode.Point };
            white.SetPixel(0, 0, Color.white);
            white.Apply();
            generatedTextures.Add(white);
            pixel = MakeSprite(white, new Vector2(0.5f, 0.5f));

            AddLayer(sky, 14.4f, -8.775f, 0.015f, -100);
            AddLayer(mountains, 14f, -3.12f, 0.055f, -90);
            AddLayer(distantForest, 14f, -3.12f, 0.11f, -80);
            AddLayer(middleForest, 14f, -3.12f, 0.22f, -70);
            AddLayer(tallTrees, 7f, -3.1f, 0.37f, -60);
            AddLayer(nearForest, 14f, -3.12f, 0.49f, -50);

            SpriteRenderer soil = Renderer("Earth below the trail", pixel, -5, environmentGroup);
            soil.color = new Color32(19, 48, 49, 255);
            soil.transform.position = new Vector3(0, -6.5f, 0);
            soil.transform.localScale = new Vector3(24, 6, 1);
            SetSoil(0);
            AddLayer(ground, 1f, -3.96f, 1f, 0);

            var shadowTexture = new Texture2D(32, 8) { name = "Dino shadow", filterMode = FilterMode.Bilinear };
            for (int y = 0; y < 8; y++)
            for (int x = 0; x < 32; x++)
            {
                float radius = new Vector2((x - 15.5f) / 16f, (y - 3.5f) / 4f).magnitude;
                shadowTexture.SetPixel(x, y, new Color(0.02f, 0.08f, 0.08f, Mathf.Clamp01(1f - radius) * 0.65f));
            }
            shadowTexture.Apply();
            generatedTextures.Add(shadowTexture);
            SpriteRenderer shadow = Renderer("Dino ground shadow", MakeSprite(shadowTexture, Vector2.one * 0.5f), 2, playerGroup);
            shadow.transform.position = new Vector3(RunnerRules.PlayerX, RunnerRules.GroundY - 0.025f, 0);
            playerShadow = shadow.transform;

            var frames = new Sprite[24];
            dinoSheet.filterMode = FilterMode.Point;
            float cell = dinoSheet.width / 24f;
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = Sprite.Create(dinoSheet, new Rect(i * cell, 0, cell, dinoSheet.height),
                    new Vector2(0.5f, 2f / 24f), cell, 0, SpriteMeshType.FullRect);
                frames[i].name = "Blue dino frame " + i;
                generatedSprites.Add(frames[i]);
            }
            dinoFrames = frames;
            dinoRenderer = Renderer("Blue dino player", frames[0], 20, playerGroup);
            player = new RunnerPlayer(dinoRenderer, frames, playerShadow);

            foreach (Texture2D texture in obstacleTextures)
                obstacleSprites.Add(MakeSprite(texture, new Vector2(0.5f, 0f)));
            for (int i = 0; i < 10; i++)
            {
                SpriteRenderer obstacle = Renderer("Pooled obstacle " + i, obstacleSprites[0], 15, obstacleGroup);
                obstacle.gameObject.SetActive(false);
                obstacles.Add(new Obstacle { renderer = obstacle });
            }

            var dirtAssets = new List<Object>();
            dirtSprites = RunnerDirtArt.CreateSprites(dirtAssets);
            foreach (Object asset in dirtAssets)
            {
                if (asset is Sprite sprite) generatedSprites.Add(sprite);
                else if (asset is Texture2D texture) generatedTextures.Add(texture);
            }
            for (int i = 0; i < 36; i++)
            {
                SpriteRenderer particle = Renderer("Trail dust " + i, dirtSprites[i % dirtSprites.Length], 25, dustGroup);
                particle.gameObject.SetActive(false);
                dust.Add(new Dust { renderer = particle });
            }
            for (int i = 0; i < 22; i++)
            {
                SpriteRenderer mote = Renderer("Forest mote " + i, pixel, -10, motesGroup);
                mote.transform.position = new Vector3(Random.Range(-10f, 10f), Random.Range(-2.5f, 4.6f), 0);
                mote.transform.localScale = Vector3.one * Random.Range(0.025f, 0.06f);
                mote.color = new Color(0.85f, 1f, 0.64f, Random.Range(0.15f, 0.4f));
                motes.Add(mote);
            }
        }

        private Sprite MakeSprite(Texture2D texture, Vector2 pivot)
        {
            texture.filterMode = FilterMode.Point;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                pivot, texture.height, 0, SpriteMeshType.FullRect);
            sprite.name = texture.name + " sprite";
            generatedSprites.Add(sprite);
            return sprite;
        }

        private SpriteRenderer Renderer(string objectName, Sprite sprite, int order, Transform parent)
        {
            var renderer = new GameObject(objectName, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.sharedMaterial = spriteMaterial;
            return renderer;
        }

        private void AddLayer(Texture2D texture, float height, float bottom, float speed, int order)
        {
            Sprite sprite = MakeSprite(texture, new Vector2(0.5f, 0));
            float width = (sprite.rect.width - 1) / sprite.pixelsPerUnit * height;
            var layer = new Layer { tiles = new Transform[3], width = width, speed = speed };
            Transform group = Group(texture.name + " parallax", environmentGroup);
            for (int i = 0; i < 3; i++)
            {
                Transform tile = Renderer(texture.name + " tile " + i, sprite, order, group).transform;
                tile.localScale = new Vector3(height, height, 1);
                tile.position = new Vector3((i - 1) * width, bottom, 0);
                layer.tiles[i] = tile;
            }
            layers.Add(layer);
        }

        private void Update()
        {
            if (!initialized) return;
            if (Screen.width != screenWidth || Screen.height != screenHeight || Screen.safeArea != screenSafeArea) UpdateViewport();
            ReadInput();
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            hud.Tick(dt);
            if (State == RunState.Paused) return;

            if (State == RunState.Running)
            {
                accumulator += dt;
                while (accumulator >= RunnerRules.SimulationStep && State == RunState.Running)
                {
                    Step(RunnerRules.SimulationStep);
                    accumulator -= RunnerRules.SimulationStep;
                }
                if (Score != lastHudScore)
                {
                    lastHudScore = Score;
                    hud.UpdateScore(Score, best, cleared, RunnerRules.Speed(elapsed));
                }
            }
            else if (State == RunState.Ready)
            {
                Scroll(dt * 1.4f);
            }
            else if (State == RunState.GameOver)
            {
                timeSinceCrash += dt;
            }

            player.Render(dt, State == RunState.Running, State == RunState.GameOver, RunnerRules.Speed(elapsed) / 6.8f);
            playerTrail.Tick(dt, player.Feet, player.IsDucking, RunnerRules.Speed(elapsed), State == RunState.Running);
            SetSkyFraming(State == RunState.Ready);
            if (State == RunState.Ready) dinoRenderer.transform.localScale *= 0.9f;
            UpdateParticles(dt);
            shake = Mathf.MoveTowards(shake, 0f, dt * 0.75f);
            gameCamera.transform.position = new Vector3(Mathf.Sin(Time.unscaledTime * 81f) * shake,
                (State == RunState.Ready ? 1.4f : 0f) + Mathf.Cos(Time.unscaledTime * 67f) * shake * 0.55f, -10);
            hud.TickDamage(dt, gameCamera);
        }

        private void Step(float dt)
        {
            if (State != RunState.Running) return;
            health.Step(dt);
            elapsed += dt;
            hud.UpdateProgress(elapsed);
            stageBannerTime = Mathf.Max(0f, stageBannerTime - dt);
            bool levelChanged = Level > announcedLevel;
            if (levelChanged)
            {
                announcedLevel = Level;
                hud.UpdateLevel(Level);
                hud.Toast(Level == 3 ? "LEVEL 03 / LOW CANOPY" : Level == 4 ?
                    "LEVEL 04 / JUMP, DROP, DUCK" : $"LEVEL {Level:00} / {RunnerRules.LevelHint(Level)}");
                stageBannerTime = 3f;
                audioCues.Milestone();
            }
            int lap = RunnerRules.EndlessLap(elapsed);
            if (lap > announcedLap)
            {
                announcedLap = lap;
                if (!levelChanged)
                {
                    hud.Toast($"LAP {lap - 1:00} COMPLETE / WILD RUN");
                    stageBannerTime = 3f;
                    audioCues.Milestone();
                }
            }
            float speed = RunnerRules.Speed(elapsed);
            float travel = speed * dt;
            distance += travel;
            bool wasGrounded = player.Grounded;
            player.Step(dt, out bool jumped, out bool landed);
            if (jumped)
            {
                audioCues.Jump();
                if (wasGrounded) EmitDirt(player.Feet, 5, DirtEmission.Takeoff);
            }
            if (landed)
            {
                EmitDirt(player.Feet, landingDirtCount, DirtEmission.Landing);
                dustTimer = runningDirtInterval * 1.5f;
            }
            Scroll(travel);

            spawnTimer = Mathf.Max(0f, spawnTimer - dt);
            if (spawnTimer <= 0f && distance >= nextEncounterDistance)
            {
                SpawnEncounter();
            }

            Rect playerBox = player.Hitbox;
            foreach (Obstacle obstacle in obstacles)
            {
                if (!obstacle.active) continue;
                Transform obstacleTransform = obstacle.renderer.transform;
                obstacleTransform.position += Vector3.left * travel;
                float x = obstacleTransform.position.x;
                Rect obstacleBox = obstacle.overhead ? RunnerRules.CanopyHitbox(x, obstacle.width) :
                    RunnerRules.ObstacleHitbox(x, obstacle.width, obstacle.height);
                if (!obstacle.hitPlayer && playerBox.Overlaps(obstacleBox))
                {
                    // Remember the contact even during grace time: one obstacle cannot drain multiple hearts.
                    obstacle.hitPlayer = true;
                    TakeDamage();
                    if (State == RunState.GameOver) return;
                }
                if (!obstacle.passed && x + obstacle.width * 0.5f < RunnerRules.PlayerX - RunnerRules.PlayerWidth * 0.5f)
                {
                    obstacle.passed = true;
                    if (!obstacle.hitPlayer) cleared++;
                }
                if (x < -12f)
                {
                    obstacle.active = false;
                    obstacle.renderer.gameObject.SetActive(false);
                }
            }

            eggs.Step(dt, travel, playerBox);
            dustTimer -= dt;
            if (player.Grounded && dustTimer <= 0f)
            {
                EmitDirt(player.Feet, runningDirtCount, DirtEmission.Running);
                dustTimer = runningDirtInterval / Mathf.Sqrt(speed / RunnerRules.Speed(0));
            }

            int nextMilestone = Score / 100;
            if (nextMilestone > milestone)
            {
                milestone = nextMilestone;
                audioCues.Milestone();
                if (stageBannerTime <= 0f) hud.Toast($"{milestone * 100} METRES  /  KEEP GOING!");
                SaveBest();
            }
        }

        private void SpawnEncounter()
        {
            int available = 0;
            foreach (Obstacle obstacle in obstacles) if (!obstacle.active) available++;
            // Never partially emit a combination if a custom scene has a small pool.
            if (available < 3) return;
            RunnerEncounter encounter = encounterDirector.Next(elapsed);
            int index = 0;
            foreach (Obstacle obstacle in obstacles)
            {
                if (obstacle.active) continue;
                RunnerObstacleSpawn item = encounter.Obstacles[index++];
                Sprite sprite = item.Overhead ? canopySprite : obstacleSprites[item.SpriteIndex];
                obstacle.renderer.sprite = sprite;
                obstacle.renderer.transform.localScale = item.Overhead ?
                    new Vector3(item.Width / sprite.bounds.size.x, 1f, 1f) :
                    new Vector3(item.Height, item.Height, 1);
                obstacle.renderer.transform.position = new Vector3(11.7f + item.Offset,
                    RunnerRules.GroundY + (item.Overhead ? RunnerRules.CanopyBottom : 0f), 0);
                obstacle.renderer.gameObject.SetActive(true);
                obstacle.width = item.Width;
                obstacle.height = item.Height;
                obstacle.overhead = item.Overhead;
                obstacle.active = true;
                obstacle.passed = false;
                obstacle.hitPlayer = false;
                if (index == encounter.Obstacles.Length) break;
            }
            nextEncounterDistance = distance + encounter.Spacing;
            eggs.SpawnEncounter(encounter, 11.7f, RunnerRules.Speed(elapsed + 6f));
        }

        private void Scroll(float travel)
        {
            if (trailSoil != null)
            {
                float width = trailSoil.GetComponent<SpriteRenderer>().sprite.bounds.size.x;
                soilOffset = Mathf.Repeat(soilOffset + travel, width);
                trailSoil.localPosition = new Vector3(-soilOffset, RunnerSoilArt.Top, 0f);
            }
            foreach (Layer layer in layers)
            {
                if (layer.stationary) continue;
                layer.offset = Mathf.Repeat(layer.offset + travel * layer.speed, layer.width);
                for (int i = 0; i < layer.tiles.Length; i++)
                {
                    Vector3 position = layer.tiles[i].position;
                    position.x = (i - 1) * layer.width - layer.offset;
                    layer.tiles[i].position = position;
                }
            }
        }

        private void EmitDirt(Vector3 position, int count, DirtEmission emission)
        {
            if (dust.Count == 0) return;
            bool impact = emission == DirtEmission.Landing || emission == DirtEmission.Crash;
            float drift = RunnerRules.Speed(elapsed) * (impact ? 0.22f : 0.4f);
            for (int i = 0; i < count; i++)
            {
                Dust particle = null;
                for (int j = 0; j < dust.Count; j++)
                {
                    int index = (nextDirtParticle + j) % dust.Count;
                    if (dust[index].life > 0f) continue;
                    particle = dust[index];
                    nextDirtParticle = (index + 1) % dust.Count;
                    break;
                }
                // Landing gets priority over older trail particles if the pool is busy.
                if (particle == null)
                {
                    if (!impact) break;
                    particle = dust[nextDirtParticle];
                    nextDirtParticle = (nextDirtParticle + 1) % dust.Count;
                }
                particle.puff = i % 3 == 0;
                particle.duration = particle.life = impact ? Random.Range(0.42f, 0.65f) : Random.Range(0.30f, 0.48f);
                particle.velocity = new Vector2((impact ? Random.Range(-3.8f, 3.8f) : Random.Range(-1.8f, -0.6f)) - drift,
                    particle.puff ? Random.Range(0.5f, 1.1f) : Random.Range(impact ? 1.3f : 0.7f, impact ? 3.2f : 1.8f));
                particle.gravity = particle.puff ? 2.2f : 10f;
                particle.spin = particle.puff ? 0f : Random.Range(-180f, 180f);
                particle.size = particle.puff ? Random.Range(impact ? 0.32f : 0.22f, impact ? 0.46f : 0.34f)
                    : Random.Range(impact ? 0.12f : 0.085f, impact ? 0.20f : 0.15f);
                particle.startColor = DirtColors[Random.Range(particle.puff ? 2 : 0, DirtColors.Length)];
                particle.startColor *= mapDustTint;
                particle.startColor.a = particle.puff ? 0.65f : 1f;
                if (dirtSprites != null && dirtSprites.Length >= 3)
                    particle.renderer.sprite = dirtSprites[particle.puff ? 1 : (i % 2 == 0 ? 0 : 2)];
                particle.renderer.transform.position = position + new Vector3(impact ? Random.Range(-0.38f, 0.38f) : Random.Range(-0.48f, -0.16f), 0.05f, 0);
                particle.renderer.transform.rotation = Quaternion.Euler(0, 0, particle.puff ? 0 : Random.Range(0, 4) * 90);
                particle.renderer.transform.localScale = Vector3.one * particle.size;
                particle.renderer.color = particle.startColor;
                particle.renderer.gameObject.SetActive(true);
            }
        }

        private void UpdateParticles(float dt)
        {
            foreach (Dust particle in dust)
            {
                if (particle.life <= 0f) continue;
                particle.life -= dt;
                if (particle.life <= 0f)
                {
                    particle.life = 0f;
                    particle.renderer.gameObject.SetActive(false);
                    continue;
                }
                particle.velocity += Vector2.down * (dt * particle.gravity);
                particle.renderer.transform.position += (Vector3)particle.velocity * dt;
                Vector3 position = particle.renderer.transform.position;
                if (position.y < RunnerRules.GroundY + 0.025f)
                {
                    position.y = RunnerRules.GroundY + 0.025f;
                    particle.renderer.transform.position = position;
                    particle.velocity = new Vector2(particle.velocity.x * 0.8f, 0);
                }
                float remaining = Mathf.Clamp01(particle.life / particle.duration);
                float age = 1f - remaining;
                Color color = particle.startColor;
                color.a *= Mathf.SmoothStep(0f, 1f, remaining);
                particle.renderer.color = color;
                particle.renderer.transform.localScale = Vector3.one * particle.size *
                    (particle.puff ? Mathf.Lerp(0.8f, 1.6f, age) : Mathf.Lerp(1f, 0.45f, age * age));
                particle.renderer.transform.Rotate(0, 0, particle.spin * dt);
            }
            for (int i = 0; i < motes.Count; i++)
            {
                Transform mote = motes[i].transform;
                Vector3 position = mote.position;
                position.x -= dt * (0.16f + i * 0.012f);
                position.y += Mathf.Sin(Time.time * 0.7f + i * 3f) * dt * 0.08f;
                if (position.x < -10.5f) position.x = 10.5f;
                mote.position = position;
            }
        }

        private void ReadInput()
        {
            // A map-confirmation click must never also dismiss the newly opened start prompt.
            if (State == RunState.AwaitingStart && Time.frameCount <= preparedInputFrame) return;
            bool waitingForStart = State == RunState.AwaitingStart;
            Keyboard keyboard = Keyboard.current;
            // Gift-code typing belongs to the InputField, not to gameplay/menu hotkeys (M/P/W/Space).
            if (State == RunState.Ready && hud.RedeemDialogOpen)
            {
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) hud.CloseMainMenuDialog();
                return;
            }
            bool duckKey = State == RunState.Running && keyboard != null &&
                (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed);
            if (keyboardDuckHeld != duckKey)
            {
                keyboardDuckHeld = duckKey;
                ApplyDuckInput();
            }
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
                {
                    if (State == RunState.Ready) hud.CloseMainMenuDialog();
                    else if (State == RunState.AwaitingStart) ReturnToMenu();
                    else TogglePause();
                    return;
                }
                if (keyboard.spaceKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame ||
                    keyboard.wKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                    PrimaryAction();
                if (keyboard.rKey.wasPressedThisFrame && State == RunState.GameOver) PrimaryAction();
                if (keyboard.mKey.wasPressedThisFrame) ToggleMute();
            }
            if (waitingForStart && State == RunState.Running) return; // Starting must not also queue a jump.

            if (!ReadTouchInput() && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                PointerAction(Mouse.current.position.ReadValue());
            }
        }

        private bool ReadTouchInput()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null) return false;
            var touch = touchscreen.primaryTouch;
            UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
            Vector2 point = touch.position.ReadValue();
            if (phase == UnityEngine.InputSystem.TouchPhase.Began && !touchGestureActive)
            {
                touchGestureActive = touchGestureDucking = false;
                if (State == RunState.Running && !hud.IsOverButton(point) && gameCamera.pixelRect.Contains(point))
                {
                    touchGestureStart = point;
                    touchGestureActive = true;
                }
                else PointerAction(point); // Tap-to-start and UI retain their existing behavior.
            }
            float threshold = Mathf.Max(24, Mathf.Min(Screen.width, Screen.height) * 0.08f);
            if (touchGestureActive && State == RunState.Running && touch.press.isPressed)
            {
                Vector2 delta = point - touchGestureStart;
                if (!touchGestureDucking && delta.y < -threshold && -delta.y > Mathf.Abs(delta.x))
                {
                    touchGestureDucking = true;
                    SetDuckHeld(true);
                }
            }
            if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                bool tap = touchGestureActive && !touchGestureDucking && State == RunState.Running &&
                    phase != UnityEngine.InputSystem.TouchPhase.Canceled && Vector2.Distance(point, touchGestureStart) < threshold;
                if (touchGestureDucking) SetDuckHeld(false);
                touchGestureActive = touchGestureDucking = false;
                if (tap) PointerAction(point); // Distinguish a tap from a duck swipe before spending a jump.
                return true;
            }
            return touch.press.isPressed || touch.press.wasPressedThisFrame;
        }

        private void PointerAction(Vector2 screenPoint)
        {
            if (State == RunState.AwaitingStart)
            {
                // Include portrait/ultrawide margins: the waiting screen really accepts a tap anywhere.
                if (Time.frameCount > preparedInputFrame && new Rect(0, 0, Screen.width, Screen.height).Contains(screenPoint))
                    PrimaryAction();
                return;
            }
            if (State == RunState.Ready || State == RunState.Paused) return; // Menus use explicit buttons.
            if (hud.IsOverButton(screenPoint) || !gameCamera.pixelRect.Contains(screenPoint)) return;
            PrimaryAction();
        }

        public void PrimaryAction()
        {
            if (!initialized) return;
            switch (State)
            {
                case RunState.Ready:
                    if (!hud.MainMenuDialogOpen)
                    {
                        if (hud.HasModeSelector) hud.OpenModeSelector();
                        else if (mapCatalog != null && mapCatalog.Count > 0 && hud.HasMapSelector) hud.OpenMapSelector();
                        else PrepareRun();
                    }
                    break;
                case RunState.AwaitingStart: BeginRun(); break;
                case RunState.Running: Jump(); break;
                case RunState.Paused: TogglePause(); break;
                case RunState.GameOver:
                    if (timeSinceCrash >= 0.45f) StartRun();
                    break;
            }
        }

        public void PrepareRun()
        {
            if (!initialized || State != RunState.Ready) return;
            ApplyMap(SelectedMapIndex);
            ResetRun();
            State = RunState.AwaitingStart;
            preparedInputFrame = Time.frameCount;
            SetSkyFraming(false);
            gameCamera.transform.position = new Vector3(0, 0, -10);
            hud.SetMapName(SelectedMapName);
            hud.UpdateLevel(Level);
            hud.UpdateScore(0, best, 0, RunnerRules.Speed(0));
            hud.ShowAwaitingStart(SelectedMapName);
        }

        public void StartRun()
        {
            if (!initialized) return;
            ApplyMap(SelectedMapIndex);
            ResetRun();
            SetSkyFraming(false);
            gameCamera.transform.position = new Vector3(0, 0, -10);
            BeginRun();
        }

        private void BeginRun()
        {
            State = RunState.Running;
            eggs.SpawnIntro();
            hud.ShowRunning();
            hud.SetMapName(SelectedMapName);
            hud.UpdateLevel(Level);
            hud.UpdateScore(0, best, 0, RunnerRules.Speed(0));
            hud.Toast($"LEVEL 01 / {RunnerRules.LevelHint(1)}");
            stageBannerTime = 3f;
        }

        private void ResetRun()
        {
            touchGestureActive = touchGestureDucking = false;
            keyboardDuckHeld = pointerDuckHeld = false;
            hud.FeedbackDuck(false);
            nextEncounterDistance = 0f;
            announcedLevel = 1;
            announcedLap = 0;
            stageBannerTime = 0f;
            var aspects = new float[obstacleSprites.Count];
            for (int i = 0; i < aspects.Length; i++) aspects[i] = obstacleSprites[i].bounds.size.x / obstacleSprites[i].bounds.size.y;
            int seed = Random.Range(1, int.MaxValue);
            encounterDirector = new RunnerEncounterDirector(seed, aspects);
            eggs.Reset(seed ^ 0x4e6719);
            hud.UpdateEggs(0);
            elapsed = distance = accumulator = dustTimer = timeSinceCrash = shake = 0f;
            cleared = milestone = 0;
            bestAtRunStart = best;
            spawnTimer = 0.7f;
            lastHudScore = -1;
            nextDirtParticle = 0;
            foreach (Obstacle obstacle in obstacles)
            {
                obstacle.active = false;
                obstacle.renderer.gameObject.SetActive(false);
            }
            foreach (Dust particle in dust)
            {
                particle.life = 0f;
                particle.renderer.gameObject.SetActive(false);
            }
            player.Reset();
            playerTrail?.Clear();
            health.Reset();
            hud.UpdateLives(Lives);
            hud.ClearDamage();
            hud.UpdateProgress(0f);
        }

        public void RestartRun()
        {
            if (!initialized || State == RunState.Ready || State == RunState.AwaitingStart) return;
            SaveBest();
            StartRun();
        }

        public void ReturnToMenu()
        {
            if (!initialized) return;
            SaveBest();
            ResetRun();
            State = RunState.Ready;
            ApplyMap(0);
            SetSkyFraming(true);
            gameCamera.transform.position = new Vector3(0, 1.4f, -10);
            hud.ShowReady(best);
            hud.UpdateLevel(0);
            hud.UpdateScore(0, best, 0, RunnerRules.Speed(0));
        }

        public void Jump()
        {
            if (State != RunState.Running) return;
            player.QueueJump();
            hud.FeedbackJump();
        }

        public void SetDuckHeld(bool held)
        {
            pointerDuckHeld = held && State == RunState.Running;
            ApplyDuckInput();
        }

        private void ApplyDuckInput()
        {
            bool held = keyboardDuckHeld || pointerDuckHeld;
            player.SetDucking(held);
            hud.FeedbackDuck(held);
        }

        private void ReleaseDuckInput()
        {
            touchGestureActive = touchGestureDucking = false;
            keyboardDuckHeld = pointerDuckHeld = false;
            ApplyDuckInput();
        }

        public void TogglePause()
        {
            if (!initialized) return;
            if (State == RunState.Running)
            {
                State = RunState.Paused;
                ReleaseDuckInput();
                SaveBest();
                hud.ShowPaused();
            }
            else if (State == RunState.Paused)
            {
                State = RunState.Running;
                hud.ShowRunning();
            }
        }

        private void ToggleMute()
        {
            audioCues.ToggleMute();
            hud.SetSound(audioCues.Muted);
        }

        private void TakeDamage()
        {
            if (State != RunState.Running || !health.TryTakeHit()) return;
            player.Hurt();
            hud.UpdateLives(Lives);
            hud.ShowDamage(player.Feet + new Vector3(0.35f, 2f, 0f), gameCamera);
            shake = 0.10f;
            audioCues.Crash();
            EmitDirt(player.Feet, 18, DirtEmission.Crash);
            if (Lives == 0) EndRun();
        }

        private void EndRun()
        {
            bool record = Score > bestAtRunStart;
            State = RunState.GameOver;
            ReleaseDuckInput();
            timeSinceCrash = 0f;
            shake = 0.14f;
            SaveBest();
            hud.UpdateScore(Score, best, cleared, RunnerRules.Speed(elapsed));
            hud.ShowGameOver(Score, best, record);
        }

        private void SaveBest()
        {
            if (Score <= best) return;
            best = Score;
            PlayerPrefs.SetInt(HighScoreKey, best);
            PlayerPrefs.Save();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && initialized && State == RunState.Running) TogglePause();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && initialized && State == RunState.Running) TogglePause();
        }

        private void OnApplicationQuit()
        {
            if (initialized) SaveBest();
        }

        private void OnDestroy()
        {
            if (initialized) SaveBest();
            audioCues?.Dispose();
            eggs?.Dispose();
            // Saved subassets belong to the project, not to this play session.
            foreach (Sprite sprite in generatedSprites)
                if (sprite != null && !IsSavedAsset(sprite)) Destroy(sprite);
            foreach (Texture2D texture in generatedTextures)
                if (texture != null && !IsSavedAsset(texture)) Destroy(texture);
        }

        private static bool IsSavedAsset(Object asset)
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUtility.IsPersistent(asset);
#else
            return false;
#endif
        }
    }
}
