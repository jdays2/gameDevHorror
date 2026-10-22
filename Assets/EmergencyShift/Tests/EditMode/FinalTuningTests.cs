using System;
using System.Reflection;
using EmergencyShift.Core.Composition;
using EmergencyShift.Core.Randomness;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Doors;
using EmergencyShift.Features.Doors.Config;
using EmergencyShift.Features.Doors.Logic;
using EmergencyShift.Features.Enemies.Logic;
using EmergencyShift.Features.LampPower.Config;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Tests
{
    public sealed class FinalTuningTests
    {
        [Test]
        public void DoorSharedChangesKeepManualDurationsAndExplicitOldDefault()
        {
            var rules = new DoorRulesSettings(.8f, 2);
            Assert.That(rules.ProjectDuration(.3f, false), Is.EqualTo(.8f));
            Assert.That(rules.ProjectDuration(.6f, false), Is.EqualTo(.6f));
            Assert.That(rules.ProjectDuration(.3f, true), Is.EqualTo(.3f));
            var clock = new Clock();
            var view = new DoorView();
            var factory = new DoorMotionControllerFactory(clock, rules);
            using (DoorMotionController controller = factory.Create(.3f, view))
            {
                Assert.That(controller.Toggle(true, 0).Success, Is.True);
                clock.Now = .4;
                controller.Tick();
                Assert.That(view.Amount, Is.EqualTo(.5f).Within(.0001f));
                Assert.That(factory.InteractionRange, Is.EqualTo(2));
            }
        }

        [TestCase(0, 1)]
        [TestCase(float.NaN, 1)]
        [TestCase(.3f, 0)]
        [TestCase(.3f, float.PositiveInfinity)]
        public void InvalidDoorSettingsAreRejected(float duration, float range)
        {
            Assert.Throws<ArgumentException>(() => new DoorRulesSettings(duration, range));
        }

        [Test]
        public void LegacyLampCurveRetainsExactStrictIntervalEdgesAndNoise()
        {
            var settings = new LocalLampSceneSettings();
            float[] times = { 0, 4.9f, 4.91f, 5.02f, 5.16f, 5.2f, 5.25f, 6.4f, 24 };
            foreach (float clock in times)
            {
                float position = Mathf.Repeat(clock, 6.4f);
                bool dark = position > 4.9f && position < 5.02f || position > 5.16f && position < 5.25f;
                float expected = dark ? 0 : Mathf.Lerp(.82f, 1, Mathf.PerlinNoise(clock * 1.4f, 3.1f));
                Assert.That(settings.Brightness(clock), Is.EqualTo(expected), "clock " + clock);
            }

            var changed = new LampFlickerSettings(10, .5f, 0, new Vector2(1, 2), new Vector2(3, 4), .4f);
            Assert.That(settings.Brightness(1.5f, changed), Is.Zero);
            Assert.That(settings.Brightness(1.5f), Is.GreaterThan(0));
            settings.flickers = false;
            Assert.That(settings.Brightness(1.5f, changed), Is.EqualTo(1));
            Assert.That(settings.SoundGain(settings.soundRadius * .4f, changed), Is.EqualTo(1));
            Assert.That(settings.SoundGain(settings.soundRadius * .4f), Is.LessThan(1));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidLampClockDoesNotReachNativeOutput(float clock)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => LampFlickerSettings.LegacyDefaults.Evaluate(clock));
        }

        [TestCase("doorRules")]
        [TestCase("fixtureFlicker")]
        public void MissingWorldProfileFailsBeforeAnyBindings(string field)
        {
            var owner = new GameObject("Missing final world profile");
            try
            {
                var installer = owner.AddComponent<WorldInstaller>();
                EmergencyShift.Editor.ThreatSignalAuthoring.ConfigureInstaller(installer);
                EmergencyShift.Editor.PlayerRulesAuthoring.ConfigureInstaller(installer);
                EmergencyShift.Editor.WorldVisualRulesAuthoring.ConfigureInstaller(installer);
                var data = new SerializedObject(installer);
                data.FindProperty(field).objectReferenceValue = null;
                data.ApplyModifiedPropertiesWithoutUndo();
                var scope = new DiContainer();
                scope.Inject(installer);
                Assert.Throws<InvalidOperationException>(() => installer.InstallBindings());
                Assert.That(scope.HasBinding<DoorRulesSettings>(), Is.False);
                Assert.That(scope.HasBinding<LampFlickerSettings>(), Is.False);
                Assert.That(scope.HasBinding<IWorldTimeScale>(), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [TestCase(typeof(PlayerAudio2D), typeof(PlayerAudioSet), "pickupVolume", "PickupVolume", .65f, .9f)]
        [TestCase(typeof(PlayerFootsteps2D), typeof(PlayerWorldAudioSet), "distancePerStep", "DistancePerStep", 1.05f, 1.7f)]
        [TestCase(typeof(DoorAudio2D), typeof(DoorAudioSet), "volume", "Volume", .85f, .4f)]
        [TestCase(typeof(StalkerAudio2D), typeof(StalkerAudioSet), "hearingDistance", "HearingDistance", 11, 18)]
        public void NativeAudioUsesSharedDefaultsAndPreservesManualAndForcedOverrides(
            Type componentType, Type profileType, string field, string property, float formerDefault, float changed)
        {
            // An inactive native view exercises its real projection without requiring gameplay Awake/physics.
            var owner = new GameObject("Inactive audio projection");
            owner.SetActive(false);
            ScriptableObject profile = ScriptableObject.CreateInstance(profileType);
            try
            {
                Component view = owner.AddComponent(componentType);
                Set(view, "sounds", profile);
                var data = new SerializedObject(profile);
                data.FindProperty(field).floatValue = changed;
                data.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(Read(view, property), Is.EqualTo(changed));
                Set(view, field, formerDefault + .1f);
                Assert.That(Read(view, property), Is.EqualTo(formerDefault + .1f));
                Set(view, field, formerDefault);
                Set(view, "useAuthoredAudioTuning", true);
                Assert.That(Read(view, property), Is.EqualTo(formerDefault));
                Assert.That(data.FindProperty(field).floatValue, Is.EqualTo(changed));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [TestCase(typeof(PlayerAudioSet), "panDistance", 0)]
        [TestCase(typeof(PlayerAudioSet), "impactHearingDistance", float.NaN)]
        [TestCase(typeof(PlayerWorldAudioSet), "distancePerStep", 0)]
        [TestCase(typeof(PlayerWorldAudioSet), "minimumInterval", -1)]
        [TestCase(typeof(DoorAudioSet), "fullVolumeDistance", 30)]
        [TestCase(typeof(DoorAudioSet), "maximumPan", 2)]
        [TestCase(typeof(StalkerAudioSet), "blockedCutoff", 30000)]
        [TestCase(typeof(StalkerAudioSet), "hearingDistance", 1)]
        public void SharedAudioRejectsInvalidTuning(Type type, string field, float value)
        {
            ScriptableObject profile = ScriptableObject.CreateInstance(type);
            try
            {
                var data = new SerializedObject(profile);
                data.FindProperty(field).floatValue = value;
                data.ApplyModifiedPropertiesWithoutUndo();
                var error = Assert.Throws<TargetInvocationException>(() => type.GetMethod("Validate").Invoke(profile, null));
                Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void NavigationReadsOneClockSampleAndRejectsInvalidTimeBeforeQuotaOrRandom()
        {
            var clock = new Clock { Now = double.NaN };
            var budget = new Budget();
            var random = new Random();
            var schedule = new EnemyPathSchedule(clock, budget, random);
            Assert.That(schedule.TryBeginBuild(), Is.False);
            Assert.That(clock.Reads, Is.EqualTo(1));
            Assert.That(budget.Reservations, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => schedule.CompleteBuild());
            Assert.That(random.Reads, Is.Zero);
            clock.Now = 2;
            clock.Reads = 0;
            Assert.That(schedule.TryBeginBuild(), Is.True);
            Assert.That(clock.Reads, Is.EqualTo(1));
            schedule.CompleteBuild();
            clock.Now = 2.5;
            Assert.That(schedule.TryBeginBuild(), Is.False);
            clock.Now = 2.8;
            Assert.That(schedule.TryBeginBuild(), Is.True);
        }

        [Test]
        public void HistoricalCheckpointProbeRejectsCurrentSceneBeforeSlotOrPlayerMutation()
        {
            var owner = new GameObject("Historical checkpoint preflight");
            try
            {
                Type type = Type.GetType("EmergencyShift.CheckpointRegressionProbe, EmergencyShift.Development", true);
                Component probe = owner.AddComponent(type);
                var run = (System.Collections.IEnumerator)type.GetMethod("Run", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(probe, null);
                Assert.That(run.MoveNext(), Is.True);
                var error = Assert.Throws<InvalidOperationException>(() => run.MoveNext());
                Assert.That(error.Message, Does.Contain("retired FirstShift"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void NativeBenchmarkLeaseRestoresGlobalSettingsAndDisposesIdempotently()
        {
            bool background = Application.runInBackground;
            int sync = QualitySettings.vSyncCount;
            int rate = Application.targetFrameRate;
            Type type = Type.GetType("EmergencyShift.NativeBenchmarkSettingsLease, EmergencyShift.Development", true);
            var lease = (IDisposable)Activator.CreateInstance(type);
            try
            {
                Application.runInBackground = !background;
                QualitySettings.vSyncCount = sync == 0 ? 1 : 0;
                Application.targetFrameRate = rate == 30 ? 60 : 30;
                lease.Dispose();
                Assert.That(Application.runInBackground, Is.EqualTo(background));
                Assert.That(QualitySettings.vSyncCount, Is.EqualTo(sync));
                Assert.That(Application.targetFrameRate, Is.EqualTo(rate));
                lease.Dispose();
            }
            finally
            {
                lease.Dispose();
                Application.runInBackground = background;
                QualitySettings.vSyncCount = sync;
                Application.targetFrameRate = rate;
            }
        }

        [TestCase("FirstShift")]
        [TestCase("MechanicsSandbox")]
        [TestCase("../Assets")]
        public void PersistenceAuthoringRejectsUnsupportedDefinitionsBeforeCreatingAssets(string sceneName)
        {
            Assert.Throws<InvalidOperationException>(() => EmergencyShift.Editor.LevelPersistenceAuthoring.Definition(sceneName));
        }

        [Test]
        public void PersistenceAuthoringRejectsUnsavedSceneWithoutAddingComponentsOrDefinitions()
        {
            var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var marker = new GameObject("Unsaved manual sentinel");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(marker, scene);
            int components = marker.GetComponents<Component>().Length;
            try
            {
                Assert.Throws<InvalidOperationException>(() => EmergencyShift.Editor.LevelPersistenceAuthoring.Prepare(scene));
                Assert.That(scene.isLoaded, Is.True);
                Assert.That(marker.GetComponents<Component>().Length, Is.EqualTo(components));
                Assert.That(scene.GetRootGameObjects().Length, Is.EqualTo(1));
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded)
                {
                    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
                }

                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static float Read(object target, string property) => (float)target.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        private sealed class Clock : IGameClock
        {
            public double Now;
            public int Reads;
            public double Time
            {
                get
                {
                    Reads++;
                    return Now;
                }
            }
        }

        private sealed class Budget : IEnemyPathBuildBudget
        {
            public int Reservations;
            public bool TryReserve()
            {
                Reservations++;
                return true;
            }
        }

        private sealed class Random : IRandomSource
        {
            public int Reads;
            public int NextInt(int minimumInclusive, int maximumExclusive) => minimumInclusive;
            public double NextUnit()
            {
                Reads++;
                return .5;
            }
        }

        private sealed class DoorView : IDoorMotionView
        {
            public float Amount;
            public void ApplyOpenAmount(float amount) => Amount = amount;
            public void MotionStarted(bool open) { }
            public void MotionStopped(bool open) { }
        }
    }
}
