using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TapPop.EditorTools
{
    // Builds the whole game scene (canvas, HUD, board, start screen, popups) with placeholder art,
    // then links any real art already in Assets/_TapPop/Art. Safe to run again.
    public static class TapPopSceneBuilder
    {
        public const string Root = "Assets/_TapPop";
        public const string ScenePath = Root + "/Scenes/Game.unity";
        const string DataFolder = Root + "/Data";
        const string PrefabFolder = Root + "/Prefabs";
        const int UiLayer = 5;

        // Figma frame 540 x 960 exported @2x.
        static readonly Vector2 Reference = new Vector2(1080f, 1920f);
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        // Minimal palette: light grey paper, white surfaces and ink; the tiles are the only colour on screen.
        static readonly Color Paper = Hex("F2F2F2");
        static readonly Color Surface = Color.white;
        static readonly Color Ink = Hex("0A0A0A");
        static readonly Color Muted = Hex("8E8E8E");
        static readonly Color StarEmpty = Hex("DDDDDD");

        // Level curve, tuned by simulating thousands of games per level: one move fewer or one tile more each time,
        // the goal always below the moves, the goal colour going round the tile set.
        // Star scores sit near the median and the top fifth of winning games.
        // Only used to create missing level assets; after that the assets are the source of truth.
        static readonly (int moves, int goal, int twoStars, int threeStars)[] LevelCurve =
        {
            (20, 10, 2200, 3600),
            (20, 11, 2400, 3800),
            (20, 12, 2500, 4000),
            (19, 12, 2600, 4000),
            (19, 13, 2700, 4200),
            (19, 14, 3000, 4400),
            (18, 14, 3000, 4400),
            (18, 15, 3100, 4500),
            (18, 16, 3300, 4700),
            (17, 16, 3300, 4700),
        };

        enum ButtonStyle { Primary, Secondary }
        enum Weight { Regular, Medium }

        [MenuItem("TapPop/Import TMP Essentials", priority = 0)]
        public static void ImportTmpEssentials()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings", new[] { "Assets" }).Length > 0) return;
            TMP_PackageResourceImporter.ImportResources(true, false, false);
        }

        [MenuItem("TapPop/Build Game Scene", priority = 1)]
        public static void BuildScene()
        {
            if (AssetDatabase.FindAssets("t:TMP_Settings", new[] { "Assets" }).Length == 0)
                throw new InvalidOperationException("TapPop: run TapPop > Import TMP Essentials first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // New scene first: opening it unloads unused assets, which would null out references loaded before it.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            foreach (string folder in new[] { Root + "/Scenes", DataFolder, PrefabFolder, Root + "/Fonts", Root + "/Audio" })
                TapPopArt.EnsureFolder(folder);
            TapPopArt.EnsureAtlas();

            var levels = new List<LevelConfig>();
            for (int i = 0; i < LevelCurve.Length; i++)
            {
                int index = i;
                levels.Add(LoadOrCreate<LevelConfig>($"{DataFolder}/Level_{i + 1:00}.asset", l => SetupLevel(l, index)));
            }
            TileSet tileSet = LoadOrCreate<TileSet>(DataFolder + "/TileSet.asset", SetupTileSet);
            TileView tilePrefab = BuildTilePrefab();
            GameObject buttonPrefab = BuildButtonPrefab();

            BuildCamera();
            BuildEventSystem();
            var game = new GameObject("Game").AddComponent<GameController>();

            RectTransform canvas = BuildCanvas();
            Background(canvas, "bg_game", Paper, false);
            RectTransform safeArea = Rect("SafeArea", canvas);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeArea>();

            Hud hud = BuildHud(safeArea, tileSet);
            Board board = BuildBoard(safeArea, out RectTransform tileRoot);
            TMP_Text footer = TextAt(safeArea, "Level", "Level 1", 30f, Muted, Vector2.zero, new Vector2(400f, 50f));
            Place(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(400f, 50f));
            Wire(hud, "levelText", footer);
            StartScreen start = BuildStartScreen(canvas, buttonPrefab, game, levels.Count);
            ResultPopup win = BuildPopup(canvas, buttonPrefab, game, "WinPopup", "Level complete", true);
            ResultPopup lose = BuildPopup(canvas, buttonPrefab, game, "LosePopup", "Out of moves", false);
            PopFx fx = BuildFx(canvas);
            Sfx sfx = BuildSfx();

            Wire(board, "tileSet", tileSet);
            Wire(board, "tilePrefab", tilePrefab);
            Wire(board, "tileRoot", tileRoot);
            Wire(board, "fx", fx);
            Wire(board, "sfx", sfx);
            Wire(win, "fx", fx);
            Wire(lose, "fx", fx);
            WireArray(game, "levels", levels);
            Wire(game, "tileSet", tileSet);
            Wire(game, "board", board);
            Wire(game, "hud", hud);
            Wire(game, "fx", fx);
            Wire(game, "startScreen", start);
            Wire(game, "winPopup", win);
            Wire(game, "losePopup", lose);
            RequireWired(board, game, hud, start, fx);

            win.gameObject.SetActive(false);
            lose.gameObject.SetActive(false);
            RecordPrefabInstances();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            TapPopArt.Relink();
            Debug.Log("TapPop: scene built at " + ScenePath);
        }

        static void BuildCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 0f, -10f);
            var camera = go.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Paper;
            go.AddComponent<AudioListener>();
        }

        static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        static RectTransform BuildCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
            {
                layer = UiLayer,
            };
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            // Expand: the whole 1080 x 1920 layout always fits; taller or wider screens get extra room, never a crop.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referencePixelsPerUnit = 100f;
            return (RectTransform)go.transform;
        }

        static void Background(RectTransform parent, string spriteName, Color tint, bool blocksInput)
        {
            RectTransform bg = Rect("Background", parent);
            Place(bg, Center, Vector2.zero, Reference);
            var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = Reference.x / Reference.y;
            Image image = Img(bg, spriteName, tint, null, false);
            image.raycastTarget = blocksInput;
            bg.GetComponent<SpriteSlot>().preserveAspect = false;
        }

        static Hud BuildHud(RectTransform parent, TileSet tileSet)
        {
            RectTransform bar = Rect("Hud", parent);
            Place(bar, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(960f, 200f));
            var hud = bar.gameObject.AddComponent<Hud>();

            TMP_Text movesValue = Stat(bar, "Moves", -340f);
            TMP_Text scoreValue = Stat(bar, "Score", 340f);

            RectTransform goal = Rect("Goal", bar);
            Place(goal, Center, Vector2.zero, new Vector2(300f, 200f));
            TextAt(goal, "Label", "Goal", 30f, Muted, new Vector2(0f, 36f), new Vector2(300f, 50f));
            // No chip: the goal tile and count sit on the paper like the other stats, centred together whatever the digits.
            RectTransform row = Rect("Row", goal);
            Place(row, Center, new Vector2(0f, -30f), new Vector2(300f, 100f));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            RectTransform icon = Rect("GoalIcon", row);
            icon.sizeDelta = Vector2.one * 68f;
            var iconSize = icon.gameObject.AddComponent<LayoutElement>();
            iconSize.preferredWidth = 68f;
            iconSize.preferredHeight = 68f;
            var goalIcon = icon.gameObject.AddComponent<Image>();
            goalIcon.raycastTarget = false;
            tileSet.Apply(goalIcon, 0);
            TMP_Text goalValue = TextAt(row, "Value", "10", 72f, Ink, Vector2.zero, new Vector2(120f, 100f), Weight.Medium);

            Wire(hud, "movesText", movesValue);
            Wire(hud, "goalText", goalValue);
            Wire(hud, "scoreText", scoreValue);
            Wire(hud, "goalIcon", goalIcon);
            return hud;
        }

        static TMP_Text Stat(RectTransform parent, string name, float x)
        {
            RectTransform root = Rect(name, parent);
            Place(root, Center, new Vector2(x, 0f), new Vector2(280f, 200f));
            TextAt(root, "Label", name, 30f, Muted, new Vector2(0f, 36f), new Vector2(280f, 50f));
            return TextAt(root, "Value", name == "Moves" ? "20" : "0", 72f, Ink, new Vector2(0f, -30f), new Vector2(280f, 100f), Weight.Medium);
        }

        static Board BuildBoard(RectTransform parent, out RectTransform tileRoot)
        {
            const float cell = 120f;
            RectTransform frame = Rect("Board", parent);
            Place(frame, Center, new Vector2(0f, -30f), Vector2.one * cell * 8f);
            var board = frame.gameObject.AddComponent<Board>();

            // Quiet empty slots behind the tiles: they show the grid while tiles fall in.
            RectTransform cells = Rect("Cells", frame);
            Stretch(cells);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    RectTransform slot = Rect($"Cell_{x}_{y}", cells);
                    Place(slot, Center, new Vector2((x - 3.5f) * cell, (y - 3.5f) * cell), Vector2.one * 112f);
                    Img(slot, "cell_empty", Surface, TapPopPlaceholders.Cell, false);
                }
            }

            tileRoot = Rect("Tiles", frame);
            Stretch(tileRoot);
            // Clips new tiles above the board; a little slack on the other sides for pop and squash.
            tileRoot.gameObject.AddComponent<RectMask2D>().padding = new Vector4(-16f, -16f, -16f, 0f);
            return board;
        }

        static StartScreen BuildStartScreen(RectTransform canvas, GameObject buttonPrefab, GameController game, int levelCount)
        {
            RectTransform root = Rect("StartScreen", canvas);
            Stretch(root);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            Background(root, "bg_start", Paper, true);

            RectTransform mark = Rect("LogoMark", root);
            Place(mark, Center, new Vector2(0f, 790f), new Vector2(130f, 130f));
            Img(mark, "logo_mark", Color.white, TapPopPlaceholders.Mark, false);

            TextAt(root, "Title", "TapPop", 72f, Ink, new Vector2(0f, 670f), new Vector2(800f, 100f), Weight.Medium);
            TextAt(root, "Tagline", "Tap two or more matching tiles", 30f, Muted, new Vector2(0f, 605f), new Vector2(900f, 50f));

            LevelPath path = BuildLevelPath(root, game, levelCount);

            RectTransform holder = Rect("PlayHolder", root);
            Place(holder, new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(560f, 128f));
            Button play = AddButton(buttonPrefab, holder, "PlayButton", "Play level 1", ButtonStyle.Primary);
            Stretch((RectTransform)play.transform);
            UnityEventTools.AddPersistentListener(play.onClick, game.Play);

            var start = root.gameObject.AddComponent<StartScreen>();
            Wire(start, "group", group);
            Wire(start, "mark", mark);
            Wire(start, "levelPath", path);
            Wire(start, "playLabel", play.GetComponentInChildren<TMP_Text>());
            return start;
        }

        // Level map: nodes swing left and right on a dotted S-curve, level 1 at the bottom, so progress reads as climbing.
        static LevelPath BuildLevelPath(RectTransform parent, GameController game, int count)
        {
            const float node = 112f, swing = 190f, bottom = -470f, top = 466f, dotGap = 30f, dotSize = 12f, clear = 76f;
            float step = (top - bottom) / Mathf.Max(1, count - 1);
            Vector2 At(float t) => new Vector2(swing * Mathf.Cos(Mathf.PI * t), bottom + t * step);

            RectTransform root = Rect("LevelPath", parent);
            Stretch(root);
            var path = root.gameObject.AddComponent<LevelPath>();

            // Dots first so the nodes sit on top. Even spacing along the curve, none under a node.
            RectTransform dotsRoot = Rect("Dots", root);
            Stretch(dotsRoot);
            var dots = new List<Image>();
            var dotLevel = new List<int>();
            float walked = dotGap * 0.5f;
            Vector2 previous = At(0f);
            for (float t = 0f; t <= count - 1; t += 0.002f)
            {
                Vector2 p = At(t);
                walked += Vector2.Distance(previous, p);
                previous = p;
                if (walked < dotGap) continue;
                walked = 0f;
                int nearest = Mathf.RoundToInt(t);
                if (Vector2.Distance(p, At(nearest)) < clear) continue;
                RectTransform dot = Rect($"Dot_{dots.Count:000}", dotsRoot);
                Place(dot, Center, p, Vector2.one * dotSize);
                dots.Add(Img(dot, "path_dot", Hex("CFCFCF"), TapPopPlaceholders.Circle, false));
                dotLevel.Add(Mathf.FloorToInt(t));
            }

            var nodes = new List<LevelNode>();
            for (int i = 0; i < count; i++)
            {
                RectTransform rt = Rect($"Level_{i + 1:00}", root);
                Place(rt, Center, At(i), Vector2.one * node);
                var levelNode = rt.gameObject.AddComponent<LevelNode>();

                // The button is a child so its press squash does not fight the node's own scale.
                RectTransform body = Rect("Button", rt);
                Stretch(body);
                Image disc = Img(body, "node_level", Surface, TapPopPlaceholders.Circle, false);
                disc.raycastTarget = true;
                var button = body.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = disc;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                body.gameObject.AddComponent<PressableButton>();
                UnityEventTools.AddIntPersistentListener(button.onClick, game.PlayLevel, i);
                TMP_Text number = TextAt(body, "Number", (i + 1).ToString(), 44f, Ink, Vector2.zero, Vector2.one * node, Weight.Medium);

                RectTransform stars = Rect("Stars", rt);
                Place(stars, Center, new Vector2(0f, -80f), new Vector2(110f, 30f));
                var fills = new List<GameObject>();
                for (int k = 0; k < 3; k++)
                {
                    RectTransform slot = Rect("Star" + (k + 1), stars);
                    Place(slot, Center, new Vector2((k - 1) * 34f, 0f), Vector2.one * 30f);
                    Img(slot, "icon_star_empty", StarEmpty, TapPopPlaceholders.Star, false);
                    RectTransform fill = Rect("Fill", slot);
                    Stretch(fill);
                    Img(fill, "icon_star_full", Ink, TapPopPlaceholders.Star, false);
                    fills.Add(fill.gameObject);
                }

                Wire(levelNode, "disc", disc);
                Wire(levelNode, "number", number);
                Wire(levelNode, "button", button);
                Wire(levelNode, "starRow", stars.gameObject);
                WireArray(levelNode, "starFills", fills);
                nodes.Add(levelNode);
            }

            WireArray(path, "nodes", nodes);
            WireArray(path, "dots", dots);
            WireInts(path, "dotLevel", dotLevel);
            return path;
        }

        static ResultPopup BuildPopup(RectTransform canvas, GameObject buttonPrefab, GameController game, string name, string title, bool withStars)
        {
            RectTransform root = Rect(name, canvas);
            Stretch(root);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var popup = root.gameObject.AddComponent<ResultPopup>();

            // Full screen on the paper colour instead of a boxed popup.
            RectTransform backdrop = Rect("Backdrop", root);
            Stretch(backdrop);
            backdrop.gameObject.AddComponent<Image>().color = Paper;

            RectTransform panel = Rect("Content", root);
            Stretch(panel);

            RectTransform chip = Rect("Chip", panel);
            Place(chip, Center, new Vector2(0f, 440f), new Vector2(200f, 64f));
            Img(chip, "tag_title", Surface, TapPopPlaceholders.Pill, true);
            TMP_Text levelLabel = TextAt(chip, "Text", "Level 1", 30f, Ink, Vector2.zero, new Vector2(200f, 64f));

            TextAt(panel, "Title", title, 72f, Ink, new Vector2(0f, 340f), new Vector2(900f, 100f), Weight.Medium);

            var fills = new List<RectTransform>();
            if (withStars)
            {
                RectTransform row = Rect("Stars", panel);
                Place(row, Center, new Vector2(0f, 150f), new Vector2(600f, 160f));
                for (int i = 0; i < 3; i++)
                {
                    RectTransform slot = Rect("Star" + (i + 1), row);
                    Place(slot, Center, new Vector2((i - 1) * 190f, 0f), Vector2.one * 150f);
                    Img(slot, "icon_star_empty", StarEmpty, TapPopPlaceholders.Star, false);
                    RectTransform fill = Rect("Fill", slot);
                    Stretch(fill);
                    Img(fill, "icon_star_full", Ink, TapPopPlaceholders.Star, false);
                    fills.Add(fill);
                }
            }
            else
            {
                TMP_Text message = TextAt(panel, "Message", "Goal 0/25", 40f, Muted, new Vector2(0f, 160f), new Vector2(760f, 70f));
                Wire(popup, "messageText", message);
            }

            TextAt(panel, "ScoreLabel", "Score", 30f, Muted, new Vector2(0f, -40f), new Vector2(600f, 50f));
            TMP_Text score = TextAt(panel, "ScoreValue", "0", 120f, Ink, new Vector2(0f, -130f), new Vector2(700f, 150f), Weight.Medium);

            Button primary = AddButton(buttonPrefab, panel, "PrimaryButton", withStars ? "Next level" : "Try again", ButtonStyle.Primary);
            Place((RectTransform)primary.transform, new Vector2(0.5f, 0f), new Vector2(0f, 320f), new Vector2(560f, 128f));
            UnityEventTools.AddPersistentListener(primary.onClick, game.Restart);

            Button menu = AddButton(buttonPrefab, panel, "MenuButton", "Menu", ButtonStyle.Secondary);
            Place((RectTransform)menu.transform, new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(300f, 96f));
            UnityEventTools.AddPersistentListener(menu.onClick, game.BackToMenu);

            Wire(popup, "group", group);
            Wire(popup, "panel", panel);
            Wire(popup, "scoreText", score);
            Wire(popup, "levelText", levelLabel);
            Wire(popup, "primaryLabel", primary.GetComponentInChildren<TMP_Text>());
            WireArray(popup, "starFills", fills);
            return popup;
        }

        static PopFx BuildFx(RectTransform canvas)
        {
            RectTransform root = Rect("Fx", canvas);
            Stretch(root);
            var fx = root.gameObject.AddComponent<PopFx>();
            Wire(fx, "spark", TapPopPlaceholders.Circle);
            return fx;
        }

        // Every clip in Assets/_TapPop/Audio named sfx_pop_* becomes a pop variant.
        static Sfx BuildSfx()
        {
            var sfx = new GameObject("Sfx").AddComponent<Sfx>();
            var pops = new List<AudioClip>();
            foreach (string guid in AssetDatabase.FindAssets("sfx_pop_ t:AudioClip", new[] { Root + "/Audio" }))
                pops.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid)));
            WireArray(sfx, "pops", pops);
            return sfx;
        }

        static TileView BuildTilePrefab()
        {
            var root = new GameObject("Tile", typeof(RectTransform), typeof(Image)) { layer = UiLayer };
            ((RectTransform)root.transform).sizeDelta = Vector2.one * 112f;
            root.GetComponent<Image>().sprite = TapPopPlaceholders.Circle;
            root.AddComponent<TileView>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/Tile.prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<TileView>();
        }

        static GameObject BuildButtonPrefab()
        {
            RectTransform root = Rect("Button", null);
            root.sizeDelta = new Vector2(560f, 128f);
            Image image = Img(root, "btn_primary", Ink, TapPopPlaceholders.Pill, true);
            image.raycastTarget = true;
            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            root.gameObject.AddComponent<PressableButton>();
            TMP_Text label = TextAt(root, "Label", "Button", 40f, Color.white, Vector2.zero, root.sizeDelta, Weight.Medium);
            Stretch(label.rectTransform);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabFolder + "/Button.prefab");
            Object.DestroyImmediate(root.gameObject);
            return prefab;
        }

        static Button AddButton(GameObject prefab, RectTransform parent, string name, string label, ButtonStyle style)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            TMP_Text text = go.GetComponentInChildren<TMP_Text>();
            text.text = label;
            if (style == ButtonStyle.Secondary)
            {
                // Text button: the image stays as an invisible hit area.
                go.GetComponent<SpriteSlot>().spriteName = "btn_secondary";
                go.GetComponent<Image>().color = Color.clear;
                text.color = Ink;
                text.font = TapPopFonts.Regular ?? TMP_Settings.defaultFontAsset;
                text.fontSize = 36f;
            }
            return go.GetComponent<Button>();
        }

        static void SetupTileSet(TileSet tileSet)
        {
            tileSet.placeholder = TapPopPlaceholders.Circle;
            // Pop particles take the colour of each tile.
            tileSet.tiles = new[]
            {
                new TileSet.Tile { id = "red", color = Hex("FF3848") },
                new TileSet.Tile { id = "blue", color = Hex("297FFF") },
                new TileSet.Tile { id = "green", color = Hex("30D754") },
                new TileSet.Tile { id = "yellow", color = Hex("FFD414") },
                new TileSet.Tile { id = "purple", color = Hex("B638FF") },
            };
        }

        static void SetupLevel(LevelConfig level, int index)
        {
            (int moves, int goal, int twoStars, int threeStars) = LevelCurve[index];
            level.moves = moves;
            level.goalCount = goal;
            level.goalColor = index % 5;
            level.twoStarScore = twoStars;
            level.threeStarScore = threeStars;
        }

        static T LoadOrCreate<T>(string path, Action<T> setup) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            setup?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // Edits made from code to prefab instances only stick once recorded as overrides.
        static void RecordPrefabInstances()
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!PrefabUtility.IsPartOfPrefabInstance(t)) continue;
                foreach (Component c in t.GetComponents<Component>()) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            }
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            return rt;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = Center;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = Center;
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
        }

        static Image Img(RectTransform rt, string spriteName, Color placeholderTint, Sprite placeholder, bool sliced)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = placeholder;
            image.color = placeholderTint;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced && placeholder != null;
            image.raycastTarget = false;
            if (spriteName != null) rt.gameObject.AddComponent<SpriteSlot>().spriteName = spriteName;
            return image;
        }

        static TextMeshProUGUI TextAt(RectTransform parent, string name, string text, float size, Color color, Vector2 position, Vector2 box,
            Weight weight = Weight.Regular)
        {
            RectTransform rt = Rect(name, parent);
            Place(rt, Center, position, box);
            var label = rt.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = (weight == Weight.Medium ? TapPopFonts.Medium : TapPopFonts.Regular) ?? TMP_Settings.defaultFontAsset;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyles.Normal;
            label.characterSpacing = weight == Weight.Medium ? -2f : 0f;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            return label;
        }

        static void Wire(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field)
                ?? throw new ArgumentException($"{target.GetType().Name} has no field '{field}'");
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Fails the build loudly if any reference field ended up empty.
        static void RequireWired(params Object[] targets)
        {
            foreach (Object target in targets)
            {
                SerializedProperty property = new SerializedObject(target).GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.name == "m_Script") continue;
                    if (property.objectReferenceValue == null)
                        throw new InvalidOperationException($"TapPop: {target.GetType().Name}.{property.propertyPath} is not wired");
                }
            }
        }

        static void WireArray<T>(Object target, string field, IReadOnlyList<T> values) where T : Object
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireInts(Object target, string field, IReadOnlyList<int> values)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) property.GetArrayElementAtIndex(i).intValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            return color;
        }
    }
}
