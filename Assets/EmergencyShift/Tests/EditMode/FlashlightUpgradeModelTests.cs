using EmergencyShift.Features.Flashlight;
using EmergencyShift.Features.Flashlight.Config;
using EmergencyShift.Features.Flashlight.Logic;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class FlashlightUpgradeModelTests
    {
        [Test]
        public void RepeatableIdsAccumulateEveryInstallAndRoundTripWithoutLosingCount()
        {
            var environment = new OpticsPort();
            FlashlightUpgradeController upgrades = Create(environment);
            Assert.That(upgrades.TryInstall("lens", new Vector2(.6f, 6), true).Success, Is.True);
            Assert.That(upgrades.TryInstall("lens", new Vector2(.6f, 6), true).Success, Is.True);
            Assert.That(upgrades.InstalledCount, Is.EqualTo(2));
            Assert.That(environment.Optics.x, Is.EqualTo(5.4f).Within(.0001));
            Assert.That(environment.Optics.y, Is.EqualTo(92));
            FlashlightModule[] saved = upgrades.Capture();
            Assert.That(saved.Length, Is.EqualTo(1));
            var restoredEnvironment = new OpticsPort();
            FlashlightUpgradeController restored = Create(restoredEnvironment);
            Assert.That(restored.Restore(saved, upgrades.InstalledCount).Success, Is.True);
            Assert.That(restored.InstalledCount, Is.EqualTo(2));
            Assert.That(restoredEnvironment.Optics, Is.EqualTo(environment.Optics));
        }

        [Test]
        public void NonrepeatableCommandRevalidatesAfterAnotherInstallWithoutMorePresentation()
        {
            var model = new FlashlightUpgradeModel(new Vector2(4.2f, 80));
            var request = Request("lens", new Vector2(1, 5), false);
            var stale = new InstallFlashlightModuleCommand(model, request);
            Assert.That(stale.CanExecute().Success, Is.True);
            Assert.That(new InstallFlashlightModuleCommand(model, request).Execute().Success, Is.True);
            Assert.That(stale.Execute().Success, Is.False);
            Assert.That(model.InstalledCount, Is.EqualTo(1));
            Assert.That(stale.Optics, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void CapsRejectFurtherInstallsAndLoweredCapsNeverDowngradeOnAcquisition()
        {
            var environment = new OpticsPort();
            FlashlightUpgradeController upgrades = Create(environment);
            Assert.That(upgrades.TryInstall("lens", new Vector2(2, 5), true).Success, Is.True);
            environment.Limits = new FlashlightUpgradeLimits(5, 110);
            Assert.That(upgrades.TryInstall("angle", new Vector2(.1f, 3), true).Success, Is.True);
            Assert.That(environment.Optics.x, Is.EqualTo(6.2f).Within(.0001));
            Assert.That(environment.Optics.y, Is.EqualTo(88));
            environment.Limits = new FlashlightUpgradeLimits(7, 110);
            Assert.That(upgrades.TryInstall("maximum", new Vector2(100, 100), true).Success, Is.True);
            int updates = environment.Updates;
            Assert.That(environment.Optics, Is.EqualTo(new Vector2(7, 110)));
            Assert.That(upgrades.TryInstall("too-much", new Vector2(1, 1), true).Success, Is.False);
            Assert.That(upgrades.InstalledCount, Is.EqualTo(3));
            Assert.That(environment.Updates, Is.EqualTo(updates));
        }

        [Test]
        public void DisabledOrDeadGateRejectsInstallationButDoesNotPreventTrustedRestore()
        {
            var environment = new OpticsPort { CanUpgrade = false };
            FlashlightUpgradeController upgrades = Create(environment);
            Assert.That(upgrades.CanImprove(new Vector2(1, 0)), Is.False);
            Assert.That(upgrades.TryInstall("lens", new Vector2(1, 0), true).Success, Is.False);
            Assert.That(environment.Updates, Is.Zero);
            Assert.That(upgrades.Restore(new[] { new FlashlightModule("saved", new Vector2(1, 0)) }, 1).Success, Is.True);
            Assert.That(upgrades.HasModule("saved"), Is.True);
            Assert.That(environment.Optics.x, Is.EqualTo(5.2f).Within(.0001));
        }

        [TestCase(float.NaN, 1)]
        [TestCase(float.PositiveInfinity, 1)]
        [TestCase(float.NegativeInfinity, 1)]
        [TestCase(1, float.NaN)]
        [TestCase(1, float.PositiveInfinity)]
        [TestCase(1, float.NegativeInfinity)]
        [TestCase(-1, 1)]
        [TestCase(1, -1)]
        [TestCase(0, 0)]
        public void InvalidBonusLeavesProgressAndOpticsUntouched(float range, float angle)
        {
            var environment = new OpticsPort();
            FlashlightUpgradeController upgrades = Create(environment);
            Assert.That(upgrades.TryInstall("invalid", new Vector2(range, angle), true).Success, Is.False);
            Assert.That(upgrades.InstalledCount, Is.Zero);
            Assert.That(upgrades.Capture(), Is.Empty);
            Assert.That(environment.Optics, Is.EqualTo(new Vector2(4.2f, 80)));
            Assert.That(environment.Updates, Is.Zero);
        }

        [Test]
        public void InvalidRestoreIsAtomicAndValidRestoreCopiesTheCallerSnapshot()
        {
            var environment = new OpticsPort();
            FlashlightUpgradeController upgrades = Create(environment);
            upgrades.TryInstall("live", new Vector2(1, 1), true);
            Vector2 before = environment.Optics;
            int updates = environment.Updates;
            Assert.That(upgrades.Restore(new[] { new FlashlightModule("broken", Vector2.zero) }, 1).Success, Is.False);
            Assert.That(upgrades.HasModule("live"), Is.True);
            Assert.That(environment.Optics, Is.EqualTo(before));
            Assert.That(environment.Updates, Is.EqualTo(updates));
            var saved = new[] { new FlashlightModule("saved", new Vector2(.6f, 6)) };
            Assert.That(upgrades.Restore(saved, 3).Success, Is.True);
            saved[0] = new FlashlightModule("foreign", Vector2.one);
            Assert.That(upgrades.HasModule("saved"), Is.True);
            Assert.That(upgrades.HasModule("foreign"), Is.False);
            Assert.That(upgrades.Restore(null, 0).Success, Is.True);
            Assert.That(upgrades.InstalledCount, Is.Zero);
            Assert.That(environment.Optics, Is.EqualTo(new Vector2(4.2f, 80)));
        }

        [Test]
        public void RestoreRequestIsFrozenUntilExecuteAndLimitsCanNeverLowerTheBase()
        {
            var model = new FlashlightUpgradeModel(new Vector2(4.2f, 80));
            var modules = new[] { new FlashlightModule("saved", new Vector2(1, 1)) };
            var command = new RestoreFlashlightModulesCommand(model, modules, 2, new FlashlightUpgradeLimits(3, 40));
            modules[0] = default;
            Assert.That(command.Execute().Success, Is.True);
            Assert.That(model.HasModule("saved"), Is.True);
            Assert.That(command.Optics, Is.EqualTo(new Vector2(4.2f, 80)));
        }

        [Test]
        public void OneFactoryCreatesPersonalProgressionWithoutSharedOpticsOrModules()
        {
            var factory = new FlashlightControllerFactory();
            var firstEnvironment = new OpticsPort();
            var secondEnvironment = new OpticsPort();
            FlashlightUpgradeController first = factory.CreateUpgrades(firstEnvironment.Optics, firstEnvironment);
            FlashlightUpgradeController second = factory.CreateUpgrades(secondEnvironment.Optics, secondEnvironment);
            first.TryInstall("lens", Vector2.one, true);
            Assert.That(second.HasModule("lens"), Is.False);
            Assert.That(second.InstalledCount, Is.Zero);
            Assert.That(secondEnvironment.Updates, Is.Zero);
            Assert.That(secondEnvironment.Optics, Is.EqualTo(new Vector2(4.2f, 80)));
        }

        [Test]
        public void ExtremeValidSnapshotClampsOpticsAndInstallCountCannotWrap()
        {
            var environment = new OpticsPort();
            FlashlightUpgradeController upgrades = Create(environment);
            var modules = new[]
            {
                new FlashlightModule("a", new Vector2(float.MaxValue, 1)),
                new FlashlightModule("b", new Vector2(float.MaxValue, 1))
            };
            Assert.That(upgrades.Restore(modules, 2).Success, Is.True);
            Assert.That(environment.Optics.x, Is.EqualTo(7));
            Assert.That(float.IsFinite(environment.Optics.y), Is.True);
            Assert.That(upgrades.Restore(new[] { new FlashlightModule("a", new Vector2(1, 1)) }, int.MaxValue).Success, Is.True);
            Assert.That(upgrades.TryInstall("new", Vector2.one, true).Success, Is.False);
            Assert.That(upgrades.InstalledCount, Is.EqualTo(int.MaxValue));
        }

        private static FlashlightUpgradeController Create(OpticsPort environment) =>
            new FlashlightControllerFactory().CreateUpgrades(environment.Optics, environment);

        private static FlashlightModuleRequest Request(string id, Vector2 bonus, bool repeatable) =>
            new FlashlightModuleRequest(id, bonus, repeatable, new Vector2(4.2f, 80), new FlashlightUpgradeLimits(7, 110), true);

        private sealed class OpticsPort : IFlashlightUpgradeEnvironment
        {
            public bool CanUpgrade { get; set; } = true;
            public Vector2 Optics { get; set; } = new Vector2(4.2f, 80);
            public FlashlightUpgradeLimits Limits { get; set; } = new FlashlightUpgradeLimits(7, 110);
            public int Updates { get; private set; }

            public void ApplyOptics(Vector2 optics)
            {
                Optics = optics;
                Updates++;
            }
        }
    }
}
