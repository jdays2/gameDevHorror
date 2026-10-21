using System;
using EmergencyShift.Features.PlayerInput.Logic;
using EmergencyShift.Features.PlayerMovement;
using EmergencyShift.Features.PlayerMovement.Config;
using EmergencyShift.Features.PlayerMovement.Logic;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class PlayerMovementTests
    {
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(.2f, 0)]
        public void AuthoredSpeedAndLegacyFullSpeedNormalizationArePreserved(float x, float y)
        {
            var model = new PlayerMovementModel(new PlayerMovementSettings(3.5f));
            Assert.That(new SetMovementIntentCommand(model, new Vector2(x, y)).Execute().Success, Is.True);
            Assert.That(model.Velocity.magnitude, Is.EqualTo(3.5f).Within(.00001));
            Assert.That(model.Direction, Is.EqualTo(new Vector2(x, y).normalized));
        }

        [Test]
        public void BlockAcquiredBetweenSampleAndPhysicsImmediatelyStopsVelocity()
        {
            var input = new Input { Intent = Vector2.right };
            var block = new GameplayInputBlock();
            var controller = new PlayerMovementControllerFactory(input, block).Create(3.5f);
            controller.SampleInput();
            Assert.That(controller.Velocity, Is.EqualTo(Vector2.right * 3.5f));
            using (block.Acquire())
            {
                Assert.That(controller.Velocity, Is.EqualTo(Vector2.zero));
                controller.SampleInput();
            }

            Assert.That(controller.Velocity, Is.EqualTo(Vector2.zero));
            controller.SampleInput();
            Assert.That(controller.Velocity, Is.EqualTo(Vector2.right * 3.5f));
        }

        [Test]
        public void ReleasingOneModalCannotUnblockAnotherOwner()
        {
            var block = new GameplayInputBlock();
            var controller = new PlayerMovementControllerFactory(new Input { Intent = Vector2.up }, block).Create(2);
            IDisposable first = block.Acquire();
            IDisposable second = block.Acquire();
            first.Dispose();
            controller.SampleInput();
            Assert.That(controller.Velocity, Is.EqualTo(Vector2.zero));
            second.Dispose();
            controller.SampleInput();
            Assert.That(controller.Velocity, Is.EqualTo(Vector2.up * 2));
        }

        [Test]
        public void StopClearsPersonalIntentWithoutAffectingAnotherController()
        {
            var input = new Input { Intent = Vector2.right };
            var factory = new PlayerMovementControllerFactory(input, new GameplayInputBlock());
            var first = factory.Create(2);
            var second = factory.Create(3);
            first.SampleInput();
            second.SampleInput();
            first.Stop();
            Assert.That(first.Velocity, Is.EqualTo(Vector2.zero));
            Assert.That(second.Velocity, Is.EqualTo(Vector2.right * 3));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.MaxValue)]
        public void RejectedCommandIsAtomicAndMalformedInputSourceStopsSafely(float invalid)
        {
            var model = new PlayerMovementModel(new PlayerMovementSettings(2));
            new SetMovementIntentCommand(model, Vector2.left).Execute();
            var command = new SetMovementIntentCommand(model, new Vector2(invalid, 0));
            Assert.That(command.CanExecute().Success, Is.False);
            Assert.That(command.Execute().Success, Is.False);
            Assert.That(model.Velocity, Is.EqualTo(Vector2.left * 2));
            var input = new Input { Intent = Vector2.right };
            var controller = new PlayerMovementControllerFactory(input, new GameplayInputBlock()).Create(2);
            controller.SampleInput();
            input.Intent = new Vector2(invalid, 0);
            Assert.That(controller.SampleInput().Success, Is.False);
            Assert.That(controller.Velocity, Is.EqualTo(Vector2.zero));
        }

        [TestCase(-1)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidAuthoredSpeedIsRejected(float speed)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerMovementSettings(speed));
        }

        [Test]
        public void ZeroSpeedAndEmptyIntentProduceNoMovement()
        {
            var input = new Input { Intent = Vector2.one };
            var factory = new PlayerMovementControllerFactory(input, new GameplayInputBlock());
            var stopped = factory.Create(0);
            stopped.SampleInput();
            Assert.That(stopped.Velocity, Is.EqualTo(Vector2.zero));
            input.Intent = Vector2.zero;
            var empty = factory.Create(3.5f);
            empty.SampleInput();
            Assert.That(empty.Velocity, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ContinuousValueCommandsAndVelocityReadsDoNotAllocateManagedObjects()
        {
            var controller = new PlayerMovementControllerFactory(new Input { Intent = Vector2.one }, new GameplayInputBlock()).Create(3.5f);
            Vector2 last = Vector2.zero;
            for (int i = 0; i < 50; i++)
            {
                controller.SampleInput();
                last = controller.Velocity;
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                controller.SampleInput();
                last = controller.Velocity;
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(last.sqrMagnitude, Is.GreaterThan(0));
            Assert.That(allocated, Is.Zero);
        }

        private sealed class Input : IPlayerInput
        {
            public Vector2 Intent;
            public Vector2 Movement => Intent;
            public Vector2 PointerPosition => Vector2.zero;
            public bool HasPointer => false;
            public bool FirePressed => false;
            public bool FireHeld => false;
            public bool InteractPressed => false;
            public bool ReloadPressed => false;
            public bool SelectPistolPressed => false;
            public bool SelectShotgunPressed => false;
            public bool ToggleLightPressed => false;
            public bool RestartPressed => false;
            public bool RefillAmmoPressed => false;
            public bool DialogueAdvancePressed => false;
            public bool PausePressed => false;
            public bool ConfirmationHeld => false;
        }
    }
}
