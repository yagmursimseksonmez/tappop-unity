using System.IO;
using UnityEditor;
using UnityEngine;

namespace TapPop.EditorTools
{
    // First-import defaults for everything under Assets/_TapPop/Art, driven by the file name.
    // Runs only when a file has no import settings yet, so manual tweaks (Sprite Editor borders) survive re-exports.
    public class TapPopImportRules : AssetPostprocessor
    {
        // 9-slice corner size in @2x pixels per name prefix. Each holds the corner radius the style guide allows (Figma 1x):
        // buttons are 64 px pills (radius 32), tags 48 px pills (radius 24), cards up to 24, the popup up to 32.
        static readonly (string prefix, int border)[] SliceBorders =
        {
            ("btn_", 64),
            ("tag_", 48),
            ("panel_", 64),
            ("frame_", 48),
            ("bar_", 48),
        };

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(TapPopArt.ArtRoot + "/")) return;
            var importer = (TextureImporter)assetImporter;
            if (!importer.importSettingsMissing) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            string file = Path.GetFileNameWithoutExtension(assetPath);
            foreach ((string prefix, int border) in SliceBorders)
            {
                if (!file.StartsWith(prefix)) continue;
                importer.spriteBorder = new Vector4(border, border, border, border);
                break;
            }
        }
    }
}
