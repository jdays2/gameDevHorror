using System;
using EmergencyShift.Features.AudioMood.Config;
using EmergencyShift.Features.AudioMood.Logic;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class AudioLayerMixTests
    {
        private AudioLayerMixModel model;
        private AudioLayerMixController controller;
        private View view;

        [SetUp]
        public void Setup()
        {
            model = new AudioLayerMixModel(Settings());
            view = new View();
            controller = new AudioLayerMixController(model, view);
        }

        [TestCase(AudioMood.Explore, .42f, 0, 0)]
        [TestCase(AudioMood.Threat, .42f, .595f, 0)]
        [TestCase(AudioMood.Combat, .42f, .595f, .63f)]
        public void MoodSelectsOnlyItsLayers(AudioMood mood, float x, float y, float z)
        {
            Step(20, mood);
            Assert.That(view.Gains.x, Is.EqualTo(x).Within(.00001));
            Assert.That(view.Gains.y, Is.EqualTo(y).Within(.00001));
            Assert.That(view.Gains.z, Is.EqualTo(z).Within(.00001));
        }

        [Test]
        public void OneRiseTimeConstantReachesExpectedExponentialFraction()
        {
            Step(.45f);
            Assert.That(view.Gains.x, Is.EqualTo(.42f * (1 - Math.Exp(-1))).Within(.00001));
        }

        [Test]
        public void LeavingCombatUsesFallTimeForOverlayLayers()
        {
            Step(20, AudioMood.Combat);
            float before = view.Gains.y;
            Step(3.5f);
            Assert.That(view.Gains.y, Is.EqualTo(before * Math.Exp(-1)).Within(.00001));
            Assert.That(view.Gains.x, Is.EqualTo(.42f).Within(.00001));
        }

        [Test]
        public void DuckUsesPromptResponseAndRecoveryUsesNormalRise()
        {
            Step(20);
            float initial = view.Gains.x;
            Step(.06f, ducking: true);
            float target = .42f * .75f;
            Assert.That(view.Gains.x, Is.EqualTo(target + (initial - target) * Math.Exp(-1)).Within(.00001));
            float ducked = view.Gains.x;
            Step(.45f);
            Assert.That(view.Gains.x, Is.EqualTo(.42f + (ducked - .42f) * Math.Exp(-1)).Within(.00001));
        }

        [Test]
        public void WoundedVolumeAndDuckingMultiplyWithoutChangingSettings()
        {
            Step(20, AudioMood.Combat, wounded: true, ducking: true, volume: .5f);
            Assert.That(view.Gains.x, Is.EqualTo(.42f * .9f * .75f * .5f).Within(.00001));
            Assert.That(view.Gains.z, Is.EqualTo(.63f * .9f * .75f * .5f).Within(.00001));
        }

        [Test]
        public void DeathUsesItsOwnFadeAndDoesNotApplyUrgentDuckResponse()
        {
            Step(20, AudioMood.Combat);
            Vector3 before = view.Gains;
            Step(.55f, AudioMood.Combat, alive: false, wounded: true, ducking: true);
            Assert.That(view.Gains.x, Is.EqualTo(before.x * Math.Exp(-1)).Within(.00001));
            Assert.That(view.Gains.z, Is.EqualTo(before.z * Math.Exp(-1)).Within(.00001));
        }

        [Test]
        public void ZeroScaledDeltaFreezesSmoothing()
        {
            Step(20, AudioMood.Combat);
            Vector3 before = view.Gains;
            Step(0, alive: false);
            Assert.That(view.Gains, Is.EqualTo(before));
        }

        [TestCase(-1, 0)]
        [TestCase(2, .42f)]
        public void FiniteExternalVolumeKeepsLegacyClamping(float volume, float expected)
        {
            Step(20, volume: volume);
            Assert.That(view.Gains.x, Is.EqualTo(expected).Within(.00001));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-1)]
        public void InvalidDeltaCannotMutateMixOrPresentANewValue(float delta)
        {
            Step(1);
            Vector3 before = model.Gains;
            Assert.Throws<ArgumentException>(() => Step(delta));
            Assert.That(model.Gains, Is.EqualTo(before));
            Assert.That(view.Gains, Is.EqualTo(before));
        }

        [Test]
        public void InvalidMoodOrNonFiniteVolumeIsRejected()
        {
            Assert.Throws<ArgumentException>(() => Step(.1f, (AudioMood)99));
            Assert.Throws<ArgumentException>(() => Step(.1f, volume: float.NaN));
            Assert.That(model.Gains, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void ResetIsQuietAndDoesNotChangeAnotherMix()
        {
            var other = new AudioLayerMixModel(Settings());
            other.Step(20, AudioMood.Combat, true, false, false, 1);
            Step(20, AudioMood.Combat);
            Assert.That(controller.Reset().Success, Is.True);
            Assert.That(view.Gains, Is.EqualTo(Vector3.zero));
            Assert.That(other.Gains.z, Is.GreaterThan(.6f));
            Assert.That(new ResetAudioLayerMixCommand(null).Execute().Success, Is.False);
        }

        [Test]
        public void InvalidSettingsAreRejectedAtComposition()
        {
            Assert.Throws<ArgumentException>(() => new AudioLayerMixSettings(float.NaN, Vector3.one, .45f, 3.5f, .55f, .75f, .06f, .9f));
            Assert.Throws<ArgumentException>(() => new AudioLayerMixSettings(.7f, new Vector3(1, float.NaN, 1), .45f, 3.5f, .55f, .75f, .06f, .9f));
            Assert.Throws<ArgumentException>(() => new AudioLayerMixSettings(.7f, Vector3.one, 0, 3.5f, .55f, .75f, .06f, .9f));
        }

        [Test]
        public void ProfileSnapshotPreservesFieldsAndDoesNotWriteBack()
        {
            AudioMoodProfile profile = ScriptableObject.CreateInstance<AudioMoodProfile>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"masterVolume\":0.8,\"explorationGain\":0.2,\"threatGain\":0.3,\"combatGain\":0.4,\"riseSeconds\":0.6,\"fallSeconds\":4,\"deathFadeSeconds\":0.7,\"duckGain\":0.5,\"duckResponseSeconds\":0.1,\"lowHealthGain\":0.9}", profile);
                string before = JsonUtility.ToJson(profile);
                AudioLayerMixSettings snapshot = profile.ToMixSettings();
                Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(before));
                Assert.That(snapshot.MasterVolume, Is.EqualTo(.8f));
                Assert.That(snapshot.LayerGains, Is.EqualTo(new Vector3(.2f, .3f, .4f)));
                Assert.That(snapshot.RiseSeconds, Is.EqualTo(.6f));
                Assert.That(snapshot.FallSeconds, Is.EqualTo(4));
                Assert.That(snapshot.DeathFadeSeconds, Is.EqualTo(.7f));
                Assert.That(snapshot.DuckGain, Is.EqualTo(.5f));
                Assert.That(snapshot.DuckResponseSeconds, Is.EqualTo(.1f));
                Assert.That(snapshot.LowHealthGain, Is.EqualTo(.9f));
                JsonUtility.FromJsonOverwrite("{\"masterVolume\":0.1}", profile);
                Assert.That(snapshot.MasterVolume, Is.EqualTo(.8f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void WarmedMixLoopDoesNotAllocateManagedMemory()
        {
            Step(.1f);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                Step(.01f, AudioMood.Combat);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }

        private void Step(float delta, AudioMood mood = AudioMood.Explore, bool alive = true, bool wounded = false, bool ducking = false, float volume = 1) =>
            controller.Step(delta, mood, alive, wounded, ducking, volume);
        private static AudioLayerMixSettings Settings() =>
            new AudioLayerMixSettings(.7f, new Vector3(.6f, .85f, .9f), .45f, 3.5f, .55f, .75f, .06f, .9f);

        private sealed class View : IAudioLayerMixView
        {
            public Vector3 Gains;
            public void ApplyGains(Vector3 gains) => Gains = gains;
        }
    }
}
