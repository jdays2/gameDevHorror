using System;
using System.Collections.Generic;
using System.Linq;
using EmergencyShift.Features.MainMenu.Config;
using EmergencyShift.Features.MainMenu.Logic;
using UnityEditor;

namespace EmergencyShift.Editor
{
    public static class MainMenuSceneRegistry
    {
        public const string SceneRoot = "Assets/EmergencyShift/Scenes";

        public static void Synchronize()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<MainMenuCatalog>(MainMenuAuthoring.CatalogPath);
            if (catalog == null || AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuAuthoring.ScenePath) == null)
            {
                return;
            }

            string[] paths = AssetDatabase.FindAssets("t:Scene", new[] { SceneRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path != MainMenuAuthoring.ScenePath)
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
            var data = new SerializedObject(catalog);
            SerializedProperty entries = data.FindProperty("scenes");
            var remaining = new HashSet<string>(paths, StringComparer.Ordinal);
            var desired = new List<(string id, string title, string path)>();
            for (int index = 0; index < entries.arraySize; index++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                string path = entry.FindPropertyRelative("scenePath").stringValue;
                if (remaining.Remove(path))
                {
                    desired.Add((entry.FindPropertyRelative("id").stringValue,
                        entry.FindPropertyRelative("title").stringValue, path));
                }
            }

            foreach (string path in paths)
            {
                if (remaining.Contains(path))
                {
                    string title = System.IO.Path.GetFileNameWithoutExtension(path);
                    desired.Add((AssetDatabase.AssetPathToGUID(path), title, path));
                }
            }

            // Validate the replacement before changing the shared authored asset.
            _ = new MenuSceneCatalog(data.FindProperty("homePath").stringValue,
                data.FindProperty("newGameId").stringValue,
                desired.Select(entry => new MenuScene(entry.id, entry.title, entry.path)));
            if (entries.arraySize != desired.Count)
            {
                entries.arraySize = desired.Count;
            }
            for (int index = 0; index < desired.Count; index++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                SetIfChanged(entry.FindPropertyRelative("id"), desired[index].id);
                SetIfChanged(entry.FindPropertyRelative("title"), desired[index].title);
                SetIfChanged(entry.FindPropertyRelative("scenePath"), desired[index].path);
            }

            if (data.ApplyModifiedPropertiesWithoutUndo())
            {
                catalog.ToCatalog();
                AssetDatabase.SaveAssetIfDirty(catalog);
            }

            var build = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(MainMenuAuthoring.ScenePath, true) };
            build.AddRange(desired.Select(entry => new EditorBuildSettingsScene(entry.path, true)));
            var ownedPaths = new HashSet<string>(paths, StringComparer.Ordinal) { MainMenuAuthoring.ScenePath };
            build.AddRange(EditorBuildSettings.scenes.Where(scene => !ownedPaths.Contains(scene.path)
                && !scene.path.StartsWith(SceneRoot + "/", StringComparison.Ordinal)));
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            if (!current.Select(scene => (scene.path, scene.enabled)).SequenceEqual(build.Select(scene => (scene.path, scene.enabled))))
            {
                EditorBuildSettings.scenes = build.ToArray();
            }
        }

        private static void SetIfChanged(SerializedProperty field, string value)
        {
            if (field.stringValue != value)
            {
                field.stringValue = value;
            }
        }
    }
}
