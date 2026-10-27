using EmergencyShift.Core.Time;
using EmergencyShift.Features.Enemies.Logic;
using EmergencyShift.Features.Scorpion.Logic;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class ScorpionTests
    {
        [Test]
        public void PacksAlertOnceAndHaveIndependentState()
        {
            var first = new ScorpionPackModel();
            var second = new ScorpionPackModel();
            Assert.That(new AlertScorpionPackCommand(first).Execute().Success, Is.True);
            Assert.That(new AlertScorpionPackCommand(first).Execute().Success, Is.False);
            Assert.That(second.IsAlerted, Is.False);
        }

        [Test]
        public void WaitingDoesNotMoveAndAlertIsPermanent()
        {
            var move = new Motor();
            var controller = Controller(new Clock(), move, new Melee());
            controller.Step(Vector2.zero, Vector2.up, true, 2.4f, .79f);
            Assert.That(move.Velocity, Is.EqualTo(Vector2.zero));
            controller.Alert();
            controller.Step(Vector2.zero, Vector2.up, true, 2.4f, .79f);
            Assert.That(move.Speed, Is.EqualTo(2.4f));
            controller.Step(Vector2.zero, Vector2.up, false, 2.4f, .79f);
            Assert.That(move.Velocity, Is.EqualTo(Vector2.zero));
            Assert.That(controller.Model.HasAggro, Is.True);
        }

        [Test]
        public void AttackKeepsMovingAndResolvesLongTickOnlyOnce()
        {
            var clock = new Clock();
            var motor = new Motor();
            var melee = new Melee { Ready = true };
            var controller = Controller(clock, motor, melee);
            controller.Alert();
            controller.Step(Vector2.zero, Vector2.up, true, 2.4f, .79f);
            Assert.That(melee.Begins, Is.EqualTo(1));
            Assert.That(motor.Speed, Is.EqualTo(2.4f * .79f).Within(.001));
            clock.Time = 1;
            controller.Step(Vector2.zero, Vector2.up, true, 2.4f, .79f);
            Assert.That(melee.LastProgress, Is.EqualTo(1));
            Assert.That(melee.Finishes, Is.EqualTo(1));
            melee.Ready = false;
            controller.Step(Vector2.zero, Vector2.up, true, 2.4f, .79f);
            Assert.That(melee.Finishes, Is.EqualTo(1));
            Assert.That(motor.Speed, Is.EqualTo(2.4f));
        }

        [Test]
        public void AttackWaitsForGaitBoundaryAndRejectsInvalidHeading()
        {
            var clock = new Clock();
            var melee = new Melee { Ready = true };
            var controller = Controller(clock, new Motor(), melee);
            controller.Alert();
            controller.Model.AdvanceTravel(.25f, .53125f);
            controller.Step(Vector2.zero, Vector2.up, true, 2.4f, .79f);
            Assert.That(melee.Begins, Is.Zero);
            // A neighbour blocks all further travel. The intentional gather still completes.
            controller.Model.AdvanceTravel(0, .53125f);
            clock.Time = .4;
            controller.Step(Vector2.zero, Vector2.up, true, 2.4f, .79f);
            Assert.That(melee.Begins, Is.EqualTo(1));
            Assert.That(new ScorpionCommand(controller.Model, ScorpionAction.Attack, new Vector2(float.NaN, 0)).Execute().Success, Is.False);
        }

        [Test]
        public void TargetDeathAndActorDeathInterruptWithoutLateStrike()
        {
            var clock = new Clock();
            var motor = new Motor();
            var melee = new Melee { Ready = true };
            var controller = Controller(clock, motor, melee);
            controller.Alert();
            controller.Step(Vector2.zero, Vector2.up, true, 2.4f, .79f);
            controller.Step(Vector2.zero, Vector2.up, false, 2.4f, .79f);
            Assert.That(melee.Interruptions, Is.EqualTo(1));
            Assert.That(controller.Model.State, Is.EqualTo(ScorpionState.Charging));
            Assert.That(motor.Velocity, Is.EqualTo(Vector2.zero));
            controller.Kill();
            clock.Time = 10;
            controller.Step(Vector2.zero, Vector2.up, true, 2.4f, .79f);
            Assert.That(melee.Finishes, Is.Zero);
            Assert.That(new ScorpionCommand(controller.Model, ScorpionAction.Alert).Execute().Success, Is.False);
        }

        private static ScorpionController Controller(Clock clock, Motor motor, Melee melee) =>
            new ScorpionController(new ScorpionModel(clock, .56f), motor, melee);

        [Test]
        public void SuspensionCannotResumeAnExpiredStrike()
        {
            var clock = new Clock();
            var melee = new Melee { Ready = true };
            var controller = Controller(clock, new Motor(), melee);
            controller.Alert();
            controller.Step(Vector2.zero, Vector2.up, true, 2.4f, .79f);
            controller.Suspend();
            Assert.That(controller.Model.State, Is.EqualTo(ScorpionState.Charging));
            clock.Time = 20;
            melee.Ready = false;
            controller.Step(Vector2.zero, Vector2.up, true, 2.4f, .79f);
            Assert.That(melee.Finishes, Is.Zero);
            Assert.That(melee.Interruptions, Is.EqualTo(1));
        }

        [Test]
        public void RearTargetCannotBeHitBeforeTheVisibleFacingTurns()
        {
            var model = new ScorpionModel(new Clock(), .56f, Vector2.up);
            new ScorpionCommand(model, ScorpionAction.Alert).Execute();
            Assert.That(new ScorpionCommand(model, ScorpionAction.Attack, Vector2.down).Execute().Success, Is.False);
            model.AdvanceFacing(Vector2.down, 180);
            Assert.That(new ScorpionCommand(model, ScorpionAction.Attack, Vector2.down).Execute().Success, Is.True);
            Assert.That(Vector2.Angle(model.Facing, model.Heading), Is.LessThan(.001f));
        }

        private sealed class Clock : IGameClock
        {
            public double Time { get; set; }
        }

        private sealed class Motor : IEnemyBrainMotor
        {
            public float Speed;
            public Vector2 Velocity;
            public Vector2 Follow(Vector2 goal, float speed)
            {
                Speed = speed;
                return goal.normalized * speed;
            }
            public void Move(Vector2 velocity, bool separateFromNeighbours) => Velocity = velocity;
        }

        private sealed class Melee : IEnemyBrainMelee
        {
            public bool Ready;
            public int Begins;
            public int Finishes;
            public int Interruptions;
            public float LastProgress;
            public bool CanStart(Vector2 at) => Ready;
            public void Begin() => Begins++;
            public Vector2 Step(float progress, Vector2 at, Vector2 heading, bool fixedHeading)
            {
                LastProgress = progress;
                return Vector2.one * 100; // Must never replace the requested on-the-move attack speed.
            }
            public void Finish() => Finishes++;
            public void Interrupt(float delay) => Interruptions++;
        }
    }
}
