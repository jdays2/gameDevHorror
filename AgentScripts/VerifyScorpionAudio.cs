using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmergencyShift;
using EmergencyShift.Features.Scorpion.View;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>One existing actor: actual attack cue, simultaneous hit, listener pause and disable.</summary>
public static class VerifyScorpionAudio
{
    public static async Task<object> Play()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires Play; changes are discarded at Stop.");
        }
        var nodes = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var context = nodes.Select(node => node.GetComponent<LevelContext2D>()).First(c => c != null);
        var pack = nodes.First(node => node.name == "R04_ScorpionPackTest").GetComponentInChildren<ScorpionPack2D>();
        Scorpion2D actor = pack.Members[0];
        foreach (Scorpion2D other in pack.Members.Skip(1))
        {
            other.enabled = false;
            other.GetComponent<Rigidbody2D>().simulated = false;
            other.GetComponent<CircleCollider2D>().enabled = false;
        }
        var player = context.Player;
        player.SetHealthCapacity(100, 100);
        Vector2 position = (Vector2)actor.transform.position + actor.Facing * .8f;
        player.GetComponent<Rigidbody2D>().position = position;
        player.transform.position = position;
        pack.Alert(actor);
        var feedback = actor.GetComponent<ScorpionAudio2D>();
        var claws = actor.transform.Find("Audio/Claw grasp").GetComponent<AudioSource>();
        var voice = actor.transform.Find("Audio/Voice").GetComponent<AudioSource>();
        await Until(() => claws.isPlaying && claws.timeSamples >= 2000);
        float phaseAtProbe = actor.AttackProgress;
        int attacks = feedback.AttacksPlayed;
        actor.ReceiveBulletHit(1, actor.transform.position, Vector2.up);
        bool simultaneousHit = voice.clip == actor.Presentation.Hit && voice.isPlaying && claws.isPlaying;
        int pausedSample = claws.timeSamples;
        float scale = Time.timeScale;
        bool listenerPaused = AudioListener.pause;
        int duringPause;
        int afterResume;
        try
        {
            AudioListener.pause = true;
            Time.timeScale = 0;
            await Task.Delay(180);
            duringPause = claws.timeSamples;
        }
        finally
        {
            Time.timeScale = scale;
            AudioListener.pause = listenerPaused;
        }
        await Task.Delay(35);
        afterResume = claws.timeSamples;
        bool pauseRetained = pausedSample > 0 && Math.Abs(duringPause - pausedSample) <= 1024 && afterResume > duringPause;
        bool oneCue = feedback.AttacksPlayed == attacks;
        feedback.enabled = false;
        bool disableStops = !claws.isPlaying && !voice.isPlaying;
        feedback.enabled = true;
        actor.enabled = false;
        await Task.Delay(80);
        actor.enabled = true;
        await Until(() => feedback.AttacksPlayed > attacks && claws.isPlaying);
        bool reactivatedCue = feedback.AttacksPlayed > attacks;
        actor.ReceiveBulletHit(99, actor.transform.position, Vector2.up);
        await Task.Delay(40);
        bool deathStops = actor.GetComponentsInChildren<AudioSource>(true).All(source => !source.isPlaying);
        var report = new
        {
            simultaneousHit, pauseRetained, oneCue, disableStops, reactivatedCue, deathStops,
            phaseAtProbe, pausedSample, duringPause, afterResume,
            scope = "One authored actor, actual native AudioSource state/playhead. No auditory judgement, stress test or scene save."
        };
        Directory.CreateDirectory("Docs/Validation/ScorpionAudio02");
        File.WriteAllText("Docs/Validation/ScorpionAudio02/play.json", JsonConvert.SerializeObject(report, Formatting.Indented));
        return report;
    }

    private static async Task Until(Func<bool> condition)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 6;
        while (!condition())
        {
            if (Time.realtimeSinceStartupAsDouble > deadline)
            {
                throw new TimeoutException("Expected Scorpion audio state was not reached.");
            }
            await Task.Delay(8);
        }
    }
}
