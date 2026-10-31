using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.Scorpion.View;
using EmergencyShift.Features.Scorpion.Config;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyScorpionFeedbackR14
{
    private const string Folder = "Docs/Validation/ScorpionFeedbackR14/";
    private static Transform[] Nodes() => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();

    public static Task<object> Baseline() => Shoot("before");
    public static Task<object> Verify() => Shoot("after");

    public static async Task<object> RapidDeath()
    {
        var nodes = Nodes();
        var context = nodes.Select(node => node.GetComponent<LevelContext2D>()).First(value => value != null);
        var actor = nodes.First(node => node.name == "R04_ScorpionPackTest").GetComponentInChildren<Scorpion2D>();
        actor.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        var visual = actor.transform.Find("Visual");
        var body = actor.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
        Vector3 restingScale = visual.localScale;
        Vector3 restingPosition = visual.localPosition;
        var eyes = actor.transform.Find("Visual/Eyes").GetComponent<SpriteRenderer>();
        Vector2 outside = new Vector2(50, -4);
        context.Player.GetComponent<Rigidbody2D>().position = outside;
        context.Player.transform.position = outside;
        await Task.Delay(100);
        bool hiddenBehindWall = !actor.HasTargetLineOfSight && !eyes.enabled;
        Vector2 inside = (Vector2)actor.transform.position + Vector2.down * 2.5f;
        context.Player.GetComponent<Rigidbody2D>().position = inside;
        context.Player.transform.position = inside;
        context.Flashlight.SetPower(false);
        await Task.Delay(100);
        bool visibleWithoutFlashlight = actor.HasTargetLineOfSight && eyes.enabled;
        actor.ReceiveBulletHit(1, actor.transform.position, Vector2.up);
        await Task.Delay(40);
        bool reactionActive = actor.GetComponent<EnemyHitResponse2D>().Reacting;
        bool geometryStable = visual.localScale == restingScale && visual.localPosition == restingPosition;
        var rupture = AssetDatabase.LoadAssetAtPath<ScorpionRuptureSet>("Assets/EmergencyShift/Data/Enemies/Scorpion/RupturePosesR10.asset");
        var pose = rupture.PoseAt(rupture.IndexOf(body.sprite));
        Matrix4x4 source = body.transform.localToWorldMatrix;
        Vector2 origin = actor.transform.position;
        int count = 0;
        float maximumError = 0;
        var gore = actor.GetComponent<GoreBurst2D>();
        gore.FragmentLaunched += part =>
        {
            Vector2 expected = origin + (Vector2)source.MultiplyVector(pose.Offset(count));
            maximumError = Mathf.Max(maximumError, Vector2.Distance(expected, part.transform.position));
            count++;
        };
        actor.ReceiveBulletHit(2, actor.transform.position, Vector2.up);
        await Until(() => count == 12);
        await Task.Delay(20);
        var report = new { task = "TASK-005", hiddenBehindWall, visibleWithoutFlashlight, reactionActive, geometryStable,
            rapidIntervalSeconds = .04f, pieces = count, maximumInitialCenterError = maximumError, eyesOffOnDeath = !eyes.enabled, sceneSaved = false };
        File.WriteAllText(Folder + "rapid-death.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        if (!hiddenBehindWall || !visibleWithoutFlashlight || !reactionActive || !geometryStable || maximumError > .0001f || eyes.enabled)
        {
            throw new InvalidOperationException("Rapid death or eye visibility regression.");
        }
        return report;
    }

    private static async Task<object> Shoot(string tag)
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires fresh Play of MechanicsSandbox.");
        }
        Directory.CreateDirectory(Folder);
        var nodes = Nodes();
        var context = nodes.Select(node => node.GetComponent<LevelContext2D>()).First(value => value != null);
        var members = nodes.First(node => node.name == "R04_ScorpionPackTest").GetComponentsInChildren<Scorpion2D>();
        foreach (var other in members.Skip(1))
        {
            other.gameObject.SetActive(false);
        }
        var actor = members[0];
        actor.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        Vector2 playerPosition = (Vector2)actor.transform.position + Vector2.down * 2.5f;
        context.Player.GetComponent<Rigidbody2D>().position = playerPosition;
        context.Player.transform.position = playerPosition;
        context.Player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        context.Player.SetHealthCapacity(999, 999);
        context.Flashlight.Acquire();
        context.Flashlight.SetPower(true);
        context.Flashlight.AimAt(actor.transform.position);
        await Task.Delay(100);
        var audio = actor.GetComponent<ScorpionAudio2D>();
        var voice = actor.transform.Find("Audio/Voice").GetComponent<AudioSource>();
        var eyes = actor.transform.Find("Visual/Eyes").GetComponent<SpriteRenderer>();
        var weapon = context.Player.GetComponent<PlayerWeapon2D>();
        weapon.Acquire(100);
        weapon.AddAmmo(100);
        var shots = new List<object>();
        for (int index = 0; index < 3; index++)
        {
            int health = actor.Health;
            context.Flashlight.AimAt(actor.transform.position);
            await Until(() => weapon.TryFire());
            await Until(() => actor.Health < health);
            await Task.Delay(80);
            var sprays = Nodes().Select(node => node.GetComponent<BloodHitSpray2D>()).Where(value => value != null).ToArray();
            var drops = sprays.SelectMany(spray => spray.GetComponentsInChildren<SpriteRenderer>()).ToArray();
            shots.Add(new
            {
                healthBefore = health,
                healthAfter = actor.Health,
                hitsPlayed = audio.HitsPlayed,
                deathsPlayed = audio.DeathsPlayed,
                clip = voice.clip != null ? voice.clip.name : null,
                voice.isPlaying,
                voice.timeSamples,
                voice.volume,
                voice.isVirtual,
                voice.mute,
                listenerPaused = AudioListener.pause,
                listenerVolume = AudioListener.volume,
                splashCount = sprays.Length,
                dropCount = drops.Length,
                dropColor = drops.FirstOrDefault() != null ? drops[0].color.ToString() : null,
                dropMaterial = drops.FirstOrDefault() != null ? drops[0].sharedMaterial.name : null,
                eyesEnabled = eyes.enabled,
                eyeColor = eyes.color.ToString(),
                lineOfSight = actor.HasTargetLineOfSight
            });
            Capture(actor.transform.position, "shot-" + tag + "-" + index + ".png");
            await Task.Delay(350);
        }
        var report = new { task = "TASK-005", tag, shots, sceneSaved = false };
        File.WriteAllText(Folder + tag + ".json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }

    private static async Task Until(Func<bool> condition)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 8;
        while (!condition())
        {
            if (Time.realtimeSinceStartupAsDouble > deadline)
            {
                throw new TimeoutException("Expected weapon hit did not arrive.");
            }
            await Task.Delay(5);
        }
    }

    private static void Capture(Vector2 center, string name)
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
            camera.orthographicSize = 2.1f;
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
