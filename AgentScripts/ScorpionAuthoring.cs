using System;
using System.IO;
using System.Linq;
using EmergencyShift;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ScorpionAuthoring
{
    private const string Evidence = "Docs/Validation/ScorpionRoom01/";
    public static object Inspect()
    {
        Scene scene = SceneManager.GetActiveScene();
        var nodes = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var report = new
        {
            scene = scene.path, dirty = scene.isDirty, playing = Application.isPlaying,
            startScene = EditorSceneManager.playModeStartScene == null ? null : AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene),
            nodes = nodes.Select(node => new {
                path = PathOf(node), active = node.gameObject.activeSelf,
                position = new[]{node.position.x,node.position.y,node.position.z},
                rotation = new[]{node.eulerAngles.x,node.eulerAngles.y,node.eulerAngles.z},
                scale = new[]{node.lossyScale.x,node.lossyScale.y,node.lossyScale.z},
                prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(node.gameObject),
                components = node.GetComponents<Component>().Select(c => c == null ? "MISSING" : c.GetType().Name).ToArray(),
                sprite = node.GetComponent<SpriteRenderer>() != null ? new {
                    asset = AssetDatabase.GetAssetPath(node.GetComponent<SpriteRenderer>().sprite),
                    size = node.GetComponent<SpriteRenderer>().size.ToString(),
                    bounds = node.GetComponent<SpriteRenderer>().bounds.ToString(),
                    order = node.GetComponent<SpriteRenderer>().sortingOrder
                } : null
            }).ToArray()
        };
        Directory.CreateDirectory(Evidence);
        string file = File.Exists(Evidence + "before.json") ? "current.json" : "before.json";
        File.WriteAllText(Evidence + file, JsonConvert.SerializeObject(report, Formatting.Indented));
        return new { report.scene, report.dirty, report.playing, count = nodes.Length,
            rooms = nodes.Where(n => n.parent != null && n.parent.name == "10_World").Select(n => new { n.name, position = n.position.ToString() }) };
    }

    public static object RestorePreexistingLampFlags()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.name != "MechanicsSandbox" || scene.isDirty)
        {
            throw new InvalidOperationException("Preserve unsaved scene state before restoring the two recorded lamp flags.");
        }
        var nodes = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var changed = new System.Collections.Generic.List<string>();
        foreach (var pair in new[] { ("Corridor lamp - warm ivory",8443129186733927316L), ("Ward lamp - muted cyan",7347024757643279034L) })
        {
            Transform root = nodes.Single(node => node.name == pair.Item1);
            foreach (var lamp in root.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>(true))
            {
                var source = PrefabUtility.GetCorrespondingObjectFromSource(lamp);
                if (source != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long id) && id == pair.Item2)
                {
                    var serial = new SerializedObject(lamp);
                    var flag = serial.FindProperty("m_ShadowsEnabled");
                    if (!flag.boolValue)
                    {
                        flag.boolValue = true;
                        serial.ApplyModifiedPropertiesWithoutUndo();
                        PrefabUtility.RecordPrefabInstancePropertyModifications(lamp);
                        EditorUtility.SetDirty(lamp);
                        changed.Add(pair.Item1);
                    }
                }
            }
        }
        if (changed.Count > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        return new { restoredRecordedFlags = changed };
    }

    private static string PathOf(Transform node)
    {
        return node.parent == null ? node.name : PathOf(node.parent) + "/" + node.name;
    }
}
