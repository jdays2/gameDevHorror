using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelReuseLibrary
{
    private const string Output = "Assets/EmergencyShift/Prefabs/Environment/LevelSurfaces";

    public static int ConnectAssemblies(Scene scene)
    {
        int connected = 0;
        var blocks = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(transform => transform.name.StartsWith("Full-height block")
                && transform.Find("Perimeter trim - follows block") != null).ToArray();
        foreach (var block in blocks)
        {
            EnsureFolder(Output);
            string name = "FullHeightBlock-" + Mathf.RoundToInt(block.lossyScale.x * 100) + "x" + Mathf.RoundToInt(block.lossyScale.y * 100);
            string path = Output + "/" + name + ".prefab";
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (template == null)
            {
                template = PrefabUtility.SaveAsPrefabAssetAndConnect(block.gameObject, path, InteractionMode.AutomatedAction);
                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    contents.transform.localPosition = Vector3.zero;
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
            else
            {
                // Rebind the authored assembly to the same durable variant instead of making an identical copy.
                PrefabUtility.UnpackPrefabInstance(block.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                PrefabUtility.ConvertToPrefabInstance(block.gameObject, template, Settings(), InteractionMode.AutomatedAction);
            }
            connected++;
        }
        return connected;
    }

    public static int ConnectExamples(Scene scene)
    {
        const string directory = "Assets/EmergencyShift/Prefabs/Levels/AuthoringExamples";
        int connected = 0;
        var candidates = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
            .Where(component => component != null && (component.GetType().Name == "SandboxAmmoSupply2D"
                || component.GetType().Name == "DialogueInteraction2D")
                && string.IsNullOrEmpty(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(component.gameObject))).ToArray();
        foreach (var component in candidates)
        {
            EnsureFolder(directory);
            string path = directory + "/" + component.GetType().Name + ".prefab";
            Vector3 position = component.transform.position;
            Quaternion rotation = component.transform.rotation;
            PrefabUtility.SaveAsPrefabAssetAndConnect(component.gameObject, path, InteractionMode.AutomatedAction);
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                contents.transform.localPosition = Vector3.zero;
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
            component.transform.SetPositionAndRotation(position, rotation);
            PrefabUtility.RecordPrefabInstancePropertyModifications(component.transform);
            connected++;
        }
        return connected;
    }

    public static int ConnectLooseSurfaces(Scene scene)
    {
        int connected = 0;
        var candidates = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true))
            .Where(renderer => renderer.transform.childCount == 0 && renderer.GetComponents<Component>().Length == 2
                && renderer.sprite != null
                && string.IsNullOrEmpty(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(renderer.gameObject))).ToArray();
        var templates = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/EmergencyShift/Prefabs" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(prefab => prefab != null && prefab.transform.childCount == 0 && prefab.GetComponents<Component>().Length == 2
                && prefab.GetComponent<SpriteRenderer>() != null).ToList();
        foreach (var renderer in candidates)
        {
            var template = templates.FirstOrDefault(prefab => SameSurface(prefab.GetComponent<SpriteRenderer>(), renderer));
            if (template == null)
            {
                EnsureFolder(Output);
                string family = Family(renderer);
                string name = family + "-" + renderer.sprite.name + "-" + renderer.drawMode;
                string path = Output + "/" + name + ".prefab";
                template = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (template == null)
                {
                    var source = new GameObject(name);
                    Scene preview = EditorSceneManager.NewPreviewScene();
                    try
                    {
                        SceneManager.MoveGameObjectToScene(source, preview);
                        var surface = source.AddComponent<SpriteRenderer>();
                        surface.sprite = renderer.sprite;
                        surface.sharedMaterial = renderer.sharedMaterial;
                        surface.drawMode = renderer.drawMode;
                        surface.size = Vector2.one;
                        surface.sortingLayerID = renderer.sortingLayerID;
                        surface.sortingOrder = renderer.sortingOrder;
                        template = PrefabUtility.SaveAsPrefabAsset(source, path);
                    }
                    finally
                    {
                        EditorSceneManager.ClosePreviewScene(preview);
                    }
                }
                templates.Add(template);
            }
            // Unity records existing colour, size, sorting and other authored values as overrides.
            PrefabUtility.ConvertToPrefabInstance(renderer.gameObject, template, Settings(), InteractionMode.AutomatedAction);
            connected++;
        }
        return connected;
    }

    private static ConvertToPrefabInstanceSettings Settings()
    {
        return new ConvertToPrefabInstanceSettings
        {
            objectMatchMode = ObjectMatchMode.ByHierarchy,
            componentsNotMatchedBecomesOverride = true,
            gameObjectsNotMatchedBecomesOverride = true,
            recordPropertyOverridesOfMatches = true,
            changeRootNameToAssetName = false
        };
    }

    public static bool HasPrefabAncestor(Transform transform)
    {
        while (transform != null)
        {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(transform.gameObject))
            {
                return true;
            }
            transform = transform.parent;
        }
        return false;
    }

    private static bool SameSurface(SpriteRenderer template, SpriteRenderer instance)
    {
        return template.sprite == instance.sprite && template.sharedMaterial == instance.sharedMaterial
            && template.drawMode == instance.drawMode;
    }

    private static string Family(SpriteRenderer renderer)
    {
        string path = AssetDatabase.GetAssetPath(renderer.sprite);
        if (path.Contains("BloodPools"))
        {
            return "BloodDecal";
        }
        if (path.Contains("CreatureFlesh"))
        {
            return "FleshDecal";
        }
        if (path.Contains("Floor"))
        {
            return "Floor";
        }
        return renderer.sortingOrder <= -9 ? "FloorBase" : "FlatMarking";
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
