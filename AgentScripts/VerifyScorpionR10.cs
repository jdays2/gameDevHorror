using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.Scorpion.Config;
using EmergencyShift.Features.Scorpion.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Functional verification of the authored four-member room. No scene or shared profile writes.</summary>
public static class VerifyScorpionR10
{
    private const string Folder = "Docs/Validation/ScorpionR10/";
    private static Transform[] Nodes() => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();

    public static object Inspect()
    {
        var nodes = Nodes();
        var room = nodes.First(node => node.name == "R04_ScorpionPackTest");
        return new
        {
            scene = SceneManager.GetActiveScene().path,
            dirty = SceneManager.GetActiveScene().isDirty,
            room = room.position.ToString(),
            members = room.GetComponentsInChildren<Scorpion2D>(true).Select(actor => new
            {
                actor.name,
                position = actor.transform.position.ToString(),
                bank = new SerializedObject(actor.GetComponent<GoreBurst2D>()).FindProperty("fragments").arraySize,
                bodyMaterial = actor.transform.Find("Visual/Body").GetComponent<SpriteRenderer>().sharedMaterial.name,
                eyes = actor.transform.Find("Visual/Eyes").GetComponent<SpriteRenderer>().color.ToString()
            }).ToArray(),
            door = room.GetComponentInChildren<Door2D>(true).transform.position.ToString()
        };
    }

    public static async Task<object> Play()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires native Play of MechanicsSandbox.");
        }
        Directory.CreateDirectory(Folder);
        var nodes = Nodes();
        var context = nodes.Select(node => node.GetComponent<LevelContext2D>()).First(value => value != null);
        var player = context.Player;
        var room = nodes.First(node => node.name == "R04_ScorpionPackTest");
        var pack = room.GetComponentInChildren<ScorpionPack2D>();
        var members = pack.Members.ToArray();
        MovePlayer(player, new Vector2(50, -4));
        player.SetHealthCapacity(999, 999);
        await Task.Delay(Mathf.CeilToInt((members[0].Presentation.IdleSqueakInterval.y + .3f) * 1000));
        bool idleSqueaks = members.Any(actor => actor.GetComponent<ScorpionAudio2D>().SqueaksPlayed > 0);
        bool waiting = members.All(actor => !actor.HasAggro);
        room.GetComponentInChildren<Door2D>().Toggle();
        await Task.Delay(150);
        bool oncePerPack = pack.AlarmCount == 1 && members.Sum(actor => actor.GetComponent<ScorpionAudio2D>().DetectionAccentsPlayed) == 1;
        int[] beforeSqueaks = members.Select(actor => actor.GetComponent<ScorpionAudio2D>().SqueaksPlayed).ToArray();
        var samples = new List<object>();
        float minimumSpacing = float.PositiveInfinity;
        float worstForwardDot = 1;
        float maximumContactPush = 0;
        Vector2 center = members.Aggregate(Vector2.zero, (sum, actor) => sum + (Vector2)actor.transform.position) / members.Length;
        Vector2[] goals = { center + Vector2.up * 2, center + Vector2.right * 2, center + Vector2.down * 2, center + Vector2.left * 2 };
        for (int segment = 0; segment < goals.Length; segment++)
        {
            MovePlayer(player, goals[segment]);
            context.Flashlight.AimAt(center);
            for (int tick = 0; tick < 45; tick++)
            {
                await FixedFrame();
                for (int i = 0; i < members.Length; i++)
                {
                    var actor = members[i];
                    if (actor.Velocity.sqrMagnitude > .25f)
                    {
                        worstForwardDot = Mathf.Min(worstForwardDot, Vector2.Dot(actor.Velocity.normalized, actor.Facing));
                    }
                    else if (Vector2.Dot(actor.Velocity, actor.Facing) < 0)
                    {
                        maximumContactPush = Mathf.Max(maximumContactPush, actor.Velocity.magnitude);
                    }
                    for (int j = i + 1; j < members.Length; j++)
                    {
                        minimumSpacing = Mathf.Min(minimumSpacing, Vector2.Distance(actor.transform.position, members[j].transform.position));
                    }
                }
                if (tick % 9 == 0)
                {
                    samples.Add(new { segment, tick, actors = members.Select(actor => new
                    {
                        position = new[] { actor.transform.position.x, actor.transform.position.y },
                        velocity = new[] { actor.Velocity.x, actor.Velocity.y },
                        facing = new[] { actor.Facing.x, actor.Facing.y },
                        attack = actor.IsAttacking
                    }).ToArray() });
                }
                if (tick == 27)
                {
                    Capture(center, 4.1f, "turn-" + segment + ".png");
                }
            }
        }
        await Task.Delay(Mathf.CeilToInt((members[0].Presentation.RunSqueakInterval.y + .2f) * 1000));
        bool runSqueaks = members.Where((actor, i) => actor.GetComponent<ScorpionAudio2D>().SqueaksPlayed > beforeSqueaks[i]).Any();
        var actorToKill = members[0];
        foreach (var other in members.Skip(1))
        {
            other.enabled = false;
            other.GetComponent<Rigidbody2D>().simulated = false;
            other.GetComponent<Collider2D>().enabled = false;
        }
        actorToKill.enabled = false;
        actorToKill.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        MovePlayer(player, (Vector2)actorToKill.transform.position + Vector2.down * 2);
        context.Flashlight.Acquire();
        context.Flashlight.SetPower(true);
        context.Flashlight.AimAt(actorToKill.transform.position);
        await FixedFrame();
        await FixedFrame();
        Capture(actorToKill.transform.position, 2.4f, "living.png");
        // A selected attack phase tests that saved stable indices do not silently restore Run00.
        var body = actorToKill.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
        actorToKill.GetComponent<ScorpionSpriteAnimator2D>().enabled = false;
        body.sprite = actorToKill.Presentation.Attack.FrameAt(actorToKill.Presentation.Attack.Duration * .55f);
        body.transform.parent.rotation = Quaternion.Euler(0, 0, 47);
        var expectedPose = AssetDatabase.LoadAssetAtPath<ScorpionRuptureSet>("Assets/EmergencyShift/Data/Enemies/Scorpion/RupturePosesR10.asset").Find(body.sprite);
        var feedback = actorToKill.GetComponent<ScorpionAudio2D>();
        var gore = actorToKill.GetComponent<GoreBurst2D>();
        var launched = new List<GibMotion2D>();
        gore.FragmentLaunched += launched.Add;
        // Fire the actual player weapon: its normal projectile sweep must deliver fatal damage.
        var weapon = player.GetComponent<PlayerWeapon2D>();
        weapon.Acquire(100);
        weapon.AddAmmo(100);
        int shots = 0;
        double deadline = Time.realtimeSinceStartupAsDouble + 8;
        while (actorToKill.IsAlive && Time.realtimeSinceStartupAsDouble < deadline)
        {
            context.Flashlight.AimAt(actorToKill.transform.position);
            if (weapon.TryFire())
            {
                shots++;
            }
            await Task.Delay(40);
        }
        await Until(() => launched.Count == 12);
        bool actualWeaponKill = !actorToKill.IsAlive && shots > 0;
        bool piecesMatch = launched.Select(part => part.GetComponentInChildren<SpriteRenderer>().sprite)
            .SequenceEqual(Enumerable.Range(0, expectedPose.Count).Select(expectedPose.Part));
        bool wholeBodyHidden = !body.enabled && launched.All(part => part.GetComponentInChildren<SpriteRenderer>().sprite != body.sprite);
        Capture(actorToKill.transform.position, 3.1f, "death-flight.png");
        await Until(() => gore.IsSettled);
        var record = launched[0].transform.parent.GetComponent<RemainsRecord2D>();
        var snapshot = record.Capture();
        var spritesBefore = record.GetComponentsInChildren<RemainsPartIdentity2D>().Select(part => part.GetComponentInChildren<SpriteRenderer>().sprite).ToArray();
        var prefabGore = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/Scorpion.prefab").GetComponent<GoreBurst2D>();
        var prefabData = new SerializedObject(prefabGore);
        var bankData = prefabData.FindProperty("fragments");
        var stableBank = Enumerable.Range(0, bankData.arraySize).Select(index => (Sprite)bankData.GetArrayElementAtIndex(index).objectReferenceValue).ToArray();
        var freshObject = new GameObject("R10 fresh checkpoint remains receiver");
        SceneManager.MoveGameObjectToScene(freshObject, actorToKill.gameObject.scene);
        var freshGore = freshObject.AddComponent<GoreBurst2D>();
        freshGore.Configure((BloodPool2D)prefabData.FindProperty("bloodPoolPrefab").objectReferenceValue, stableBank,
            (Material)prefabData.FindProperty("litMaterial").objectReferenceValue);
        var freshData = new SerializedObject(freshGore);
        freshData.FindProperty("profile").objectReferenceValue = prefabData.FindProperty("profile").objectReferenceValue;
        freshData.ApplyModifiedPropertiesWithoutUndo();
        record.gameObject.SetActive(false);
        freshGore.RestoreRemains(snapshot);
        var restored = Nodes().Select(node => node.GetComponent<RemainsRecord2D>()).Where(value => value != null && value.gameObject.activeInHierarchy).Last();
        var restoredRenderers = restored.GetComponentsInChildren<RemainsPartIdentity2D>().Select(part => part.GetComponentInChildren<SpriteRenderer>()).ToArray();
        bool stableRestoredSprites = restoredRenderers.Select(renderer => renderer.sprite).SequenceEqual(spritesBefore);
        bool stableRestoredOrder = restoredRenderers.Select(renderer => renderer.sortingOrder).SequenceEqual(Enumerable.Range(1, 12));
        bool groupBelowLiving = restored.GetComponent<SortingGroup>().sortingOrder == RemainsVisualStyle.DeadOrder;
        bool yellowFluid = restored.GetComponentInChildren<BloodPool2D>().GetComponentsInChildren<SpriteRenderer>(true)
            .All(renderer => renderer.sharedMaterial.name == "ToxicBlood");
        bool deathOnce = feedback.DeathsPlayed == 1;
        actorToKill.ReceiveBulletHit(100, actorToKill.transform.position, Vector2.up);
        deathOnce &= feedback.DeathsPlayed == 1;
        Capture(actorToKill.transform.position, 3.1f, "death-settled.png");
        var report = new
        {
            waiting, idleSqueaks, oncePerPack, runSqueaks, minimumSpacing, worstForwardDot, maximumContactPush,
            actualWeaponKill, shots, parts = launched.Count, piecesMatch, wholeBodyHidden,
            stableRestoredSprites, stableRestoredOrder, groupBelowLiving, yellowFluid, deathOnce,
            variant = snapshot.variantId,
            sceneSaved = false,
            scope = "Four authored actors, actual weapon projectiles. Functional check; no stress benchmark or listening assessment."
        };
        File.WriteAllText(Folder + "play.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        File.WriteAllText(Folder + "movement.json", JsonConvert.SerializeObject(samples, Formatting.Indented));
        return report;
    }

    public static async Task<object> SimultaneousBurst()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires a fresh Play of the authored room.");
        }
        var nodes = Nodes();
        var context = nodes.Select(node => node.GetComponent<LevelContext2D>()).First(value => value != null);
        var members = nodes.First(node => node.name == "R04_ScorpionPackTest").GetComponentInChildren<ScorpionPack2D>().Members.ToArray();
        foreach (var actor in members)
        {
            actor.enabled = false;
            actor.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            actor.GetComponent<ScorpionSpriteAnimator2D>().enabled = false;
        }
        MovePlayer(context.Player, new Vector2(50, -17));
        context.Player.SetHealthCapacity(999, 999);
        context.Flashlight.Acquire();
        context.Flashlight.SetPower(true);
        context.Flashlight.AimAt(new Vector2(50, -12));
        var results = new List<object>();
        var launched = new List<GibMotion2D>[3];
        var frames = new List<int>[3];
        var expected = new Sprite[3][];
        var profiles = new GoreProfile[3];
        int[] selectedFrames = { 7, 13, 31 };
        var rupture = AssetDatabase.LoadAssetAtPath<ScorpionRuptureSet>("Assets/EmergencyShift/Data/Enemies/Scorpion/RupturePosesR10.asset");
        for (int i = 0; i < 3; i++)
        {
            var actor = members[i];
            var body = actor.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
            var motion = i == 0 ? actor.Presentation.Run : actor.Presentation.Attack;
            var motionData = new SerializedObject(motion);
            body.sprite = (Sprite)motionData.FindProperty("frames").GetArrayElementAtIndex(selectedFrames[i]).objectReferenceValue;
            body.transform.parent.rotation = Quaternion.Euler(0, 0, 23 + i * 47);
            var pose = rupture.Find(body.sprite);
            expected[i] = Enumerable.Range(0, pose.Count).Select(pose.Part).ToArray();
            var goreData = new SerializedObject(actor.GetComponent<GoreBurst2D>());
            profiles[i] = UnityEngine.Object.Instantiate((GoreProfile)goreData.FindProperty("profile").objectReferenceValue);
            var profileData = new SerializedObject(profiles[i]);
            var variants = profileData.FindProperty("variants");
            for (int variant = 0; variant < variants.arraySize; variant++)
            {
                variants.GetArrayElementAtIndex(variant).FindPropertyRelative("restorationOnly").boolValue = variant != 4 + i;
            }
            profileData.ApplyModifiedPropertiesWithoutUndo();
            goreData.FindProperty("profile").objectReferenceValue = profiles[i];
            goreData.ApplyModifiedPropertiesWithoutUndo();
            int index = i;
            launched[i] = new List<GibMotion2D>();
            frames[i] = new List<int>();
            actor.GetComponent<GoreBurst2D>().FragmentLaunched += part =>
            {
                launched[index].Add(part);
                frames[index].Add(Time.frameCount);
            };
            var presence = actor.transform.Find("Audio/Quiet presence").GetComponent<AudioSource>();
            presence.clip = actor.Presentation.Squeak(i % actor.Presentation.SqueakCount);
            presence.Play();
        }
        foreach (var actor in members.Take(3))
        {
            actor.ReceiveBulletHit(99, actor.transform.position, Vector2.up);
        }
        await Until(() => launched.All(parts => parts.Count == 12));
        Capture(new Vector2(50, -13), 5.2f, "three-ruptures-flight.png");
        await Until(() => members.Take(3).All(actor => actor.GetComponent<GoreBurst2D>().IsSettled));
        for (int i = 0; i < 3; i++)
        {
            var actor = members[i];
            var actual = launched[i].Select(part => part.GetComponentInChildren<SpriteRenderer>().sprite).ToArray();
            var snapshot = actor.GetComponent<GoreBurst2D>().CaptureRemains();
            results.Add(new
            {
                variant = snapshot.variantId,
                parts = launched[i].Count,
                exactPoseSprites = actual.SequenceEqual(expected[i]),
                atomicFrame = frames[i].Distinct().Count() == 1,
                firstFrame = frames[i][0],
                noWholeBody = !actor.transform.Find("Visual/Body").GetComponent<SpriteRenderer>().enabled,
                presenceStopped = !actor.transform.Find("Audio/Quiet presence").GetComponent<AudioSource>().isPlaying,
                deathOnce = actor.GetComponent<ScorpionAudio2D>().DeathsPlayed == 1
            });
        }
        Capture(new Vector2(50, -13), 5.2f, "three-ruptures-settled.png");
        File.WriteAllText(Folder + "simultaneous-burst.json", JsonConvert.SerializeObject(results, Formatting.Indented));
        return results;
    }

    public static async Task<object> FollowerRelease()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires fresh authored-room Play.");
        }
        var nodes = Nodes();
        var context = nodes.Select(node => node.GetComponent<LevelContext2D>()).First(value => value != null);
        var members = nodes.First(node => node.name == "R04_ScorpionPackTest").GetComponentInChildren<ScorpionPack2D>().Members.ToArray();
        foreach (var other in members.Skip(2))
        {
            other.gameObject.SetActive(false);
        }
        var follower = members[0];
        var leader = members[1];
        leader.enabled = false;
        leader.GetComponent<Rigidbody2D>().position = new Vector2(50, -12.64f);
        follower.GetComponent<Rigidbody2D>().position = new Vector2(50, -14);
        MovePlayer(context.Player, new Vector2(50, -9));
        follower.Alert();
        await Task.Delay(700);
        float stoppedSpeed = follower.Velocity.magnitude;
        float stoppedSpacing = Vector2.Distance(follower.transform.position, leader.transform.position);
        Vector2 before = follower.transform.position;
        leader.GetComponent<Rigidbody2D>().position = new Vector2(53, -12.64f);
        var speeds = new List<float>();
        var distances = new List<float>();
        for (int tick = 0; tick < 12; tick++)
        {
            await FixedFrame();
            speeds.Add(follower.Velocity.magnitude);
            distances.Add(Vector2.Distance(before, follower.transform.position));
        }
        var report = new
        {
            stoppedSpeed, stoppedSpacing,
            firstReleasedSpeed = speeds[0],
            speeds, distances,
            stoppedForLeader = stoppedSpeed < .05f,
            acceleratedFromRest = speeds[0] < .6f && speeds.Last() > 1,
            resumedForwardTravel = distances.Last() > .1f
        };
        File.WriteAllText(Folder + "follower-release.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }

    private static void MovePlayer(PlayerLife2D player, Vector2 point)
    {
        player.GetComponent<Rigidbody2D>().position = point;
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        player.transform.position = point;
    }

    private static async Task FixedFrame()
    {
        float before = Time.fixedTime;
        await Until(() => Time.fixedTime != before);
    }

    private static async Task Until(Func<bool> condition)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 8;
        while (!condition())
        {
            if (Time.realtimeSinceStartupAsDouble > deadline)
            {
                throw new TimeoutException("Scorpion R10 expected condition was not reached.");
            }
            await Task.Delay(5);
        }
    }

    private static void Capture(Vector2 center, float size, string name)
    {
        Camera camera = Nodes().Select(node => node.GetComponent<Camera>()).First(value => value != null);
        Vector3 oldPosition = camera.transform.position;
        float oldSize = camera.orthographicSize;
        RenderTexture oldTarget = camera.targetTexture;
        RenderTexture oldActive = RenderTexture.active;
        var target = new RenderTexture(1280, 900, 24);
        var pixels = new Texture2D(1280, 900, TextureFormat.RGBA32, false);
        try
        {
            camera.transform.position = new Vector3(center.x, center.y, oldPosition.z);
            camera.orthographicSize = size;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 1280, 900), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Folder + name, pixels.EncodeToPNG());
        }
        finally
        {
            camera.transform.position = oldPosition;
            camera.orthographicSize = oldSize;
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
}
