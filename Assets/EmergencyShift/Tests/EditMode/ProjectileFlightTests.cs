using System;
using EmergencyShift.Features.Projectiles;
using EmergencyShift.Features.Projectiles.Config;
using EmergencyShift.Features.Projectiles.Logic;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class ProjectileFlightTests
    {
        [Test]
        public void TravelIsClampedToRangeAndReleasesOnFollowingStep()
        {
            var environment = new Environment();
            ProjectileFlightController controller = Create(environment, range: 1);
            controller.Step(.15f);
            Assert.That(controller.Model.Distance, Is.EqualTo(.6f).Within(.0001f));
            controller.Step(.15f);
            Assert.That(environment.LastSweep, Is.EqualTo(.4f).Within(.0001f));
            Assert.That(controller.Model.Position, Is.EqualTo(Vector2.right));
            Assert.That(environment.Releases, Is.Zero);
            controller.Step(.02f);
            controller.Step(.02f);
            Assert.That(environment.Releases, Is.EqualTo(1));
            Assert.That(environment.Sweeps, Is.EqualTo(2));
        }

        [Test]
        public void LifetimePreservesStrictGreaterThanBoundary()
        {
            var environment = new Environment();
            ProjectileFlightController controller = Create(environment, range: 100, life: 1);
            controller.Step(1);
            Assert.That(environment.Sweeps, Is.EqualTo(1));
            controller.Step(.001f);
            Assert.That(environment.Releases, Is.EqualTo(1));
            Assert.That(environment.Sweeps, Is.EqualTo(1));
        }

        [Test]
        public void CollisionCompletesBeforeCallbackAndCannotHitTwice()
        {
            var environment = new Environment { Hit = true };
            ProjectileFlightController controller = Create(environment);
            environment.OnImpact = () => controller.Step(.02f);
            controller.Step(.02f);
            controller.Step(.02f);
            Assert.That(environment.Impacts, Is.EqualTo(1));
            Assert.That(environment.Damage, Is.EqualTo(2));
            Assert.That(environment.Releases, Is.EqualTo(1));
            Assert.That(controller.Model.Distance, Is.Zero);
        }

        [Test]
        public void ImpactCallbackFailureStillReleasesAndCannotRepeatDamage()
        {
            var environment = new Environment { Hit = true, OnImpact = () => throw new InvalidOperationException("receiver") };
            ProjectileFlightController controller = Create(environment);
            Assert.Throws<InvalidOperationException>(() => controller.Step(.02f));
            controller.Step(.02f);
            Assert.That(environment.Impacts, Is.EqualTo(1));
            Assert.That(environment.Releases, Is.EqualTo(1));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-1)]
        public void InvalidStepDoesNotChangeStateOrQueryEnvironment(float step)
        {
            var environment = new Environment();
            ProjectileFlightController controller = Create(environment);
            Assert.That(controller.Step(step).Success, Is.False);
            Assert.That(controller.Model.Age, Is.Zero);
            Assert.That(controller.Model.Position, Is.EqualTo(Vector2.zero));
            Assert.That(environment.Sweeps, Is.Zero);
        }

        [Test]
        public void SharedSettingsDoNotShareFlightState()
        {
            var factory = new ProjectileFlightControllerFactory();
            var settings = new ProjectileFlightSettings(4, 5, 2, .035f, 3);
            ProjectileFlightController a = factory.Create(settings, Vector2.zero, Vector2.right, new Environment());
            ProjectileFlightController b = factory.Create(settings, Vector2.up, Vector2.up, new Environment());
            a.Step(.1f);
            Assert.That(b.Model.Distance, Is.Zero);
            Assert.That(b.Model.Position, Is.EqualTo(Vector2.up));
            Assert.That(b.Model.Direction, Is.EqualTo(Vector2.up));
        }

        [Test]
        public void RepeatedFlightStepsDoNotAllocateManagedObjects()
        {
            var environment = new Environment();
            ProjectileFlightController controller = Create(environment, range: 1000, life: 1000);
            controller.Step(.001f);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                controller.Step(.001f);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }

        [Test]
        public void LaunchRechecksAfterCanExecuteAndRejectsInvalidGeometry()
        {
            var model = new ProjectileFlightModel(new ProjectileFlightSettings(4, 5, 2, .035f, 3));
            var launch = new LaunchProjectileCommand(model, Vector2.zero, Vector2.right);
            Assert.That(launch.CanExecute().Success, Is.True);
            Assert.That(launch.Execute().Success, Is.True);
            Assert.That(launch.Execute().Success, Is.False);
            var unused = new ProjectileFlightModel(model.Settings);
            Assert.That(new LaunchProjectileCommand(unused, new Vector2(float.NaN, 0), Vector2.right).Execute().Success, Is.False);
            Assert.That(unused.HasLaunched, Is.False);
        }

        [TestCase("speed")]
        [TestCase("range")]
        [TestCase("damage")]
        [TestCase("radius")]
        [TestCase("age")]
        public void InvalidTuningCannotCreateFlight(string fault)
        {
            Assert.Throws<ArgumentException>(() => new ProjectileFlightSettings(fault == "speed" ? float.NaN : 4,
                fault == "range" ? float.PositiveInfinity : 5, fault == "damage" ? 0 : 2,
                fault == "radius" ? -1 : .035f, fault == "age" ? 0 : 3));
        }

        [Test]
        public void FlightSnapshotRetainsLegacyProfileDefaultsAndMinimumRangePolicy()
        {
            var profile = ScriptableObject.CreateInstance<ProjectileVisualProfile>();
            try
            {
                ProjectileFlightSettings settings = profile.ToFlightSettings(-1, 2);
                Assert.That(settings.Speed, Is.EqualTo(44));
                Assert.That(settings.Radius, Is.EqualTo(.035f));
                Assert.That(settings.MaximumAge, Is.EqualTo(3));
                Assert.That(settings.Range, Is.EqualTo(.1f));
                profile.bulletSpeed = 10;
                Assert.That(settings.Speed, Is.EqualTo(44));
                Assert.Throws<ArgumentException>(() => profile.ToFlightSettings(float.NaN, 2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        private static ProjectileFlightController Create(Environment environment, float range = 5, float life = 3) =>
            new ProjectileFlightControllerFactory().Create(new ProjectileFlightSettings(4, range, 2, .035f, life), Vector2.zero, Vector2.right, environment);

        private sealed class Environment : IProjectileFlightEnvironment
        {
            public bool Hit;
            public Action OnImpact;
            public int Sweeps;
            public int Impacts;
            public int Damage;
            public int Releases;
            public float LastSweep;

            public bool Sweep(Vector2 origin, Vector2 direction, float radius, float distance)
            {
                Sweeps++;
                LastSweep = distance;
                return Hit;
            }

            public void Move(Vector2 position) { }
            public void Impact(int damage, Vector2 direction)
            {
                Impacts++;
                Damage += damage;
                OnImpact?.Invoke();
            }

            public void Release() => Releases++;
        }
    }
}
