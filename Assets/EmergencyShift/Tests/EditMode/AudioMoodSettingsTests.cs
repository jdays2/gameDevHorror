using System;
using EmergencyShift.Features.AudioMood.Config;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class AudioMoodSettingsTests
    {
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(0)]
        public void InvalidObservationIntervalIsRejected(float interval)
        {
            Assert.Throws<ArgumentException>(() => new AudioMoodSettings(interval, 1, 9, 2, 2, 5, 3, 2, .2f, .6f, .4f));
        }

        [Test]
        public void CloseDistanceCannotExceedAwareness()
        {
            Assert.Throws<ArgumentException>(() => new AudioMoodSettings(.12f, 1, 2, 3, 2, 5, 3, 2, .2f, .6f, .4f));
        }

        [Test]
        public void CrowdRequiresAtLeastOneEnemy()
        {
            Assert.Throws<ArgumentException>(() => new AudioMoodSettings(.12f, 1, 9, 2, 0, 5, 3, 2, .2f, .6f, .4f));
        }

        [Test]
        public void SnapshotKeepsPreviousSerializedValuesAndIsIndependentOfLaterAuthoring()
        {
            AudioMoodProfile profile = ScriptableObject.CreateInstance<AudioMoodProfile>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"observationInterval\":0.2,\"perceptionMemorySeconds\":0,\"awarenessDistance\":12,\"closeCombatDistance\":4,\"crowdEnemiesForCombat\":3,\"threatHoldSeconds\":6,\"combatHoldSeconds\":4,\"combatAfterShotSeconds\":3,\"shotDuckSeconds\":0.3,\"damageDuckSeconds\":0.8,\"cueDuckSeconds\":0.5}", profile);
                string original = JsonUtility.ToJson(profile);
                AudioMoodSettings settings = profile.ToMoodSettings();
                Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(original));
                Assert.That(settings.ObservationInterval, Is.EqualTo(.2f));
                Assert.That(settings.MemorySeconds, Is.Zero);
                Assert.That(settings.AwarenessDistance, Is.EqualTo(12));
                Assert.That(settings.CloseDistance, Is.EqualTo(4));
                Assert.That(settings.CrowdCount, Is.EqualTo(3));
                Assert.That(settings.ThreatHold, Is.EqualTo(6));
                Assert.That(settings.CombatHold, Is.EqualTo(4));
                Assert.That(settings.CombatAfterShot, Is.EqualTo(3));
                Assert.That(settings.ShotDuck, Is.EqualTo(.3f));
                Assert.That(settings.DamageDuck, Is.EqualTo(.8f));
                Assert.That(settings.CueDuck, Is.EqualTo(.5f));
                JsonUtility.FromJsonOverwrite("{\"threatHoldSeconds\":9}", profile);
                Assert.That(settings.ThreatHold, Is.EqualTo(6));
                Assert.That(profile.ToMoodSettings().ThreatHold, Is.EqualTo(9));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }
    }
}
