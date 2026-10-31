using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.Scorpion.View;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyScorpionSqueakR11
{
    public static async Task<object> Play()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires fresh authored-room Play.");
        }
        var nodes = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var context = nodes.Select(node => node.GetComponent<LevelContext2D>()).First(value => value != null);
        var room = nodes.First(node => node.name == "R04_ScorpionPackTest");
        var pack = room.GetComponentInChildren<ScorpionPack2D>();
        var members = pack.Members.ToArray();
        var player = context.Player;
        var playerBody = player.GetComponent<Rigidbody2D>();
        playerBody.position = new Vector2(50, -4);
        playerBody.linearVelocity = Vector2.zero;
        player.SetHealthCapacity(999, 999);
        var clip = members[0].Presentation.Alarm;
        bool sameClipConfigured = members.All(actor => actor.Presentation.SqueakCount == 1 && actor.Presentation.Squeak(0) == clip);
        var idleObserved = new HashSet<int>();
        var runObserved = new HashSet<int>();
        var errors = new List<string>();
        await Observe(members, clip, members[0].Presentation.IdleSqueakInterval.y + .3f, idleObserved, errors);
        bool idleWaiting = members.All(actor => !actor.HasAggro);
        room.GetComponentInChildren<Door2D>().Toggle();
        await Observe(members, clip, members[0].Presentation.RunSqueakInterval.y + members[0].Presentation.Attack.Duration + .4f, runObserved, errors);
        bool oncePerPack = pack.AlarmCount == 1 && members.Sum(actor => actor.GetComponent<ScorpionAudio2D>().DetectionAccentsPlayed) == 1;
        var report = new
        {
            task = "TASK-005",
            date = "2026-10-31",
            sameClipConfigured,
            clip = AssetDatabase.GetAssetPath(clip),
            idleWaiting,
            idleActualSourcesObserved = idleObserved.Count,
            runActualSourcesObserved = runObserved.Count,
            oncePerPack,
            allObservedClipsAndPitchCorrect = errors.Count == 0,
            errors,
            normalVolume = members[0].Presentation.SqueakVolume,
            discoveryVolume = members[0].Presentation.DetectionVolume,
            channelsPerActor = members.Select(actor => actor.GetComponentsInChildren<AudioSource>(true).Length).ToArray(),
            scope = "Actual AudioSource.isPlaying/clip/pitch in authored four-member room. No direct listening or artistic approval inferred. No scene saves."
        };
        File.WriteAllText("Docs/Validation/ScorpionPaletteR11/squeak-play.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        if (!sameClipConfigured || !idleWaiting || idleObserved.Count == 0 || runObserved.Count == 0 || !oncePerPack || errors.Count > 0)
        {
            throw new InvalidOperationException("Scorpion ordinary squeak verification failed; see saved report.");
        }
        return report;
    }

    private static async Task Observe(Scorpion2D[] actors, AudioClip expected, float seconds, HashSet<int> observed, List<string> errors)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + seconds;
        while (Time.realtimeSinceStartupAsDouble < deadline)
        {
            for (int index = 0; index < actors.Length; index++)
            {
                var source = actors[index].transform.Find("Audio/Quiet presence").GetComponent<AudioSource>();
                if (!source.isPlaying)
                {
                    continue;
                }
                observed.Add(index);
                if (source.clip != expected && !errors.Contains("Unexpected ordinary clip"))
                {
                    errors.Add("Unexpected ordinary clip");
                }
                if (Mathf.Abs(source.pitch - 1) > .0001f && !errors.Contains("Ordinary pitch changed"))
                {
                    errors.Add("Ordinary pitch changed");
                }
            }
            await Task.Delay(20);
        }
    }
}
