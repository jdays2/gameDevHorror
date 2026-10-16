using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Editor
{
    /// <summary>Shared offline sprite import for silhouette-based URP shadows.</summary>
    internal static class EnvironmentSpriteAuthoring
    {
        private const float ShadowAlphaCutoff = .2f;
        private const float OutlineDetail = .05f;

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
            ImportAlphaOutline(importer);
        }

        private static void ImportAlphaOutline(TextureImporter importer)
        {
            // Unity's default Tight outline includes almost transparent fringe fragments.
            // Bake the same alpha threshold as the caster; never trace texture pixels at runtime.
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(importer.assetPath);
            var utility = typeof(TextureImporter).Assembly.GetType("UnityEditor.Sprites.SpriteUtility");
            var generate = utility?.GetMethod("GenerateOutlineFromSprite",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (generate == null)
            {
                throw new InvalidOperationException("This Unity version lacks the sprite outline authoring API.");
            }

            object[] arguments = { sprite, OutlineDetail, (byte)(ShadowAlphaCutoff * 255), true, null };
            generate.Invoke(null, arguments);
            var paths = (Vector2[][])arguments[4];
            if (paths == null || paths.Length == 0)
            {
                throw new InvalidOperationException("No opaque prop outline: " + importer.assetPath);
            }

            var serialized = new SerializedObject(importer);
            var outline = serialized.FindProperty("m_SpriteSheet.m_Outline");
            outline.arraySize = paths.Length;
            for (int i = 0; i < paths.Length; i++)
            {
                var points = outline.GetArrayElementAtIndex(i);
                points.arraySize = paths[i].Length;
                for (int j = 0; j < paths[i].Length; j++)
                {
                    // Native outline coordinates are sprite units; importer contours use centered pixels.
                    points.GetArrayElementAtIndex(j).vector2Value = paths[i][j] * sprite.pixelsPerUnit;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();
        }
    }
}
