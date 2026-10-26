using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using EmergencyShift;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Asset-only authoring. Does not load, regenerate, save or populate user maps.</summary>
public static class BuildCreatureVisuals
{
    private const string Root = "Assets/EmergencyShift/";
    private const string Evidence = "Docs/Validation/CreatureVisuals01/";

    public static object Build()
    {
        return BuildFamilies(true);
    }

    public static object BuildScorpion()
    {
        return BuildFamilies(false);
    }

    private static object BuildFamilies(bool includeClaw)
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Requires stopped, compiled Editor.");
        }
        Directory.CreateDirectory(Evidence);
        var before = Scenes();
        var outputs = new List<object>();
        if (includeClaw)
        {
            outputs.Add(Family("ClawVisual", "Concepts/Unused/Visual/FastGroupEnemy23AssetPack/asset-pack.json"));
        }
        string scorpion = "Concepts/Unused/Visual/Scorpion01/asset-pack.json";
        if (File.Exists(scorpion))
        {
            outputs.Add(Family("ScorpionVisual", scorpion));
        }
        var after = Scenes();
        if (!JToken.DeepEquals(before, after))
        {
            throw new InvalidOperationException("Loaded scene state changed; inspect before continuing.");
        }
        var report = new { families = outputs, scenesBefore = before, scenesAfter = after,
            sceneStatePreserved = true, addedSceneObjects = 0, audioSources = 0, gameplayScripts = 0 };
        string filename = includeClaw ? "prefabs.json" : "scorpion-r5.json";
        File.WriteAllText(Evidence + filename, JObject.FromObject(report).ToString());
        return report;
    }

    private static object Family(string name, string manifestPath)
    {
        JObject manifest = JObject.Parse(File.ReadAllText(manifestPath));
        string folder = Root + "Animations/Enemies/" + name + "/";
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        var sprites = new Dictionary<string, Sprite[]>();
        var clips = new List<AnimationClip>();
        foreach (JObject motion in manifest["states"])
        {
            Sprite[] frames = ImportSheet(motion);
            sprites.Add((string)motion["name"], frames);
            Sprite[] eyes = null;
            if (motion["emission"] is JObject emission)
            {
                eyes = ImportSheet(emission);
            }
            clips.Add(Clip(folder, (string)motion["name"], frames,
                motion["durationsMs"].ToObject<int[]>(), (bool)motion["loop"], false, eyes));
        }
        Sprite idle;
        if (manifest["idle"] is JObject idleSpec)
        {
            idle = ImportSingle(idleSpec);
        }
        else
        {
            idle = sprites["Sweep"][0];
        }
        Sprite idleEye = null;
        if (manifest["idle"]?["emission"] is JObject idleEmission)
        {
            idleEye = ImportSingle(idleEmission);
        }
        Sprite[] idleEyes = idleEye == null ? null : new[] { idleEye };
        clips.Insert(0, Clip(folder, "Idle", new[] { idle }, new[] { 1000 }, true, false, idleEyes));
        if (manifest["remains"] is JArray remains)
        {
            foreach (JObject item in remains)
            {
                Sprite frame = ImportSingle(item);
                clips.Add(Clip(folder, (string)item["name"], new[] { frame }, new[] { 1000 }, false, true));
            }
        }
        string controllerPath = folder + name + ".controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Motion", AnimatorControllerParameterType.Int);
        }
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach (AnimatorStateTransition old in machine.anyStateTransitions)
        {
            machine.RemoveAnyStateTransition(old);
        }
        for (int index = 0; index < clips.Count; index++)
        {
            AnimationClip clip = clips[index];
            AnimatorState state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == clip.name);
            if (state == null)
            {
                state = machine.AddState(clip.name, new Vector3(260, index * 65));
            }
            state.motion = clip;
            state.writeDefaultValues = false;
            AnimatorStateTransition transition = machine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.duration = 0;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.Equals, index, "Motion");
            if (index == 0)
            {
                machine.defaultState = state;
            }
        }
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);
        string prefabPath = Root + "Prefabs/Enemies/" + name + ".prefab";
        Scene preview = EditorSceneManager.NewPreviewScene();
        GameObject actor = null;
        try
        {
            actor = new GameObject(name);
            SceneManager.MoveGameObjectToScene(actor, preview);
            Animator animator = actor.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var visual = new GameObject("Visual");
            visual.transform.SetParent(actor.transform, false);
            var body = new GameObject("Body").AddComponent<SpriteRenderer>();
            body.transform.SetParent(visual.transform, false);
            body.sprite = idle;
            body.sortingOrder = RemainsVisualStyle.LiveOrder;
            body.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Shared/FlatLit.mat");
            if (idleEye != null)
            {
                var eyes = new GameObject("Eyes").AddComponent<SpriteRenderer>();
                eyes.transform.SetParent(visual.transform, false);
                eyes.sprite = idleEye;
                eyes.sortingOrder = RemainsVisualStyle.LiveOrder + 1;
                eyes.sharedMaterial = EyeMaterial((string)manifest["emissionMaterial"]);
            }
            PrefabUtility.SaveAsPrefabAsset(actor, prefabPath);
            if (actor.GetComponentsInChildren<AudioSource>(true).Length != 0 ||
                actor.GetComponentsInChildren<MonoBehaviour>(true).Length != 0 ||
                actor.GetComponentsInChildren<Collider2D>(true).Length != 0)
            {
                throw new InvalidOperationException("Visual prefab unexpectedly contains gameplay/audio.");
            }
            var samples = new List<object>();
            foreach (AnimationClip clip in clips)
            {
                foreach (float fraction in new[] { 0f, .35f, .6f, .999f })
                {
                    clip.SampleAnimation(actor, clip.length * fraction);
                    if (body.sprite == null)
                    {
                        throw new InvalidOperationException("Missing sprite in " + clip.name);
                    }
                    samples.Add(new { clip = clip.name, fraction, sprite = body.sprite.name,
                        width = body.bounds.size.x, height = body.bounds.size.y, color = body.color.ToString() });
                }
            }
            return new { name, prefab = prefabPath, controller = controllerPath,
                motionValues = clips.Select((clip, index) => new { value = index, clip = clip.name, duration = clip.length }),
                samples, audioSources = 0, gameplayScripts = 0, colliders = 0,
                spriteRenderers = actor.GetComponentsInChildren<SpriteRenderer>(true).Length,
                emissiveEyes = idleEye != null };
        }
        finally
        {
            if (actor != null)
            {
                UnityEngine.Object.DestroyImmediate(actor);
            }
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    private static Sprite[] ImportSheet(JObject spec)
    {
        string path = (string)spec["asset"];
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        Configure(importer, spec, SpriteImportMode.Multiple);
        int count = (int)spec["count"];
        int cell = (int)spec["cell"];
        int columns = (int)spec["columns"];
        int rows = Mathf.CeilToInt(count / (float)columns);
        Vector2 pivot = Pivot(spec, cell, cell);
        importer.SaveAndReimport();
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var previous = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);
        var slices = new SpriteRect[count];
        for (int index = 0; index < count; index++)
        {
            string name = (string)spec["name"] + "_" + index.ToString("00");
            GUID id = previous.TryGetValue(name, out GUID existing) ? existing : GUID.Generate();
            slices[index] = new SpriteRect { name = name, spriteID = id,
                rect = new Rect(index % columns * cell, (rows - 1 - index / columns) * cell, cell, cell),
                alignment = SpriteAlignment.Custom, pivot = pivot };
        }
        provider.SetSpriteRects(slices);
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        names.SetNameFileIdPairs(slices.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
        Sprite[] result = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (result.Length != count)
        {
            throw new InvalidOperationException("Wrong sprite slice count: " + path);
        }
        return result;
    }

    private static Sprite ImportSingle(JObject spec)
    {
        string path = (string)spec["asset"];
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        Configure(importer, spec, SpriteImportMode.Single);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = Pivot(spec, texture.width, texture.height);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void Configure(TextureImporter importer, JObject spec, SpriteImportMode mode)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = mode;
        importer.spritePixelsPerUnit = (float)spec["pixelsPerUnit"];
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.sRGBTexture = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 4096;
    }

    private static Vector2 Pivot(JObject spec, int width, int height) =>
        new Vector2((float)spec["pivotPixelsTopLeft"][0] / width, 1 - (float)spec["pivotPixelsTopLeft"][1] / height);

    private static Material EyeMaterial(string path)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        AssetDatabase.Refresh();
        Shader shader = Shader.Find("EmergencyShift/Prop Emission");
        if (shader == null)
        {
            throw new InvalidOperationException("Missing shared emission shader.");
        }
        material = new Material(shader) { name = "EyeGlow" };
        material.SetColor("_Color", new Color(1.8f, .025f, .012f, 1));
        material.SetColor("_EmissionKey", Color.red);
        material.SetFloat("_MinimumChroma", .25f);
        material.SetFloat("_HueMatch", .96f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static AnimationClip Clip(string folder, string name, Sprite[] frames, int[] durations, bool loop, bool dead, Sprite[] eyes = null)
    {
        string path = folder + name + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        bool create = clip == null;
        if (create)
        {
            clip = new AnimationClip { name = name, frameRate = 1000 };
        }
        // These clips are wholly owned by this builder. Reset old curve ranges
        // before shortening a revision, retaining the asset and its GUID.
        clip.ClearCurves();
        clip.frameRate = 1000;
        var keys = new List<ObjectReferenceKeyframe>();
        float age = 0;
        for (int index = 0; index < frames.Length; index++)
        {
            keys.Add(new ObjectReferenceKeyframe { time = age, value = frames[index] });
            age += durations[index] * .001f;
        }
        // Unity includes the final object-reference sample interval in length.
        // Let loopTime wrap to frame zero, rather than adding an early frame zero.
        keys.Add(new ObjectReferenceKeyframe { time = Mathf.Max(0, age - .001f), value = frames[frames.Length - 1] });
        AnimationUtility.SetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve("Visual/Body", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
        if (eyes != null)
        {
            var eyeKeys = keys.Select((key, index) => new ObjectReferenceKeyframe
                { time = key.time, value = eyes[Math.Min(index, eyes.Length - 1)] }).ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve("Visual/Eyes", typeof(SpriteRenderer), "m_Sprite"), eyeKeys);
        }
        Color tint = dead ? RemainsVisualStyle.Tint : Color.white;
        string[] channels = { "r", "g", "b", "a" };
        for (int index = 0; index < channels.Length; index++)
        {
            clip.SetCurve("Visual/Body", typeof(SpriteRenderer), "m_Color." + channels[index], AnimationCurve.Constant(0, age, tint[index]));
        }
        clip.SetCurve("Visual/Body", typeof(SpriteRenderer), "m_SortingOrder", AnimationCurve.Constant(0, age, dead ? 0 : 1));
        clip.SetCurve("Visual", typeof(Transform), "m_LocalPosition.z", AnimationCurve.Constant(0, age, dead ? RemainsVisualStyle.SurfaceZ : 0));
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.startTime = 0;
        settings.stopTime = age;
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        if (create)
        {
            AssetDatabase.CreateAsset(clip, path);
        }
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssetIfDirty(clip);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (Mathf.Abs(clip.length - age) > .0011f)
        {
            throw new InvalidOperationException("Clip duration differs from frame timings: " + path);
        }
        return clip;
    }

    private static JArray Scenes()
    {
        var result = new JArray();
        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene scene = SceneManager.GetSceneAt(index);
            if (EditorSceneManager.IsPreviewScene(scene))
            {
                continue;
            }
            result.Add(new JObject { ["path"] = scene.path, ["dirty"] = scene.isDirty,
                ["objects"] = scene.GetRootGameObjects().Sum(g => g.GetComponentsInChildren<Transform>(true).Length) });
        }
        return result;
    }
}
