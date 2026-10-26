using System;
using System.IO;
using System.Linq;
using EmergencyShift;
using EmergencyShift.Features.ClawEnemy.Config;
using EmergencyShift.Features.ClawEnemy.View;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Adds only the new prototype family to the existing authored sandbox.</summary>
public static class InstallClawSandbox
{
    const string Root = "Assets/EmergencyShift/";
    const string Art = Root + "Art/Enemies/ClawPrototype/";
    const string Data = Root + "Data/Enemies/ClawPrototype/";
    const string Source = "Concepts/Unused/Visual/FastGroupEnemy17SimplifiedMotion/";
    const string Remains = "Concepts/Unused/Visual/FastGroupEnemy18Remains/";
    const string PrefabPath = Root + "Prefabs/Enemies/ClawPrototype.prefab";
    public static object Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.name != "MechanicsSandbox" || scene.isDirty)
            throw new InvalidOperationException("Requires the clean, open authored MechanicsSandbox outside Play.");
        if (File.Exists(PrefabPath) || Find(scene, "10_World/R01_StalkerTest/Gameplay/Claw encounter") != null)
            throw new InvalidOperationException("Already installed; edit existing assets instead of regenerating them.");
        Directory.CreateDirectory("Docs/Validation/ClawSandbox01");
        File.Copy(scene.path, "Docs/Validation/ClawSandbox01/scene-before.unity.txt", false);
        Directory.CreateDirectory(Art);
        Directory.CreateDirectory(Data);
        AssetDatabase.Refresh();
        JObject source = JObject.Parse(File.ReadAllText(Source + "animation-set.json"));
        TimedSpriteMotion walk = Motion("Walk", "walk", (JObject)source["states"]["walk"], true, .3205128205f * 270 / 400);
        TimedSpriteMotion run = Motion("Run", "run", (JObject)source["states"]["run"], true, 1.6472868217f * 270 / 400);
        var attackSprites = Enumerable.Range(0, 18).Select(i => Import(Source + "alternatives/heavy/sweep/frames/sweep-" + i.ToString("00") + ".png", "Sweep/sweep-" + i.ToString("00") + ".png", false)).ToArray();
        TimedSpriteMotion attack = MotionAsset("Sweep", attackSprites, new[]{70, 70, 70, 80, 80, 80, 90, 100, 30, 30, 40, 50, 60, 70, 70, 70, 70, 80}, false, 0);
        var motions = ScriptableObject.CreateInstance<ClawMotionSet>();
        Edit(motions, s =>
        {
            Ref(s, "walk", walk);
            Ref(s, "run", run);
            Ref(s, "attack", attack);
            Float(s, "turnDegreesPerSecond", 360);
        });
        Save(motions, "Motions");
        motions.Validate();
        var corpses = new[]{"corpse-v2.png", "torn-v3.png", "heap-v2.png"}.Select(p => Import(Remains + p, "Remains/" + p, true)).ToArray();
        var definition = ScriptableObject.CreateInstance<EnemyDefinition>();
        Edit(definition, s =>
        {
            s.FindProperty("displayName").stringValue = "Когтистый · прототип";
            s.FindProperty("maximumHealth").intValue = 4;
            Float(s, "pursuitSpeed", 1.65f);
            Float(s, "hitStaggerSeconds", .09f);
            Float(s, "hitPushSpeed", 1.2f);
            s.FindProperty("attackDamage").intValue = 1;
            Float(s, "attackDistance", 1.22f);
            Float(s, "attackDuration", 1.21f);
            Float(s, "attackCooldown", 1f);
            Float(s, "lungeSpeed", 1.2f);
            Float(s, "biteDistance", 1.6f);
            Float(s, "lungeStart", .529f);
            Float(s, "lungeEnd", .60f);
            Float(s, "strikeAt", .554f);
            Float(s, "attackArcDegrees", 110);
        });
        Save(definition, "Definition");
        definition.Validate();
        var gore = ScriptableObject.CreateInstance<GoreProfile>();
        Edit(gore, s =>
        {
            Float(s, "poolSize", 1.6f);
            Float(s, "releaseDelay", .06f);
            s.FindProperty("impulseRange").vector2Value = Vector2.zero;
            s.FindProperty("liftRange").vector2Value = Vector2.zero;
            s.FindProperty("settledTint").colorValue = new Color(.65f, .61f, .64f, 1);
            s.FindProperty("airborneTint").colorValue = new Color(.65f, .61f, .64f, 1);
            var variants = s.FindProperty("variants");
            variants.arraySize = 3;
            string[] ids = {"bloody-carcass", "torn-forelimb", "meat-heap"};
            for (int i = 0; i < 3; i++)
            {
                var v = variants.GetArrayElementAtIndex(i);
                v.FindPropertyRelative("id").stringValue = ids[i];
                v.FindPropertyRelative("fragmentCount").intValue = 1;
                v.FindPropertyRelative("scale").vector2Value = Vector2.one;
                v.FindPropertyRelative("impulseMultiplier").floatValue = 1;
                v.FindPropertyRelative("selectionWeight").intValue = 1;
                v.FindPropertyRelative("replaceOriginalBody").boolValue = true;
                var parts = v.FindPropertyRelative("pieces");
                parts.arraySize = 1;
                var p = parts.GetArrayElementAtIndex(0);
                p.FindPropertyRelative("spriteIndex").intValue = i;
                p.FindPropertyRelative("offset").vector2Value = Vector2.zero;
                p.FindPropertyRelative("scale").vector2Value = Vector2.one;
                p.FindPropertyRelative("impulseMultiplier").floatValue = 1;
                p.FindPropertyRelative("liftMultiplier").floatValue = 1;
                p.FindPropertyRelative("preserveSourcePose").boolValue = true;
            }
        });
        Save(gore, "Remains");
        gore.Validate();
        GameObject donor = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Enemies/ShadowStalker.prefab");
        var donorActor = new SerializedObject(donor.GetComponent<ShadowStalker2D>());
        int mask = donorActor.FindProperty("blockingGeometry").intValue;
        Material material = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Shared/FlatLit.mat");
        GameObject temp = new GameObject("Claw prototype");
        temp.SetActive(false);
        temp.layer = donor.layer;
        var rigid = temp.AddComponent<Rigidbody2D>();
        rigid.gravityScale = 0;
        rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
        rigid.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rigid.interpolation = RigidbodyInterpolation2D.Interpolate;
        temp.AddComponent<CircleCollider2D>().radius = .4f;
        temp.AddComponent<EnemyHealth2D>();
        temp.AddComponent<EnemyMotor2D>();
        temp.AddComponent<EnemyPerception2D>();
        temp.AddComponent<EnemyMeleeAttack2D>();
        var patrol = temp.AddComponent<EnemyPatrol2D>();
        var donorPatrol = new SerializedObject(donor.GetComponent<EnemyPatrol2D>());
        Edit(patrol, s =>
        {
            Ref(s, "profile", donorPatrol.FindProperty("profile").objectReferenceValue);
            s.FindProperty("areaSize").vector2Value = new Vector2(3.5f, 3.5f);
        });
        temp.AddComponent<CrowdAgent2D>();
        var actor = temp.AddComponent<ClawEnemy2D>();
        Edit(actor, s =>
        {
            Ref(s, "definition", definition);
            s.FindProperty("blockingGeometry").intValue = mask;
            Float(s, "detectionRadius", 3.2f);
        });
        var visual = new GameObject("Visual", typeof(SortingGroup)).transform;
        visual.SetParent(temp.transform, false);
        visual.GetComponent<SortingGroup>().sortingOrder = RemainsVisualStyle.LiveOrder;
        var body = new GameObject("Body", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        body.transform.SetParent(visual, false);
        body.sprite = walk.FrameAt(0);
        body.sharedMaterial = material;
        var animation = temp.AddComponent<ClawSpriteAnimator2D>();
        Edit(animation, s =>
        {
            Ref(s, "actor", actor);
            Ref(s, "visualRoot", visual);
            Ref(s, "body", body);
            Ref(s, "motions", motions);
        });
        var feedback = temp.AddComponent<EnemyHitResponse2D>();
        var oldFeedback = new SerializedObject(donor.GetComponent<EnemyHitResponse2D>());
        feedback.Configure(visual, body, oldFeedback.FindProperty("bloodSprite").objectReferenceValue as Sprite, oldFeedback.FindProperty("bloodMaterial").objectReferenceValue as Material);
        var burst = temp.AddComponent<GoreBurst2D>();
        burst.Configure(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Enemies/BloodPool.prefab").GetComponent<BloodPool2D>(), corpses, material);
        Edit(burst, s =>
        {
            Ref(s, "profile", gore);
            Ref(s, "replacedBody", body);
        });
        Edit(temp.AddComponent<EnemyDeathEffects2D>(), s => s.FindProperty("blockingGeometry").intValue = mask);
        temp.SetActive(true);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath);
        UnityEngine.Object.DestroyImmediate(temp);
        Transform encounter = Group(Find(scene, "10_World/R01_StalkerTest/Gameplay"), "Claw encounter");
        Spawn(prefab, encounter, "Claw · waiting · 4 HP", new Vector2(5, 4.5f), false, false);
        Spawn(prefab, encounter, "Claw · patrol · 4 HP", new Vector2(15, 5.5f), true, false);
        Spawn(prefab, Find(scene, "10_World/R03_WeaponsRange/Gameplay"), "Claw · stationary · 4 HP", new Vector2(42.5f, 8), false, true);
        // Reusable display prefab: all three layouts use sprite overrides, with no AI/physics/audio.
        GameObject display = new GameObject("Claw remains", typeof(SpriteRenderer));
        var renderer = display.GetComponent<SpriteRenderer>();
        renderer.sprite = corpses[0];
        renderer.sharedMaterial = material;
        renderer.sortingOrder = RemainsVisualStyle.DeadOrder;
        renderer.color = gore.SettledTint;
        GameObject displayPrefab = PrefabUtility.SaveAsPrefabAsset(display, Root + "Prefabs/Enemies/ClawRemainsPreview.prefab");
        UnityEngine.Object.DestroyImmediate(display);
        Transform review = Group(Find(scene, "10_World/R03_WeaponsRange/Dressing"), "Claw remains · A B C");
        for (int i = 0; i < 3; i++)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(displayPrefab, review);
            instance.name = new[]{"A · bloody carcass", "B · torn forelimb", "C · meat heap"}[i];
            instance.transform.position = new Vector3(47, 10.7f + i * 1.7f, RemainsVisualStyle.SurfaceZ);
            instance.GetComponent<SpriteRenderer>().sprite = corpses[i];
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<SpriteRenderer>());
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return new
        {
        scene = scene.path, actors = 3, remainsVariants = 3, newAudioSources = 0, prefab = PrefabPath
        }

        ;
    }

    public static object CorrectTornSelection()
    {
        if (Application.isPlaying || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Stop Play and preserve unsaved edits first.");
        string oldPath = Art + "Remains/torn-v4.png";
        string path = Art + "Remains/torn-v3.png";
        string guid = AssetDatabase.AssetPathToGUID(oldPath);
        string error = AssetDatabase.MoveAsset(oldPath, path);
        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException(error);
        File.Copy(Remains + "torn-v3.png", path, true);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        if (AssetDatabase.AssetPathToGUID(path) != guid)
            throw new InvalidOperationException("Sprite GUID changed.");
        return new
        {
        path, guid, reason = "B4 exceeds fixed 3D forearm length; B3 has a feasible unchanged-length pose."
        }

        ;
    }

    static TimedSpriteMotion Motion(string name, string state, JObject plan, bool loop, float speed)
    {
        Sprite[] frames = plan["frames"].Values<string>().Select(path => Import(Source + path, state + "/" + Path.GetFileName(path), false)).ToArray();
        return MotionAsset(name, frames, plan["durationsMs"].Values<int>().ToArray(), loop, speed);
    }

    static TimedSpriteMotion MotionAsset(string name, Sprite[] frames, int[] durations, bool loop, float speed)
    {
        var asset = ScriptableObject.CreateInstance<TimedSpriteMotion>();
        Edit(asset, s =>
        {
            var f = s.FindProperty("frames");
            f.arraySize = frames.Length;
            var d = s.FindProperty("durationsMs");
            d.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
            {
                f.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
                d.GetArrayElementAtIndex(i).intValue = durations[i];
            }

            s.FindProperty("loop").boolValue = loop;
            Float(s, "referenceSpeed", speed);
        });
        Save(asset, name);
        asset.Validate();
        return asset;
    }

    static Sprite Import(string source, string relative, bool remains)
    {
        string destination = Art + relative;
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        File.Copy(source, destination, false);
        AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(destination);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = remains ? 400 / .72f : 400;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.sRGBTexture = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = remains ? 2048 : 1024;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = remains ? new Vector2(760f / 1254, 1 - 665f / 1254) : new Vector2(602.2f / 1024, 1 - 511.8f / 1024);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(destination);
    }

    static void Spawn(GameObject prefab, Transform parent, string name, Vector2 at, bool patrol, bool stationary)
    {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        obj.name = name;
        obj.transform.position = at;
        Edit(obj.GetComponent<EnemyPatrol2D>(), s => s.FindProperty("patrolEnabled").boolValue = patrol);
        Edit(obj.GetComponent<ClawEnemy2D>(), s => s.FindProperty("stationary").boolValue = stationary);
        PrefabUtility.RecordPrefabInstancePropertyModifications(obj.GetComponent<EnemyPatrol2D>());
        PrefabUtility.RecordPrefabInstancePropertyModifications(obj.GetComponent<ClawEnemy2D>());
    }

    static Transform Group(Transform parent, string name)
    {
        if (parent == null)
            throw new InvalidOperationException("Missing authored parent");
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    static Transform Find(Scene scene, string path)
    {
        string[] parts = path.Split('/');
        var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == parts[0]);
        return root == null ? null : parts.Length == 1 ? root.transform : root.transform.Find(string.Join("/", parts.Skip(1)));
    }

    static void Edit(UnityEngine.Object target, Action<SerializedObject> apply)
    {
        var serialized = new SerializedObject(target);
        apply(serialized);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Ref(SerializedObject s, string name, UnityEngine.Object value) => s.FindProperty(name).objectReferenceValue = value;
    static void Float(SerializedObject s, string name, float value) => s.FindProperty(name).floatValue = value;
    static void Save(UnityEngine.Object asset, string name)
    {
        AssetDatabase.CreateAsset(asset, Data + name + ".asset");
        AssetDatabase.SaveAssetIfDirty(asset);
    }
}
