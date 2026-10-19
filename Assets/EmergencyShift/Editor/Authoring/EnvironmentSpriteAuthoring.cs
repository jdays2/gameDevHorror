using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace EmergencyShift.Editor
{
    /// <summary>Shared offline sprite import for silhouette-based URP shadows.</summary>
    internal static class EnvironmentSpriteAuthoring
    {
        private const float ShadowAlphaCutoff = .2f;
        private const float OutlineDetail = .25f;
        private const float OutlineTolerancePixels = 2f;

        internal static void Import(string path, float pixelsPerUnit)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.npotScale = TextureImporterNPOTScale.None;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            // URP's SpriteRenderer shadow provider extrudes this mesh, including its alpha outline.
            settings.spriteMeshType = SpriteMeshType.Tight;
            settings.spriteGenerateFallbackPhysicsShape = true;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            RebuildAlphaOutline(path);
        }

        internal static void RebuildAlphaOutline(string path)
        {
            if (Application.isPlaying)
            {
                throw new InvalidOperationException("Stop Play before rebuilding an imported outline.");
            }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected a single-sprite PNG: " + path);
            }

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null)
            {
                throw new InvalidOperationException("Sprite Editor data is unavailable: " + path);
            }

            provider.InitSpriteEditorDataProvider();
            var capability = provider.GetDataProvider<ISpriteFrameEditCapability>();
            var outlines = provider.GetDataProvider<ISpriteOutlineDataProvider>();
            if (capability == null || !capability.GetEditCapability().HasCapability(EEditCapability.EditSpriteRect)
                || outlines == null)
            {
                throw new InvalidOperationException("Importer does not support outline editing: " + path);
            }

            var rects = provider.GetSpriteRects();
            if (rects.Length != 1)
            {
                throw new InvalidOperationException("Expected exactly one sprite rectangle: " + path);
            }

            // Read the current source pixels, rather than an already imported sprite's cached data.
            // Unity's native outline generator is internal; fail explicitly if its signature changes.
            var utility = typeof(TextureImporter).Assembly.GetType("UnityEditor.Sprites.SpriteUtility");
            var generate = utility?.GetMethod("GenerateOutline", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(Texture2D), typeof(Rect), typeof(float), typeof(byte), typeof(bool),
                    typeof(Vector2[][]).MakeByRefType(), typeof(bool) }, null);
            if (generate == null)
            {
                throw new InvalidOperationException("This Unity version lacks the sprite outline authoring API.");
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Vector2[][] paths;
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path)))
                {
                    throw new InvalidOperationException("Could not read source pixels: " + path);
                }

                // Provider rectangles and returned contours use original-image pixels, not world units.
                object[] arguments = { texture, rects[0].rect, OutlineDetail,
                    (byte)(ShadowAlphaCutoff * 255), true, null, true };
                generate.Invoke(null, arguments);
                paths = (Vector2[][])arguments[5];
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            if (paths == null || paths.Length == 0)
            {
                throw new InvalidOperationException("No opaque prop outline: " + path);
            }

            var simplified = new List<Vector2[]>(paths.Length);
            foreach (var contour in paths)
            {
                simplified.Add(SimplifyContour(contour));
            }

            outlines.SetOutlines(rects[0].spriteID, simplified);
            outlines.SetTessellationDetail(rects[0].spriteID, OutlineDetail);
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static Vector2[] SimplifyContour(Vector2[] contour)
        {
            // Small contours are already cheap; retain their narrow corners verbatim.
            if (contour.Length <= 16)
            {
                return contour;
            }

            // Close the polyline so RDP also checks the wrap-around edge. Bound the change
            // in source pixels, rather than discarding a fixed percentage of vertices.
            var closed = new List<Vector3>(contour.Length + 1);
            foreach (var point in contour)
            {
                closed.Add(point);
            }

            closed.Add(contour[0]);
            var simplified = new List<Vector3>();
            LineUtility.Simplify(closed, OutlineTolerancePixels, simplified);
            if (simplified.Count < 4)
            {
                return contour;
            }

            var result = new Vector2[simplified.Count - 1];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = simplified[i];
            }

            float originalArea = SignedArea(contour);
            float newArea = SignedArea(result);
            if (originalArea * newArea <= 0 || Mathf.Abs(newArea - originalArea) > Mathf.Abs(originalArea) * .01f)
            {
                return contour;
            }

            // Near-touching edges can cross after RDP, which breaks Unity's triangulation.
            // Keep the original contour when simplification changes its topology.
            for (int i = 0; i < result.Length; i++)
            {
                int nextI = (i + 1) % result.Length;
                for (int j = i + 1; j < result.Length; j++)
                {
                    int nextJ = (j + 1) % result.Length;
                    if (nextI != j && nextJ != i && SegmentsIntersect(result[i], result[nextI], result[j], result[nextJ]))
                    {
                        return contour;
                    }
                }
            }

            return result;
        }

        private static float SignedArea(Vector2[] contour)
        {
            float area = 0;
            for (int i = 0; i < contour.Length; i++)
            {
                Vector2 a = contour[i];
                Vector2 b = contour[(i + 1) % contour.Length];
                area += a.x * b.y - a.y * b.x;
            }

            return area * .5f;
        }

        private static bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            if (Mathf.Max(a.x, b.x) < Mathf.Min(c.x, d.x) || Mathf.Max(c.x, d.x) < Mathf.Min(a.x, b.x)
                || Mathf.Max(a.y, b.y) < Mathf.Min(c.y, d.y) || Mathf.Max(c.y, d.y) < Mathf.Min(a.y, b.y))
            {
                return false;
            }

            Vector2 ab = b - a;
            Vector2 cd = d - c;
            Vector2 ac = c - a;
            Vector2 ad = d - a;
            Vector2 ca = a - c;
            Vector2 cb = b - c;
            float crossC = ab.x * ac.y - ab.y * ac.x;
            float crossD = ab.x * ad.y - ab.y * ad.x;
            float crossA = cd.x * ca.y - cd.y * ca.x;
            float crossB = cd.x * cb.y - cd.y * cb.x;
            return crossC * crossD <= 0 && crossA * crossB <= 0;
        }
    }
}
