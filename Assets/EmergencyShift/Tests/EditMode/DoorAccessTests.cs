using EmergencyShift.Core.Time;
using EmergencyShift.Features.AccessControl;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class DoorAccessTests
    {
        private GameObject doorObject;
        private GameObject actorObject;
        private DoorAccess2D access;
        private PlayerInteraction actor;
        private PlayerAccessCards2D cards;

        [SetUp]
        public void Setup()
        {
            doorObject = new GameObject("Reader test");
            doorObject.SetActive(false);
            access = doorObject.AddComponent<DoorAccess2D>();
            access.Construct(new DoorAccessControllerFactory(new UnityGameClock()));
            access.Configure("blue", "Синяя карта");
            actorObject = new GameObject("Card holder");
            cards = actorObject.AddComponent<PlayerAccessCards2D>();
            cards.Construct(new AccessControlFactory());
            actor = actorObject.AddComponent<PlayerInteraction>();
            actor.Construct(null, new GameplayInputBlock(), new Core.Composition.SceneRegistry<Door2D>(),
                new Features.Interaction.PlayerInteractionControllerFactory(new UnityGameClock()));
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(doorObject);
            Object.DestroyImmediate(actorObject);
        }

        [Test]
        public void WrongCardCannotUnlockAndRepeatedAttemptDoesNotSpamFeedback()
        {
            cards.Add("green");
            int denied = 0;
            access.AccessChecked += granted =>
            {
                if (!granted)
                {
                    denied++;
                }
            };

            Assert.That(access.TryUnlock(actor), Is.False);
            Assert.That(access.TryUnlock(actor), Is.False);
            Assert.That(access.IsUnlocked, Is.False);
            Assert.That(denied, Is.EqualTo(1));
        }

        [Test]
        public void SuccessfulReadingIsOnceAndUnlockPersistsWithoutTheCard()
        {
            cards.Add("blue");
            int readings = 0;
            access.AccessChecked += granted => readings++;

            Assert.That(access.TryUnlock(actor), Is.True);
            cards.Restore(new string[0]);
            Assert.That(access.TryUnlock(actor), Is.True);
            Assert.That(readings, Is.EqualTo(1));
            Assert.That(access.IsUnlocked, Is.True);
        }

        [Test]
        public void RestorationDoesNotEmitReaderFeedbackAndLegacyDoorsHaveNoDelay()
        {
            int readings = 0;
            access.AccessChecked += granted => readings++;

            access.RestoreState(true);
            Assert.That(access.TryUnlock(actor), Is.True);
            access.RestoreState(false);

            Assert.That(readings, Is.Zero);
            Assert.That(access.ConfirmationDelay, Is.Zero);
        }
    }
}
