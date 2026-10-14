using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>Сохраняет два рендера силуэта и общий материал теней в префабе/сцене.</summary>
    public static class CreatureShadowSetup
    {
        private const string MaterialPath = "Assets/EmergencyShift/Materials/CreatureSilhouetteShadow.mat";
        [MenuItem("Emergency Shift/Add creature ground shadows")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Остановите Play.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != PrototypeSceneBuilder.ScenePath) throw new InvalidOperationException("Откройте BasementPrototype.");
            Material material = EnsureMaterial();
            const string path = "Assets/EmergencyShift/Prefabs/ShadowStalker.prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try { Configure(contents, material); PrefabUtility.SaveAsPrefabAsset(contents, path); }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            foreach (ShadowStalker2D actor in UnityEngine.Object.FindObjectsByType<ShadowStalker2D>(FindObjectsInactive.Include))
                Configure(actor.gameObject, material);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("CREATURE_SHADOWS_READY: animated six-leg silhouettes and short flashlight projection");
        }

        private static void Configure(GameObject root, Material material)
        {
            Transform shadows = root.transform.Find("Ground shadows");
            if (shadows == null)
            {
                shadows = new GameObject("Ground shadows").transform;
                shadows.SetParent(root.transform, false);
            }
            Transform visual = root.transform.Find("Visual");
            SpriteRenderer body = visual.Find("Body").GetComponent<SpriteRenderer>();
            SpriteRenderer contact = Part("Contact shadow", shadows, body.sprite, material);
            SpriteRenderer projection = Part("Flashlight shadow", shadows, body.sprite, material);
            contact.transform.localPosition = new Vector3(0.02f, -0.03f, -0.04f);
            contact.transform.rotation = body.transform.rotation;
            contact.transform.localScale = Vector3.one * 1.04f;
            contact.color = new Color(0, 0, 0, 0.55f);
            contact.enabled = true; projection.enabled = false;
            var component = root.GetComponent<CreatureGroundShadow2D>();
            if (component == null) component = root.AddComponent<CreatureGroundShadow2D>();
            component.Configure(root.GetComponent<ShadowStalker2D>(), visual, contact, projection);
            // Улучшаем только первоначальные слабые значения; ручную настройку сохраняем.
            var settings = new SerializedObject(component);
            UpgradeDefault(settings, "projectionOpacity", 0.40f, 0.62f);
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
            if (PrefabUtility.IsPartOfPrefabInstance(root))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                foreach (Transform child in shadows.GetComponentsInChildren<Transform>()) PrefabUtility.RecordPrefabInstancePropertyModifications(child);
                PrefabUtility.RecordPrefabInstancePropertyModifications(contact); PrefabUtility.RecordPrefabInstancePropertyModifications(projection);
            }
        }
        private static SpriteRenderer Part(string name, Transform parent, Sprite silhouette, Material material)
        {
            Transform existing = parent.Find(name);
            GameObject obj = existing != null ? existing.gameObject : new GameObject(name, typeof(SpriteRenderer));
            if (existing == null) obj.transform.SetParent(parent, false);
            var renderer = obj.GetComponent<SpriteRenderer>();
            renderer.sprite = silhouette; renderer.sharedMaterial = material; renderer.sortingOrder = 0;
            return renderer;
        }
        private static Material EnsureMaterial()
        {
            Shader shader = Shader.Find("EmergencyShift/Creature Silhouette Shadow");
            if (shader == null) throw new InvalidOperationException("Не импортирован шейдер силуэтной тени.");
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null) { existing.shader = shader; EditorUtility.SetDirty(existing); return existing; }
            var material = new Material(shader) { name = "Creature Silhouette Shadow" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }
        private static void UpgradeDefault(SerializedObject settings, string name, float previous, float improved)
        {
            var property = settings.FindProperty(name);
            if (Mathf.Approximately(property.floatValue, previous)) property.floatValue = improved;
        }
    }
}
