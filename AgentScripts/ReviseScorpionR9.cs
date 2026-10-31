using System;
using System.IO;
using System.Linq;
using EmergencyShift;
using EmergencyShift.Features.Scorpion.Config;
using EmergencyShift.Features.Scorpion.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Narrow prefab/profile update. No authored scene is saved or rebuilt.</summary>
public static class ReviseScorpionR9
{
    private const string Root = "Assets/EmergencyShift/";
    private const string PrefabPath = Root + "Prefabs/Enemies/Scorpion.prefab";
    private const string Evidence = "Docs/Validation/ScorpionR9/";
    private static readonly string[] FleshPaths = {
        Root + "Art/Effects/CreatureFlesh04/MuscleWad.asset",
        Root + "Art/Effects/CreatureFlesh04/DenseClot.asset",
        Root + "Art/Effects/CreatureFlesh04/TissueStrip.asset"
    };

    public static object Install()
    {
        if (AssetDatabase.LoadAssetAtPath<ScorpionRuptureSet>(Root + "Data/Enemies/Scorpion/RupturePosesR10.asset") != null)
        {
            throw new InvalidOperationException("R10 is already installed. Use ReviseScorpionR10; R9 would undo the new rupture and palette.");
        }
        if (Application.isPlaying || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Requires stopped compiled Editor.");
        }
        string before = SceneState();
        string audioPath = Root + "Audio/Enemies/Scorpion/Detection.wav";
        AssetDatabase.ImportAsset(audioPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (AudioImporter)AssetImporter.GetAtPath(audioPath);
        importer.forceToMono = true;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
        var presentation = AssetDatabase.LoadAssetAtPath<ScorpionPresentationSet>(Root + "Data/Enemies/Scorpion/Presentation.asset");
        Edit(presentation, serialized =>
        {
            Ref(serialized, "alarm", AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath));
            Ref(serialized, "deathSounds", AssetDatabase.LoadAssetAtPath<EnemyDeathAudioSet>(Root + "Data/Audio/CreatureRupture06.asset"));
            serialized.FindProperty("detectionVolume").floatValue = .6f;
        });
        presentation.Validate();
        var glow = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Characters/Scorpion/EyeGlow.mat");
        glow.SetColor("_Color", new Color(1.05f, .0146f, .007f, 1));
        EditorUtility.SetDirty(glow);
        var profile = AssetDatabase.LoadAssetAtPath<GoreProfile>(Root + "Data/Enemies/Scorpion/Remains.asset");
        ConfigureRemains(profile);
        Sprite[] flesh = FleshPaths.Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path)).ToArray();
        if (flesh.Any(sprite => sprite == null))
        {
            throw new InvalidOperationException("Missing shared flesh sprites.");
        }
        GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform audio = prefab.transform.Find("Audio");
            Transform cue = audio.Find("Detection squeal");
            if (cue == null)
            {
                cue = new GameObject("Detection squeal", typeof(AudioSource)).transform;
                cue.SetParent(audio, false);
            }
            var source = cue.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0;
            source.dopplerLevel = 0;
            source.priority = 100;
            Edit(prefab.GetComponent<ScorpionAudio2D>(), serialized => Ref(serialized, "detection", source));
            if (prefab.GetComponent<ScorpionAudioThreat2D>() == null)
            {
                prefab.AddComponent<ScorpionAudioThreat2D>();
            }
            Edit(prefab.GetComponent<ScorpionCorpsePose2D>(), serialized =>
            {
                var list = serialized.FindProperty("flesh");
                list.arraySize = flesh.Length;
                for (int i = 0; i < flesh.Length; i++)
                {
                    list.GetArrayElementAtIndex(i).objectReferenceValue = flesh[i];
                }
            });
            // Bank order is stable for saved remains; pose capture replaces only index zero.
            var body = prefab.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
            // SpriteRenderer supplies its own color to this shader; material _Color alone is insufficient.
            prefab.transform.Find("Visual/Eyes").GetComponent<SpriteRenderer>().color = new Color(.6f, .6f, .6f, 1);
            var bank = new[]{body.sprite}.Concat(flesh).ToArray();
            var pool = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Enemies/BloodPool.prefab").GetComponent<BloodPool2D>();
            prefab.GetComponent<GoreBurst2D>().Configure(pool, bank, body.sharedMaterial);
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        GameObject visualPrefab = PrefabUtility.LoadPrefabContents(Root + "Prefabs/Enemies/ScorpionVisual.prefab");
        try
        {
            foreach (SpriteRenderer renderer in visualPrefab.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.sharedMaterial == glow)
                {
                    renderer.color = new Color(.6f, .6f, .6f, 1);
                }
            }
            PrefabUtility.SaveAsPrefabAsset(visualPrefab, Root + "Prefabs/Enemies/ScorpionVisual.prefab");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(visualPrefab);
        }
        AssetDatabase.SaveAssets();
        if (before != SceneState())
        {
            throw new InvalidOperationException("Loaded scene state changed.");
        }
        return Inspect();
    }

    public static void ConfigureRemains(GoreProfile profile)
    {
        Edit(profile, serialized =>
        {
            serialized.FindProperty("impulseRange").vector2Value = new Vector2(2.2f, 3.6f);
            serialized.FindProperty("liftRange").vector2Value = new Vector2(2.4f, 3.2f);
            serialized.FindProperty("settledTint").colorValue = RemainsVisualStyle.Tint;
            serialized.FindProperty("airborneTint").colorValue = new Color(.74f, .68f, .72f, 1);
            var variants = serialized.FindProperty("variants");
            // Keep legacy ID for restoring old records; new deaths use the three compositions.
            variants.arraySize = 4;
            for (int v = 0; v < 4; v++)
            {
                var variant = variants.GetArrayElementAtIndex(v);
                variant.FindPropertyRelative("id").stringValue = v == 0 ? "scorpion-carcass" : "scorpion-flesh-r9-" + v;
                variant.FindPropertyRelative("restorationOnly").boolValue = v == 0;
                variant.FindPropertyRelative("fragmentCount").intValue = v == 0 ? 1 : v + 3;
                variant.FindPropertyRelative("selectionWeight").intValue = 1;
                variant.FindPropertyRelative("spriteOffset").intValue = 0;
                variant.FindPropertyRelative("rotationOffset").floatValue = v * 23;
                variant.FindPropertyRelative("scale").vector2Value = Vector2.one;
                variant.FindPropertyRelative("impulseMultiplier").floatValue = 1;
                variant.FindPropertyRelative("replaceOriginalBody").boolValue = true;
                var pieces = variant.FindPropertyRelative("pieces");
                pieces.arraySize = v == 0 ? 1 : v + 3;
                for (int i = 0; i < pieces.arraySize; i++)
                {
                    var piece = pieces.GetArrayElementAtIndex(i);
                    bool corpse = i == 0;
                    piece.FindPropertyRelative("spriteIndex").intValue = corpse ? 0 : 1 + (i + v) % 3;
                    piece.FindPropertyRelative("offset").vector2Value = Vector2.zero;
                    piece.FindPropertyRelative("scale").vector2Value = corpse ? Vector2.one : new Vector2(.8f, 1.05f);
                    piece.FindPropertyRelative("preserveSourcePose").boolValue = corpse;
                    piece.FindPropertyRelative("eyeSurface").boolValue = false;
                    piece.FindPropertyRelative("impulseMultiplier").floatValue = corpse ? .01f : 1;
                    piece.FindPropertyRelative("liftMultiplier").floatValue = corpse ? .01f : 1;
                }
            }
        });
        profile.Validate();
    }

    public static object Inspect()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var set = AssetDatabase.LoadAssetAtPath<ScorpionPresentationSet>(Root + "Data/Enemies/Scorpion/Presentation.asset");
        set.Validate();
        var profile = AssetDatabase.LoadAssetAtPath<GoreProfile>(Root + "Data/Enemies/Scorpion/Remains.asset");
        profile.Validate();
        var sources = new SerializedObject(prefab.GetComponent<ScorpionAudio2D>());
        string[] fields = {"voice", "claws", "contact", "presence", "detection"};
        var scene = SceneManager.GetActiveScene();
        var actors = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Scorpion2D>(true)).ToArray();
        var report = new {
            scene = scene.path, sceneDirty = scene.isDirty, scenesSaved = false,
            channelsWired = fields.All(field => sources.FindProperty(field).objectReferenceValue != null),
            distinctChannels = fields.Select(field => sources.FindProperty(field).objectReferenceValue).Distinct().Count(),
            detectionClip = AssetDatabase.GetAssetPath(set.Alarm), detectionSeconds = set.Alarm.length,
            deathBank = AssetDatabase.GetAssetPath(set.DeathSounds), deathVariants = set.DeathSounds.Clips.Count,
            fleshSprites = FleshPaths,
            remainsVariants = Enumerable.Range(0, profile.VariantCount).Select(i => new {id=profile.VariantAt(i).Id, pieces=profile.VariantAt(i).PieceCount, legacy=profile.VariantAt(i).RestorationOnly}).ToArray(),
            eyeEmission = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Characters/Scorpion/EyeGlow.mat").GetColor("_Color").ToString(),
            eyeRendererTint = prefab.transform.Find("Visual/Eyes").GetComponent<SpriteRenderer>().color.ToString(),
            missingScripts = prefab.GetComponentsInChildren<Transform>(true).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),
            actors = actors.Select(actor => new {actor.name, threat=actor.GetComponent<ScorpionAudioThreat2D>() != null, channels=actor.GetComponentsInChildren<AudioSource>(true).Length, pack=actor.GetComponentInParent<ScorpionPack2D>(true)?.name}).ToArray(),
            authoringGate = "Scene object authoring not applicable; existing prefab updated, inherited room references inspected including inactive."
        };
        Directory.CreateDirectory(Evidence);
        File.WriteAllText(Evidence + "import.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }

    private static void Edit(UnityEngine.Object target, Action<SerializedObject> change)
    {
        var serialized = new SerializedObject(target);
        change(serialized);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
    private static void Ref(SerializedObject serialized, string field, UnityEngine.Object value) => serialized.FindProperty(field).objectReferenceValue = value;
    private static string SceneState() => string.Join("|", Enumerable.Range(0, SceneManager.sceneCount).Select(i => {
        var scene = SceneManager.GetSceneAt(i);
        return scene.handle + ":" + scene.path + ":" + scene.isDirty + ":" + scene.rootCount;
    }));
}
