using EmergencyShift.Features.Encounter;
using EmergencyShift.Features.Encounter.Logic;
using NUnit.Framework;

namespace EmergencyShift.Tests
{
    public sealed class EncounterControllerTests
    {
        [Test]
        public void ActivationIsOneShotAndStateIsPersonal()
        {
            var factory = new EncounterControllerFactory();
            var firstView = new View();
            var secondView = new View();
            EncounterController first = factory.Create(firstView);
            EncounterController second = factory.Create(secondView);
            Assert.That(first.Activate().Success, Is.True);
            Assert.That(first.Activate().Success, Is.False);
            Assert.That(firstView.Calls, Is.EqualTo(1));
            Assert.That(first.Activated, Is.True);
            Assert.That(second.Activated, Is.False);
            Assert.That(secondView.Calls, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RestoreIsSilentAndControlsTheNextCommand(bool activated)
        {
            var view = new View();
            EncounterController controller = new EncounterControllerFactory().Create(view);
            Assert.That(controller.Restore(activated).Success, Is.True);
            Assert.That(controller.Activated, Is.EqualTo(activated));
            Assert.That(view.Calls, Is.Zero);
            Assert.That(controller.Activate().Success, Is.EqualTo(!activated));
            Assert.That(view.Calls, Is.EqualTo(activated ? 0 : 1));
        }

        [Test]
        public void MissingModelIsAnExpectedCommandRejection()
        {
            Assert.That(new ActivateEncounterCommand(null).Execute().Success, Is.False);
            Assert.That(new RestoreEncounterCommand(null, true).Execute().Success, Is.False);
        }

        private sealed class View : IEncounterView
        {
            public int Calls;
            public void ActivateActors() => Calls++;
        }
    }
}
