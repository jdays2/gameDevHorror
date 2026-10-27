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

public static class InstallCreatureAttack05
{
    public static string Install()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Stop Play before importing attack frames.");
        }
        return InstallStalker() + " " + InstallSleeper();
    }

    public static string InstallStalker()
    {
        return InstallSet("Concepts/Used/Stalker/Postures05/AttackAdjacent01/", "Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset",
            "Assets/EmergencyShift/Data/Enemies/StalkerAttackSprites.asset", "Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab", typeof(StalkerSpriteAnimator));
    }

    public static string InstallSleeper()
    {
        return InstallSet("Concepts/Used/Sleeper/Postures05/AttackAdjacent01/", "Assets/EmergencyShift/Data/Enemies/SleeperSprites05.asset",
            "Assets/EmergencyShift/Data/Enemies/SleeperAttackSprites05.asset", "Assets/EmergencyShift/Prefabs/Enemies/Sleeper.prefab", typeof(SleeperSpriteAnimator));
    }

    private static string InstallSet(string folder, string walkingPath, string setPath, string prefabPath, Type animatorType)
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Stop Play before importing attack frames.");
        }
        JObject data = JObject.Parse(File.ReadAllText(folder + "attack-sprites.json"));
        int phases = (int)data["phases"];
        int bridge = (int)data["bridgeFrames"];
        int strike = (int)data["strikeFrames"];
        var sprites = new Dictionary<string, Sprite>();
        for (int atlas = 0; atlas < data["atlases"].Count(); atlas++)
        {
            ImportAtlas(data, atlas, sprites);
        }
        var walking = AssetDatabase.LoadAssetAtPath<StalkerSpriteSet>(walkingPath);
        walking.Validate();
        if (walking.Phases != phases)
        {
            throw new InvalidOperationException("Attack and locomotion phase counts differ.");
        }
        CreatureAttackSpriteSet set = AssetDatabase.LoadAssetAtPath<CreatureAttackSpriteSet>(setPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<CreatureAttackSpriteSet>();
            AssetDatabase.CreateAsset(set, setPath);
        }
        var serialized = new SerializedObject(set);
        serialized.FindProperty("phases").intValue = phases;
        serialized.FindProperty("bridgeFrames").intValue = bridge;
        serialized.FindProperty("strikeFrames").intValue = strike;
        serialized.FindProperty("entrySeconds").floatValue = .24f;
        serialized.FindProperty("returnSeconds").floatValue = .24f;
        serialized.FindProperty("interruptionReturnSeconds").floatValue = .36f;
        SerializedProperty frames = serialized.FindProperty("frames");
        frames.arraySize = phases * bridge * 2 + strike;
        for (int phase = 0; phase < phases; phase++)
        {
            for (int sample = 0; sample < bridge; sample++)
            {
                Sprite entry = sprites["entry-" + phase.ToString("D2") + "-" + sample.ToString("D2")];
                Sprite returning = sprites["return-" + phase.ToString("D2") + "-" + sample.ToString("D2")];
                if (sample == 0)
                {
                    entry = walking.Frame(phase);
                }
                if (sample == bridge - 1)
                {
                    returning = walking.Frame(phase);
                }
                frames.GetArrayElementAtIndex(phase * bridge + sample).objectReferenceValue = entry;
                frames.GetArrayElementAtIndex(phases * bridge + strike + phase * bridge + sample).objectReferenceValue = returning;
            }
        }
        for (int sample = 0; sample < strike; sample++)
        {
            frames.GetArrayElementAtIndex(phases * bridge + sample).objectReferenceValue = sprites["strike-" + sample.ToString("D2")];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        set.Validate();
        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssetIfDirty(set);
        AssignPrefab(prefabPath, animatorType, set);
        File.WriteAllText(folder + "attack-installation.json", new JObject
        {
            ["frames"] = set.FrameCount,
            ["phases"] = phases,
            ["bridgeFrames"] = bridge,
            ["strikeFrames"] = strike,
            ["entrySeconds"] = set.EntrySeconds,
            ["returnSeconds"] = set.ReturnSeconds,
            ["interruptionReturnSeconds"] = set.InterruptionReturnSeconds,
            ["setGuid"] = AssetDatabase.AssetPathToGUID(setPath),
            ["entryAndReturnEdgesReferenceExactWalkingSprites"] = true,
            ["prefab"] = prefabPath,
            ["walkingSet"] = walkingPath,
            ["scenesSaved"] = 0,
            ["meleeTimingsChanged"] = false,
            ["runtimeIk"] = false
        }.ToString());
        return set.FrameCount + " whole-body attack frames assigned to " + prefabPath + "; no scene saves.";
    }

    private static void ImportAtlas(JObject data, int atlas, Dictionary<string, Sprite> sprites)
    {
        string path = (string)data["atlases"][atlas]["asset"];
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = data.Value<float?>("ppu") ?? 224;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
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
            float[] rect = frame["rect"].ToObject<float[]>();
            float[] pivot = frame["pivot"].ToObject<float[]>();
            slices.Add(new SpriteRect
            {
                name = name,
                spriteID = previous.TryGetValue(name, out GUID id) ? id : GUID.Generate(),
                rect = new Rect(rect[0], rect[1], rect[2], rect[3]),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(pivot[0], pivot[1])
            });
        }
        provider.SetSpriteRects(slices.ToArray());
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(slices.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
        foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
        {
            sprites.Add(sprite.name, sprite);
        }
    }

    private static void AssignPrefab(string path, Type animatorType, CreatureAttackSpriteSet set)
    {
        GameObject prefab = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Component animator = prefab.GetComponent(animatorType);
            if (animator == null)
            {
                throw new InvalidOperationException("Install the species sprite animator before attack frames: " + path);
            }
            var serialized = new SerializedObject(animator);
            serialized.FindProperty("attackSpriteSet").objectReferenceValue = set;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }
}
