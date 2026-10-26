using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmergencyShift;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.View;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class InstallStalkerPostures
{
    private const string Folder = "Concepts/Used/Stalker/Postures04/";
    private const string PrefabPath = "Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab";

    public static string Install()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Stop Play before importing sprite assets.");
        }
        JObject data = JObject.Parse(File.ReadAllText(Folder + "sprites.json"));
        var sprites = new Dictionary<string, Sprite>();
        for (int atlas = 0; atlas < data["atlases"].Count(); atlas++)
        {
            string path = (string)data["atlases"][atlas]["asset"];
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 224;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 4096;
            var platform = importer.GetPlatformTextureSettings("Standalone");
            platform.overridden = true;
            platform.format = TextureImporterFormat.BC7;
            platform.maxTextureSize = 4096;
            platform.compressionQuality = 100;
            importer.SetPlatformTextureSettings(platform);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var previous = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);
            var slices = new List<SpriteRect>();
            foreach (JToken frame in data["frames"].Where(frame => (int)frame["atlas"] == atlas))
            {
                string name = (string)frame["name"];
                var r = frame["rect"].ToObject<float[]>();
                var p = frame["pivot"].ToObject<float[]>();
                slices.Add(new SpriteRect { name = name,
                    spriteID = previous.TryGetValue(name, out GUID id) ? id : GUID.Generate(),
                    rect = new Rect(r[0], r[1], r[2], r[3]), alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(p[0], p[1]) });
            }
            provider.SetSpriteRects(slices.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
                slices.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
            foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
            {
                sprites.Add(sprite.name, sprite);
            }
        }
        const string setPath = "Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset";
        StalkerSpriteSet set = AssetDatabase.LoadAssetAtPath<StalkerSpriteSet>(setPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<StalkerSpriteSet>();
            AssetDatabase.CreateAsset(set, setPath);
        }
        var serializedSet = new SerializedObject(set);
        SerializedProperty frames = serializedSet.FindProperty("frames");
        frames.arraySize = 432;
        for (int closure = 0; closure < 9; closure++)
        {
            for (int phase = 0; phase < 48; phase++)
            {
                frames.GetArrayElementAtIndex(closure * 48 + phase).objectReferenceValue = sprites["body-" + closure + "-" + phase.ToString("D2")];
            }
        }
        serializedSet.ApplyModifiedPropertiesWithoutUndo();
        set.Validate();
        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssetIfDirty(set);

        // The previous profile is shared with Sleeper; give the new sprite cycle its own config.
        const string motionPath = "Assets/EmergencyShift/Data/Enemies/StalkerSpriteLocomotion.asset";
        StalkerLocomotionProfile motion = AssetDatabase.LoadAssetAtPath<StalkerLocomotionProfile>(motionPath);
        if (motion == null)
        {
            AssetDatabase.CopyAsset("Assets/EmergencyShift/Data/Enemies/StalkerLocomotion.asset", motionPath);
            motion = AssetDatabase.LoadAssetAtPath<StalkerLocomotionProfile>(motionPath);
        }
        var serializedMotion = new SerializedObject(motion);
        serializedMotion.FindProperty("distancePerCycle").floatValue = 1;
        serializedMotion.FindProperty("supportHold").floatValue = 0;
        serializedMotion.FindProperty("supportYawDegrees").floatValue = 0;
        serializedMotion.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(motion);
        AssetDatabase.SaveAssetIfDirty(motion);

        GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var animator = new SerializedObject(prefab.GetComponent<StalkerSpriteAnimator>());
            animator.FindProperty("spriteSet").objectReferenceValue = set;
            animator.FindProperty("locomotionProfile").objectReferenceValue = motion;
            SerializedProperty walk = animator.FindProperty("walkFrames");
            walk.arraySize = 48;
            for (int phase = 0; phase < 48; phase++)
            {
                walk.GetArrayElementAtIndex(phase).objectReferenceValue = set.Frame(phase);
            }
            animator.ApplyModifiedPropertiesWithoutUndo();
            var shadowRig = prefab.GetComponent<StalkerRigShadows2D>();
            if (shadowRig != null)
            {
                UnityEngine.Object.DestroyImmediate(shadowRig);
            }
            var rig = prefab.GetComponent<StalkerLimbRig2D>();
            if (rig != null)
            {
                UnityEngine.Object.DestroyImmediate(rig);
            }
            prefab.transform.Find("Visual/Body").GetComponent<SpriteRenderer>().sprite = set.Frame(0);
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        File.WriteAllText(Folder + "installation.json", new JObject {
            ["wholeBodyFrames"] = sprites.Count, ["runtimeRigComponents"] = 0,
            ["prefabGuid"] = AssetDatabase.AssetPathToGUID(PrefabPath),
            ["cycleDistance"] = motion.DistancePerCycle, ["closureSeconds"] = set.ClosureSeconds,
            ["sleeperSharedProfileChanged"] = false, ["authoredScenesSaved"] = 0
        }.ToString());
        return sprites.Count + " sprites assigned; runtime IK removed from prefab; no scene saves.";
    }
}
