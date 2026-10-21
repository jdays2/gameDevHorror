using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class FlashlightOwnershipTests
    {
        [Test]
        public void AcquisitionCommandRejectsDuplicateAndKeepsOtherPlayersIndependent()
        {
            var first = new FlashlightOwnership(false);
            var second = new FlashlightOwnership(false);
            var command = new AcquireFlashlightCommand(first);
            Assert.That(command.CanExecute, Is.True);
            Assert.That(command.Execute(), Is.True);
            Assert.That(command.CanExecute, Is.False);
            Assert.That(command.Execute(), Is.False);
            Assert.That(first.IsOwned, Is.True);
            Assert.That(second.IsOwned, Is.False);
            first.Restore(false);
            Assert.That(command.CanExecute, Is.True);
        }

        [Test]
        public void MissingFlashlightCannotBePoweredUntilAcquired()
        {
            var player = new GameObject("Ownership test");
            player.SetActive(false);
            var source = new GameObject("Beam");
            try
            {
                var light = source.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
                var flashlight = player.AddComponent<Flashlight2D>();
                flashlight.Construct(null, new GameplayInputBlock(),
                    new EmergencyShift.Features.Flashlight.FlashlightControllerFactory());
                flashlight.Configure(null, source.transform, light);
                flashlight.RestoreOwnership(false);
                flashlight.SetPower(true);
                Assert.That(flashlight.IsOn, Is.False);
                Assert.That(flashlight.Acquire(), Is.True);
                Assert.That(flashlight.IsOwned, Is.True);
                Assert.That(flashlight.IsOn, Is.True);
                Assert.That(flashlight.Acquire(), Is.False);
                flashlight.RestoreOwnership(false);
                Assert.That(flashlight.IsOn, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(source);
            }
        }

        [TestCase(1.7777778f)]
        [TestCase(.75f)]
        public void CameraExpandsForAcquisitionAndRangeUpgrade(float aspect)
        {
            var profile = ScriptableObject.CreateInstance<CameraFramingProfile>();
            try
            {
                profile.unarmedViewRadius = 2.4f;
                profile.lightRangeMargin = .25f;
                float before = profile.OrthographicSize(3.2f, aspect, false);
                float acquired = profile.OrthographicSize(3.2f, aspect, true);
                float upgraded = profile.OrthographicSize(4.2f, aspect, true);
                Assert.That(acquired, Is.GreaterThan(before));
                Assert.That(upgraded - acquired, Is.EqualTo(1 / Mathf.Min(1, aspect)).Within(.001));
                Assert.That(profile.OrthographicSize(3.2f, aspect), Is.EqualTo(acquired));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }
    }
}
