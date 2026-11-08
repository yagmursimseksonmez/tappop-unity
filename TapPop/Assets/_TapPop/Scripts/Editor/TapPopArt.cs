using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TapPop.EditorTools
{
    // Art pipeline: Figma exports in, sorted by name prefix, linked to the UI by name, packed into one atlas.
    public static class TapPopArt
    {
        public const string ArtRoot = "Assets/_TapPop/Art";
        public const string AtlasPath = "Assets/_TapPop/Atlases/UI.spriteatlasv2";

        // Name prefix -> folder under Art. Anything else goes to UI.
        static readonly (string prefix, string folder)[] Folders =
        {
            ("tile_", "Tiles"),
            ("icon_", "Icons"),
            ("logo_", "Icons"),
            ("fx_", "FX"),
            ("bg_", "Backgrounds"),
        };

        // Backgrounds stay out of the atlas: they are full-screen and would waste most of the page.
        static readonly string[] AtlasFolders = { "Tiles", "UI", "Icons", "FX" };

        static string FigmaExportFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../figma-export"));

        [MenuItem("TapPop/Import From figma-export", priority = 10)]
        public static void ImportFromFigma()
        {
            if (!Directory.Exists(FigmaExportFolder))
            {
                Debug.LogWarning("TapPop: no figma-export folder next to the Unity project.");
                return;
            }

            int count = 0;
            foreach (string source in Directory.GetFiles(FigmaExportFolder, "*.png", SearchOption.AllDirectories))
            {
                string name = CleanName(Path.GetFileNameWithoutExtension(source));
                string folder = FolderFor(name);
                Directory.CreateDirectory(Path.Combine(ArtRoot, folder));
                File.Copy(source, Path.Combine(ArtRoot, folder, name + ".png"), true);
                count++;
            }
            AssetDatabase.Refresh();
            Debug.Log($"TapPop: imported {count} PNGs from {FigmaExportFolder}");
            Relink();
        }

        [MenuItem("TapPop/Relink Art", priority = 11)]
        public static void Relink()
        {
            if (SceneManager.GetActiveScene().path != TapPopSceneBuilder.ScenePath && File.Exists(TapPopSceneBuilder.ScenePath))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(TapPopSceneBuilder.ScenePath);
            }

            Dictionary<string, Sprite> sprites = LoadSprites();

            foreach (string guid in AssetDatabase.FindAssets("t:TileSet"))
            {
                var tileSet = AssetDatabase.LoadAssetAtPath<TileSet>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (TileSet.Tile tile in tileSet.tiles)
                    if (sprites.TryGetValue("tile_" + tile.id, out Sprite sprite)) tile.sprite = sprite;
                EditorUtility.SetDirty(tileSet);
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { TapPopSceneBuilder.Root + "/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                foreach (SpriteSlot slot in contents.GetComponentsInChildren<SpriteSlot>(true)) Fill(slot, sprites);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
            }

            foreach (SpriteSlot slot in Object.FindObjectsByType<SpriteSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Fill(slot, sprites);

            if (sprites.TryGetValue("fx_spark", out Sprite spark))
            {
                foreach (PopFx fx in Object.FindObjectsByType<PopFx>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var so = new SerializedObject(fx);
                    so.FindProperty("spark").objectReferenceValue = spark;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EnsureAtlas();
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log($"TapPop: relinked {sprites.Count} sprites");
        }

        static void Fill(SpriteSlot slot, Dictionary<string, Sprite> sprites)
        {
            if (string.IsNullOrEmpty(slot.spriteName) || !sprites.TryGetValue(slot.spriteName, out Sprite sprite)) return;
            var image = slot.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = image.type == Image.Type.Simple && slot.preserveAspect;
            if (slot.placeholder != null) slot.placeholder.SetActive(false);
            EditorUtility.SetDirty(image);
            EditorUtility.SetDirty(slot);
        }

        public static void EnsureAtlas()
        {
            if (AssetDatabase.LoadMainAssetAtPath(AtlasPath) != null) return;

            foreach (string folder in AtlasFolders) EnsureFolder(ArtRoot + "/" + folder);
            EnsureFolder(Path.GetDirectoryName(AtlasPath));

            var atlas = new SpriteAtlasAsset();
            var folders = new List<Object>();
            foreach (string folder in AtlasFolders) folders.Add(AssetDatabase.LoadAssetAtPath<DefaultAsset>(ArtRoot + "/" + folder));
            atlas.Add(folders.ToArray());
            SpriteAtlasAsset.Save(atlas, AtlasPath);
            AssetDatabase.ImportAsset(AtlasPath);

            var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(AtlasPath);
            importer.includeInBuild = true;
            importer.packingSettings = new SpriteAtlasPackingSettings
            {
                enableRotation = false,
                enableTightPacking = false,
                padding = 4,
                blockOffset = 1,
            };
            importer.textureSettings = new SpriteAtlasTextureSettings
            {
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear,
                readable = false,
            };
            TextureImporterPlatformSettings platform = importer.GetPlatformSettings("DefaultTexturePlatform");
            platform.maxTextureSize = 2048;
            platform.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SetPlatformSettings(platform);
            importer.SaveAndReimport();
        }

        static Dictionary<string, Sprite> LoadSprites()
        {
            var map = new Dictionary<string, Sprite>();
            if (!AssetDatabase.IsValidFolder(ArtRoot)) return map;
            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { ArtRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) map[CleanName(Path.GetFileNameWithoutExtension(path))] = sprite;
            }
            return map;
        }

        // Figma adds "@2x" to scaled exports; the art is named without it.
        static string CleanName(string name) => name.Replace("@2x", "").Trim().ToLowerInvariant();

        static string FolderFor(string name)
        {
            foreach ((string prefix, string folder) in Folders)
                if (name.StartsWith(prefix, StringComparison.Ordinal)) return folder;
            return "UI";
        }

        public static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
