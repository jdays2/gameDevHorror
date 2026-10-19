using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Editor
{
    /// <summary>Offline paint-mask rasterizer and prefab authoring. No runtime generation or scene replacement.</summary>
    public static class FloorMarkingsAuthoring
    {
        public const string ArtPath = "Assets/EmergencyShift/Art/Environment/FloorMarkings";
        public const string PrefabPath = "Assets/EmergencyShift/Prefabs/Environment/FloorMarkings";
        public const int Resolution = 512;
        public const float PixelsPerUnit = 256;
        public const float ThinWidth = .125f;
        public const float ThickWidth = .25f;
        private const string SourcePath = "Concepts/Used/Environment/FloorMarkings01/Sources/paint-coverage.png";
        public static readonly string[] Names =
        {
            "thin-straight", "thin-corner", "thin-tee", "thin-cross",
            "thick-straight", "thick-corner", "thick-tee", "thick-cross",
            "arrow-forward", "arrow-turn", "arrow-double", "chevron",
            "thin-dashes", "thick-dashes", "thin-end", "thick-end"
        };

        [MenuItem("Emergency Shift/Art/Generate floor marking assets")]
        public static void Generate()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
            {
                throw new InvalidOperationException("Stop Play and compilation before generating assets.");
            }

            Directory.CreateDirectory(ArtPath);
            Directory.CreateDirectory(PrefabPath);
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                if (!source.LoadImage(File.ReadAllBytes(Path.Combine(projectRoot, SourcePath))))
                {
                    throw new IOException("Cannot decode paint coverage source.");
                }

                var paint = new PaintCoverage(source);
                for (int index = 0; index < Names.Length; index++)
                {
                    WriteSprite(index, paint);
                }

                AssetDatabase.Refresh();
                foreach (string name in Names)
                {
                    ConfigureImporter(name);
                    CreatePrefab(name, name, 2);
                }

                CreatePrefab("thin-straight-short", "thin-straight", 1);
                CreatePrefab("thin-straight-long", "thin-straight", 4);
                CreatePrefab("thick-straight-short", "thick-straight", 1);
                CreatePrefab("thick-straight-long", "thick-straight", 4);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private sealed class PaintCoverage
        {
            private readonly Color32[] pixels;
            private readonly int width;
            private readonly int height;

            public PaintCoverage(Texture2D texture)
            {
                pixels = texture.GetPixels32();
                width = texture.width;
                height = texture.height;
            }

            public float At(int x, int y)
            {
                var color = pixels[(y * height / Resolution) * width + x * width / Resolution];
                return (color.r + color.g + color.b) / (3f * 255);
            }
        }

        private static void WriteSprite(int shape, PaintCoverage paint)
        {
            var pixels = new Color32[Resolution * Resolution];
            float width = shape < 4 || shape == 12 || shape == 14 ? ThinWidth : ThickWidth;
            for (int y = 0; y < Resolution; y++)
            {
                for (int x = 0; x < Resolution; x++)
                {
                    var point = new Vector2((x + .5f) * 2 / Resolution - 1, (y + .5f) * 2 / Resolution - 1);
                    float grain = paint.At(x, y);
                    // Matching dense collars keep all module sockets continuous at the cell edges.
                    float atSocket = Mathf.InverseLerp(.96f, 1, Mathf.Max(Mathf.Abs(point.x), Mathf.Abs(point.y)));
                    float interior = 1 - Mathf.SmoothStep(0, 1, atSocket);
                    float edge = Distance(shape, point, width) + (grain - .92f) * .008f * interior;
                    float coverage = Mathf.Clamp01(.5f + edge * Resolution / 2);
                    float wear = Mathf.Clamp01((grain - .72f) / .25f);
                    float alpha = coverage * Mathf.Lerp(1, wear, interior);
                    pixels[y * Resolution + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255));
                }
            }

            var output = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);
            try
            {
                output.SetPixels32(pixels);
                output.Apply();
                File.WriteAllBytes(ArtPath + "/" + Names[shape] + ".png", output.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(output);
            }
        }

        private static float Rect(Vector2 p, float left, float bottom, float right, float top)
        {
            return Mathf.Min(Mathf.Min(p.x - left, right - p.x), Mathf.Min(p.y - bottom, top - p.y));
        }

        private static float Triangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            return Mathf.Min(HalfPlane(p, a, b), Mathf.Min(HalfPlane(p, b, c), HalfPlane(p, c, a)));
        }

        private static float HalfPlane(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 edge = b - a;
            Vector2 offset = p - a;
            return (edge.x * offset.y - edge.y * offset.x) / edge.magnitude;
        }

        private static float Stroke(Vector2 p, Vector2 a, Vector2 b, float width)
        {
            Vector2 direction = b - a;
            float along = Mathf.Clamp01(Vector2.Dot(p - a, direction) / direction.sqrMagnitude);
            return width / 2 - Vector2.Distance(p, a + direction * along);
        }

        private static float ArrowHead(Vector2 p)
        {
            return Triangle(p, new Vector2(-.42f, .15f), new Vector2(.42f, .15f), new Vector2(0, .85f));
        }

        private static float Distance(int shape, Vector2 p, float width)
        {
            float half = width / 2;
            float horizontal = half - Mathf.Abs(p.y);
            float vertical = half - Mathf.Abs(p.x);
            if (shape < 8)
            {
                switch (shape % 4)
                {
                    case 0:
                        return horizontal;
                    case 1:
                        return Mathf.Max(Rect(p, -half, -half, 1, half), Rect(p, -half, -half, half, 1));
                    case 2:
                        return Mathf.Max(horizontal, Rect(p, -half, -half, half, 1));
                    default:
                        return Mathf.Max(horizontal, vertical);
                }
            }

            switch (shape)
            {
                case 8:
                    return Mathf.Max(Rect(p, -.125f, -.85f, .125f, .3f), ArrowHead(p));
                case 9:
                    float elbow = Mathf.Max(Rect(p, -1, -.55f, .125f, -.3f), Rect(p, -.125f, -.55f, .125f, .3f));
                    return Mathf.Max(elbow, ArrowHead(p));
                case 10:
                    float right = Triangle(p, new Vector2(.35f, -.4f), new Vector2(.9f, 0), new Vector2(.35f, .4f));
                    float left = Triangle(p, new Vector2(-.35f, .4f), new Vector2(-.9f, 0), new Vector2(-.35f, -.4f));
                    return Mathf.Max(Rect(p, -.55f, -.125f, .55f, .125f), Mathf.Max(right, left));
                case 11:
                    return Mathf.Max(Stroke(p, new Vector2(-.7f, -.3f), new Vector2(0, .4f), .18f), Stroke(p, new Vector2(0, .4f), new Vector2(.7f, -.3f), .18f));
                case 12:
                case 13:
                    return Mathf.Max(Rect(p, -1, -half, -.5f, half), Mathf.Max(Rect(p, -.25f, -half, .25f, half), Rect(p, .5f, -half, 1, half)));
                default:
                    return Mathf.Max(Rect(p, -.65f, -half, 1, half), half - Vector2.Distance(p, new Vector2(-.65f, 0)));
            }
        }

        private static void ConfigureImporter(string name)
        {
            string path = ArtPath + "/" + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.spritePivot = new Vector2(.5f, .5f);
            importer.spriteBorder = Vector4.zero;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = Resolution;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static void CreatePrefab(string name, string source, float length)
        {
            string path = PrefabPath + "/" + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                return; // Existing hand-authored prefab overrides belong to its author.
            }

            var obj = new GameObject(name, typeof(SpriteRenderer));
            try
            {
                var renderer = obj.GetComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "/" + source + ".png");
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergencyShift/Materials/Shared/FlatLit.mat");
                renderer.color = Color.white;
                renderer.sortingOrder = -9;
                if (source.EndsWith("straight", StringComparison.Ordinal))
                {
                    renderer.drawMode = SpriteDrawMode.Tiled;
                    renderer.tileMode = SpriteTileMode.Continuous;
                    renderer.size = new Vector2(length, 2);
                }

                PrefabUtility.SaveAsPrefabAsset(obj, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
    }
}
