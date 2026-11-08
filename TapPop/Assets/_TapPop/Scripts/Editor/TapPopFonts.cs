using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace TapPop.EditorTools
{
    // Picks up font files dropped into Assets/_TapPop/Fonts by weight suffix (e.g. Geist-Regular.ttf, Geist-Medium.ttf)
    // and makes a TextMeshPro font asset for each. Without them the scene falls back to the TMP default font.
    public static class TapPopFonts
    {
        public const string Folder = TapPopSceneBuilder.Root + "/Fonts";

        public static TMP_FontAsset Regular => Load("Regular");
        public static TMP_FontAsset Medium => Load("Medium") ?? Regular;

        static TMP_FontAsset Load(string weight)
        {
            if (!AssetDatabase.IsValidFolder(Folder)) return null;
            foreach (string guid in AssetDatabase.FindAssets("t:Font", new[] { Folder }))
            {
                string fontPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!Path.GetFileNameWithoutExtension(fontPath).EndsWith("-" + weight, StringComparison.OrdinalIgnoreCase)) continue;

                string assetPath = Path.ChangeExtension(fontPath, null) + " SDF.asset";
                var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
                if (existing != null) return existing;

                var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
                // Dynamic: glyphs are added as text needs them, so the scores and labels never miss a character.
                TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic);
                AssetDatabase.CreateAsset(asset, assetPath);
                asset.atlasTextures[0].name = asset.name + " Atlas";
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
                asset.material.name = asset.name + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
                AssetDatabase.SaveAssets();
                return asset;
            }
            return null;
        }
    }
}
