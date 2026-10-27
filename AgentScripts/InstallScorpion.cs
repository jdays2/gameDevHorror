using System;
using System.IO;
using System.Linq;
using EmergencyShift;
using EmergencyShift.Features.ClawEnemy.Config;
using EmergencyShift.Features.Scorpion.Config;
using EmergencyShift.Features.Scorpion.View;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Asset composition and one additive room. Never regenerates the authored map.</summary>
public static class InstallScorpion
{
    private const string Root = "Assets/EmergencyShift/";
    private const string Data = Root + "Data/Enemies/Scorpion/";
    private const string Prefab = Root + "Prefabs/Enemies/Scorpion.prefab";
    private const string Evidence = "Docs/Validation/ScorpionRoom01/";

    public static object Assets()
    {
        if (Application.isPlaying || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("Requires stopped compiled editor.");
        }
        Directory.CreateDirectory(Data);
        AssetDatabase.Refresh();
        JObject pack = JObject.Parse(File.ReadAllText("Concepts/Unused/Visual/Scorpion01/asset-pack.json"));
        JObject run = (JObject)pack["states"][0];
        JObject attack = (JObject)pack["states"][1];
        TimedSpriteMotion runBody = Motion("Run", run, run);
        TimedSpriteMotion runEyes = Motion("RunEyes", (JObject)run["emission"], run);
        TimedSpriteMotion attackBody = Motion("Attack", attack, attack);
        TimedSpriteMotion attackEyes = Motion("AttackEyes", (JObject)attack["emission"], attack);
        var presentation = Asset<ScorpionPresentationSet>("Presentation");
        Edit(presentation, s =>
        {
            Ref(s, "run", runBody);
            Ref(s, "runEyes", runEyes);
            Ref(s, "attack", attackBody);
            Ref(s, "attackEyes", attackEyes);
            // Neutral uses another foot placement. Waiting on Run00 avoids a start-up pop.
            Ref(s, "idle", runBody.FrameAt(0));
            Ref(s, "idleEyes", runEyes.FrameAt(0));
            var steps = s.FindProperty("steps");
            steps.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                steps.GetArrayElementAtIndex(i).objectReferenceValue = Audio("Step" + (i + 1));
            }
            Ref(s, "hit", Audio("Hit"));
            Ref(s, "presence", Audio("Presence"));
            Ref(s, "grasp", Audio("Attack"));
            Ref(s, "alarm", Audio("Alarm"));
        });
        presentation.Validate();
        var definition = Asset<EnemyDefinition>("Definition");
        Edit(definition, s =>
        {
            s.FindProperty("displayName").stringValue = "Скорпион";
            s.FindProperty("maximumHealth").intValue = 3;
            Float(s, "pursuitSpeed", 2.4f);
            Float(s, "hitStaggerSeconds", 0);
            Float(s, "hitPushSpeed", 0);
            s.FindProperty("attackDamage").intValue = 1;
            Float(s, "attackDistance", 1.35f);
            Float(s, "attackDuration", .56f);
            Float(s, "attackCooldown", .7f);
            Float(s, "lungeSpeed", .1f);
            Float(s, "biteDistance", 1.25f);
            Float(s, "lungeStart", .32f);
            Float(s, "lungeEnd", .5f);
            Float(s, "strikeAt", .4f);
            Float(s, "attackArcDegrees", 95);
        });
        definition.Validate();
        var gore = Asset<GoreProfile>("Remains");
        Edit(gore, s =>
        {
            Float(s, "poolSize", 1.1f);
            Float(s, "releaseDelay", .06f);
            s.FindProperty("impulseRange").vector2Value = Vector2.zero;
            s.FindProperty("liftRange").vector2Value = Vector2.zero;
            var variants = s.FindProperty("variants");
            variants.arraySize = 1;
            var variant = variants.GetArrayElementAtIndex(0);
            variant.FindPropertyRelative("id").stringValue = "scorpion-carcass";
            variant.FindPropertyRelative("fragmentCount").intValue = 1;
            variant.FindPropertyRelative("selectionWeight").intValue = 1;
            variant.FindPropertyRelative("scale").vector2Value = Vector2.one;
            variant.FindPropertyRelative("impulseMultiplier").floatValue = 1;
            variant.FindPropertyRelative("replaceOriginalBody").boolValue = true;
            var pieces = variant.FindPropertyRelative("pieces");
            pieces.arraySize = 1;
            var piece = pieces.GetArrayElementAtIndex(0);
            piece.FindPropertyRelative("spriteIndex").intValue = 0;
            piece.FindPropertyRelative("offset").vector2Value = Vector2.zero;
            piece.FindPropertyRelative("scale").vector2Value = Vector2.one;
            piece.FindPropertyRelative("preserveSourcePose").boolValue = true;
            piece.FindPropertyRelative("impulseMultiplier").floatValue = 1;
            piece.FindPropertyRelative("liftMultiplier").floatValue = 1;
        });
        gore.Validate();
        Scene preview = EditorSceneManager.NewPreviewScene();
        GameObject actor = null;
        try
        {
            actor = new GameObject("Scorpion");
            SceneManager.MoveGameObjectToScene(actor, preview);
            actor.SetActive(false);
            actor.layer = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Enemies/ShadowStalker.prefab").layer;
            var rigid = actor.AddComponent<Rigidbody2D>();
            rigid.gravityScale = 0;
            rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
            rigid.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rigid.interpolation = RigidbodyInterpolation2D.Interpolate;
            actor.AddComponent<CircleCollider2D>().radius = .29f;
            actor.AddComponent<EnemyHealth2D>();
            actor.AddComponent<EnemyMotor2D>();
            actor.AddComponent<EnemyPerception2D>();
            actor.AddComponent<EnemyMeleeAttack2D>();
            actor.AddComponent<CrowdAgent2D>();
            var brain = actor.AddComponent<Scorpion2D>();
            int obstacles = (1 << 8) | (1 << 9);
            Edit(brain, s =>
            {
                Ref(s, "definition", definition);
                Ref(s, "presentation", presentation);
                s.FindProperty("blockingGeometry").intValue = obstacles;
            });
            var source = actor.AddComponent<ScorpionPresentationSource2D>();
            Transform visual = Group(actor.transform, "Visual");
            visual.gameObject.AddComponent<SortingGroup>().sortingOrder = RemainsVisualStyle.LiveOrder;
            var body = Renderer(visual, "Body", runBody.FrameAt(0), Root + "Materials/Shared/FlatLit.mat", 0);
            var eyes = Renderer(visual, "Eyes", runEyes.FrameAt(0), Root + "Materials/Characters/Scorpion/EyeGlow.mat", 1);
            var animator = actor.AddComponent<ScorpionSpriteAnimator2D>();
            Edit(animator, s =>
            {
                Ref(s, "actor", brain);
                Ref(s, "visual", visual);
                Ref(s, "body", body);
                Ref(s, "eyes", eyes);
            });
            Transform shadows = Group(actor.transform, "Ground shadows");
            var contact = Renderer(shadows, "Contact shadow", body.sprite, Root + "Materials/Characters/CreatureSilhouetteShadow.mat", 0);
            var projection = Renderer(shadows, "Flashlight shadow", body.sprite, Root + "Materials/Characters/CreatureSilhouetteShadow.mat", 0);
            actor.AddComponent<CreatureGroundShadow2D>().Configure(source, visual, contact, projection);
            Transform audio = Group(actor.transform, "Audio");
            var feedback = actor.AddComponent<ScorpionAudio2D>();
            Edit(feedback, s =>
            {
                Ref(s, "voice", Source(audio, "Voice", 120));
                Ref(s, "claws", Source(audio, "Claw grasp", 120));
                Ref(s, "contact", Source(audio, "Foot contacts", 180));
                Ref(s, "presence", Source(audio, "Quiet presence", 220));
            });
            var burst = actor.AddComponent<GoreBurst2D>();
            burst.Configure(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Enemies/BloodPool.prefab").GetComponent<BloodPool2D>(),
                new[]{body.sprite}, body.sharedMaterial);
            Edit(burst, s =>
            {
                Ref(s, "profile", gore);
                Ref(s, "replacedBody", body);
            });
            Edit(actor.AddComponent<ScorpionCorpsePose2D>(), s =>
            {
                Ref(s, "body", body);
                Ref(s, "pool", AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Enemies/BloodPool.prefab").GetComponent<BloodPool2D>());
            });
            Edit(actor.AddComponent<EnemyDeathEffects2D>(), s => s.FindProperty("blockingGeometry").intValue = obstacles);
            actor.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(actor, Prefab);
        }
        finally
        {
            if (actor != null)
            {
                UnityEngine.Object.DestroyImmediate(actor);
            }
            EditorSceneManager.ClosePreviewScene(preview);
        }
        AssetDatabase.SaveAssets();
        return new { prefab = Prefab, hp = 3, speed = 2.4, attackSpeed = attackBody.ReferenceSpeed, run = runBody.Duration, attack = attackBody.Duration };
    }

    public static object Room()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.name != "MechanicsSandbox" || scene.isDirty)
        {
            throw new InvalidOperationException("Requires clean open MechanicsSandbox; preserve unsaved edits.");
        }
        Transform world = Find(scene, "10_World");
        if (world.Find("R04_ScorpionPackTest") != null)
        {
            throw new InvalidOperationException("Room already installed; edit instead of recreating it.");
        }
        Directory.CreateDirectory(Evidence);
        File.Copy(scene.path, Evidence + "scene-before.unity.txt", false);
        Transform room = Group(world, "R04_ScorpionPackTest");
        Transform geometry = Group(room, "Geometry");
        var floor = new GameObject("Concrete floor", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        floor.transform.SetParent(geometry, false);
        floor.transform.position = new Vector3(50, -12, 0);
        floor.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/Environment/FloorMaterials/warehouse-concrete-base-01.png");
        floor.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Shared/FlatLit.mat");
        floor.drawMode = SpriteDrawMode.Tiled;
        floor.size = new Vector2(12, 12);
        floor.sortingOrder = -10;
        Wall(geometry, "West wall", new Vector2(44, -12), new Vector2(.4f, 12.4f));
        Wall(geometry, "East wall", new Vector2(56, -12), new Vector2(.4f, 12.4f));
        Wall(geometry, "South wall", new Vector2(50, -18), new Vector2(12, .4f));
        // One shared corridor boundary is split only at the new three-unit entrance.
        Transform boundary = Find(scene, "10_World/C01_SafeCorridor/Geometry/South boundary");
        boundary.position = new Vector3(24.15f, -6, 0);
        boundary.localScale = new Vector3(48.7f, .4f, 1);
        PrefabUtility.RecordPrefabInstancePropertyModifications(boundary);
        Wall(boundary.parent, "South boundary east of scorpion entry", new Vector2(55.85f, -6), new Vector2(8.7f, .4f));
        Transform gameplay = Group(room, "Gameplay");
        GameObject door = Spawn(Root + "Prefabs/World/Door.prefab", gameplay, "Entry door · alarms scorpion pack", new Vector2(50, -6));
        var motion = door.GetComponent<Door2D>();
        Edit(motion, s => s.FindProperty("openOffset").vector3Value = new Vector3(3.1f, 0, 0));
        Transform leaf = motion.Blocker.transform;
        leaf.localScale = new Vector3(3, .3f, 1);
        leaf.localRotation = Quaternion.identity;
        PrefabUtility.RecordPrefabInstancePropertyModifications(leaf);
        var encounter = Group(gameplay, "Scorpion pack · four members");
        var pack = encounter.gameObject.AddComponent<ScorpionPack2D>();
        Vector2[] positions = {new Vector2(47.5f,-11.3f),new Vector2(51,-12.2f),new Vector2(52.5f,-15),new Vector2(48.4f,-15.7f)};
        Scorpion2D[] members = new Scorpion2D[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            var instance = Spawn(Prefab, encounter, "Scorpion · pack member " + (i + 1), positions[i]);
            members[i] = instance.GetComponent<Scorpion2D>();
            instance.transform.rotation = Quaternion.Euler(0, 0, new[]{-12,18,-30,28}[i]);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        }
        Edit(pack, s =>
        {
            var list = s.FindProperty("members");
            list.arraySize = members.Length;
            for (int i = 0; i < members.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = members[i];
            }
            var doors = s.FindProperty("entranceDoors");
            doors.arraySize = 1;
            doors.GetArrayElementAtIndex(0).objectReferenceValue = motion;
        });
        Transform dressing = Group(room, "Dressing · feeding traces");
        string[] traces = AssetDatabase.FindAssets("t:Prefab", new[]{Root + "Prefabs/Environment/BloodTraces05"}).Select(AssetDatabase.GUIDToAssetPath).Take(3).ToArray();
        for (int i = 0; i < traces.Length; i++)
        {
            Spawn(traces[i], dressing, "Feeding blood · " + (i + 1), new Vector2(47.7f + i * 2, -14.8f + (i % 2) * .8f));
        }
        var lighting = Group(room, "LightingAudio");
        Spawn(Root + "Prefabs/Environment/HospitalLamp01.prefab", lighting, "Dim inspection lamp", new Vector2(44.5f, -9));
        Label(room, "04 · СКОРПИОНЫ\nДверь или видимость → тревога стаи\nАтака клешнями на ходу · свет не пугает", new Vector2(50,-7.4f));
        Label(world.Find("C01_SafeCorridor"), "04 ↓ СКОРПИОНЫ", new Vector2(50,-4.6f));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return new { room = "10_World/R04_ScorpionPackTest", bounds = "44..56, -18..-6", members = 4, scene = scene.path };
    }

    public static object FeedingRemains()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.name != "MechanicsSandbox" || scene.isDirty)
        {
            throw new InvalidOperationException("Requires clean stopped authored sandbox.");
        }
        Transform dressing = Find(scene, "10_World/R04_ScorpionPackTest/Dressing · feeding traces");
        if (dressing.Find("Feeding remains · rear alcove") != null)
        {
            return new { alreadyPresent = true };
        }
        Transform feeding = Group(dressing, "Feeding remains · rear alcove");
        Spawn(Root + "Prefabs/Environment/BloodTraces05/m04-spilled-edge.prefab", feeding, "Dense blood under ribs", new Vector2(46.3f,-16.5f));
        Spawn(Root + "Prefabs/Environment/BloodTraces05/scattered-stains.prefab", feeding, "Feeding transfer marks", new Vector2(47,-16));
        var ribs = Spawn(Root + "Prefabs/Environment/MedicalRemains01/rib-section.prefab", feeding, "Feeding remains · ribs", new Vector2(46.3f,-16.5f));
        ribs.transform.rotation = Quaternion.Euler(0,0,17);
        PrefabUtility.RecordPrefabInstancePropertyModifications(ribs.transform);
        Spawn(Root + "Prefabs/Environment/MedicalRemains01/crushed-fragment.prefab", feeding, "Feeding remains · small fragment", new Vector2(46.9f,-16.2f));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return new { decorativeRemains = 2, bloodOverlays = 2, collidersAdded = 0 };
    }

    private static TimedSpriteMotion Motion(string name, JObject spec, JObject timing)
    {
        var motion = Asset<TimedSpriteMotion>(name);
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath((string)spec["asset"]).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        int[] durations = timing["durationsMs"].ToObject<int[]>();
        Edit(motion, s =>
        {
            var frames = s.FindProperty("frames");
            var times = s.FindProperty("durationsMs");
            frames.arraySize = sprites.Length;
            times.arraySize = durations.Length;
            for (int i = 0; i < sprites.Length; i++)
            {
                frames.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
                times.GetArrayElementAtIndex(i).intValue = durations[i];
            }
            s.FindProperty("loop").boolValue = (bool)timing["loop"];
            Float(s, "referenceSpeed", (float)timing["referenceSpeed"]);
        });
        motion.Validate();
        return motion;
    }

    private static T Asset<T>(string name) where T : ScriptableObject
    {
        string path = Data + name + ".asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    private static AudioClip Audio(string name)
    {
        string path = Root + "Audio/Enemies/Scorpion/" + name + ".wav";
        var importer = (AudioImporter)AssetImporter.GetAtPath(path);
        importer.forceToMono = true;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
        settings.sampleRateOverride = 32000;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    private static SpriteRenderer Renderer(Transform parent, string name, Sprite sprite, string material, int order)
    {
        var renderer = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        renderer.transform.SetParent(parent, false);
        renderer.sprite = sprite;
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(material);
        renderer.sortingOrder = order;
        return renderer;
    }

    private static AudioSource Source(Transform parent, string name, int priority)
    {
        var source = new GameObject(name, typeof(AudioSource)).GetComponent<AudioSource>();
        source.transform.SetParent(parent, false);
        source.playOnAwake = false;
        source.priority = priority;
        source.dopplerLevel = 0;
        return source;
    }

    private static Transform Group(Transform parent, string name)
    {
        var node = new GameObject(name).transform;
        node.SetParent(parent, false);
        return node;
    }

    private static GameObject Spawn(string path, Transform parent, string name, Vector2 position)
    {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);
        obj.name = name;
        obj.transform.position = position;
        PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
        return obj;
    }

    private static void Wall(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject wall = Spawn(Root + "Prefabs/World/Wall.prefab", parent, name, position);
        wall.transform.localScale = new Vector3(size.x, size.y, 1);
        wall.GetComponent<SpriteRenderer>().color = new Color(.12f,.14f,.16f,1);
        PrefabUtility.RecordPrefabInstancePropertyModifications(wall.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(wall.GetComponent<SpriteRenderer>());
    }

    private static void Label(Transform parent, string value, Vector2 position)
    {
        var text = new GameObject(value.Split('\n')[0], typeof(TextMesh)).GetComponent<TextMesh>();
        text.transform.SetParent(parent, false);
        text.transform.position = position;
        text.text = value;
        text.fontSize = 40;
        text.characterSize = .075f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color(.65f,.72f,.69f,1);
        text.GetComponent<MeshRenderer>().sortingOrder = -7;
    }

    private static Transform Find(Scene scene, string path)
    {
        string[] parts = path.Split('/');
        Transform node = scene.GetRootGameObjects().First(g => g.name == parts[0]).transform;
        foreach (string part in parts.Skip(1))
        {
            node = node.Find(part);
        }
        return node;
    }

    private static void Edit(UnityEngine.Object obj, Action<SerializedObject> action)
    {
        var serial = new SerializedObject(obj);
        action(serial);
        serial.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(obj);
        if (EditorUtility.IsPersistent(obj))
        {
            AssetDatabase.SaveAssetIfDirty(obj);
        }
        else
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
        }
    }
    private static void Ref(SerializedObject s, string field, UnityEngine.Object value) => s.FindProperty(field).objectReferenceValue = value;
    private static void Float(SerializedObject s, string field, float value) => s.FindProperty(field).floatValue = value;
}
