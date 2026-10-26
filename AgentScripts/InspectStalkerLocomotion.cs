using System;
using System.IO;
using System.Linq;
using EmergencyShift;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InspectStalkerLocomotion
{
    public static string Read()
    {
        var rows = new System.Collections.Generic.List<object>();
        foreach (string name in new[] { "MechanicsSandbox", "MorgueRouteTest" })
        {
            string path = "Assets/EmergencyShift/Scenes/" + name + ".unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool alreadyOpen = scene.IsValid() && scene.isLoaded;
            if (!alreadyOpen)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }
            rows.Add(new
            {
                path, scene.isDirty,
                camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Select(c => new { c.name, c.orthographicSize, c.aspect }).ToArray(),
                actors = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ShadowStalker2D>(true)).Select(a => new
                {
                    path = Hierarchy(a.transform),
                    prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(a),
                    position = a.transform.position.ToString("R"), scale = a.transform.lossyScale.ToString("R"),
                    serialized = EditorJsonUtility.ToJson(a),
                    animator = EditorJsonUtility.ToJson(a.GetComponent<StalkerSpriteAnimator>())
                }).ToArray()
            });
            if (!alreadyOpen)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(new { playModeStartScene = EditorSceneManager.playModeStartScene, rows }, Newtonsoft.Json.Formatting.Indented);
        Directory.CreateDirectory("Concepts/Used/Stalker/Locomotion02");
        File.WriteAllText("Concepts/Used/Stalker/Locomotion02/baseline-placements.json", json);
        return json;
    }

    private static string Hierarchy(Transform t) => t.parent == null ? t.name : Hierarchy(t.parent) + "/" + t.name;
}
