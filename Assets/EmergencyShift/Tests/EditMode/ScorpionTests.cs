using EmergencyShift.Core.Time;
using EmergencyShift.Features.Enemies.Logic;
using EmergencyShift.Features.Scorpion;
using EmergencyShift.Features.Scorpion.Config;
using EmergencyShift.Features.Scorpion.Logic;
using NUnit.Framework;
using UnityEditor;
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
            controller.Step(Vector2.zero, Vector2.up, true, 1f);
            Assert.That(move.Velocity, Is.EqualTo(Vector2.zero));
            controller.Alert();
            controller.Step(Vector2.zero, Vector2.up, true, 1f);
            Assert.That(move.Speed, Is.EqualTo(2.4f));
            controller.Step(Vector2.zero, Vector2.up, false, 1f);
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
            controller.Step(Vector2.zero, Vector2.up, true, 1f);
            Assert.That(melee.Begins, Is.EqualTo(1));
            Assert.That(motor.Velocity.magnitude, Is.EqualTo(2.4f * .79f).Within(.001));
            clock.Time = 1;
            controller.Step(Vector2.zero, Vector2.up, true, 1f);
            Assert.That(melee.LastProgress, Is.EqualTo(1));
            Assert.That(melee.Finishes, Is.EqualTo(1));
            melee.Ready = false;
            controller.Step(Vector2.zero, Vector2.up, true, 1f);
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
            controller.Model.AdvanceTravel(.25f);
            controller.Step(Vector2.zero, Vector2.up, true, 1f);
            Assert.That(melee.Begins, Is.Zero);
            // A neighbour blocks all further travel. The intentional gather still completes.
            controller.Model.AdvanceTravel(0);
            clock.Time = .4;
            controller.Step(Vector2.zero, Vector2.up, true, 1f);
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
            controller.Step(Vector2.zero, Vector2.up, true, 1f);
            controller.Step(Vector2.zero, Vector2.up, false, 1f);
            Assert.That(melee.Interruptions, Is.EqualTo(1));
            Assert.That(controller.Model.State, Is.EqualTo(ScorpionState.Charging));
            Assert.That(motor.Velocity, Is.EqualTo(Vector2.zero));
            controller.Kill();
            clock.Time = 10;
            controller.Step(Vector2.zero, Vector2.up, true, 1f);
            Assert.That(melee.Finishes, Is.Zero);
            Assert.That(new ScorpionCommand(controller.Model, ScorpionAction.Alert).Execute().Success, Is.False);
        }

        [Test]
        public void ZeroPursuitSpeedDoesNotContaminateTheGraspVelocity()
        {
            var motor = new Motor();
            float attackReferenceSpeed = 1.8973215f;
            var settings = Settings(0, attackReferenceSpeed);
            var controller = new ScorpionController(new ScorpionModel(new Clock(), settings), motor,
                new Melee { Ready = true });
            controller.Alert();
            controller.Step(Vector2.zero, Vector2.up, true, .02f);

            Assert.That(controller.Model.State, Is.EqualTo(ScorpionState.Attacking));
            // The fake port observes the requested value; no NaN is sent to native physics.
            Assert.That(float.IsFinite(motor.Velocity.x) && float.IsFinite(motor.Velocity.y), Is.True);
            Assert.That(motor.Velocity.magnitude, Is.EqualTo(attackReferenceSpeed).Within(.0001f));
        }

        [Test]
        public void BlockedGatherUsesTheConfiguredTravelCycleRatherThanTheAttackDuration()
        {
            var clock = new Clock();
            var melee = new Melee { Ready = true };
            var settings = Settings(attackSpeed: 1.8973215f);
            var controller = new ScorpionController(new ScorpionModel(clock, settings), new Motor(), melee);
            controller.Alert();
            controller.Model.AdvanceTravel(settings.DistancePerCycle * .5f);
            controller.Step(Vector2.zero, Vector2.up, true, .02f);
            Assert.That(controller.Model.State, Is.EqualTo(ScorpionState.Gathering));

            clock.Time = settings.DistancePerCycle / settings.AttackSpeed * .5 - .0001;
            controller.Step(Vector2.zero, Vector2.up, true, .02f);
            Assert.That(controller.Model.State, Is.EqualTo(ScorpionState.Gathering));
            Assert.That(melee.Begins, Is.Zero);
            clock.Time += .0002;
            controller.Step(Vector2.zero, Vector2.up, true, .02f);
            Assert.That(controller.Model.State, Is.EqualTo(ScorpionState.Attacking));
            Assert.That(melee.Begins, Is.EqualTo(1));
        }

        [TestCase(-.02f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidTickCannotFinishOrMoveAnActiveAttack(float deltaTime)
        {
            var clock = new Clock();
            var motor = new Motor();
            var melee = new Melee { Ready = true };
            var controller = Controller(clock, motor, melee);
            controller.Alert();
            controller.Step(Vector2.zero, Vector2.up, true, .02f);
            Vector2 before = motor.Velocity;
            clock.Time = 1;
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                controller.Step(Vector2.zero, Vector2.up, true, deltaTime));
            Assert.That(controller.Model.State, Is.EqualTo(ScorpionState.Attacking));
            Assert.That(motor.Velocity, Is.EqualTo(before));
            Assert.That(melee.Finishes, Is.Zero);
            Assert.That(melee.LastProgress, Is.Zero);
        }

        [Test]
        public void SharedSettingsDoNotShareGaitAggroOrDeath()
        {
            var settings = Settings();
            var first = new ScorpionModel(new Clock(), settings);
            var second = new ScorpionModel(new Clock(), settings);
            new ScorpionCommand(first, ScorpionAction.Alert).Execute();
            first.AdvanceTravel(settings.DistancePerCycle * .5f);
            Assert.That(first.GaitPhase, Is.EqualTo(.5f));
            Assert.That(second.GaitPhase, Is.Zero);
            Assert.That(second.HasAggro, Is.False);
            new ScorpionCommand(first, ScorpionAction.Kill).Execute();
            Assert.That(first.IsAlive, Is.False);
            Assert.That(second.IsAlive, Is.True);
            Assert.That(second.Settings, Is.SameAs(settings));
        }

        [Test]
        public void FactorySnapshotsAuthoredMotionAndCombatWithoutChangingEitherAsset()
        {
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
                "Assets/EmergencyShift/Data/Enemies/Scorpion/Definition.asset");
            var presentation = AssetDatabase.LoadAssetAtPath<ScorpionPresentationSet>(
                "Assets/EmergencyShift/Data/Enemies/Scorpion/Presentation.asset");
            string definitionBefore = JsonUtility.ToJson(definition);
            string presentationBefore = JsonUtility.ToJson(presentation);
            var factory = new ScorpionControllerFactory(new Clock());
            ScorpionController controller = factory.Create(definition, presentation, new Motor(), new Melee(), Vector2.right);
            ScorpionSettings settings = controller.Model.Settings;
            Assert.That(settings.PursuitSpeed, Is.EqualTo(definition.PursuitSpeed));
            Assert.That(settings.AttackSpeed, Is.EqualTo(presentation.Attack.ReferenceSpeed));
            Assert.That(settings.AttackSeconds, Is.EqualTo(definition.AttackDuration));
            Assert.That(settings.DistancePerCycle, Is.EqualTo(presentation.DistancePerCycle));
            Assert.That(settings.GatherCycleSeconds, Is.EqualTo(presentation.DistancePerCycle / presentation.Attack.ReferenceSpeed));
            Assert.That(settings.TurnDegrees, Is.EqualTo(presentation.TurnDegrees));
            Assert.That(settings.Acceleration, Is.EqualTo(presentation.Acceleration));
            Assert.That(settings.Braking, Is.EqualTo(presentation.Braking));
            Assert.That(controller.Model.Facing, Is.EqualTo(Vector2.right));
            Assert.That(JsonUtility.ToJson(definition), Is.EqualTo(definitionBefore));
            Assert.That(JsonUtility.ToJson(presentation), Is.EqualTo(presentationBefore));
        }

        [Test]
        public void SuspensionCannotResumeAnExpiredStrike()
        {
            var clock = new Clock();
            var melee = new Melee { Ready = true };
            var controller = Controller(clock, new Motor(), melee);
            controller.Alert();
            controller.Step(Vector2.zero, Vector2.up, true, 1f);
            controller.Suspend();
            Assert.That(controller.Model.State, Is.EqualTo(ScorpionState.Charging));
            clock.Time = 20;
            melee.Ready = false;
            controller.Step(Vector2.zero, Vector2.up, true, 1f);
            Assert.That(melee.Finishes, Is.Zero);
            Assert.That(melee.Interruptions, Is.EqualTo(1));
        }

        [Test]
        public void RearTargetCannotBeHitBeforeTheVisibleFacingTurns()
        {
            var model = new ScorpionModel(new Clock(), Settings(), Vector2.up);
            new ScorpionCommand(model, ScorpionAction.Alert).Execute();
            Assert.That(new ScorpionCommand(model, ScorpionAction.Attack, Vector2.down).Execute().Success, Is.False);
            model.AdvanceFacing(Vector2.down, 180);
            Assert.That(new ScorpionCommand(model, ScorpionAction.Attack, Vector2.down).Execute().Success, Is.True);
            Assert.That(Vector2.Angle(model.Facing, model.Heading), Is.LessThan(.001f));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void StoppedLocomotionRestartsAcceleration(bool suspended)
        {
            var clock = new Clock();
            var motor = new Motor();
            var controller = Controller(clock, motor, new Melee());
            controller.Alert();
            for (int i = 0; i < 20; i++)
            {
                MotionStep(controller, Vector2.up);
            }
            Assert.That(motor.Velocity.magnitude, Is.EqualTo(2.4f).Within(.001f));
            if (suspended)
            {
                controller.Suspend();
            }
            else
            {
                MotionStep(controller, Vector2.up, false);
            }
            Assert.That(motor.Velocity, Is.EqualTo(Vector2.zero));
            clock.Time = 20;
            MotionStep(controller, Vector2.up);
            Assert.That(motor.Velocity.magnitude, Is.EqualTo(.18f).Within(.001f));
            Assert.That(controller.Model.HasAggro, Is.True);
        }

        [Test]
        public void ReversingRouteBrakesAlongTheTurningBodyWithoutStrafing()
        {
            var model = new ScorpionModel(new Clock(), Settings());
            new ScorpionCommand(model, ScorpionAction.Alert).Execute();
            for (int i = 0; i < 20; i++)
            {
                model.Steer(Vector2.up * 2.4f, .02f);
            }
            Vector2 before = model.Facing;
            Vector2 velocity = model.Steer(Vector2.down * 2.4f, .02f);
            Assert.That(Vector2.Angle(before, model.Facing), Is.LessThanOrEqualTo(7.201f));
            Assert.That(Vector2.Angle(model.Facing, velocity), Is.LessThan(.05f));
            Assert.That(velocity.magnitude, Is.LessThanOrEqualTo(2.041f));
        }

        [Test]
        public void LocomotionFacesTheResolvedRouteRatherThanTheTargetAcrossTheWall()
        {
            var motor = new Motor { RouteDirection = Vector2.right };
            var controller = Controller(new Clock(), motor, new Melee());
            controller.Alert();
            controller.Step(Vector2.zero, Vector2.up, true, 1f);
            Assert.That(Vector2.Angle(controller.Model.Facing, Vector2.right), Is.LessThan(.001f));
            Assert.That(Vector2.Angle(motor.Velocity, Vector2.right), Is.LessThan(.001f));
        }

        [Test]
        public void GraspKeepsItsVisibleHeadingWhenTargetMovesAcrossIt()
        {
            var clock = new Clock();
            var motor = new Motor();
            var melee = new Melee { Ready = true };
            var controller = Controller(clock, motor, melee);
            controller.Alert();
            Vector2 nearHeading = Quaternion.Euler(0, 0, 10) * Vector2.up;
            MotionStep(controller, nearHeading);
            Assert.That(melee.Begins, Is.EqualTo(1));
            Vector2 committed = controller.Model.Facing;
            Assert.That(Vector2.Angle(controller.Model.Heading, committed), Is.LessThan(.001f));
            clock.Time = .2;
            MotionStep(controller, Vector2.left * 4);
            Assert.That(controller.Model.State, Is.EqualTo(ScorpionState.Attacking));
            Assert.That(Vector2.Angle(controller.Model.Facing, committed), Is.LessThan(.001f));
            Assert.That(Vector2.Angle(motor.Velocity, committed), Is.LessThan(.001f));
            Assert.That(Vector2.Angle(melee.LastHeading, committed), Is.LessThan(.001f));
            Assert.That(motor.Velocity.magnitude, Is.EqualTo(2.4f * .79f).Within(.001f));
            Assert.That(motor.Follows, Is.Zero);
        }

        [Test]
        public void RecoveryFromAnImmediateGraspContinuesMovingWithoutASecondStartFromRest()
        {
            var clock = new Clock();
            var motor = new Motor();
            var melee = new Melee { Ready = true };
            var controller = Controller(clock, motor, melee);
            controller.Alert();
            MotionStep(controller, Vector2.up);
            float attackSpeed = motor.Velocity.magnitude;
            melee.Ready = false;
            clock.Time = .57;
            MotionStep(controller, Vector2.up);
            Assert.That(melee.Finishes, Is.EqualTo(1));
            Assert.That(motor.Velocity.magnitude, Is.GreaterThanOrEqualTo(attackSpeed - .001f));
            Assert.That(motor.Velocity.magnitude, Is.LessThanOrEqualTo(attackSpeed + .181f));
        }

        private static ScorpionSettings Settings(float pursuitSpeed = 2.4f, float attackSpeed = 2.4f * .79f) =>
            new ScorpionSettings(pursuitSpeed, attackSpeed, .56f, .53125f, 360, 9, 18);

        private static ScorpionController Controller(Clock clock, Motor motor, Melee melee) =>
            new ScorpionController(new ScorpionModel(clock, Settings()), motor, melee);

        private static void MotionStep(ScorpionController controller, Vector2 target, bool targetAlive = true) =>
            controller.Step(Vector2.zero, target, targetAlive, .02f);

        private sealed class Clock : IGameClock
        {
            public double Time { get; set; }
        }

        private sealed class Motor : IEnemyBrainMotor
        {
            public float Speed;
            public Vector2 Velocity;
            public Vector2? RouteDirection;
            public int Follows;
            public Vector2 Follow(Vector2 goal, float speed)
            {
                Speed = speed;
                Follows++;
                return (RouteDirection ?? goal.normalized) * speed;
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
            public Vector2 LastHeading;
            public bool CanStart(Vector2 at) => Ready;
            public void Begin() => Begins++;
            public Vector2 Step(float progress, Vector2 at, Vector2 heading, bool fixedHeading)
            {
                LastProgress = progress;
                LastHeading = heading;
                return Vector2.one * 100; // Must never replace the requested on-the-move attack speed.
            }
            public void Finish() => Finishes++;
            public void Interrupt(float delay) => Interruptions++;
        }
    }
}
