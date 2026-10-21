using System;
using System.IO;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class StalkerModelTests
    {
        [Test]
        public void DetectionNeedsRangeAndSightThenAggroSurvivesLosingBoth()
        {
            var model = Model();
            model.Step(Frame(visible: false));
            Assert.That(model.HasAggro, Is.False);
            model.Step(Frame(target: Vector2.right * 10));
            Assert.That(model.HasAggro, Is.False);
            model.Step(Frame());
            Assert.That(model.HasAggro, Is.True);
            Assert.That(model.State, Is.EqualTo(StalkerState.Sneaking));
            model.Step(Frame(visible: false, target: Vector2.right * 20));
            Assert.That(model.HasAggro, Is.True);
            Assert.That(model.State, Is.EqualTo(StalkerState.Sneaking));
        }

        [Test]
        public void PatrolStopsWhenAggroBeginsAndTargetDeathCannotResumeIt()
        {
            var motor = new Locomotion();
            var model = Model();
            using (var controller = new StalkerController(model, motor, new Melee()))
            {
                controller.Step(Frame(visible: false, patrol: true));
                Assert.That(model.State, Is.EqualTo(StalkerState.Patrolling));
                Assert.That(motor.Patrols, Is.EqualTo(1));
                controller.Step(Frame(patrol: true));
                Assert.That(model.State, Is.EqualTo(StalkerState.Sneaking));
                Assert.That(motor.Resets, Is.EqualTo(1));
                controller.Step(Frame(alive: false, patrol: true));
                Assert.That(model.State, Is.EqualTo(StalkerState.TargetDead));
                Assert.That(motor.Patrols, Is.EqualTo(1));
                Assert.That(motor.Velocity, Is.EqualTo(Vector2.zero));
                Assert.That(motor.Separate, Is.False);
            }
        }

        [Test]
        public void ReactionDelayRecoilFasterRetreatAndLitWaitKeepTheirOrder()
        {
            var model = Model();
            Assert.That(model.Step(Frame(.2f, exposed: true)), Is.EqualTo(StalkerIntent.Approach));
            Assert.That(model.State, Is.EqualTo(StalkerState.Sneaking));
            model.Step(Frame(.24f, exposed: true));
            Assert.That(model.State, Is.EqualTo(StalkerState.Sneaking));
            model.Step(Frame(.02f, exposed: true));
            Assert.That(model.State, Is.EqualTo(StalkerState.Recoiling));
            Assert.That(model.Closure, Is.Zero);
            model.Step(Frame(.36f, exposed: true));
            Assert.That(model.State, Is.EqualTo(StalkerState.Retreating));
            Assert.That(model.Step(Frame(.02f, exposed: true)), Is.EqualTo(StalkerIntent.Retreat));
            Assert.That(model.RetreatSpeed, Is.GreaterThan(model.PursuitSpeed));
            model.Step(Frame(1.06f, exposed: true));
            Assert.That(model.State, Is.EqualTo(StalkerState.Waiting));
            model.Step(Frame(4, exposed: true));
            Assert.That(model.State, Is.EqualTo(StalkerState.Waiting));
            model.Step(Frame(.02f, exposed: false));
            Assert.That(model.State, Is.EqualTo(StalkerState.Sneaking));
            Assert.That(model.HasAggro, Is.True);
        }

        [Test]
        public void DistantIlluminationAloneDoesNotCauseFearOrEyeDamage()
        {
            var model = Model();
            model.Step(Frame(2, exposed: true, light: Vector2.right * 6));
            Assert.That(model.State, Is.EqualTo(StalkerState.Sneaking));
            Assert.That(model.EyeDamageProgress, Is.Zero);
            Assert.That(model.ReactionProgress, Is.Zero);
        }

        [Test]
        public void EyeBurnHasLeadInKeepsDamageAcrossContactsAndThrottlesCues()
        {
            var clock = new Clock();
            var model = Model(clock);
            int cues = 0;
            model.EyeBurnStarted += () => cues++;
            model.Step(Frame(.1f, exposed: true));
            Assert.That(cues, Is.EqualTo(1));
            Assert.That(model.EyeDamageProgress, Is.Zero);
            model.Step(Frame(.1f, exposed: true));
            float damage = model.EyeDamageProgress;
            Assert.That(damage, Is.EqualTo(.06f / 1.25f).Within(.00001));
            model.Step(Frame(.02f, exposed: false));
            clock.Time = .2;
            model.Step(Frame(.1f, exposed: true));
            Assert.That(cues, Is.EqualTo(1));
            Assert.That(model.EyeDamageProgress, Is.EqualTo(damage));
            model.Step(Frame(2, exposed: true));
            Assert.That(model.EyeDamageProgress, Is.EqualTo(1));
            model.Step(Frame(.1f, exposed: false));
            clock.Time = 10;
            model.Step(Frame(.2f, exposed: true));
            Assert.That(cues, Is.EqualTo(1));
        }

        [Test]
        public void RestoreRebasesTimersSilentlyAndPreservesPartialEyeContact()
        {
            var clock = new Clock { Time = 5 };
            var model = Model(clock);
            var state = Snapshot(stagger: .4f, damage: .3f, burn: .2f);
            int changes = 0;
            int cues = 0;
            model.StateChanged += _ => changes++;
            model.EyeBurnStarted += () => cues++;
            Assert.That(new RestoreStalkerCommand(model, state).Execute().Success, Is.True);
            Assert.That(model.Capture(), Is.EqualTo(state));
            model.Step(Frame(.02f, exposed: true));
            Assert.That(changes + cues, Is.Zero);
            Assert.That(model.EyeDamageProgress, Is.GreaterThan(.3f));
            clock.Time = 5.5;
            Assert.That(model.IsStaggered, Is.False);
        }

        [Test]
        public void PursuitUsesBehindGoalOnlyWhenItIsStandableAndVisible()
        {
            var motor = new Locomotion { Standable = false };
            var model = Model();
            using (var controller = new StalkerController(model, motor, new Melee()))
            {
                StalkerObservation frame = Frame(target: Vector2.right * 2);
                controller.Step(frame);
                Assert.That(motor.Goal, Is.EqualTo(frame.TargetPosition));
                motor.Standable = true;
                controller.Step(frame);
                Assert.That(motor.Goal, Is.EqualTo(frame.TargetPosition - Vector2.up * 1.05f));
                controller.Step(Frame(visible: false, target: Vector2.right * 4));
                Assert.That(motor.Goal, Is.EqualTo(Vector2.right * 4));
            }
        }

        [Test]
        public void AttackUsesLegacySteeringAndHitInterruptsWithAuthoredDelay()
        {
            var melee = new Melee { Ready = true };
            var model = Model();
            using (var controller = new StalkerController(model, new Locomotion(), melee))
            {
                controller.Step(Frame());
                Assert.That(model.State, Is.EqualTo(StalkerState.Attacking));
                Assert.That(melee.Begins, Is.EqualTo(1));
                controller.Step(Frame(.2f));
                Assert.That(melee.FixedHeading, Is.False);
                controller.Stagger(Vector2.left, true);
                Assert.That(model.State, Is.EqualTo(StalkerState.Sneaking));
                Assert.That(melee.Delay, Is.EqualTo(.35f));
                Assert.That(model.IsStaggered, Is.True);
            }
        }

        [Test]
        public void AttackCompletesOnceAndReturnsToPursuit()
        {
            var melee = new Melee { Ready = true };
            var model = Model();
            using (var controller = new StalkerController(model, new Locomotion(), melee))
            {
                controller.Step(Frame());
                controller.Step(Frame(.81f));
                Assert.That(model.State, Is.EqualTo(StalkerState.Sneaking));
                Assert.That(melee.Finishes, Is.EqualTo(1));
            }
        }

        [Test]
        public void CommandsRejectInvalidAttackAndDeadActorCannotAggroOrStagger()
        {
            var model = Model();
            Assert.That(new BeginStalkerAttackCommand(model, Vector2.right).Execute().Success, Is.False);
            Assert.That(new KillStalkerCommand(model).Execute().Success, Is.True);
            Assert.That(new ActivateStalkerAggroCommand(model).Execute().Success, Is.False);
            Assert.That(new StaggerStalkerCommand(model, Vector2.right, true).Execute().Success, Is.False);
            model.Step(Frame(3));
            Assert.That(model.CanStopTicking, Is.True);
            Assert.That(model.Closure, Is.EqualTo(1));
        }

        [Test]
        public void DisposingControllerDetachesItsRouteResetSubscription()
        {
            var model = Model();
            var motor = new Locomotion();
            var controller = new StalkerController(model, motor, new Melee());
            controller.Dispose();
            new ActivateStalkerAggroCommand(model).Execute();
            Assert.That(motor.Resets, Is.Zero);
        }

        [TestCase(float.NaN, 0)]
        [TestCase(-1, 0)]
        [TestCase(0, float.PositiveInfinity)]
        public void InvalidSnapshotsLeaveExistingStateUntouched(float age, float push)
        {
            var model = Model();
            model.Step(Frame());
            StalkerStateSnapshot before = model.Capture();
            var invalid = new StalkerStateSnapshot(StalkerState.Sneaking, true, .2f, 0, age, 0, 0,
                Vector2.right, Vector2.up, 0, new Vector2(push, 0));
            Assert.That(new RestoreStalkerCommand(model, invalid).Execute().Success, Is.False);
            Assert.That(model.Capture(), Is.EqualTo(before));
        }

        [Test]
        public void FrozenFrameAndNonfiniteClockDoNotAdvanceStateOrAggro()
        {
            var clock = new Clock();
            var model = Model(clock);
            model.Step(Frame(0, exposed: true));
            clock.Time = double.NaN;
            model.Step(Frame(1, exposed: true));
            Assert.That(model.State, Is.EqualTo(StalkerState.Dormant));
            Assert.That(model.HasAggro, Is.False);
            Assert.That(model.EyeDamageProgress, Is.Zero);
        }

        [Test]
        public void SharedSettingsDoNotShareAggroEyeDamageOrHome()
        {
            StalkerSettings settings = Settings();
            var first = new StalkerModel(settings, new Clock(), Vector2.right);
            var second = new StalkerModel(settings, new Clock(), Vector2.up);
            first.Step(Frame(1, exposed: true));
            Assert.That(first.HasAggro, Is.True);
            Assert.That(first.EyeDamageProgress, Is.GreaterThan(0));
            Assert.That(second.HasAggro, Is.False);
            Assert.That(second.EyeDamageProgress, Is.Zero);
            Assert.That(second.Home, Is.EqualTo(Vector2.up));
        }

        [TestCase(float.NaN, .45f)]
        [TestCase(3.8f, 0)]
        [TestCase(float.PositiveInfinity, .45f)]
        public void InvalidSettingsAreRejectedBeforeCreatingAnActor(float retreat, float reaction)
        {
            Assert.Throws<ArgumentException>(() => Settings(retreat, reaction));
        }

        [Test]
        public void AuthoredProfilesAndLegacyStateNumbersStayCompatibleWithoutAssetWrites()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
            EnemyDefinition definition = prefab.GetComponent<ShadowStalker2D>().Definition;
            string[] guids = AssetDatabase.FindAssets("t:StalkerBehaviourProfile", new[] { "Assets/EmergencyShift" });
            Assert.That(guids, Is.Not.Empty);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                byte[] before = File.ReadAllBytes(path);
                var profile = AssetDatabase.LoadAssetAtPath<StalkerBehaviourProfile>(path);
                Assert.That(profile.ToSettings(definition).ReactionDelay, Is.EqualTo(profile.ReactionDelay));
                CollectionAssert.AreEqual(before, File.ReadAllBytes(path));
            }

            foreach (ShadowStalker2D.BehaviourState state in Enum.GetValues(typeof(ShadowStalker2D.BehaviourState)))
            {
                Assert.That(((StalkerState)(int)state).ToString(), Is.EqualTo(state.ToString()));
            }
        }

        private static StalkerSettings Settings(float retreat = 3.8f, float reaction = .45f) => new StalkerSettings(
            1.5f, retreat, 1.05f, .35f, 1.05f, 3, 3.2f, reaction, .8f, .09f, 1.6f, .14f, 1.25f, 1.2f, .35f);
        private static StalkerModel Model(Clock clock = null) => new StalkerModel(Settings(), clock ?? new Clock(), Vector2.zero);
        private static StalkerStateSnapshot Snapshot(float stagger = 0, float damage = 0, float burn = 0) =>
            new StalkerStateSnapshot(StalkerState.Sneaking, true, damage, burn, .1f, 0, 0, Vector2.right, Vector2.up, stagger, Vector2.left);
        private static StalkerObservation Frame(float dt = .02f, bool visible = true, bool exposed = false, bool alive = true,
            bool patrol = false, Vector2? target = null, Vector2? light = null) =>
            new StalkerObservation(dt, Vector2.zero, Vector2.zero, target ?? Vector2.right,
                light ?? Vector2.right, Vector2.up, true, alive, visible, exposed, patrol, true, 3.2f, .3f, 256);
        private sealed class Clock : IGameClock
        {
            public double Time { get; set; }
        }

        private sealed class Locomotion : IStalkerLocomotion
        {
            public int Patrols;
            public int Resets;
            public bool Standable = true;
            public bool Separate;
            public Vector2 Velocity;
            public Vector2 Goal;
            public Vector2 ClearDirection(Vector2 preferred) => preferred;
            public bool CanStand(Vector2 goal) => Standable;
            public bool Clear(Vector2 from, Vector2 to) => true;
            public Vector2 Patrol(Vector2 at, float radius, int solids, float deltaTime)
            {
                Patrols++;
                return Vector2.right;
            }

            public void ResetPatrol() => Resets++;
            public Vector2 Follow(Vector2 goal, float speed)
            {
                Goal = goal;
                return goal.normalized * speed;
            }

            public void Move(Vector2 velocity, bool separateFromNeighbours)
            {
                Velocity = velocity;
                Separate = separateFromNeighbours;
            }
        }

        private sealed class Melee : IEnemyBrainMelee
        {
            public bool Ready;
            public int Begins;
            public int Finishes;
            public bool FixedHeading;
            public float Delay;
            public bool CanStart(Vector2 at) => Ready;
            public void Begin() => Begins++;
            public Vector2 Step(float progress, Vector2 at, Vector2 heading, bool fixedHeading)
            {
                FixedHeading = fixedHeading;
                return heading;
            }

            public void Finish() => Finishes++;
            public void Interrupt(float delay) => Delay = delay;
        }
    }
}
