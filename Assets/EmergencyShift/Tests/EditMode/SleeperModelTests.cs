using System;
using System.Collections.Generic;
using System.IO;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class SleeperModelTests
    {
        [Test]
        public void BeamAccumulatesDecaysAndWakesOnceWithoutProximityTrigger()
        {
            var model = Model();
            int changes = 0;
            model.StateChanged += _ => changes++;
            model.Step(5, true, false);
            Assert.That(model.State, Is.EqualTo(SleeperState.Sleeping));
            model.Step(.4f, true, true);
            model.Step(.2f, true, false);
            Assert.That(model.ExposureProgress, Is.EqualTo(.25f / .65f).Within(.00001));
            model.Step(.41f, true, true);
            Assert.That(model.State, Is.EqualTo(SleeperState.Waking));
            Assert.That(model.HasAggro, Is.True);
            Assert.That(model.ExposureProgress, Is.Zero);
            model.Step(1, true, false);
            model.Step(10, true, false);
            Assert.That(model.State, Is.EqualTo(SleeperState.Hunting));
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void TargetDeathStopsAwakeEnemyAndDoesNotWakeSleepingEnemy()
        {
            var model = Model();
            model.Step(1, false, true);
            Assert.That(model.State, Is.EqualTo(SleeperState.Sleeping));
            new WakeSleeperCommand(model).Execute();
            model.Step(.1f, false, true);
            Assert.That(model.State, Is.EqualTo(SleeperState.TargetDead));
            model.Step(1, true, true);
            Assert.That(model.State, Is.EqualTo(SleeperState.TargetDead));
            Assert.That(model.HasAggro, Is.True);
        }

        [Test]
        public void AttackCommitsHeadingThenCompletesAndRecoversBeforeNextHunt()
        {
            var motor = new Motor();
            var melee = new Melee { Ready = true };
            var model = Model();
            var controller = new SleeperController(model, motor, melee);
            controller.Wake();
            controller.Step(1, true, false, true, Vector2.zero, Vector2.right);
            controller.Step(.02f, true, false, true, Vector2.zero, Vector2.right);
            Assert.That(model.State, Is.EqualTo(SleeperState.Attacking));
            Assert.That(melee.Begins, Is.EqualTo(1));
            controller.Step(.4f, true, false, true, Vector2.zero, Vector2.up);
            Assert.That(melee.Heading, Is.EqualTo(Vector2.right));
            Assert.That(melee.FixedHeading, Is.True);
            controller.Step(.7f, true, false, true, Vector2.zero, Vector2.up);
            Assert.That(model.State, Is.EqualTo(SleeperState.Recovering));
            Assert.That(melee.Finishes, Is.EqualTo(1));
            Assert.That(motor.Velocity, Is.EqualTo(Vector2.zero));
            controller.Step(.3f, true, false, true, Vector2.zero, Vector2.up);
            Assert.That(model.State, Is.EqualTo(SleeperState.Recovering));
            controller.Step(.36f, true, false, true, Vector2.zero, Vector2.up);
            Assert.That(model.State, Is.EqualTo(SleeperState.Hunting));
        }

        [Test]
        public void DisabledAttacksFollowWithoutStartingCombat()
        {
            var motor = new Motor();
            var melee = new Melee { Ready = true };
            var controller = new SleeperController(Model(), motor, melee);
            controller.Wake();
            controller.Step(1, true, false, false, Vector2.zero, Vector2.right);
            controller.Step(.02f, true, false, false, Vector2.zero, Vector2.right);
            Assert.That(motor.Follows, Is.EqualTo(1));
            Assert.That(melee.Begins, Is.Zero);
            Assert.That(motor.Separate, Is.True);
        }

        [Test]
        public void HitInterruptsAttackOnceAndStaggerDoesNotSeparateNeighbours()
        {
            var clock = new Clock();
            var motor = new Motor();
            var melee = new Melee { Ready = true };
            var controller = new SleeperController(Model(clock), motor, melee);
            controller.Wake();
            controller.Step(1, true, false, true, Vector2.zero, Vector2.right);
            controller.Step(.02f, true, false, true, Vector2.zero, Vector2.right);
            Assert.That(controller.Stagger(Vector2.left).Success, Is.True);
            Assert.That(controller.Model.State, Is.EqualTo(SleeperState.Recovering));
            Assert.That(melee.Interrupts, Is.EqualTo(1));
            Assert.That(melee.Delay, Is.EqualTo(.8f));
            controller.Restore(new SleeperStateSnapshot(SleeperState.Hunting, true, 0, 0, .1f, Vector2.left, Vector2.right));
            controller.Step(.02f, true, false, true, Vector2.zero, Vector2.right);
            Assert.That(motor.Separate, Is.False);
            Assert.That(motor.Velocity.x, Is.LessThan(0));
            clock.Time = 1;
            Assert.That(controller.Model.IsStaggered, Is.False);
        }

        [Test]
        public void DeathCannotWakeOrStaggerAndStopsTickingAfterPresentationDelay()
        {
            var model = Model();
            Assert.That(new KillSleeperCommand(model).Execute().Success, Is.True);
            Assert.That(new WakeSleeperCommand(model).Execute().Success, Is.False);
            Assert.That(new StaggerSleeperCommand(model, Vector2.up).Execute().Success, Is.False);
            model.Step(2, true, true);
            Assert.That(model.CanStopTicking, Is.False);
            model.Step(1, true, true);
            Assert.That(model.CanStopTicking, Is.True);
            Assert.That(model.DeathTime, Is.EqualTo(3));
        }

        [Test]
        public void RestoreIsSilentAndRebasesStaggerAgainstInjectedTime()
        {
            var clock = new Clock { Time = 4 };
            var model = Model(clock);
            var saved = new SleeperStateSnapshot(SleeperState.Attacking, true, .4f, .2f, .5f, Vector2.left, Vector2.right);
            int events = 0;
            model.StateChanged += _ => events++;
            Assert.That(new RestoreSleeperCommand(model, saved).Execute().Success, Is.True);
            Assert.That(model.Capture(), Is.EqualTo(saved));
            Assert.That(events, Is.Zero);
            clock.Time = 4.6;
            Assert.That(model.IsStaggered, Is.False);
        }

        [TestCase(-1, 0)]
        [TestCase(float.NaN, 0)]
        [TestCase(0, float.PositiveInfinity)]
        public void InvalidRestoreLeavesExistingStateUntouched(float age, float direction)
        {
            var model = Model();
            new WakeSleeperCommand(model).Execute();
            SleeperStateSnapshot before = model.Capture();
            var invalid = new SleeperStateSnapshot(SleeperState.Hunting, true, age, 0, 0, new Vector2(direction, 0), Vector2.right);
            Assert.That(new RestoreSleeperCommand(model, invalid).Execute().Success, Is.False);
            Assert.That(model.Capture(), Is.EqualTo(before));
        }

        [Test]
        public void InvalidCommandsAndNonfiniteClockCannotStartOrAdvanceCombat()
        {
            var clock = new Clock();
            var model = Model(clock);
            Assert.That(new BeginSleeperAttackCommand(model, Vector2.right).Execute().Success, Is.False);
            clock.Time = double.NaN;
            Assert.That(new WakeSleeperCommand(model).Execute().Success, Is.False);
            model.Step(1, true, true);
            Assert.That(model.ExposureProgress, Is.Zero);
        }

        [Test]
        public void SharedSettingsDoNotShareExposureOrAggro()
        {
            SleeperSettings settings = Settings();
            var first = new SleeperModel(settings, new Clock());
            var second = new SleeperModel(settings, new Clock());
            first.Step(1, true, true);
            Assert.That(first.HasAggro, Is.True);
            Assert.That(second.HasAggro, Is.False);
            Assert.That(second.State, Is.EqualTo(SleeperState.Sleeping));
        }

        [TestCase(float.NaN, .65f)]
        [TestCase(2.1f, 0)]
        [TestCase(float.PositiveInfinity, .65f)]
        public void InvalidSettingsRejectNonfiniteValuesAndZeroExposure(float speed, float exposure)
        {
            Assert.Throws<ArgumentException>(() => new SleeperSettings(speed, 1.05f, .8f, .09f, 1.6f, exposure, .75f, .9f, .65f));
        }

        [Test]
        public void AuthoredSleeperConfigAndSerializedEnumValuesRemainCompatible()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/Sleeper.prefab");
            var actor = prefab.GetComponent<Sleeper2D>();
            string path = AssetDatabase.GetAssetPath(actor.BehaviourProfile);
            byte[] before = File.ReadAllBytes(path);
            SleeperSettings settings = actor.BehaviourProfile.ToSettings(actor.Definition);
            Assert.That(settings.ExposureSeconds, Is.EqualTo(.65f));
            foreach (Sleeper2D.BehaviourState state in Enum.GetValues(typeof(Sleeper2D.BehaviourState)))
            {
                Assert.That(((SleeperState)(int)state).ToString(), Is.EqualTo(state.ToString()));
            }

            CollectionAssert.AreEqual(before, File.ReadAllBytes(path));
        }

        private static SleeperSettings Settings() => new SleeperSettings(2.1f, 1.05f, .8f, .09f, 1.6f, .65f, .75f, .9f, .65f);
        private static SleeperModel Model(Clock clock = null) => new SleeperModel(Settings(), clock ?? new Clock());
        private sealed class Clock : IGameClock
        {
            public double Time { get; set; }
        }

        private sealed class Motor : IEnemyBrainMotor
        {
            public int Follows;
            public bool Separate;
            public Vector2 Velocity;
            public Vector2 Follow(Vector2 goal, float speed)
            {
                Follows++;
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
            public int Interrupts;
            public float Delay;
            public Vector2 Heading;
            public bool FixedHeading;
            public bool CanStart(Vector2 at) => Ready;
            public void Begin() => Begins++;
            public Vector2 Step(float progress, Vector2 at, Vector2 heading, bool fixedHeading)
            {
                Heading = heading;
                FixedHeading = fixedHeading;
                return heading;
            }

            public void Finish() => Finishes++;
            public void Interrupt(float delay)
            {
                Interrupts++;
                Delay = delay;
            }
        }
    }
}
