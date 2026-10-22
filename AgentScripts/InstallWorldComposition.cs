using System;
using EmergencyShift.Core.Composition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public static class InstallWorldComposition
{
    private const string Folder = "Assets/EmergencyShift/Resources";
    private const string Path = Folder + "/ProjectContext.prefab";

    public static object Apply()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Path) != null)
        {
            throw new InvalidOperationException("The existing world composition prefab must be reviewed rather than overwritten.");
        }

        if (ProjectContext.TryGetPrefab() != null)
        {
            throw new InvalidOperationException("Another ProjectContext already owns application composition.");
        }

        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets/EmergencyShift", "Resources");
        }

        Scene preview = EditorSceneManager.NewPreviewScene();
        GameObject root = null;
        try
        {
            root = new GameObject("WorldComposition");
            root.SetActive(false);
            SceneManager.MoveGameObjectToScene(root, preview);
            ProjectContext context = root.AddComponent<ProjectContext>();
            WorldInstaller installer = root.AddComponent<WorldInstaller>();
            EmergencyShift.Editor.ThreatSignalAuthoring.ConfigureInstaller(installer);
            EmergencyShift.Editor.PlayerRulesAuthoring.ConfigureInstaller(installer);
            EmergencyShift.Editor.WorldVisualRulesAuthoring.ConfigureInstaller(installer);
            context.Installers = new MonoInstaller[] { installer };
            root.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(root, Path);
        }
        finally
        {
            if (root != null)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            EditorSceneManager.ClosePreviewScene(preview);
        }

        return new { prefab = Path, userSceneSaved = false };
    }
}
