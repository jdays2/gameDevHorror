using System;
using System.IO;
using EmergencyShift;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.View;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InstallStalkerRig
{
    public static string Install()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Stop Play first.");
        }
        const string art = "Assets/EmergencyShift/Art/Enemies/Stalker/Locomotion02";
        const string data = "Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset";
        const string prefabPath = "Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab";
        const string texturePath = art + "/rig-atlas.png";
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        string materialPath = "Assets/EmergencyShift/Materials/Characters/StalkerRigSurface.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("EmergencyShift/Stalker Rig Surface"));
            material.mainTexture = texture;
            AssetDatabase.CreateAsset(material, materialPath);
        }
        var torso = AssetDatabase.LoadAssetAtPath<Sprite>(art + "/Torso.asset");
        string shadowPath = "Assets/EmergencyShift/Materials/Characters/StalkerRigShadow.mat";
        var shadow = AssetDatabase.LoadAssetAtPath<Material>(shadowPath);
        if (shadow == null)
        {
            shadow = new Material(Shader.Find("EmergencyShift/Stalker Rig Shadow"));
            shadow.mainTexture = texture;
            AssetDatabase.CreateAsset(shadow, shadowPath);
        }
        if (torso == null)
        {
            torso = Sprite.Create(texture, new Rect(0, 1218, 358, 406), new Vector2(187f / 358, 206f / 406), 224, 0, SpriteMeshType.FullRect);
            torso.name = "Stalker neutral torso";
            AssetDatabase.CreateAsset(torso, art + "/Torso.asset");
        }
        var profile = AssetDatabase.LoadAssetAtPath<StalkerRigProfile>(data);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<StalkerRigProfile>();
            AssetDatabase.CreateAsset(profile, data);
        }
        var serialized = new SerializedObject(profile);
        serialized.FindProperty("atlas").objectReferenceValue = texture;
        serialized.FindProperty("torso").objectReferenceValue = torso;
        serialized.FindProperty("surfaceMaterial").objectReferenceValue = material;
        serialized.FindProperty("shadowMaterial").objectReferenceValue = shadow;
        JObject anatomy = JObject.Parse(File.ReadAllText("Concepts/Used/Stalker/Locomotion02/anatomy.json"));
        var limbs = (JArray)anatomy["limbs"];
        var array = serialized.FindProperty("limbs");
        array.arraySize = limbs.Count;
        for (int i = 0; i < limbs.Count; i++)
        {
            var row = array.GetArrayElementAtIndex(i);
            row.FindPropertyRelative("Id").stringValue = (string)limbs[i]["id"];
            foreach (string joint in new[] { "Hip", "Knee", "Tip" })
            {
                var pixel = limbs[i][joint.ToLowerInvariant()];
                row.FindPropertyRelative(joint).vector2Value = new Vector2(((float)pixel[0] - 187) / 224, (200 - (float)pixel[1]) / 224);
            }
            row.FindPropertyRelative("UpperLength").floatValue = (float)limbs[i]["upperLength"];
            row.FindPropertyRelative("LowerLength").floatValue = (float)limbs[i]["lowerLength"];
            row.FindPropertyRelative("PhaseOffset").floatValue = i == 0 || i == 3 || i == 4 ? 0 : .5f;
        }
        var bounds = (JArray)JObject.Parse(File.ReadAllText("Concepts/Used/Stalker/Locomotion02/extraction.json"))["bounds"];
        array = serialized.FindProperty("layerBounds");
        array.arraySize = bounds.Count;
        for (int i = 0; i < bounds.Count; i++)
        {
            array.GetArrayElementAtIndex(i).rectValue = Rect.MinMaxRect((float)bounds[i][0], (float)bounds[i][1], (float)bounds[i][2], (float)bounds[i][3]);
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        profile.Validate();
        AssetDatabase.SaveAssetIfDirty(profile);
        GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var animator = new SerializedObject(prefab.GetComponent<StalkerSpriteAnimator>());
            var rig = prefab.GetComponent<StalkerLimbRig2D>();
            if (rig == null)
            {
                rig = prefab.AddComponent<StalkerLimbRig2D>();
            }
            if (prefab.GetComponent<StalkerRigShadows2D>() == null)
            {
                prefab.AddComponent<StalkerRigShadows2D>();
            }
            var fields = new SerializedObject(rig);
            fields.FindProperty("profile").objectReferenceValue = profile;
            fields.FindProperty("visual").objectReferenceValue = animator.FindProperty("visualRoot").objectReferenceValue;
            fields.FindProperty("body").objectReferenceValue = animator.FindProperty("bodyRenderer").objectReferenceValue;
            fields.FindProperty("iris").objectReferenceValue = animator.FindProperty("iris").objectReferenceValue;
            fields.FindProperty("pupil").objectReferenceValue = animator.FindProperty("pupil").objectReferenceValue;
            fields.FindProperty("backing").objectReferenceValue = animator.FindProperty("eyeUnderlay").objectReferenceValue;
            fields.FindProperty("contact").objectReferenceValue = prefab.transform.Find("Ground shadows/Contact shadow").GetComponent<SpriteRenderer>();
            fields.FindProperty("projection").objectReferenceValue = prefab.transform.Find("Ground shadows/Flashlight shadow").GetComponent<SpriteRenderer>();
            fields.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        return "Stalker-only profile + original prefab updated. Scene saves: zero. playModeStartScene=" + EditorSceneManager.playModeStartScene;
    }
}
