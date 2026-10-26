using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class FinalizeClawSandbox
{
    public static object Placement()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.name != "MechanicsSandbox" || scene.isDirty)
            throw new InvalidOperationException("Preserve unsaved scene changes before final placement.");
        Transform world = scene.GetRootGameObjects().Single(g => g.name == "10_World").transform;
        Transform displays = world.Find("R03_WeaponsRange/Dressing/Claw remains · A B C");
        foreach (Transform display in displays)
        {
            Vector3 position = display.position;
            position.x = 47;
            display.position = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(display);
        }

        int restored = 0;
        foreach (Light2D light in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Light2D>(true)))
        {
            Light2D source = PrefabUtility.GetCorrespondingObjectFromSource(light);
            if (source == null)
                continue;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long fileId);
            // The Editor upgraded these old lamp overrides when loading the map.
            // Retain the exact authored flags; their zero shadow intensity is unchanged.
            if ((guid == "ead06a4f0404b5b45a8822f287f7c12e" && fileId == 8443129186733927316L) || (guid == "6e9c7d78be32ed54f8e3677953f62a52" && fileId == 7347024757643279034L))
            {
                light.shadowsEnabled = true;
                light.enabled = true;
                light.intensity = 1.2f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(light);
                restored++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return new
        {
        displaysX = 47, restoredLampFlags = restored, preservedShadowIntensities = true
        }

        ;
    }
}
