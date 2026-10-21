using System;
using EmergencyShift.Core.Randomness;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class WeaponSettingsTests
    {
        [Test]
        public void ExistingCycleAndInventoryIgnoreLaterAssetChanges()
        {
            var asset = ScriptableObject.CreateInstance<PumpShotgunDefinition>();
            try
            {
                asset.contentId = "frozen-shotgun";
                var inventory = new WeaponInventory();
                WeaponSettings settings = asset.ToSettings();
                Assert.That(inventory.Acquire(settings, 12), Is.True);
                WeaponCycle cycle = inventory.ActiveCycle;
                asset.contentId = "changed";
                asset.magazineCapacity = 1;
                asset.maximumAmmo = 1;
                asset.pumpSeconds = 40;
                asset.secondsBetweenShots = 40;
                asset.selectionSlot = 25;
                Assert.That(cycle.TryFire(0), Is.True);
                cycle.Tick(1);
                Assert.That(cycle.Phase, Is.EqualTo(WeaponPhase.Ready));
                Assert.That(cycle.TryFire(1), Is.True);
                Assert.That(cycle.MagazineCapacity, Is.EqualTo(4));
                Assert.That(inventory.Capture(1).activeId, Is.EqualTo("frozen-shotgun"));
                Assert.That(cycle.Settings.MaximumAmmo, Is.EqualTo(120));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(73)]
        public void CosmeticDrawsBetweenEveryPelletCannotChangeGameplayStream(int cosmeticDraws)
        {
            var settings = Shotgun();
            var first = new SystemRandomSource(new System.Random(71403));
            var second = new SystemRandomSource(new System.Random(71403));
            UnityEngine.Random.State original = UnityEngine.Random.state;
            try
            {
                for (int volley = 0; volley < 10; volley++)
                {
                    for (int pellet = 0; pellet < settings.ProjectileCount; pellet++)
                    {
                        Vector2 expected = settings.ProjectileDirection(Vector2.up, pellet, first);
                        for (int draw = 0; draw < cosmeticDraws; draw++)
                        {
                            _ = UnityEngine.Random.value;
                        }

                        Vector2 actual = settings.ProjectileDirection(Vector2.up, pellet, second);
                        Assert.That(actual, Is.EqualTo(expected));
                        Assert.That(Mathf.Abs(Vector2.SignedAngle(Vector2.up, actual)), Is.LessThanOrEqualTo(9));
                    }
                }
            }
            finally
            {
                UnityEngine.Random.state = original;
            }
        }

        [Test]
        public void AnotherOwnersShotsDoNotAdvanceThisOwnersStream()
        {
            var settings = Shotgun();
            var reference = new RandomSourceFactory(new System.Random(481));
            var interleaved = new RandomSourceFactory(new System.Random(481));
            IRandomSource expectedFirst = reference.Create();
            IRandomSource expectedSecond = reference.Create();
            IRandomSource actualFirst = interleaved.Create();
            IRandomSource actualSecond = interleaved.Create();
            for (int i = 0; i < 17; i++)
            {
                settings.ProjectileDirection(Vector2.up, 0, actualSecond);
                settings.ProjectileDirection(Vector2.up, 0, expectedSecond);
            }

            Assert.That(settings.ProjectileDirection(Vector2.up, 0, actualFirst),
                Is.EqualTo(settings.ProjectileDirection(Vector2.up, 0, expectedFirst)));
            Assert.That(settings.ProjectileDirection(Vector2.up, 0, actualSecond),
                Is.EqualTo(settings.ProjectileDirection(Vector2.up, 0, expectedSecond)));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(-1f)]
        public void InvalidExternalTimeCannotChangeAmmoOrPhase(float invalid)
        {
            WeaponCycle cycle = Shotgun().CreateCycle(12);
            string before = JsonUtility.ToJson(cycle.Capture(0));
            Assert.That(new FireWeaponCommand(cycle, invalid).CanExecute().Success, Is.False);
            Assert.That(new FireWeaponCommand(cycle, invalid).Execute().Success, Is.False);
            Assert.That(new ReloadWeaponCommand(cycle, invalid).Execute().Success, Is.False);
            Assert.That(new RestoreWeaponCycleCommand(cycle, cycle.Capture(0), invalid).Execute().Success, Is.False);
            cycle.Tick(invalid);
            Assert.That(JsonUtility.ToJson(cycle.Capture(0)), Is.EqualTo(before));
        }

        [Test]
        public void CommandsValidateWithoutMutatingAndRejectInvalidRestoreBeforeApplying()
        {
            WeaponCycle cycle = Shotgun().CreateCycle(12);
            string before = JsonUtility.ToJson(cycle.Capture(0));
            var fire = new FireWeaponCommand(cycle, 0);
            Assert.That(fire.CanExecute().Success, Is.True);
            Assert.That(JsonUtility.ToJson(cycle.Capture(0)), Is.EqualTo(before));
            Assert.That(fire.Execute().Success, Is.True);
            Assert.That(new AddWeaponAmmoCommand(cycle, -2).Execute().Success, Is.False);
            WeaponCycleSnapshot invalid = cycle.Capture(0);
            invalid.ammo = -1;
            before = JsonUtility.ToJson(cycle.Capture(0));
            Assert.That(new RestoreWeaponCycleCommand(cycle, invalid, 0).Execute().Success, Is.False);
            Assert.That(JsonUtility.ToJson(cycle.Capture(0)), Is.EqualTo(before));
        }

        [Test]
        public void CatalogCopiesItsInputAndRejectsDuplicateIdsOrSlots()
        {
            var original = new WeaponSettings("pistol", 0, 1, 120, .3f, 10, false);
            var array = new[] { original };
            var catalog = new WeaponSettingsCatalog(array);
            array[0] = null;
            Assert.That(catalog.Find("pistol"), Is.SameAs(original));
            Assert.That(catalog.Find(null), Is.Null);
            Assert.Throws<InvalidOperationException>(() => new WeaponSettingsCatalog(new[] { original, original }));
            var conflict = new WeaponSettings("other", 0, 1, 120, .3f, 10, false);
            Assert.Throws<InvalidOperationException>(() => new WeaponSettingsCatalog(new[] { original, conflict }));
        }

        [Test]
        public void InventoryCommandsRejectConflictsAndBusySwitchesBeforeChangingOwnership()
        {
            var inventory = new WeaponInventory();
            var pistol = new WeaponSettings("pistol", 0, 1, 120, .3f, 10, false);
            var conflict = new WeaponSettings("conflict", 0, 1, 120, .3f, 10, false);
            Assert.That(new AcquireWeaponCommand(inventory, pistol, 7).Execute().Success, Is.True);
            Assert.That(new AcquireWeaponCommand(inventory, conflict, 7).Execute().Success, Is.False);
            Assert.That(inventory.Count, Is.EqualTo(1));
            Assert.That(new AcquireWeaponCommand(inventory, Shotgun(), 12).Execute().Success, Is.True);
            Assert.That(inventory.ActiveCycle.TryFire(0), Is.True);
            Assert.That(new EquipWeaponCommand(inventory, 0).Execute().Success, Is.False);
            Assert.That(new ReplaceWeaponDefinitionCommand(inventory, pistol).Execute().Success, Is.False);
            Assert.That(inventory.ActiveDefinition.ContentId, Is.EqualTo("shotgun"));
            Assert.That(inventory.ActiveCycle.TotalAmmo, Is.EqualTo(11));
            inventory.ActiveCycle.Tick(1);
            Assert.That(new EquipWeaponCommand(inventory, 0).Execute().Success, Is.True);
            Assert.That(inventory.ActiveCycle.TotalAmmo, Is.EqualTo(7));
        }

        [Test]
        public void InventoryRestoreCommandValidatesBeforeApplyingAndKeepsLegacyFormat()
        {
            var inventory = new WeaponInventory();
            PumpShotgunSettings settings = Shotgun();
            inventory.Acquire(settings, 12);
            var catalog = new WeaponSettingsCatalog(new[] { settings });
            WeaponInventorySnapshot state = inventory.Capture(0);
            state.weapons[0].cycle.ammo = -1;
            string before = JsonUtility.ToJson(inventory.Capture(0));
            var invalid = new RestoreWeaponInventoryCommand(inventory, state, catalog, 0);
            Assert.That(invalid.CanExecute().Success, Is.False);
            Assert.That(invalid.Execute().Success, Is.False);
            Assert.That(JsonUtility.ToJson(inventory.Capture(0)), Is.EqualTo(before));
            inventory.RestoreLegacy(true, 8, settings);
            WeaponInventorySnapshot restored = inventory.Capture(0);
            Assert.That(restored.format, Is.EqualTo(1));
            Assert.That(restored.weapons[0].cycle.format, Is.EqualTo(1));
            Assert.That(inventory.ActiveCycle.Loaded, Is.EqualTo(4));
            Assert.That(inventory.ActiveCycle.Reserve, Is.EqualTo(4));
            inventory.RestoreLegacy(false, 0, null);
            Assert.That(inventory.Count, Is.Zero);
        }

        [TestCase("cue-after-operation")]
        [TestCase("inactive-cue-duration")]
        [TestCase("full-magazine-pumping")]
        [TestCase("loading-uses-pump-limit")]
        public void ImpossiblePumpSnapshotIsRejectedWithoutChangingTheCycle(string defect)
        {
            WeaponCycle cycle = Shotgun().CreateCycle(12);
            cycle.TryFire(0);
            WeaponCycleSnapshot state = cycle.Capture(0);
            switch (defect)
            {
                case "cue-after-operation":
                    state.operationRemaining = .05f;
                    break;
                case "inactive-cue-duration":
                    state.cuePending = false;
                    break;
                case "full-magazine-pumping":
                    state.loaded = 4;
                    break;
                case "loading-uses-pump-limit":
                    state.phase = WeaponPhase.Loading;
                    state.cuePending = false;
                    state.cueRemaining = 0;
                    state.operationRemaining = .83f;
                    break;
            }

            string before = JsonUtility.ToJson(cycle.Capture(0));
            Assert.That(new RestoreWeaponCycleCommand(cycle, state, 0).Execute().Success, Is.False);
            Assert.That(JsonUtility.ToJson(cycle.Capture(0)), Is.EqualTo(before));
        }

        [Test]
        public void ElapsedUntickedPumpCanRestoreAndEmitItsPendingCueExactlyOnce()
        {
            WeaponCycle cycle = Shotgun().CreateCycle(12);
            cycle.TryFire(0);
            WeaponCycleSnapshot state = cycle.Capture(2);
            Assert.That(state.cuePending, Is.True);
            Assert.That(state.operationRemaining, Is.Zero);
            var restored = Shotgun().CreateCycle(0);
            int cues = 0;
            restored.Operation += operation => { if (operation == WeaponOperation.Pump) { cues++; } };
            restored.Restore(state, 10);
            restored.Tick(10);
            restored.Tick(11);
            Assert.That(cues, Is.EqualTo(1));
            Assert.That(restored.Phase, Is.EqualTo(WeaponPhase.Ready));
            Assert.That(restored.TotalAmmo, Is.EqualTo(11));
        }

        private static PumpShotgunSettings Shotgun()
        {
            var common = new WeaponSettings("shotgun", 1, 1, 120, .3f, 10, false);
            var magazine = new MagazineWeaponSettings(common, 4, .8f);
            return new PumpShotgunSettings(magazine, 8, 18, .85f, .16f);
        }
    }
}
