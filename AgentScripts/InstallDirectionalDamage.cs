using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using EmergencyShift;
using EmergencyShift.Features.DamageFeedback.Config;
using EmergencyShift.Features.DamageFeedback.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InstallDirectionalDamage
{
    private const string Root = "Assets/EmergencyShift";
    private const string HudPath = Root + "/Prefabs/UI/GameplayHud01.prefab";
    private const string WidgetPath = Root + "/Prefabs/UI/Feedback/DirectionalDamage.prefab";
    private const string StylePath = Root + "/Data/UI/Feedback/DirectionalDamage01.asset";
    private const string MaterialPath = Root + "/Materials/UI/DamageFeedback/DirectionalBlood.mat";
    private const string ShaderPath = Root + "/Materials/UI/DamageFeedback/DirectionalBlood.shader";

    public static object Read()
    {
        GameObject hud = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
        RectTransform rect = hud.GetComponent<RectTransform>();
        return new
        {
            hud = HudPath,
            anchors = rect.anchorMin + " .. " + rect.anchorMax,
            offsets = rect.offsetMin + " .. " + rect.offsetMax,
            children = Enumerable.Range(0, hud.transform.childCount).Select(i => hud.transform.GetChild(i).name).ToArray(),
            activeScene = SceneManager.GetActiveScene().path,
            activeDirty = SceneManager.GetActiveScene().isDirty,
            liveHuds = UnityEngine.Object.FindObjectsByType<GameplayHudView>(FindObjectsInactive.Include)
                .Select(view => new { view.name, scene = view.gameObject.scene.path,
                    canvas = view.GetComponentInParent<Canvas>().renderMode.ToString() }).ToArray(),
            scenesUsingHud = AssetDatabase.FindAssets("t:Scene", new[] { Root })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(path => AssetDatabase.GetDependencies(path, true).Contains(HudPath)).ToArray()
        };
    }

    public static object Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Finish compilation and stop Play before editing the reusable HUD.");
        }

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
        RectTransform rect = existing.GetComponent<RectTransform>();
        if (rect.anchorMin != Vector2.zero || rect.anchorMax != Vector2.one)
        {
            throw new InvalidOperationException("Review the HUD bounds before installing a full-screen feedback layer.");
        }

        if (existing.GetComponentInChildren<DirectionalDamageFeedbackView>(true) != null || File.Exists(WidgetPath) || File.Exists(StylePath))
        {
            throw new InvalidOperationException("Feedback is already present; edit existing assets instead of rebuilding them.");
        }

        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        if (shader == null)
        {
            throw new InvalidOperationException("Import the feedback shader before installing its assets.");
        }

        string[] errors = ShaderUtil.GetShaderMessages(shader).Where(message => message.severity.ToString() == "Error")
            .Select(message => message.message).ToArray();
        if (shader == null || errors.Length != 0)
        {
            throw new InvalidOperationException("The feedback shader must import without errors: " + string.Join("; ", errors));
        }

        Scene active = SceneManager.GetActiveScene();
        bool originallyDirty = active.isDirty;
        var savedScenes = Directory.GetFiles(Root + "/Scenes", "*.unity", SearchOption.AllDirectories)
            .ToDictionary(path => path, Hash);
        string backup = "work/directional-damage/before";
        Directory.CreateDirectory(backup);
        foreach (string path in new[] { HudPath, HudPath + ".meta" })
        {
            string target = backup + "/" + Path.GetFileName(path);
            if (File.Exists(target))
            {
                throw new InvalidOperationException("Keep the existing backup; inspect the previous installation attempt.");
            }

            File.Copy(path, target);
        }

        EnsureFolder(Root + "/Prefabs/UI/Feedback");
        EnsureFolder(Root + "/Data/UI/Feedback");
        var material = new Material(shader) { name = "DirectionalBlood" };
        AssetDatabase.CreateAsset(material, MaterialPath);
        var style = ScriptableObject.CreateInstance<DirectionalDamageStyle>();
        var styleData = new SerializedObject(style);
        styleData.FindProperty("material").objectReferenceValue = material;
        styleData.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(style, StylePath);

        var widget = new GameObject("Directional Damage", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(UnityEngine.UI.RawImage), typeof(DirectionalDamageFeedbackView));
        try
        {
            widget.layer = LayerMask.NameToLayer("UI");
            Stretch(widget.GetComponent<RectTransform>());
            var image = widget.GetComponent<UnityEngine.UI.RawImage>();
            image.color = Color.white;
            image.material = material;
            image.raycastTarget = false;
            image.enabled = false;
            var data = new SerializedObject(widget.GetComponent<DirectionalDamageFeedbackView>());
            data.FindProperty("style").objectReferenceValue = style;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(widget, WidgetPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(widget);
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(HudPath);
        int compared = 0;
        try
        {
            // Verify all pre-existing values before saving. Only the root child list may grow.
            Component[] components = contents.GetComponentsInChildren<Component>(true);
            var before = components.ToDictionary(component => component, component => Snapshot(component, contents.transform));
            Transform[] oldChildren = Enumerable.Range(0, contents.transform.childCount)
                .Select(i => contents.transform.GetChild(i)).ToArray();
            GameObject nested = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WidgetPath), contents.transform);
            Stretch(nested.GetComponent<RectTransform>());
            nested.transform.SetAsFirstSibling();
            for (int i = 0; i < oldChildren.Length; i++)
            {
                if (contents.transform.GetChild(i + 1) != oldChildren[i])
                {
                    throw new InvalidOperationException("The original HUD sibling order changed.");
                }
            }
            foreach (Component component in components)
            {
                if (Snapshot(component, contents.transform) != before[component])
                {
                    throw new InvalidOperationException("Existing HUD state changed before save: " + component.name + " / " + component.GetType().Name);
                }

                compared++;
            }

            PrefabUtility.SaveAsPrefabAsset(contents, HudPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.SaveAssetIfDirty(style);
        AssetDatabase.SaveAssetIfDirty(material);
        foreach (var scene in savedScenes)
        {
            if (Hash(scene.Key) != scene.Value)
            {
                throw new InvalidOperationException("An authored scene changed: " + scene.Key);
            }
        }

        if (SceneManager.GetActiveScene() != active || (originallyDirty && !active.isDirty))
        {
            throw new InvalidOperationException("The active authoring scene or its unsaved state was lost.");
        }

        return new { widget = WidgetPath, style = StylePath, material = MaterialPath,
            existingHudComponentsUnchanged = compared, savedScenesUnchanged = savedScenes.Count, scenesSaved = false };
    }

    private static string Snapshot(Component component, Transform root)
    {
        if (component == null)
        {
            throw new InvalidOperationException("The HUD contains a missing script.");
        }

        var json = Newtonsoft.Json.Linq.JObject.Parse(EditorJsonUtility.ToJson(component));
        if (component is Transform)
        {
            json.Remove("m_RootOrder");
        }
        if (component == root)
        {
            json.Remove("m_Children");
        }

        return json.ToString(Newtonsoft.Json.Formatting.None);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
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

    private static string Hash(string path)
    {
        using (SHA256 sha = SHA256.Create())
        {
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
        }
    }
}
