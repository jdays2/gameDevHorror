using System.Collections.Generic;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Health.Config;
using EmergencyShift.Features.Health.Logic;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class PlayerLifeControllerTests
    {
        [Test]
        public void ProtectionAndDeathAreAuthoritativeBeforeNativeFeedback()
        {
            var clock = new Clock();
            var model = new HealthModel(new HealthSettings(3, .7), clock);
            var ports = new Ports();
            var controller = new PlayerLifeController(model, ports, ports);
            Assert.That(controller.TakeDamage(1, Vector2.right).Success, Is.True);
            Assert.That(ports.Trace, Is.EqualTo(new[] { "damage:1:False", "health:2:False" }));
            Assert.That(controller.TakeDamage(2, Vector2.right).Success, Is.False);
            Assert.That(ports.Trace.Count, Is.EqualTo(2));
            clock.Time = .7;
            Assert.That(controller.TakeDamage(2, Vector2.up).Success, Is.True);
            Assert.That(controller.Health, Is.Zero);
            Assert.That(ports.Trace, Is.EqualTo(new[] { "damage:1:False", "health:2:False", "defeat:True", "damage:2:True", "health:0:False" }));
            Assert.That(controller.TakeDamage(1, Vector2.zero).Success, Is.False);
            Assert.That(ports.Trace.Count, Is.EqualTo(5));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void RejectedDamageDoesNotReachTheEnvironmentOrView(int amount)
        {
            var ports = new Ports();
            PlayerLifeController controller = Create(ports);
            Assert.That(controller.TakeDamage(amount, Vector2.zero).Success, Is.False);
            Assert.That(controller.Health, Is.EqualTo(3));
            Assert.That(ports.Trace, Is.Empty);
        }

        [TestCase(float.NaN, 0)]
        [TestCase(0, float.PositiveInfinity)]
        public void InvalidDirectionCannotPartiallyDamageHealth(float x, float y)
        {
            var ports = new Ports();
            PlayerLifeController controller = Create(ports);
            Assert.That(controller.TakeDamage(1, new Vector2(x, y)).Success, Is.False);
            Assert.That(controller.Health, Is.EqualTo(3));
            Assert.That(ports.Trace, Is.Empty);
        }

        [TestCase(0, 1)]
        [TestCase(2, 2)]
        [TestCase(20, 3)]
        public void TrustedRestoreRevivesSilentlyWithTheExistingClamp(int requested, int expected)
        {
            var ports = new Ports();
            PlayerLifeController controller = Create(ports);
            controller.TakeDamage(999, Vector2.zero);
            ports.Trace.Clear();
            Assert.That(controller.Restore(requested).Success, Is.True);
            Assert.That(controller.Health, Is.EqualTo(expected));
            Assert.That(controller.IsDead, Is.False);
            Assert.That(ports.Trace, Is.EqualTo(new[] { "defeat:False", "health:" + expected + ":True" }));
            Assert.That(controller.TakeDamage(1, Vector2.zero).Success, Is.True, "Restoration clears hit protection.");
        }

        [Test]
        public void CapacityIsPersonalAndCannotReviveTheDead()
        {
            var ports = new Ports();
            PlayerLifeController first = Create(ports);
            PlayerLifeController second = Create(new Ports());
            Assert.That(first.SetCapacity(20, 15).Success, Is.True);
            Assert.That(first.MaximumHealth, Is.EqualTo(20));
            Assert.That(first.Health, Is.EqualTo(15));
            Assert.That(second.MaximumHealth, Is.EqualTo(3));
            Assert.That(second.Health, Is.EqualTo(3));
            Assert.That(ports.Trace, Is.EqualTo(new[] { "health:15:False" }));
            first.TakeDamage(999, Vector2.zero);
            ports.Trace.Clear();
            Assert.That(first.SetCapacity(20, 20).Success, Is.False);
            Assert.That(first.IsDead, Is.True);
            Assert.That(ports.Trace, Is.Empty);
        }

        [Test]
        public void RestartRequiresTheDefeatedModelRatherThanAViewFlag()
        {
            var ports = new Ports();
            PlayerLifeController controller = Create(ports);
            Assert.That(controller.Restart().Success, Is.False);
            Assert.That(ports.Restarts, Is.Zero);
            controller.TakeDamage(999, Vector2.zero);
            Assert.That(controller.Restart().Success, Is.True);
            Assert.That(ports.Restarts, Is.EqualTo(1));
            controller.Restore(2);
            Assert.That(controller.Restart().Success, Is.False);
            Assert.That(ports.Restarts, Is.EqualTo(1));
        }

        [Test]
        public void DefeatDoesNotWaitForACosmeticViewOrCoroutine()
        {
            var model = new HealthModel(new HealthSettings(2, 0), new Clock());
            var environment = new Ports();
            var controller = new PlayerLifeController(model, environment, new SilentView());
            Assert.That(controller.TakeDamage(2, Vector2.zero).Success, Is.True);
            Assert.That(model.IsAlive, Is.False);
            Assert.That(environment.Trace, Is.EqualTo(new[] { "defeat:True" }));
            Assert.That(controller.Restart().Success, Is.True);
        }

        [Test]
        public void MissingRestoreTargetIsAnExpectedRejection()
        {
            Assert.That(new RestorePlayerHealthCommand(null, 2).Execute().Success, Is.False);
            Assert.That(new RestartPlayerRoomCommand(null, new Ports()).Execute().Success, Is.False);
            Assert.That(new RestartPlayerRoomCommand(new HealthModel(new HealthSettings(2, 0), new Clock()), null).Execute().Success, Is.False);
        }

        private static PlayerLifeController Create(Ports ports) =>
            new HealthModelFactory(new Clock()).CreatePlayer(new HealthSettings(3, 0), ports, ports);

        private sealed class Clock : IGameClock
        {
            public double Time { get; set; }
        }

        private sealed class Ports : IPlayerLifeEnvironment, IPlayerLifeView
        {
            public readonly List<string> Trace = new List<string>();
            public int Restarts;
            public void SetDefeated(bool defeated) => Trace.Add("defeat:" + defeated);
            public void RestartRoom() => Restarts++;
            public void PresentDamage(int amount, Vector2 direction, bool defeated) => Trace.Add("damage:" + amount + ":" + defeated);
            public void PresentHealth(int current, bool restored) => Trace.Add("health:" + current + ":" + restored);
        }

        private sealed class SilentView : IPlayerLifeView
        {
            public void PresentDamage(int amount, Vector2 direction, bool defeated)
            {
            }

            public void PresentHealth(int current, bool restored)
            {
            }
        }
    }
}
