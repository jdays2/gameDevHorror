using System;
using EmergencyShift.Core.Composition;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.AudioMood;
using EmergencyShift.Features.AudioMood.Config;
using EmergencyShift.Features.AudioMood.Logic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class AudioThreatSignalTests
    {
        private Clock clock;
        private AudioThreatSignalController signal;

        [SetUp]
        public void Setup()
        {
            clock = new Clock { Time = 10 };
            signal = Factory().Create();
        }

        [TestCase(-1, 10.25f)]
        [TestCase(.1f, 10.25f)]
        [TestCase(1, 11)]
        [TestCase(10, 12)]
        public void DurationKeepsLegacyClamp(float duration, float expected)
        {
            Assert.That(signal.Report(duration, 1, true, false).Success, Is.True);
            Assert.That(signal.HeardUntil, Is.EqualTo(expected));
            Assert.That(signal.UrgentUntil, Is.EqualTo(float.NegativeInfinity));
        }

        [Test]
        public void ShorterLaterCueCannotShortenHearingMemory()
        {
            signal.Report(2, 1, true, false);
            clock.Time = 10.1;
            signal.Report(.25f, 1, true, false);
            Assert.That(signal.HeardUntil, Is.EqualTo(12));
        }

        [Test]
        public void UrgencyUsesItsOwnHoldAndOrdinaryCueDoesNotClearIt()
        {
            signal.Report(1, 1, true, true);
            Assert.That(signal.UrgentUntil, Is.EqualTo(10.4f));
            clock.Time = 10.1;
            signal.Report(1, 1, true, false);
            Assert.That(signal.UrgentUntil, Is.EqualTo(10.4f));
            signal.Report(1, 1, true, true);
            Assert.That(signal.UrgentUntil, Is.EqualTo(10.5f).Within(.00001));
        }

        [TestCase(.019f, false)]
        [TestCase(.02f, true)]
        public void MinimumGainBoundaryIsInclusive(float gain, bool expected)
        {
            Assert.That(signal.Report(1, gain, true, true).Success, Is.EqualTo(expected));
        }

        [Test]
        public void DeadOrNonFiniteCueCannotMutateState()
        {
            Assert.That(signal.Report(1, 1, false, true).Success, Is.False);
            Assert.That(signal.Report(float.NaN, 1, true, true).Success, Is.False);
            Assert.That(signal.Report(1, float.PositiveInfinity, true, true).Success, Is.False);
            Assert.That(signal.HeardUntil, Is.EqualTo(float.NegativeInfinity));
            Assert.That(signal.UrgentUntil, Is.EqualTo(float.NegativeInfinity));
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(-1d)]
        public void InvalidClockRejectsBeforeMutation(double time)
        {
            clock.Time = time;
            Assert.That(signal.Report(1, 1, true, true).Success, Is.False);
            Assert.That(signal.HeardUntil, Is.EqualTo(float.NegativeInfinity));
        }

        [Test]
        public void ExecutionSamplesClockOnce()
        {
            var advancing = new AdvancingClock();
            var model = new AudioThreatSignalModel(Settings(), advancing);
            var command = new ReportThreatCueCommand(model, 1, 1, true, true);
            Assert.That(command.Execute().Success, Is.True);
            Assert.That(advancing.Reads, Is.EqualTo(1));
            Assert.That(model.HeardUntil, Is.EqualTo(11));
            Assert.That(model.UrgentUntil, Is.EqualTo(10.4f));
        }

        [Test]
        public void CheckDoesNotApplyAndExecutionRechecksClock()
        {
            var model = new AudioThreatSignalModel(Settings(), clock);
            var command = new ReportThreatCueCommand(model, 1, 1, true, true);
            Assert.That(command.CanExecute().Success, Is.True);
            Assert.That(model.HeardUntil, Is.EqualTo(float.NegativeInfinity));
            clock.Time = double.NaN;
            Assert.That(command.Execute().Success, Is.False);
        }

        [Test]
        public void ResetAndSeparateInstancesDoNotShareDeadlines()
        {
            AudioThreatSignalController other = Factory().Create();
            signal.Report(1, 1, true, true);
            Assert.That(other.HeardUntil, Is.EqualTo(float.NegativeInfinity));
            other.Report(2, 1, true, false);
            signal.Reset();
            Assert.That(signal.HeardUntil, Is.EqualTo(float.NegativeInfinity));
            Assert.That(signal.UrgentUntil, Is.EqualTo(float.NegativeInfinity));
            Assert.That(other.HeardUntil, Is.EqualTo(12));
            Assert.That(new ResetThreatSignalCommand(null).Execute().Success, Is.False);
            Assert.That(new ReportThreatCueCommand(null, 1, 1, true, true).Execute().Success, Is.False);
        }

        [Test]
        public void InvalidSettingsRejectBeforeComposition()
        {
            Assert.Throws<ArgumentException>(() => new AudioThreatSignalSettings(float.NaN, .25f, 2, .4f));
            Assert.Throws<ArgumentException>(() => new AudioThreatSignalSettings(.02f, 3, 2, .4f));
            Assert.Throws<ArgumentException>(() => new AudioThreatSignalSettings(.02f, .25f, 2, 0));
        }

        [Test]
        public void ProductionProfileIsAssignedAndRetainsOriginalNumbers()
        {
            var profile = AssetDatabase.LoadAssetAtPath<AudioThreatSignalProfile>(EmergencyShift.Editor.ThreatSignalAuthoring.ProfilePath);
            Assert.That(profile, Is.Not.Null);
            AudioThreatSignalSettings settings = profile.ToSettings();
            Assert.That(settings.MinimumGain, Is.EqualTo(.02f));
            Assert.That(settings.MinimumHeardSeconds, Is.EqualTo(.25f));
            Assert.That(settings.MaximumHeardSeconds, Is.EqualTo(2));
            Assert.That(settings.UrgentSeconds, Is.EqualTo(.4f));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EmergencyShift.Editor.ThreatSignalAuthoring.ContextPath);
            var serialized = new SerializedObject(prefab.GetComponent<WorldInstaller>());
            Assert.That(serialized.FindProperty("threatSignals").objectReferenceValue, Is.SameAs(profile));
            string before = JsonUtility.ToJson(profile);
            profile.ToSettings();
            Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(before));
        }

        [Test]
        public void WorldMissingProfileFailsBeforeBindings()
        {
            var owner = new GameObject("Missing world signal config");
            try
            {
                var scope = new DiContainer();
                var installer = owner.AddComponent<WorldInstaller>();
                scope.Inject(installer);
                Assert.Throws<InvalidOperationException>(() => installer.InstallBindings());
                Assert.That(scope.HasBinding<AudioThreatSignalSettings>(), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void WarmedCueCommandsDoNotAllocateManagedMemory()
        {
            signal.Report(1, 1, true, true);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                signal.Report(1, 1, true, true);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }

        [Test]
        public void WorldInvalidProfileFailsBeforeBindings()
        {
            var owner = new GameObject("Invalid world signal config");
            var profile = ScriptableObject.CreateInstance<AudioThreatSignalProfile>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"minimumGain\":-1}", profile);
                var scope = new DiContainer();
                var installer = owner.AddComponent<WorldInstaller>();
                var serialized = new SerializedObject(installer);
                serialized.FindProperty("threatSignals").objectReferenceValue = profile;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                scope.Inject(installer);
                Assert.Throws<ArgumentException>(() => installer.InstallBindings());
                Assert.That(scope.HasBinding<AudioThreatSignalSettings>(), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(profile);
            }
        }

        private AudioThreatSignalControllerFactory Factory() => new AudioThreatSignalControllerFactory(Settings(), clock);
        private static AudioThreatSignalSettings Settings() => new AudioThreatSignalSettings(.02f, .25f, 2, .4f);
        private sealed class Clock : IGameClock
        {
            public double Time { get; set; }
        }
        private sealed class AdvancingClock : IGameClock
        {
            public int Reads;
            public double Time => 10 + Reads++;
        }
    }
}
