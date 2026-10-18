using System;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class LocalLampAudioTests
    {
        [Test]
        public void LampIsOnlyAudibleCloseToTheFixture()
        {
            var sounds = ScriptableObject.CreateInstance<LocalLampAudioSet>();
            try
            {
                Assert.That(sounds.DistanceGain(.1f), Is.EqualTo(1));
                Assert.That(sounds.DistanceGain(1), Is.InRange(0f, 1f));
                Assert.That(sounds.DistanceGain(1.8f), Is.LessThan(.1f));
                Assert.That(sounds.DistanceGain(2), Is.Zero);
                Assert.That(sounds.DistanceGain(30), Is.Zero);
                Assert.That(sounds.DistanceGain(1.5f), Is.LessThan(sounds.DistanceGain(.5f)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sounds);
            }
        }

        [Test]
        public void EvaluationDoesNotModifyTheSharedMix()
        {
            var sounds = ScriptableObject.CreateInstance<LocalLampAudioSet>();
            try
            {
                string before = JsonUtility.ToJson(sounds);
                sounds.DistanceGain(.7f);
                sounds.DistanceGain(20);
                Assert.That(JsonUtility.ToJson(sounds), Is.EqualTo(before));
                Assert.Throws<ArgumentOutOfRangeException>(() => sounds.DistanceGain(float.NaN));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sounds);
            }
        }
    }
}
