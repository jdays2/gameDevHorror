using System;
using System.IO;
using System.Linq;
using EmergencyShift;
using EmergencyShift.Features.ClawEnemy.Config;
using EmergencyShift.Features.Scorpion.Config;
using EmergencyShift.Features.Scorpion.View;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ReviseScorpionR10
{
    private const string Root = "Assets/EmergencyShift/";
    private const string Data = Root + "Data/Enemies/Scorpion/";
    private const string Materials = Root + "Materials/Characters/Scorpion/";
    private const string PrefabPath = Root + "Prefabs/Enemies/Scorpion.prefab";
    private const string PoolPath = Root + "Prefabs/Enemies/ScorpionToxicPool.prefab";

    public static object Install()
    {
        if (Application.isPlaying || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Requires stopped compiled Editor; preserve authored scenes.");
        }
        string before = SceneState();
        JObject export = JObject.Parse(File.ReadAllText("Concepts/Unused/Visual/Scorpion01/rupture-r10.json"));
        var sprites = ImportParts(export);
        var rupture = LoadOrCreate<ScorpionRuptureSet>(Data + "RupturePosesR10.asset");
        Edit(rupture, serialized =>
        {
            var poses = serialized.FindProperty("poses");
            poses.arraySize = export["poses"].Count();
            for (int i = 0; i < poses.arraySize; i++)
            {
                JObject row = (JObject)export["poses"][i];
                string state = (string)row["state"] == "run" ? "Run" : "Attack";
                var motion = new SerializedObject(AssetDatabase.LoadAssetAtPath<TimedSpriteMotion>(Data + state + ".asset"));
                Sprite source = (Sprite)motion.FindProperty("frames").GetArrayElementAtIndex((int)row["frame"]).objectReferenceValue;
                var pose = poses.GetArrayElementAtIndex(i);
                pose.FindPropertyRelative("source").objectReferenceValue = source;
                var parts = pose.FindPropertyRelative("parts");
                var offsets = pose.FindPropertyRelative("offsets");
                parts.arraySize = 12;
                offsets.arraySize = 12;
                for (int p = 0; p < 12; p++)
                {
                    JObject spec = (JObject)row["parts"][p];
                    parts.GetArrayElementAtIndex(p).objectReferenceValue = sprites[(string)spec["name"]];
                    offsets.GetArrayElementAtIndex(p).vector2Value = new Vector2((float)spec["offset"][0], (float)spec["offset"][1]);
                }
            }
        });
        rupture.Validate();
        var shell = Palette("BurgundyShell", new Color(.12f, .04f, .055f), new Color(.42f, .115f, .14f), new Color(.64f, .32f, .27f), 0);
        var fluid = Palette("ToxicBlood", new Color(.22f,.25f,.015f), new Color(.72f,.79f,.065f), new Color(.94f,.98f,.38f), 1);
        var pool = BuildPool(fluid);
        var presentation = AssetDatabase.LoadAssetAtPath<ScorpionPresentationSet>(Data + "Presentation.asset");
        Edit(presentation, serialized =>
        {
            serialized.FindProperty("turnDegrees").floatValue = 420;
            serialized.FindProperty("acceleration").floatValue = 9;
            serialized.FindProperty("braking").floatValue = 18;
            serialized.FindProperty("squeakVolume").floatValue = .22f;
            serialized.FindProperty("idleSqueakInterval").vector2Value = new Vector2(5, 8);
            serialized.FindProperty("runSqueakInterval").vector2Value = new Vector2(3, 5);
            var bank = serialized.FindProperty("squeaks");
            bank.arraySize = 1;
            var discovery = serialized.FindProperty("alarm").objectReferenceValue;
            bank.GetArrayElementAtIndex(0).objectReferenceValue = discovery;
            serialized.FindProperty("presence").objectReferenceValue = discovery;
        });
        presentation.Validate();
        var profile = AssetDatabase.LoadAssetAtPath<GoreProfile>(Data + "Remains.asset");
        Edit(profile, serialized =>
        {
            serialized.FindProperty("poolSize").floatValue = .7f;
            serialized.FindProperty("releaseDelay").floatValue = .035f;
            serialized.FindProperty("impulseRange").vector2Value = new Vector2(2.6f,4.4f);
            serialized.FindProperty("liftRange").vector2Value = new Vector2(1.6f,2.4f);
            serialized.FindProperty("settledTint").colorValue = RemainsVisualStyle.Tint;
            serialized.FindProperty("airborneTint").colorValue = new Color(.74f,.68f,.72f,1);
            Ref(serialized,"fluidMaterial",fluid);
            serialized.FindProperty("fluidTint").colorValue = Color.white;
            var variants = serialized.FindProperty("variants");
            variants.arraySize = 7;
            for (int v = 0; v < 4; v++)
            {
                variants.GetArrayElementAtIndex(v).FindPropertyRelative("restorationOnly").boolValue = true;
            }
            for (int v = 4; v < 7; v++)
            {
                var variant = variants.GetArrayElementAtIndex(v);
                variant.FindPropertyRelative("id").stringValue = "scorpion-r10-rupture-" + (v-3);
                variant.FindPropertyRelative("restorationOnly").boolValue = false;
                variant.FindPropertyRelative("assembleBeforeRelease").boolValue = true;
                variant.FindPropertyRelative("replaceOriginalBody").boolValue = true;
                variant.FindPropertyRelative("selectionWeight").intValue = 1;
                variant.FindPropertyRelative("fragmentCount").intValue = 12;
                variant.FindPropertyRelative("scale").vector2Value = Vector2.one;
                variant.FindPropertyRelative("impulseMultiplier").floatValue = .8f + (v-4)*.2f;
                variant.FindPropertyRelative("rotationOffset").floatValue = (v-4)*43;
                var pieces = variant.FindPropertyRelative("pieces");
                pieces.arraySize = 12;
                for (int p = 0; p < 12; p++)
                {
                    var piece = pieces.GetArrayElementAtIndex(p);
                    piece.FindPropertyRelative("spriteIndex").intValue = 4+p;
                    piece.FindPropertyRelative("offset").vector2Value = Vector2.zero;
                    piece.FindPropertyRelative("scale").vector2Value = Vector2.one;
                    piece.FindPropertyRelative("alignSourcePose").boolValue = true;
                    piece.FindPropertyRelative("preserveSourcePose").boolValue = p == 11;
                    piece.FindPropertyRelative("eyeSurface").boolValue = false;
                    piece.FindPropertyRelative("impulseMultiplier").floatValue = p == 11 ? .4f : 1;
                    piece.FindPropertyRelative("liftMultiplier").floatValue = p == 11 ? .45f : 1;
                }
            }
        });
        profile.Validate();
        GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            prefab.GetComponent<CircleCollider2D>().radius = .4f;
            Edit(prefab.GetComponent<CrowdAgent2D>(), serialized =>
            {
                serialized.FindProperty("personalRadius").floatValue = .68f;
                serialized.FindProperty("clearance").floatValue = .22f;
                serialized.FindProperty("strength").floatValue = 1.5f;
                serialized.FindProperty("yieldToNeighbours").boolValue = true;
            });
            var body = prefab.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
            body.sharedMaterial = shell;
            body.color = Color.white;
            prefab.transform.Find("Visual/Eyes").GetComponent<SpriteRenderer>().color = new Color(.12f,.12f,.12f,1);
            var corpse = prefab.GetComponent<ScorpionCorpsePose2D>();
            prefab.transform.Find("Audio/Quiet presence").GetComponent<AudioSource>().clip = presentation.Alarm;
            Edit(corpse, serialized =>
            {
                Ref(serialized, "rupture", rupture);
                Ref(serialized, "pool", pool);
            });
            var legacy = new SerializedObject(corpse).FindProperty("flesh");
            var bank = new Sprite[4 + rupture.PoseCount * 12];
            bank[0] = body.sprite;
            for (int i = 0; i < 3; i++)
            {
                bank[i + 1] = (Sprite)legacy.GetArrayElementAtIndex(i).objectReferenceValue;
            }
            for (int poseIndex = 0; poseIndex < rupture.PoseCount; poseIndex++)
            {
                var pose = rupture.PoseAt(poseIndex);
                for (int partIndex = 0; partIndex < pose.Count; partIndex++)
                {
                    bank[4 + poseIndex * 12 + partIndex] = pose.Part(partIndex);
                }
            }
            var gore = prefab.GetComponent<GoreBurst2D>();
            gore.Configure(pool,bank,shell);
            // Reuse the existing accepted spray artwork; its new material remaps pigment to yellow.
            var stalker = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Enemies/ShadowStalker.prefab");
            var source = new SerializedObject(stalker.GetComponent<GoreBurst2D>());
            Edit(gore, serialized => Ref(serialized,"bloodSpraySprite",source.FindProperty("bloodSpraySprite").objectReferenceValue));
            PrefabUtility.SaveAsPrefabAsset(prefab,PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        var visual = PrefabUtility.LoadPrefabContents(Root+"Prefabs/Enemies/ScorpionVisual.prefab");
        try
        {
            foreach (var renderer in visual.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.sharedMaterial.name == "EyeGlow")
                {
                    renderer.color = new Color(.12f, .12f, .12f, 1);
                }
                else
                {
                    renderer.sharedMaterial = shell;
                    renderer.color = Color.white;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(visual,Root+"Prefabs/Enemies/ScorpionVisual.prefab");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(visual);
        }
        if (before != SceneState())
        {
            throw new InvalidOperationException("Scene state changed during prefab update.");
        }
        var report = new {poses=56,partsPerPose=12,sprites=sprites.Count,bodyCollider=.4f,personalRadius=.68f,squeaks=presentation.SqueakCount,sceneStatePreserved=true,
            scene=SceneManager.GetActiveScene().path,sceneSave=false,shaderErrors=ShaderUtil.GetShaderMessages(shell.shader).Where(m=>m.severity.ToString()=="Error").Select(m=>m.message).ToArray()};
        Directory.CreateDirectory("Docs/Validation/ScorpionR10");
        File.WriteAllText("Docs/Validation/ScorpionR10/import.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        return report;
    }

    private static System.Collections.Generic.Dictionary<string,Sprite> ImportParts(JObject spec)
    {
        string path=(string)spec["atlas"];
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit=200;
        importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false;
        importer.wrapMode=TextureWrapMode.Clamp;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.maxTextureSize=4096;
        importer.SaveAndReimport();
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var previous=provider.GetSpriteRects().ToDictionary(rect=>rect.name,rect=>rect.spriteID);
        var rectangles=spec["poses"].SelectMany(pose=>pose["parts"]).Select(part=> {
            string name = (string)part["name"];
            var rect = part["rectTopLeft"];
            return new SpriteRect {name=name,spriteID=previous.TryGetValue(name,out GUID id)?id:GUID.Generate(),alignment=SpriteAlignment.Center,pivot=new Vector2(.5f,.5f),
                rect=new Rect((int)rect[0],(int)spec["size"][1]-(int)rect[1]-(int)rect[3],(int)rect[2],(int)rect[3])};
        }).ToArray();
        provider.SetSpriteRects(rectangles);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rectangles.Select(rect=>new SpriteNameFileIdPair(rect.name,rect.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite=>sprite.name);
    }
    private static Material Palette(string name,Color dark,Color middle,Color light,float strongest)
    {
        string path=Materials+name+".mat";
        var shader=AssetDatabase.LoadAssetAtPath<Shader>(Materials+"ScorpionPaletteLit.shader");
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_Dark", dark);
        material.SetColor("_Middle", middle);
        material.SetColor("_Light", light);
        material.SetFloat("_UseMaxChannel", strongest);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        return material;
    }
    private static BloodPool2D BuildPool(Material material)
    {
        var original=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/Enemies/BloodPool.prefab");
        var originalProfile=(BloodPoolProfile)new SerializedObject(original.GetComponent<BloodPool2D>()).FindProperty("profile").objectReferenceValue;
        string profilePath=Data+"ToxicBlood.asset";
        if (AssetDatabase.LoadAssetAtPath<BloodPoolProfile>(profilePath) == null)
        {
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(originalProfile), profilePath);
        }
        var profile=AssetDatabase.LoadAssetAtPath<BloodPoolProfile>(profilePath);
        Edit(profile, serialized =>
        {
            serialized.FindProperty("poolTint").colorValue = Color.white;
            serialized.FindProperty("dropletTint").colorValue = Color.white;
        });
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PoolPath) == null)
        {
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(original), PoolPath);
        }
        var copy=PrefabUtility.LoadPrefabContents(PoolPath);
        try
        {
            Edit(copy.GetComponent<BloodPool2D>(),serialized=>Ref(serialized,"profile",profile));
            foreach (var renderer in copy.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.sharedMaterial = material;
            }
            PrefabUtility.SaveAsPrefabAsset(copy,PoolPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(copy);
        }
        return AssetDatabase.LoadAssetAtPath<GameObject>(PoolPath).GetComponent<BloodPool2D>();
    }
    private static T LoadOrCreate<T>(string path) where T:ScriptableObject
    {
        var asset=AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }
    private static void Ref(SerializedObject serialized,string field,UnityEngine.Object value)=>serialized.FindProperty(field).objectReferenceValue=value;
    private static void Edit(UnityEngine.Object target,Action<SerializedObject> change)
    {
        var serialized = new SerializedObject(target);
        change(serialized);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssetIfDirty(target);
    }
    private static string SceneState() => string.Join("|", Enumerable.Range(0, SceneManager.sceneCount).Select(i =>
    {
        var scene = SceneManager.GetSceneAt(i);
        return scene.handle + ":" + scene.path + ":" + scene.isDirty + ":" + scene.rootCount;
    }));
}
