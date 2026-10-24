using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using EmergencyShift.Features.ScriptedEncounters.Config;
using EmergencyShift.Features.ScriptedEncounters.Logic;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class ScriptedRouteTests
    {
        [Test]
        public void OnlyFirstRealLightContactStartsTheRoute()
        {
            var route = Route(reaction: .12f);
            Assert.That(new StartScriptedRouteCommand(route, false).Execute().Success, Is.False);
            route.Step(1, Vector2.zero, true);
            Assert.That(route.State, Is.EqualTo(ScriptedRouteState.WaitingForLight));
            Assert.That(new StartScriptedRouteCommand(route, true).Execute().Success, Is.True);
            route.Step(.02f, Vector2.zero, true);
            Assert.That(route.State, Is.EqualTo(ScriptedRouteState.Reacting));
            Assert.That(route.Closure, Is.GreaterThan(0));
            route.Step(.12f, Vector2.zero, true);
            Assert.That(route.State, Is.EqualTo(ScriptedRouteState.Moving));
            Assert.That(new StartScriptedRouteCommand(route, true).Execute().Success, Is.False);
        }

        [Test]
        public void OrderedMarkersCompleteOnceAndReleaseOwnership()
        {
            var points = new[] { Vector2.right, Vector2.one };
            var route = new ScriptedRouteModel(Settings(), points);
            points[0] = Vector2.left * 10;
            Assert.That(route.Goal, Is.EqualTo(Vector2.right), "Model owns a copy of authored route values.");
            new StartScriptedRouteCommand(route, true).Execute();
            route.Step(.02f, Vector2.one, true);
            Assert.That(route.PointIndex, Is.Zero, "The final marker cannot bypass the first.");
            route.Step(.02f, Vector2.right, true);
            Assert.That(route.PointIndex, Is.EqualTo(1));
            Assert.That(route.ControlsActor, Is.True);
            route.Step(.02f, Vector2.one, true);
            Assert.That(route.State, Is.EqualTo(ScriptedRouteState.Completed));
            Assert.That(route.ControlsActor, Is.False);
            Assert.That(new StartScriptedRouteCommand(route, true).Execute().Success, Is.False);
        }

        [Test]
        public void BlockedRouteKeepsControlAndRecoversWhenProgressResumes()
        {
            var route = Route();
            new StartScriptedRouteCommand(route, true).Execute();
            route.Step(.02f, Vector2.zero, true);
            route.Step(3.1f, Vector2.zero, true);
            Assert.That(route.State, Is.EqualTo(ScriptedRouteState.Blocked));
            Assert.That(route.ControlsActor, Is.True);
            route.Step(.02f, Vector2.right * .2f, true);
            Assert.That(route.State, Is.EqualTo(ScriptedRouteState.Moving));
            Assert.That(route.PointIndex, Is.Zero);
        }

        [Test]
        public void StaggerPausesProgressAndInvalidFramesCannotCompleteTheRoute()
        {
            var route = Route();
            new StartScriptedRouteCommand(route, true).Execute();
            route.Step(10, Vector2.right, false);
            route.Step(float.NaN, Vector2.right, true);
            route.Step(.02f, new Vector2(float.NaN, 0), true);
            Assert.That(route.State, Is.EqualTo(ScriptedRouteState.Moving));
            Assert.That(route.PointIndex, Is.Zero);
        }

        [Test]
        public void ControllerKeepsMovingAfterLightIsSwitchedOffAndStopsOnArrival()
        {
            var motor = new Motor();
            var route = Route();
            var controller = new ScriptedRouteController(route, motor);
            controller.Step(.02f, Vector2.zero, true, true, true, Vector2.zero);
            Assert.That(motor.Velocity.magnitude, Is.EqualTo(3.8f).Within(.001f));
            controller.Step(.02f, Vector2.right * .5f, false, true, true, Vector2.zero);
            Assert.That(motor.Follows, Is.EqualTo(2));
            controller.Step(.02f, Vector2.right, false, true, true, Vector2.zero);
            Assert.That(route.ControlsActor, Is.False);
            Assert.That(motor.Velocity, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void TargetDeathAbortsInsteadOfCompletingOrAttacking()
        {
            var motor = new Motor();
            var route = Route();
            var controller = new ScriptedRouteController(route, motor);
            controller.Step(.02f, Vector2.zero, true, false, true, Vector2.zero);
            Assert.That(route.State, Is.EqualTo(ScriptedRouteState.Aborted));
            Assert.That(motor.Velocity, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void DifferentInstancesNeverShareProgressOrTimers()
        {
            var first = Route();
            var second = Route();
            new StartScriptedRouteCommand(first, true).Execute();
            first.Step(.02f, Vector2.right, true);
            Assert.That(first.State, Is.EqualTo(ScriptedRouteState.Completed));
            Assert.That(second.State, Is.EqualTo(ScriptedRouteState.WaitingForLight));
        }

        [Test]
        public void ScriptedOwnershipGuardsDetectionExternalAggroAttackAndBulletStagger()
        {
            var actor = Stalker();
            Assert.That(new SetStalkerScriptedControlCommand(actor, true).Execute().Success, Is.True);
            actor.Step(Frame());
            Assert.That(actor.HasAggro, Is.False);
            Assert.That(new ActivateStalkerAggroCommand(actor).Execute().Success, Is.False);
            Assert.That(new BeginStalkerAttackCommand(actor, Vector2.right).Execute().Success, Is.False);
            Assert.That(new StaggerStalkerCommand(actor, Vector2.left, true).Execute().Success, Is.True);
            Assert.That(actor.IsStaggered, Is.True);
            Assert.That(actor.HasAggro, Is.False);
            Assert.That(actor.State, Is.EqualTo(StalkerState.Dormant));
        }

        [Test]
        public void ArrivalReleaseDoesNotForceAggroButNextNormalObservationCanDetect()
        {
            var actor = Stalker();
            new SetStalkerScriptedControlCommand(actor, true).Execute();
            new SetStalkerScriptedControlCommand(actor, false).Execute();
            Assert.That(actor.State, Is.EqualTo(StalkerState.Dormant));
            Assert.That(actor.HasAggro, Is.False);
            actor.Step(Frame());
            Assert.That(actor.HasAggro, Is.True);
            Assert.That(new SetStalkerScriptedControlCommand(actor, true).Execute().Success, Is.False,
                "A scenario cannot erase permanent normal aggro.");
        }

        [Test]
        public void DeathWinsOverOwnershipAndCannotBeReplacedByDormant()
        {
            var actor = Stalker();
            new SetStalkerScriptedControlCommand(actor, true).Execute();
            new KillStalkerCommand(actor).Execute();
            Assert.That(actor.IsScripted, Is.False);
            Assert.That(new SetStalkerScriptedControlCommand(actor, false).Execute().Success, Is.False);
            Assert.That(actor.State, Is.EqualTo(StalkerState.Dead));
        }

        [Test]
        public void InvalidSettingsAndMissingOrInvalidPointsAreExplicitErrors()
        {
            Assert.Throws<ArgumentException>(() => new ScriptedRouteSettings(0, 0, .16f, 3, .03f));
            Assert.Throws<ArgumentException>(() => new ScriptedRouteSettings(3.8f, -1, .16f, 3, .03f));
            Assert.Throws<ArgumentException>(() => new ScriptedRouteModel(Settings(), Array.Empty<Vector2>()));
            Assert.Throws<ArgumentException>(() => new ScriptedRouteModel(Settings(), new[] { new Vector2(float.PositiveInfinity, 0) }));
        }

        private static ScriptedRouteSettings Settings(float reaction = 0) => new ScriptedRouteSettings(3.8f, reaction, .16f, 3, .03f);
        private static ScriptedRouteModel Route(float reaction = 0) => new ScriptedRouteModel(Settings(reaction), new[] { Vector2.right });
        private static StalkerModel Stalker() => new StalkerModel(new StalkerSettings(1.5f, 3.8f, 1.05f, .35f,
            1.05f, 3, 3.2f, .45f, .8f, .09f, 1.6f, .14f, 1.25f, 1.2f, .35f), new Clock(), Vector2.zero);
        private static StalkerObservation Frame() => new StalkerObservation(.02f, Vector2.zero, Vector2.zero,
            Vector2.right, Vector2.right, Vector2.left, true, true, true, true, false, true, 3.2f, .3f, 256);

        private sealed class Clock : IGameClock
        {
            public double Time => 0;
        }

        private sealed class Motor : IEnemyBrainMotor
        {
            public Vector2 Velocity;
            public int Follows;
            public Vector2 Follow(Vector2 goal, float speed)
            {
                Follows++;
                return goal.normalized * speed;
            }

            public void Move(Vector2 velocity, bool separateFromNeighbours) => Velocity = velocity;
        }
    }
}
