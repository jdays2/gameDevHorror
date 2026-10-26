using EmergencyShift.Core.Time;
using EmergencyShift.Features.ClawEnemy.Config;
using EmergencyShift.Features.ClawEnemy.Logic;
using EmergencyShift.Features.Enemies.Logic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class ClawEnemyTests
    {
        [Test]
        public void AggroRequiresDetectionAndSurvivesOcclusionWithoutLeakingToAnotherActor()
        {
            var first = Model(new Clock());
            var second = Model(new Clock());
            Assert.That(first.Step(Frame(false)), Is.EqualTo(ClawEnemyIntent.Stop));
            Assert.That(first.Step(Frame(true)), Is.EqualTo(ClawEnemyIntent.Pursue));
            Assert.That(first.Step(Frame(false)), Is.EqualTo(ClawEnemyIntent.Pursue));
            Assert.That(second.HasAggro, Is.False);
            new ClawEnemyCommand(first, ClawEnemyAction.Kill).Execute();
            Assert.That(first.Step(Frame(true)), Is.EqualTo(ClawEnemyIntent.Stop));
            Assert.That(second.IsAlive, Is.True);
        }

        [Test]
        public void AttackKeepsItsHeadingAndResolvesCrossedEndExactlyOnce()
        {
            var clock = new Clock();
            var movement = new Movement();
            var melee = new Melee();
            var controller = new ClawEnemyController(Model(clock), movement, melee);
            controller.Step(Frame(true));
            Assert.That(melee.Begins, Is.EqualTo(1));
            clock.Time = .7;
            controller.Step(Frame(false, target: Vector2.up));
            Assert.That(melee.Heading, Is.EqualTo(Vector2.right));
            Assert.That(melee.FixedHeading, Is.True);
            clock.Time = 2;
            controller.Step(Frame(false));
            Assert.That(melee.Progress, Is.EqualTo(1));
            Assert.That(melee.Finishes, Is.EqualTo(1));
            melee.Ready = false;
            controller.Step(Frame(false));
            Assert.That(melee.Finishes, Is.EqualTo(1));
        }

        [Test]
        public void StationaryTargetNeverMovesOrAttacksAfterHit()
        {
            var movement = new Movement();
            var melee = new Melee();
            var controller = new ClawEnemyController(Model(new Clock()), movement, melee);
            controller.Hit(Vector2.right);
            controller.Step(Frame(true, stationary: true));
            Assert.That(movement.Velocity, Is.EqualTo(Vector2.zero));
            Assert.That(melee.Begins, Is.Zero);
        }

        [Test]
        public void BulletInterruptsWindupAndInvalidCommandCannotCorruptHeading()
        {
            var model = Model(new Clock());
            var melee = new Melee();
            var controller = new ClawEnemyController(model, new Movement(), melee);
            controller.Step(Frame(true));
            controller.Hit(Vector2.left);
            Assert.That(melee.Interruptions, Is.EqualTo(1));
            Assert.That(model.State, Is.EqualTo(ClawEnemyState.Staggered));
            Assert.That(new ClawEnemyCommand(model, ClawEnemyAction.Hit, new Vector2(float.NaN, 0)).Execute().Success, Is.False);
            controller.Kill();
            Assert.That(new ClawEnemyCommand(model, ClawEnemyAction.BeginAttack, Vector2.right).Execute().Success, Is.False);
        }

        [Test]
        public void ImportedAttackPreservesWindupFastStrikeAndRecoveryTimings()
        {
            var attack = AssetDatabase.LoadAssetAtPath<TimedSpriteMotion>("Assets/EmergencyShift/Data/Enemies/ClawPrototype/Sweep.asset");
            attack.Validate();
            Assert.That(attack.Duration, Is.EqualTo(1.21f).Within(.0001));
            Assert.That(attack.FrameAt(.641f).name, Is.EqualTo("sweep-08"));
            Assert.That(attack.FrameAt(.671f).name, Is.EqualTo("sweep-09"));
            Assert.That(attack.FrameAt(2).name, Is.EqualTo("sweep-17"));
        }

        private static ClawEnemyModel Model(Clock clock) => new ClawEnemyModel(new ClawEnemySettings(1.65f, 1.21f, .09f, 1.2f), clock);
        private static ClawEnemyObservation Frame(bool detected, bool stationary = false, Vector2? target = null) =>
            new ClawEnemyObservation(.02f, Vector2.zero, target ?? Vector2.right, true, detected, false, !stationary, stationary, .4f, 0);

        private sealed class Clock : IGameClock
        {
            public double Time { get; set; }
        }

        private sealed class Movement : IClawLocomotion
        {
            public Vector2 Velocity;
            public Vector2 Follow(Vector2 target, float speed) => target.normalized * speed;
            public Vector2 Patrol(ClawEnemyObservation frame) => Vector2.up;
            public void Move(Vector2 velocity, bool separateFromNeighbours) => Velocity = velocity;
        }

        private sealed class Melee : IEnemyBrainMelee
        {
            public bool Ready = true;
            public int Begins;
            public int Finishes;
            public int Interruptions;
            public float Progress;
            public Vector2 Heading;
            public bool FixedHeading;
            public bool CanStart(Vector2 at) => Ready;
            public void Begin() => Begins++;
            public Vector2 Step(float progress, Vector2 at, Vector2 heading, bool fixedHeading)
            {
                Progress = progress;
                Heading = heading;
                FixedHeading = fixedHeading;
                return Vector2.zero;
            }
            public void Finish() => Finishes++;
            public void Interrupt(float delay) => Interruptions++;
        }
    }
}
