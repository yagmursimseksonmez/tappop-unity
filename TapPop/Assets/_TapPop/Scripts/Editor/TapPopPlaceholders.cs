using System.IO;
using UnityEditor;
using UnityEngine;

namespace TapPop.EditorTools
{
    // Clean generated stand-ins (rounded card, pill, circle, star) so the build reads as the intended style
    // before the real Figma art lands. They live outside Art, so they never reach the atlas or the relink.
    public static class TapPopPlaceholders
    {
        const string Folder = TapPopSceneBuilder.Root + "/Placeholders";

        public static Sprite Card => RoundedRect("ph_card", 160, 160, 48, 56);
        public static Sprite Pill => RoundedRect("ph_pill", 128, 128, 64, 63);
        public static Sprite Cell => RoundedRect("ph_cell", 112, 112, 28, 0);
        public static Sprite Circle => RoundedRect("ph_circle", 128, 128, 64, 0);
        public static Sprite Star => Get("ph_star", () => Render(128, 128, StarCoverage), 0);
        // Logo mark stand-in: the five tile colours as a ring of dots.
        public static Sprite Mark => Get("ph_mark", RenderMark, 0);

        static Sprite RoundedRect(string name, int width, int height, float radius, int border) =>
            Get(name, () => Render(width, height, (x, y) => RoundedRectCoverage(x, y, width, height, radius)), border);

        static Sprite Get(string name, System.Func<Texture2D> draw, int border)
        {
            string path = $"{Folder}/{name}.png";
            if (!File.Exists(path))
            {
                TapPopArt.EnsureFolder(Folder);
                Texture2D texture = draw();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.spriteBorder = new Vector4(border, border, border, border);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Texture2D Render(int width, int height, System.Func<float, float, float> coverage)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(coverage(x + 0.5f, y + 0.5f)) * 255f));
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        static readonly Color32[] MarkColors =
        {
            new Color32(0xFF, 0x38, 0x48, 0xFF),
            new Color32(0xFF, 0xD4, 0x14, 0xFF),
            new Color32(0x30, 0xD7, 0x54, 0xFF),
            new Color32(0x29, 0x7F, 0xFF, 0xFF),
            new Color32(0xB6, 0x38, 0xFF, 0xFF),
        };

        static Texture2D RenderMark()
        {
            const int size = 256;
            const float ring = 78f, dot = 36f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color32 c = new Color32(255, 255, 255, 0);
                    for (int i = 0; i < MarkColors.Length; i++)
                    {
                        float angle = Mathf.PI / 2f - i * Mathf.PI * 2f / MarkColors.Length;
                        float dx = x + 0.5f - (size / 2f + Mathf.Cos(angle) * ring);
                        float dy = y + 0.5f - (size / 2f + Mathf.Sin(angle) * ring);
                        float coverage = Mathf.Clamp01(dot + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
                        if (coverage <= 0f) continue;
                        c = MarkColors[i];
                        c.a = (byte)(coverage * 255f);
                        break;
                    }
                    pixels[y * size + x] = c;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        // Signed distance to a rounded rectangle, turned into one pixel of anti-aliasing.
        static float RoundedRectCoverage(float x, float y, int width, int height, float radius)
        {
            float qx = Mathf.Abs(x - width * 0.5f) - (width * 0.5f - radius);
            float qy = Mathf.Abs(y - height * 0.5f) - (height * 0.5f - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            float distance = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
            return 0.5f - distance;
        }

        // Five-point star, 4x4 supersampled.
        static float StarCoverage(float x, float y)
        {
            int hits = 0;
            for (int sy = 0; sy < 4; sy++)
                for (int sx = 0; sx < 4; sx++)
                    if (InStar(x - 0.5f + (sx + 0.5f) / 4f, y - 0.5f + (sy + 0.5f) / 4f)) hits++;
            return hits / 16f;
        }

        static readonly Vector2[] StarPoints = BuildStar();

        static Vector2[] BuildStar()
        {
            var points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = i % 2 == 0 ? 60f : 27f;
                points[i] = new Vector2(64f + Mathf.Cos(angle) * r, 60f + Mathf.Sin(angle) * r);
            }
            return points;
        }

        static bool InStar(float x, float y)
        {
            bool inside = false;
            for (int i = 0, j = StarPoints.Length - 1; i < StarPoints.Length; j = i++)
            {
                Vector2 a = StarPoints[i], b = StarPoints[j];
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }
    }
}
