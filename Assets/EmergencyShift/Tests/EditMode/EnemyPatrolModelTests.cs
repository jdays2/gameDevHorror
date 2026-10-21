using System;
using System.IO;
using EmergencyShift.Core.Randomness;
using EmergencyShift.Features.Enemies;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class EnemyPatrolModelTests
    {
        [Test]
        public void DisabledFrozenAndTooSmallAreaConsumeNoRandomOrGeometry()
        {
            var random = new Random();
            var environment = new Environment();
            EnemyPatrolController route = Create(random, environment);
            route.Step(Frame(enabled: false));
            route.Step(Frame(delta: 0));
            route.Step(new EnemyPatrolStep(true, Vector2.zero, .3f,
                new EnemyPatrolBounds(true, Vector2.zero, Vector2.one * .2f), 256, .02f));
            Assert.That(random.Calls, Is.Zero);
            Assert.That(environment.Calls, Is.Zero);
            Assert.That(route.HasTarget, Is.False);
        }

        [Test]
        public void FailedSearchIsBoundedAndRetriesAfterPause()
        {
            var random = new Random();
            var environment = new Environment { Blocked = true };
            EnemyPatrolController route = Create(random, environment);
            Assert.That(route.Step(Frame()), Is.EqualTo(Vector2.zero));
            Assert.That(environment.Calls, Is.EqualTo(12));
            Assert.That(random.Calls, Is.EqualTo(24));
            Assert.That(route.Capture().Pause, Is.EqualTo(.4f));
            route.Step(Frame(delta: .4f));
            Assert.That(environment.Calls, Is.EqualTo(12));
            route.Step(Frame());
            Assert.That(environment.Calls, Is.EqualTo(24));
        }

        [Test]
        public void BoundedTargetHonorsBodyPaddingAndVelocityLimit()
        {
            var random = new Random { Unit = 1 };
            EnemyPatrolController route = Create(random, new Environment());
            Vector2 velocity = route.Step(Frame());
            Assert.That(route.Target.x, Is.EqualTo(1.16f).Within(.00001f));
            Assert.That(route.Target.y, Is.EqualTo(1.16f).Within(.00001f));
            Assert.That(velocity.magnitude, Is.EqualTo(.6f).Within(.00001f));
            Assert.That(route.Capture().Origin, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void UnboundedWanderUsesTheSameAuthoredDiskRadius()
        {
            EnemyPatrolController route = Create(new Random { Unit = .81 }, new Environment());
            route.Step(new EnemyPatrolStep(true, Vector2.zero, .3f,
                new EnemyPatrolBounds(false, Vector2.zero, Vector2.one), 256, .02f));
            Assert.That(route.Target.magnitude, Is.EqualTo(1.44f).Within(.00001f));
        }

        [Test]
        public void ArrivalAndStallPauseWithoutTeleportingOrKeepingAnActiveTarget()
        {
            EnemyPatrolController route = Create(new Random(), new Environment());
            route.Restore(new EnemyPatrolState(Vector2.zero, true, Vector2.right, 0, 0, 1));
            Assert.That(route.Step(Frame(position: Vector2.right)), Is.EqualTo(Vector2.zero));
            Assert.That(route.IsPausing, Is.True);
            Assert.That(route.HasTarget, Is.False);
            route.Restore(new EnemyPatrolState(Vector2.zero, true, Vector2.right, 0, .89f, 1));
            Assert.That(route.Step(Frame()), Is.EqualTo(Vector2.zero));
            Assert.That(route.IsPausing, Is.True);
            Assert.That(route.HasTarget, Is.False);
        }

        [Test]
        public void NextStepCollisionPausesEvenAfterAClearCandidateSearch()
        {
            var environment = new Environment { BlockAfterFirst = true };
            EnemyPatrolController route = Create(new Random(), environment);
            Assert.That(route.Step(Frame()), Is.EqualTo(Vector2.zero));
            Assert.That(environment.Calls, Is.EqualTo(2));
            Assert.That(environment.LastDistance, Is.EqualTo(.052f).Within(.00001f));
            Assert.That(route.IsPausing, Is.True);
        }

        [Test]
        public void RestoreAndResetPreserveOriginAndDoNotConsumeRandom()
        {
            var random = new Random();
            EnemyPatrolController route = Create(random, new Environment());
            var saved = new EnemyPatrolState(new Vector2(3, 4), true, new Vector2(4, 4), .2f, .1f, .5f);
            Assert.That(route.Restore(saved).Success, Is.True);
            Assert.That(route.Capture(), Is.EqualTo(saved));
            route.Reset();
            Assert.That(route.Origin, Is.EqualTo(saved.Origin));
            Assert.That(route.Target, Is.EqualTo(saved.Target));
            Assert.That(route.Capture().PreviousDistance, Is.EqualTo(saved.PreviousDistance));
            Assert.That(route.HasTarget || route.IsPausing, Is.False);
            Assert.That(random.Calls, Is.Zero);
        }

        [TestCase(float.NaN, 0, 0)]
        [TestCase(0, -1, 0)]
        [TestCase(0, 0, float.PositiveInfinity)]
        public void MalformedRestoreIsRejectedAtomically(float origin, float pause, float previousDistance)
        {
            EnemyPatrolController route = Create(new Random(), new Environment());
            route.Step(Frame());
            EnemyPatrolState before = route.Capture();
            var invalid = new EnemyPatrolState(new Vector2(origin, 0), false, Vector2.zero, pause, 0, previousDistance);
            Assert.That(route.Restore(invalid).Success, Is.False);
            Assert.That(route.Capture(), Is.EqualTo(before));
        }

        [Test]
        public void FactoryGivesEachActorItsOwnRouteAndRandomStream()
        {
            var firstFactory = new EnemyPatrolControllerFactory(new RandomSourceFactory(new System.Random(17)));
            var secondFactory = new EnemyPatrolControllerFactory(new RandomSourceFactory(new System.Random(17)));
            EnemyPatrolController first = firstFactory.Create(Settings(), new Environment());
            EnemyPatrolController other = firstFactory.Create(Settings(), new Environment());
            EnemyPatrolController expected = secondFactory.Create(Settings(), new Environment());
            first.Initialize(Vector2.zero);
            other.Initialize(Vector2.zero);
            expected.Initialize(Vector2.zero);
            for (int i = 0; i < 5; i++)
            {
                other.Reset();
                other.Step(Frame());
                UnityEngine.Random.value.ToString();
            }

            first.Step(Frame());
            expected.Step(Frame());
            Assert.That(first.Target, Is.EqualTo(expected.Target));
            first.Reset();
            Assert.That(other.HasTarget, Is.True);
        }

        [TestCase(float.NaN, .6f, 1.8f, 12)]
        [TestCase(.6f, 2, 1, 12)]
        [TestCase(.6f, .6f, 1.8f, 0)]
        [TestCase(.6f, .6f, 1.8f, 65)]
        public void InvalidSettingsFailBeforeAnActorCanMove(float speed, float pauseMin, float pauseMax, int attempts)
        {
            Assert.Throws<ArgumentException>(() => new EnemyPatrolSettings(speed, 1.6f,
                new Vector2(pauseMin, pauseMax), attempts, .04f, .12f, .3f, .4f, .9f, .15f));
        }

        [Test]
        public void AuthoredProfilesLoadDefaultsWithoutMutatingTheirAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:EnemyPatrolProfile", new[] { "Assets/EmergencyShift" });
            Assert.That(guids, Is.Not.Empty);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                byte[] before = File.ReadAllBytes(path);
                var profile = AssetDatabase.LoadAssetAtPath<EnemyPatrolProfile>(path);
                EnemyPatrolSettings settings = profile.ToSettings();
                Assert.That(settings.Attempts, Is.EqualTo(12));
                Assert.That(settings.Skin, Is.EqualTo(.04f));
                Assert.That(settings.Speed, Is.EqualTo(profile.speed));
                CollectionAssert.AreEqual(before, File.ReadAllBytes(path), path);
            }
        }

        private static EnemyPatrolSettings Settings() => new EnemyPatrolSettings(.6f, 1.6f,
            new Vector2(.6f, 1.8f), 12, .04f, .12f, .3f, .4f, .9f, .15f);

        private static EnemyPatrolController Create(Random random, Environment environment)
        {
            var route = new EnemyPatrolController(new EnemyPatrolModel(Settings(), random, environment));
            route.Initialize(Vector2.zero);
            return route;
        }

        private static EnemyPatrolStep Frame(bool enabled = true, float delta = .02f, Vector2 position = default) =>
            new EnemyPatrolStep(enabled, position, .3f, new EnemyPatrolBounds(true, Vector2.zero, Vector2.one * 3), 256, delta);

        private sealed class Random : IRandomSource
        {
            public double Unit = .9;
            public int Calls;
            public int NextInt(int min, int max) => min;
            public double NextUnit()
            {
                Calls++;
                return Unit;
            }
        }

        private sealed class Environment : IEnemyPatrolEnvironment
        {
            public int Calls;
            public bool Blocked;
            public bool BlockAfterFirst;
            public float LastDistance;
            public bool IsBlocked(Vector2 position, float radius, Vector2 direction, float distance, int solids)
            {
                Calls++;
                LastDistance = distance;
                return Blocked || (BlockAfterFirst && Calls > 1);
            }
        }
    }
}
