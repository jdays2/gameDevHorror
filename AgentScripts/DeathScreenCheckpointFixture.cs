using System;
using System.IO;
using System.Linq;
using EmergencyShift;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class DeathScreenCheckpointFixture
{
    public const string Folder = "Assets/EmergencyShift/Tests/Fixtures/DeathScreenTask006";
    public const string ScenePath = Folder + "/CheckpointScene.unity";
    public static string Work => Path.GetFullPath(Path.Combine(Application.dataPath, "../../../work/task006-deathscreen"));
    public static string Slot => Path.Combine(Work, "checkpoint", "slot.json");

    public static object Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Use a stopped, compiled Editor.");
        }
        if (AssetDatabase.IsValidFolder(Folder) || Directory.Exists(Folder))
        {
            throw new InvalidOperationException("A fixture already exists; inspect/clean up the owned fixture first.");
        }
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        foreach (SceneSetup item in setup)
        {
            if (SceneManager.GetSceneByPath(item.path).isDirty)
            {
                throw new InvalidOperationException("Preserve dirty authored scenes before fixture work.");
            }
        }
        bool createdParent = !AssetDatabase.IsValidFolder("Assets/EmergencyShift/Tests/Fixtures");
        EnsureFolder(Folder);
        Directory.CreateDirectory(Work);
        var originalBuild = EditorBuildSettings.scenes.Select(item => new { item.path, item.enabled }).ToArray();
        File.WriteAllText(Path.Combine(Work, "checkpoint-fixture-before.json"),
            JsonConvert.SerializeObject(new { setup, build = originalBuild, createdParent }, Formatting.Indented));
        File.WriteAllText(Folder + "/owner.json", "{\"task\":\"TASK-006\",\"temporary\":true}");

        Scene template = SceneManager.GetSceneByPath("Assets/EmergencyShift/Scenes/LevelTemplate.unity");
        bool opened = !template.IsValid() || !template.isLoaded;
        if (opened)
        {
            template = EditorSceneManager.OpenScene("Assets/EmergencyShift/Scenes/LevelTemplate.unity", OpenSceneMode.Additive);
        }
        if (!EditorSceneManager.SaveScene(template, ScenePath, true))
        {
            throw new IOException("Fixture scene copy failed.");
        }
        if (opened)
        {
            EditorSceneManager.CloseScene(template, true);
        }

        var definition = ScriptableObject.CreateInstance<LevelDefinition>();
        var serialized = new SerializedObject(definition);
        serialized.FindProperty("levelId").stringValue = "task006-checkpoint-validation";
        serialized.FindProperty("scenePath").stringValue = ScenePath;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        definition.Validate();
        AssetDatabase.CreateAsset(definition, Folder + "/Definition.asset");
        var catalog = ScriptableObject.CreateInstance<LevelCatalog>();
        serialized = new SerializedObject(catalog);
        serialized.FindProperty("levels").arraySize = 1;
        serialized.FindProperty("levels").GetArrayElementAtIndex(0).objectReferenceValue = definition;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        catalog.Validate();
        AssetDatabase.CreateAsset(catalog, Folder + "/Catalog.asset");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var context = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LevelContext2D>(true)).Single();
        PlayerAccessCards2D cards = context.Player.GetComponent<PlayerAccessCards2D>();
        if (cards == null)
        {
            cards = context.Player.gameObject.AddComponent<PlayerAccessCards2D>();
        }
        var root = new GameObject("Checkpoint Fixture Services");
        SceneManager.MoveGameObjectToScene(root, scene);
        var run = root.AddComponent<LevelRunController2D>();
        run.Configure(context.Player, cards);
        run.SetDefinition(definition, null);
        var checkpoint = root.AddComponent<LevelCheckpointController2D>();
        checkpoint.Configure(catalog, definition, run, context.Player);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        }).ToArray();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var result = new { fixture = ScenePath, slot = Slot, originalScenes = setup.Length,
            originalBuildEntries = originalBuild.Length, temporaryBuildEntry = true };
        return result;
    }

    public static object CompleteFixturePlayer()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Use stopped fixture.");
        }
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        var context = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LevelContext2D>(true)).Single();
        PlayerAccessCards2D cards = context.Player.GetComponent<PlayerAccessCards2D>();
        if (cards == null)
        {
            cards = context.Player.gameObject.AddComponent<PlayerAccessCards2D>();
        }
        var run = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LevelRunController2D>(true)).Single();
        run.Configure(context.Player, cards);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        foreach (SceneSetup item in EditorSceneManager.GetSceneManagerSetup())
        {
            if (item.path != ScenePath && SceneManager.GetSceneByPath(item.path).isDirty)
            {
                throw new InvalidOperationException("Unrelated dirty scene must remain loaded.");
            }
        }
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return new { fixturePlayerCompleted = true, loadedScenes = SceneManager.sceneCount };
    }

    public static object Cleanup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Stop Play before fixture cleanup.");
        }
        JObject state = JObject.Parse(File.ReadAllText(Path.Combine(Work, "checkpoint-fixture-before.json")));
        SceneSetup[] setup = state["setup"].ToObject<SceneSetup[]>();
        foreach (SceneSetup item in EditorSceneManager.GetSceneManagerSetup())
        {
            if (item.path != ScenePath && SceneManager.GetSceneByPath(item.path).isDirty)
            {
                throw new InvalidOperationException("Preserve unrelated unsaved scene: " + item.path);
            }
        }
        EditorSceneManager.RestoreSceneManagerSetup(setup);
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(item => item.path != ScenePath).ToArray();
        string actual = Path.GetFullPath(Folder);
        string allowed = Path.GetFullPath(Path.Combine(Application.dataPath, "EmergencyShift/Tests/Fixtures/DeathScreenTask006"));
        if (!string.Equals(actual, allowed, StringComparison.OrdinalIgnoreCase) ||
            File.ReadAllText(Folder + "/owner.json") != "{\"task\":\"TASK-006\",\"temporary\":true}")
        {
            throw new InvalidOperationException("Refuse cleanup outside the owned fixture.");
        }
        bool deleted = AssetDatabase.DeleteAsset(Folder);
        bool createdParent = state["createdParent"].Value<bool>();
        if (createdParent && Directory.GetFileSystemEntries("Assets/EmergencyShift/Tests/Fixtures").Length == 0)
        {
            string parent = Path.GetFullPath("Assets/EmergencyShift/Tests/Fixtures");
            string allowedParent = Path.GetFullPath(Path.Combine(Application.dataPath, "EmergencyShift/Tests/Fixtures"));
            if (string.Equals(parent, allowedParent, StringComparison.OrdinalIgnoreCase))
            {
                AssetDatabase.DeleteAsset("Assets/EmergencyShift/Tests/Fixtures");
            }
        }
        var currentBuild = EditorBuildSettings.scenes.Select(item => new { item.path, item.enabled }).ToArray();
        bool buildRestored = JToken.DeepEquals(state["build"], JToken.FromObject(currentBuild));
        var result = new { fixtureDeleted = deleted && !AssetDatabase.IsValidFolder(Folder), buildRestored,
            current = SceneManager.GetActiveScene().path, currentDirty = SceneManager.GetActiveScene().isDirty,
            originalMapsNeverSavedByFixture = true };
        Directory.CreateDirectory("Docs/Validation/DeathScreenD01");
        File.WriteAllText("Docs/Validation/DeathScreenD01/checkpoint-fixture-cleanup.json",
            JsonConvert.SerializeObject(result, Formatting.Indented));
        if (!deleted || !buildRestored)
        {
            throw new InvalidOperationException("Fixture cleanup or Build Settings restoration failed.");
        }
        return result;
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }
    }
}
