using System.IO;
using EmergencyShift;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildWeaponFxBenchmark
{
    public static object Prepare()
    {
        // This recipe is for an isolated verification copy, never an authored gameplay scene.
        EditorUserBuildSettings.development = true;
        PlayerSettings.enableFrameTimingStats = true;
        string symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
        if (!symbols.Contains("UNITY_INCLUDE_INSTRUMENTATION"))
        {
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, symbols + ";UNITY_INCLUDE_INSTRUMENTATION");
        }
        const string scenePath = "Assets/EmergencyShift/Scenes/FxBenchmark.unity";
        const string baselinePath = "Assets/EmergencyShift/Prefabs/Combat/FxBaseline.prefab";
        var baseline = PrefabUtility.LoadPrefabContents("Assets/EmergencyShift/Prefabs/Combat/PistolShotEffect.prefab");
        try
        {
            baseline.GetComponent<ShotEffect2D>().ConfigureSmokePrefab(null);
            PrefabUtility.SaveAsPrefabAsset(baseline, baselinePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(baseline);
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
        {
            AssetDatabase.CopyAsset("Assets/EmergencyShift/Scenes/MechanicsSandbox.unity", scenePath);
        }
        var scene = EditorSceneManager.OpenScene(scenePath);
        foreach (var oldProbe in Object.FindObjectsByType<WeaponFxBenchmarkProbe>())
        {
            Object.DestroyImmediate(oldProbe.gameObject);
        }
        var probe = new GameObject("Temporary standalone FX benchmark").AddComponent<WeaponFxBenchmarkProbe>();
        probe.Configure(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/PistolShotEffect.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/PistolBullet.prefab"),
            AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PumpShotgun.asset"));
        probe.ConfigureBaseline(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/FxBaseline.prefab"));
        EditorSceneManager.SaveScene(scene);
        return new { scenePath, PlayerSettings.enableFrameTimingStats };
    }

    public static object ReleaseSettings()
    {
        EditorUserBuildSettings.development = false;
        string symbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
        PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone,
            symbols.Replace(";UNITY_INCLUDE_INSTRUMENTATION", "").Replace("UNITY_INCLUDE_INSTRUMENTATION", ""));
        PlayerSettings.enableFrameTimingStats = false;
        return "Normal release settings restored in the verification copy";
    }
}
