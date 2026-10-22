using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift.Editor
{
    /// <summary>Offline import and prefab authoring for this prop family only. Never edits scenes.</summary>
    public static class HospitalServicePrefabAuthoring
    {
        private const string Family = "HospitalService01";
        private const string ArtRoot = "Assets/EmergencyShift/Art/Environment/" + Family;
        private const string PrefabRoot = "Assets/EmergencyShift/Prefabs/Environment/" + Family;
        private const string RecipePath = "Concepts/Used/Environment/" + Family + "/import-recipes.json";

        [Serializable]
        private sealed class Recipe
        {
            public string id;
            public float pixelsPerUnit;
            public int[] canvas;
            public int[] opaqueBounds;
            public string heightClass;
            public string collider;
        }

        [MenuItem("Emergency Shift/Art/Build Hospital Service Prefabs")]
        public static void BuildPrefabs()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                throw new InvalidOperationException("Stop Play before importing hospital props.");
            }

            var recipes = JsonConvert.DeserializeObject<Recipe[]>(File.ReadAllText(RecipePath));
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergencyShift/Materials/Shared/FlatLit.mat");
            if (material == null || recipes == null)
            {
                throw new InvalidOperationException("Missing hospital prop recipes or FlatLit material.");
            }

            ValidateNewRecipes(recipes);
            Directory.CreateDirectory(PrefabRoot);
            AssetDatabase.Refresh();
            var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                foreach (var recipe in recipes)
                {
                    ImportSprite(recipe);
                    CreatePrefab(recipe, material, preview);
                }

                AssetDatabase.SaveAssets();
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        private static void ValidateNewRecipes(Recipe[] recipes)
        {
            if (recipes.Length == 0)
            {
                throw new InvalidOperationException("Hospital prop recipes are empty.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (Recipe recipe in recipes)
            {
                if (recipe == null || string.IsNullOrWhiteSpace(recipe.id) ||
                    recipe.id != Path.GetFileName(recipe.id) || recipe.id.Contains("..") ||
                    !ids.Add(recipe.id) || !float.IsFinite(recipe.pixelsPerUnit) || recipe.pixelsPerUnit <= 0 ||
                    recipe.canvas == null || recipe.canvas.Length != 2 || recipe.canvas[0] <= 0 || recipe.canvas[1] <= 0 ||
                    recipe.opaqueBounds == null || recipe.opaqueBounds.Length != 4 ||
                    recipe.opaqueBounds[0] < 0 || recipe.opaqueBounds[1] < 0 ||
                    recipe.opaqueBounds[2] <= recipe.opaqueBounds[0] || recipe.opaqueBounds[3] <= recipe.opaqueBounds[1] ||
                    recipe.opaqueBounds[2] > recipe.canvas[0] || recipe.opaqueBounds[3] > recipe.canvas[1] ||
                    (recipe.collider != "outline" && recipe.collider != "circle" && recipe.collider != "box"))
                {
                    throw new InvalidOperationException("Invalid or duplicate hospital prop recipe.");
                }

                string artPath = ArtRoot + "/" + recipe.id + ".png";
                string prefabPath = PrefabRoot + "/" + recipe.id + ".prefab";
                if (!File.Exists(artPath) || File.Exists(prefabPath) || File.Exists(prefabPath + ".meta"))
                {
                    throw new InvalidOperationException("Missing source art or existing prefab; edit this family explicitly: " + recipe.id);
                }
            }
        }

        private static void ImportSprite(Recipe recipe)
        {
            string path = ArtRoot + "/" + recipe.id + ".png";
            EnvironmentSpriteAuthoring.Import(path, recipe.pixelsPerUnit);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            // Align the opaque object center, not the generator's uneven transparent margins.
            int[] bounds = recipe.opaqueBounds;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(
                (bounds[0] + bounds[2]) / (2f * recipe.canvas[0]),
                1f - (bounds[1] + bounds[3]) / (2f * recipe.canvas[1]));
            importer.SetTextureSettings(settings);
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static void CreatePrefab(Recipe recipe, Material material, UnityEngine.SceneManagement.Scene preview)
        {
            var obj = new GameObject(recipe.id);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj, preview);
            try
            {
                obj.layer = 8;
                var renderer = obj.AddComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/" + recipe.id + ".png");
                renderer.sharedMaterial = material;
                renderer.sortingOrder = 1;
                AddCollider(obj, renderer.sprite, recipe);

                var shadow = obj.AddComponent<ShadowCaster2D>();
                shadow.castingOption = ShadowCaster2D.ShadowCastingOptions.CastShadow;
                shadow.alphaCutoff = .2f;
                shadow.trimEdge = 0;

                PrefabUtility.SaveAsPrefabAsset(obj, PrefabRoot + "/" + recipe.id + ".prefab");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        private static void AddCollider(GameObject obj, Sprite sprite, Recipe recipe)
        {
            if (recipe.collider == "outline")
            {
                var polygon = obj.AddComponent<PolygonCollider2D>();
                int count = sprite.GetPhysicsShapeCount();
                if (count == 0)
                {
                    throw new InvalidOperationException("Missing imported physics outline: " + recipe.id);
                }

                polygon.pathCount = count;
                var points = new System.Collections.Generic.List<Vector2>();
                for (int i = 0; i < count; i++)
                {
                    sprite.GetPhysicsShape(i, points);
                    polygon.SetPath(i, points);
                }

                return;
            }

            int[] bounds = recipe.opaqueBounds;
            var size = new Vector2(bounds[2] - bounds[0], bounds[3] - bounds[1]) / recipe.pixelsPerUnit;
            if (recipe.collider == "circle")
            {
                var circle = obj.AddComponent<CircleCollider2D>();
                circle.radius = Mathf.Min(size.x, size.y) * .42f;
                return;
            }

            var box = obj.AddComponent<BoxCollider2D>();
            box.size = size * .92f;
        }
    }
}

