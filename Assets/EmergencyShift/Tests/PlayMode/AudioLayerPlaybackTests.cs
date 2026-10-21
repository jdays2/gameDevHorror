using System.Collections;
using EmergencyShift.Features.AudioMood.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class AudioLayerPlaybackTests
    {
        [UnityTest]
        public IEnumerator PauseBeforeScheduledStartStopsAndAllowsFreshScheduling()
        {
            var owner = new GameObject("Layer playback lifecycle");
            AudioMoodProfile profile = ScriptableObject.CreateInstance<AudioMoodProfile>();
            var exploration = owner.AddComponent<AudioSource>();
            var threat = owner.AddComponent<AudioSource>();
            var combat = owner.AddComponent<AudioSource>();
            var playback = new AudioLayerPlayback(exploration, threat, combat);
            try
            {
                JsonUtility.FromJsonOverwrite("{\"scheduleLeadSeconds\":10}", profile);
                playback.Start(profile);
                Assert.That(playback.Started, Is.True);
                Assert.That(playback.UpdatePause(true), Is.True);
                Assert.That(playback.Started, Is.False);
                Assert.That(playback.UpdatePause(false), Is.False);
                playback.Start(profile);
                playback.ApplyGains(new Vector3(.1f, .2f, .3f));
                Assert.That(exploration.volume, Is.EqualTo(.1f));
                Assert.That(threat.volume, Is.EqualTo(.2f));
                Assert.That(combat.volume, Is.EqualTo(.3f));
                Assert.That(exploration.priority, Is.EqualTo(profile.LayerPriority));
                playback.Dispose();
                playback.Dispose();
                Assert.That(playback.Started, Is.False);
                Assert.That(exploration.isPlaying || threat.isPlaying || combat.isPlaying, Is.False);
            }
            finally
            {
                playback.Dispose();
                Object.Destroy(owner);
                Object.Destroy(profile);
            }

            yield return null;
        }
    }
}
