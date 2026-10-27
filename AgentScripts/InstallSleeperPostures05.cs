using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmergencyShift;
using EmergencyShift.Features.Enemies.Config;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>Imports the reviewed palette variant and updates only the Sleeper prefab presentation.</summary>
public static class InstallSleeperPostures05
{
    private const string Folder = "Concepts/Used/Sleeper/Postures05/";
    private const string PrefabPath = "Assets/EmergencyShift/Prefabs/Enemies/Sleeper.prefab";
    private const string ScriptPath = "Assets/EmergencyShift/Runtime/Enemies/Sleeper/SleeperSpriteAnimator.cs";
    private const string SetPath = "Assets/EmergencyShift/Data/Enemies/SleeperSprites05.asset";
    private const string MotionPath = "Assets/EmergencyShift/Data/Enemies/SleeperSpriteLocomotion.asset";

    public static string Install()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Stop Play before importing Sleeper presentation assets.");
        }

        JObject data = JObject.Parse(File.ReadAllText(Folder + "sprites.json"));
        if ((bool?)data["studyOnly"] == true)
        {
            throw new InvalidOperationException("A temporary palette study cannot be installed.");
        }

        if ((int)data["phases"] != 48 || (int)data["closureRows"] != 9 || (int)data["pixelsPerUnit"] != 224)
        {
            throw new InvalidOperationException("Sleeper must use the reviewed 48 by 9, 224 PPU body matrix.");
        }

        string prefabGuid = AssetDatabase.AssetPathToGUID(PrefabPath);
        var originalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var originalAnimator = originalPrefab.GetComponent<CreatureSpriteAnimator2D>();
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(originalAnimator, out string animatorGuid, out long animatorId);
        string mechanicsBefore = MechanicsSnapshot(originalPrefab);
        string eyeCombatBefore = EyeCombatSnapshot(originalAnimator);
        if (!File.Exists(Folder + "prefab-before.txt"))
        {
            File.Copy(PrefabPath, Folder + "prefab-before.txt");
        }

        Dictionary<string, Sprite> sprites = ImportAtlases(data);
        StalkerSpriteSet set = UpdateSet(data, sprites);
        StalkerLocomotionProfile motion = UpdateMotion(data);
        UpgradeAnimatorScript();
        ConfigurePrefab(set, motion);

        var updatedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var updatedAnimator = updatedPrefab.GetComponent<SleeperSpriteAnimator>();
        if (updatedAnimator == null)
        {
            throw new InvalidOperationException("Sleeper prefab did not retain its derived sprite animator.");
        }

        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(updatedAnimator, out string updatedGuid, out long updatedId);
        if (prefabGuid != AssetDatabase.AssetPathToGUID(PrefabPath) || animatorGuid != updatedGuid ||
            animatorId != updatedId || mechanicsBefore != MechanicsSnapshot(updatedPrefab) ||
            eyeCombatBefore != EyeCombatSnapshot(updatedAnimator))
        {
            throw new InvalidOperationException("Sleeper presentation migration changed an authored reference or mechanics field.");
        }

        var report = new JObject
        {
            ["wholeBodyFrames"] = sprites.Count,
            ["prefabGuid"] = prefabGuid,
            ["animatorFileIdBefore"] = animatorId,
            ["animatorFileIdAfter"] = updatedId,
            ["mechanicsUnchanged"] = true,
            ["eyeAttackDeathFramesUnchanged"] = true,
            ["bodyRendererTint"] = "white; palette is baked in textures",
            ["cycleDistance"] = motion.DistancePerCycle,
            ["wakeUsesBrainClosureDirectly"] = true,
            ["authoredScenesSaved"] = 0
        };
        File.WriteAllText(Folder + "installation.json", report.ToString());
        return sprites.Count + " Sleeper body sprites assigned; prefab identity retained; no scene saves.";
    }

    private static Dictionary<string, Sprite> ImportAtlases(JObject data)
    {
        var sprites = new Dictionary<string, Sprite>();
        int atlasCount = data["atlases"].Count();
        for (int atlas = 0; atlas < atlasCount; atlas++)
        {
            string path = (string)data["atlases"][atlas]["asset"];
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 224;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 4096;
            TextureImporterPlatformSettings platform = importer.GetPlatformTextureSettings("Standalone");
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
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            Dictionary<string, GUID> previous = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);
            var slices = new List<SpriteRect>();
            foreach (JToken frame in data["frames"].Where(frame => (int)frame["atlas"] == atlas))
            {
                string name = (string)frame["name"];
                float[] rectangle = frame["rect"].ToObject<float[]>();
                float[] pivot = frame["pivot"].ToObject<float[]>();
                slices.Add(new SpriteRect
                {
                    name = name,
                    spriteID = previous.TryGetValue(name, out GUID id) ? id : GUID.Generate(),
                    rect = new Rect(rectangle[0], rectangle[1], rectangle[2], rectangle[3]),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(pivot[0], pivot[1])
                });
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

        return sprites;
    }

    private static StalkerSpriteSet UpdateSet(JObject data, Dictionary<string, Sprite> sprites)
    {
        StalkerSpriteSet set = AssetDatabase.LoadAssetAtPath<StalkerSpriteSet>(SetPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<StalkerSpriteSet>();
            AssetDatabase.CreateAsset(set, SetPath);
        }

        var serialized = new SerializedObject(set);
        serialized.FindProperty("phases").intValue = (int)data["phases"];
        serialized.FindProperty("closureRows").intValue = (int)data["closureRows"];
        serialized.FindProperty("compactStrideScale").floatValue = (float)data["compactStrideScale"];
        SerializedProperty frames = serialized.FindProperty("frames");
        frames.arraySize = 432;
        for (int row = 0; row < 9; row++)
        {
            for (int phase = 0; phase < 48; phase++)
            {
                frames.GetArrayElementAtIndex(row * 48 + phase).objectReferenceValue = sprites["body-" + row + "-" + phase.ToString("D2")];
            }
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        set.Validate();
        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssetIfDirty(set);
        return set;
    }

    private static StalkerLocomotionProfile UpdateMotion(JObject data)
    {
        StalkerLocomotionProfile motion = AssetDatabase.LoadAssetAtPath<StalkerLocomotionProfile>(MotionPath);
        if (motion == null)
        {
            AssetDatabase.CopyAsset("Assets/EmergencyShift/Data/Enemies/StalkerLocomotion.asset", MotionPath);
            motion = AssetDatabase.LoadAssetAtPath<StalkerLocomotionProfile>(MotionPath);
        }

        var serialized = new SerializedObject(motion);
        serialized.FindProperty("distancePerCycle").floatValue = (float)data["cycleDistance"];
        serialized.FindProperty("supportHold").floatValue = 0;
        serialized.FindProperty("supportYawDegrees").floatValue = 0;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(motion);
        AssetDatabase.SaveAssetIfDirty(motion);
        return motion;
    }

    private static void UpgradeAnimatorScript()
    {
        MonoScript replacement = AssetDatabase.LoadAssetAtPath<MonoScript>(ScriptPath);
        if (replacement == null || replacement.GetClass() != typeof(SleeperSpriteAnimator))
        {
            throw new InvalidOperationException("Compile the Sleeper sprite animator before migrating its prefab.");
        }

        GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            if (prefab.GetComponent<SleeperSpriteAnimator>() != null)
            {
                return;
            }

            CreatureSpriteAnimator2D animator = prefab.GetComponent<CreatureSpriteAnimator2D>();
            var serialized = new SerializedObject(animator);
            serialized.FindProperty("m_Script").objectReferenceValue = replacement;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    private static void ConfigurePrefab(StalkerSpriteSet set, StalkerLocomotionProfile motion)
    {
        GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var serialized = new SerializedObject(prefab.GetComponent<SleeperSpriteAnimator>());
            serialized.FindProperty("spriteSet").objectReferenceValue = set;
            serialized.FindProperty("locomotionProfile").objectReferenceValue = motion;
            SerializedProperty walk = serialized.FindProperty("walkFrames");
            walk.arraySize = set.Phases;
            for (int phase = 0; phase < set.Phases; phase++)
            {
                walk.GetArrayElementAtIndex(phase).objectReferenceValue = set.Frame(phase);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            var body = prefab.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
            body.sprite = set.Frame(set.FrameIndex(0, 1));
            body.color = Color.white;
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    private static string MechanicsSnapshot(GameObject prefab)
    {
        var snapshot = new JObject();
        foreach (MonoBehaviour component in prefab.GetComponents<MonoBehaviour>())
        {
            if (component is CreatureSpriteAnimator2D)
            {
                continue;
            }

            snapshot[component.GetType().FullName] = StableSerializedFields(component);
        }

        return snapshot.ToString(Newtonsoft.Json.Formatting.None);
    }

    private static string EyeCombatSnapshot(CreatureSpriteAnimator2D animator)
    {
        JObject fields = StableSerializedFields(animator);
        var snapshot = new JObject();
        string[] preservedFields =
        {
            "presentation", "visualRoot", "bodyRenderer", "iris", "pupil", "eyeUnderlay",
            "eyeSurfaceMaterial", "eyeFrames", "eyeFrameOffsets", "recoilFrames", "attackFrames",
            "deathFrames", "pupilTravel", "pupilWarningExpansion", "turnSpeed"
        };
        foreach (string field in preservedFields)
        {
            snapshot[field] = fields[field]?.DeepClone();
        }

        return snapshot.ToString(Newtonsoft.Json.Formatting.None);
    }

    private static JObject StableSerializedFields(MonoBehaviour component)
    {
        JObject document = JObject.Parse(EditorJsonUtility.ToJson(component));
        JObject fields = document["MonoBehaviour"] as JObject ?? document;
        fields.Remove("m_EditorClassIdentifier");
        var serialized = new SerializedObject(component);
        SerializedProperty property = serialized.GetIterator();
        while (property.Next(true))
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                continue;
            }

            string path = property.propertyPath.Replace(".Array.data[", "[");
            JToken reference = fields.SelectToken(path, false);
            if (reference == null)
            {
                continue;
            }

            UnityEngine.Object value = property.objectReferenceValue;
            string stableId = value != null ? GlobalObjectId.GetGlobalObjectIdSlow(value).ToString() : "null";
            reference.Replace(stableId);
        }

        return fields;
    }
}
