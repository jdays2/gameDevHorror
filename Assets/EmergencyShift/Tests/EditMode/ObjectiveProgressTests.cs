using System;
using EmergencyShift.Features.LevelObjectives.Config;
using EmergencyShift.Features.LevelObjectives.Logic;
using NUnit.Framework;

namespace EmergencyShift.Tests
{
    public sealed class ObjectiveProgressTests
    {
        [Test]
        public void FirstMatchingRuleWinsInAuthoredOrder()
        {
            var view = new View();
            var controller = Controller(view,
                Rule(ObjectiveCondition.HasCard, 3, "service"),
                Rule(ObjectiveCondition.WeaponMissing, 1),
                Rule(ObjectiveCondition.Always, 2));
            Assert.That(controller.Refresh(new Facts { Card = "service" }).Success, Is.True);
            Assert.That(controller.Phase, Is.EqualTo(3));
            Assert.That(controller.Text, Is.EqualTo("phase 3"));
            Assert.That(view.Refreshes, Is.EqualTo(1));
        }

        [Test]
        public void PhaseMayMoveBackwardsWhenAuthoredFactsChange()
        {
            var controller = Controller(new View(), Rule(ObjectiveCondition.HasCard, 3, "service"), Rule(ObjectiveCondition.Always, 1));
            controller.Refresh(new Facts { Card = "service" });
            controller.Refresh(new Facts());
            Assert.That(controller.Phase, Is.EqualTo(1));
        }

        [Test]
        public void NoMatchRetainsPreviousStateAndDoesNotNotify()
        {
            var view = new View();
            var controller = Controller(view, Rule(ObjectiveCondition.HasCard, 3, "service"));
            controller.Refresh(new Facts { Card = "service" });
            Assert.That(controller.Refresh(new Facts()).Success, Is.False);
            Assert.That(controller.Phase, Is.EqualTo(3));
            Assert.That(view.Refreshes, Is.EqualTo(1));
        }

        [Test]
        public void RepeatedMatchingRefreshStillNotifiesAsLegacyContract()
        {
            var view = new View();
            var controller = Controller(view, Rule(ObjectiveCondition.Always, 1));
            controller.Refresh(new Facts());
            controller.Refresh(new Facts());
            Assert.That(view.Refreshes, Is.EqualTo(2));
        }

        [Test]
        public void ReconfigurationDoesNotResetProgressOrNotifyBeforeRefresh()
        {
            var view = new View();
            var controller = Controller(view, Rule(ObjectiveCondition.Always, 3));
            controller.Refresh(new Facts());
            Assert.That(controller.Configure(new ObjectiveRuleSet(new[] { Rule(ObjectiveCondition.Always, 1) })).Success, Is.True);
            Assert.That(controller.Phase, Is.EqualTo(3));
            Assert.That(view.Refreshes, Is.EqualTo(1));
            controller.Refresh(new Facts());
            Assert.That(controller.Phase, Is.EqualTo(1));
        }

        [Test]
        public void InvalidCommandsCannotChangeProgressOrNotify()
        {
            var view = new View();
            var controller = Controller(view, Rule(ObjectiveCondition.Always, 3));
            Assert.That(controller.Configure(null).Success, Is.False);
            Assert.That(controller.Refresh(null).Success, Is.False);
            Assert.That(new RefreshObjectivesCommand(null, new Facts()).Execute().Success, Is.False);
            Assert.That(new ConfigureObjectivesCommand(null, new ObjectiveRuleSet(Array.Empty<LevelObjectiveRule>())).Execute().Success, Is.False);
            Assert.That(controller.Phase, Is.Zero);
            Assert.That(view.Refreshes, Is.Zero);
        }

        [Test]
        public void SnapshotOwnsRulesAndPreservesNullText()
        {
            var source = new[] { new LevelObjectiveRule(ObjectiveCondition.Always, null, 2, null) };
            var settings = new ObjectiveRuleSet(source);
            source[0] = Rule(ObjectiveCondition.Always, 9);
            Assert.That(settings.At(0).Phase, Is.EqualTo(2));
            Assert.That(settings.At(0).Text, Is.Null);
            Assert.Throws<ArgumentException>(() => new ObjectiveRuleSet(new LevelObjectiveRule[] { null }));
            Assert.Throws<ArgumentNullException>(() => new ObjectiveRuleSet(null));
        }

        [Test]
        public void ControllersDoNotShareProgressEvenWhenRulesAreShared()
        {
            var rules = new ObjectiveRuleSet(new[] { Rule(ObjectiveCondition.HasCard, 3, "service") });
            var first = new ObjectiveProgressController(new ObjectiveProgressModel(rules), new View());
            var second = new ObjectiveProgressController(new ObjectiveProgressModel(rules), new View());
            first.Refresh(new Facts { Card = "service" });
            Assert.That(second.Phase, Is.Zero);
            Assert.That(second.Text, Is.Empty);
        }

        private static LevelObjectiveRule Rule(ObjectiveCondition condition, int phase, string id = null) =>
            new LevelObjectiveRule(condition, id, phase, "phase " + phase);

        private static ObjectiveProgressController Controller(View view, params LevelObjectiveRule[] rules) =>
            new ObjectiveProgressController(new ObjectiveProgressModel(new ObjectiveRuleSet(rules)), view);

        private sealed class Facts : IObjectiveFacts
        {
            public string Card;
            public bool HasWeapon => false;
            public bool HasCard(string id) => id == Card;
        }

        private sealed class View : IObjectiveProgressView
        {
            public int Refreshes;
            public void ObjectivesRefreshed() => Refreshes++;
        }
    }
}
