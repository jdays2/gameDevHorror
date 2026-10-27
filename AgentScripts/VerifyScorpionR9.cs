using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.Scorpion.View;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Existing four-member room only. Actual sources, death event order and settled remains.</summary>
public static class VerifyScorpionR9
{
    private const string Folder = "Docs/Validation/ScorpionR9/";
    private static Transform[] Nodes() => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();

    public static async Task<object> Play()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires native Play; all changes discarded on Stop.");
        }
        Directory.CreateDirectory(Folder);
        var nodes = Nodes();
        var context = nodes.Select(node => node.GetComponent<LevelContext2D>()).First(value => value != null);
        var player = context.Player;
        var room = nodes.First(node => node.name == "R04_ScorpionPackTest");
        var pack = room.GetComponentInChildren<ScorpionPack2D>();
        var director = nodes.Select(node => node.GetComponent<LevelAudioDirector2D>()).First(value => value != null);
        player.SetHealthCapacity(100, 100);
        MovePlayer(player, new Vector2(50, -4));
        await Task.Delay(250);
        bool closedDoorWaiting = pack.Members.All(actor => !actor.HasAggro);
        var door = room.GetComponentInChildren<Door2D>();
        door.Toggle();
        await Task.Delay(180);
        bool onePackAccent = pack.AlarmCount == 1 && pack.Members.Sum(actor => actor.GetComponent<ScorpionAudio2D>().DetectionAccentsPlayed) == 1;
        bool packEngaged = pack.Members.All(actor => actor.HasAggro);
        Scorpion2D caller = pack.Members.First(actor => actor.GetComponent<ScorpionAudio2D>().DetectionAccentsPlayed == 1);
        var accent = caller.transform.Find("Audio/Detection squeal").GetComponent<AudioSource>();
        bool nativeAccentPlaying = accent.isPlaying && accent.clip == caller.Presentation.Alarm && accent.timeSamples > 0;
        MovePlayer(player, (Vector2)caller.transform.position + Vector2.up * 1.5f);
        await Task.Delay(160);
        bool heardRegistered = pack.Members.Any(actor => actor.GetComponent<ScorpionAudioThreat2D>().HeardUntil > Time.time);
        bool sharedMood = director.EngagedEnemies > 0 && director.Mood.ToString() != "Explore";
        pack.Alert(caller);
        bool repeatedAlertSilent = pack.AlarmCount == 1 && pack.Members.Sum(actor => actor.GetComponent<ScorpionAudio2D>().DetectionAccentsPlayed) == 1;
        foreach (Scorpion2D other in pack.Members.Where(actor => actor != caller))
        {
            other.enabled = false;
            other.GetComponent<Rigidbody2D>().simulated = false;
            other.GetComponent<CircleCollider2D>().enabled = false;
        }
        MovePlayer(player, (Vector2)caller.transform.position + caller.Facing * .85f);
        await Until(() => caller.IsAttacking && caller.AttackProgress > .34f);
        var body = caller.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
        var eyes = caller.transform.Find("Visual/Eyes").GetComponent<SpriteRenderer>();
        var material = eyes.sharedMaterial;
        Color dim = material.GetColor("_Color");
        Capture(caller.transform.position, 2.2f, "eyes-dim.png");
        // Only a temporary per-renderer override for comparison; shared material remains immutable.
        var block = new MaterialPropertyBlock();
        eyes.GetPropertyBlock(block);
        block.SetColor("_Color", new Color(1.8f, .025f, .012f, 1));
        eyes.SetPropertyBlock(block);
        Capture(caller.transform.position, 2.2f, "eyes-before.png");
        eyes.SetPropertyBlock(null);
        var sourceSprite = body.sprite;
        Quaternion rotation = body.transform.rotation;
        var feedback = caller.GetComponent<ScorpionAudio2D>();
        var voice = caller.transform.Find("Audio/Voice").GetComponent<AudioSource>();
        var gore = caller.GetComponent<GoreBurst2D>();
        var launched = new List<GibMotion2D>();
        gore.FragmentLaunched += launched.Add;
        int hits = feedback.HitsPlayed;
        caller.ReceiveBulletHit(99, caller.transform.position, Vector2.up);
        await Task.Delay(100);
        bool deathSourcePlaying = feedback.DeathsPlayed == 1 && voice.isPlaying && voice.clip != caller.Presentation.Hit;
        bool fatalHitDidNotReplaceDeath = feedback.HitsPlayed == hits;
        bool livingChannelsStopped = new[]{"Claw grasp", "Foot contacts", "Quiet presence"}.All(name => !caller.transform.Find("Audio/" + name).GetComponent<AudioSource>().isPlaying);
        bool flyingFlesh = launched.Skip(1).Any(part => part.Height > .01f && part.Velocity.magnitude > .1f);
        Capture(caller.transform.position, 2.2f, "death-flight.png");
        int pauseSample = voice.timeSamples;
        int pausedSample;
        float oldScale = Time.timeScale;
        bool oldPause = AudioListener.pause;
        try
        {
            Time.timeScale = 0;
            AudioListener.pause = true;
            await Task.Delay(150);
            pausedSample = voice.timeSamples;
        }
        finally
        {
            Time.timeScale = oldScale;
            AudioListener.pause = oldPause;
        }
        await Task.Delay(90);
        bool deathPauseRetained = pauseSample > 0 && Math.Abs(pausedSample - pauseSample) <= 1024 && voice.timeSamples > pausedSample;
        caller.ReceiveBulletHit(99, caller.transform.position, Vector2.up);
        bool onceOnly = feedback.DeathsPlayed == 1;
        await Until(() => gore.IsSettled);
        await Task.Delay(1000);
        bool deathTailEnded = !voice.isPlaying;
        bool deathFeedbackReleased = !feedback.enabled;
        var corpse = launched[0].GetComponentInChildren<SpriteRenderer>();
        bool samePose = corpse.sprite == sourceSprite && Quaternion.Angle(corpse.transform.rotation, rotation) < .001f;
        bool sameDeadTint = corpse.color == body.color;
        bool eyesOff = !eyes.enabled;
        bool darkerBelowLiving = launched.All(part => part.GetComponentInChildren<SpriteRenderer>().sortingOrder == RemainsVisualStyle.DeadOrder && part.GetComponentInChildren<SpriteRenderer>().color.r < 1);
        var record = launched[0].transform.parent.GetComponent<RemainsRecord2D>();
        var snapshot = record.Capture();
        bool onePool = record.GetComponentsInChildren<BloodPool2D>(true).Length == 1;
        bool indexedPieces = snapshot.parts.Length == launched.Count && snapshot.parts.Any(part => part.spriteIndex > 0);
        Capture(caller.transform.position, 2.2f, "death-settled.png");
        string variant = snapshot.variantId;
        int pieces = launched.Count;
        // Existing remains are independent of their dead actor's lifetime.
        caller.gameObject.SetActive(false);
        bool survivesActor = record != null && record.gameObject.activeInHierarchy && record.Capture().parts.Length == pieces;
        bool disableStopsSources = caller.GetComponentsInChildren<AudioSource>(true).All(source => !source.isPlaying);
        var report = new {
            closedDoorWaiting, onePackAccent, packEngaged, nativeAccentPlaying, heardRegistered, sharedMood,
            repeatedAlertSilent, deathSourcePlaying, fatalHitDidNotReplaceDeath, livingChannelsStopped,
            flyingFlesh, deathPauseRetained, onceOnly, deathTailEnded, deathFeedbackReleased, samePose, sameDeadTint, eyesOff, darkerBelowLiving,
            onePool, indexedPieces, survivesActor, disableStopsSources, variant, pieces,
            eyeEmission=dim.ToString(), registry=director.RegisteredThreatCount, mood=director.Mood.ToString(),
            pauseSample, pausedSample,
            scope="Authored room functional verification. Actual playback/playheads; no listening judgement, stress benchmark, scene save or shared runtime config mutation."
        };
        File.WriteAllText(Folder + "play.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }

    private static void MovePlayer(PlayerLife2D player, Vector2 point)
    {
        player.GetComponent<Rigidbody2D>().position = point;
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        player.transform.position = point;
    }

    private static async Task Until(Func<bool> condition)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 7;
        while (!condition())
        {
            if (Time.realtimeSinceStartupAsDouble > deadline)
            {
                throw new TimeoutException("Scorpion r9 expected state was not reached.");
            }
            await Task.Delay(8);
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
