using EmergencyShift.Features.Doors.Logic;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class DoorCrushTests
    {
        [TestCase(true, 1, 0, 3, true)]
        [TestCase(true, .5f, .49f, .01f, true)]
        [TestCase(true, 0, 1, 3, false)]
        [TestCase(true, .5f, .5f, 3, false)]
        [TestCase(true, 1, 0, 0, false)]
        [TestCase(false, 1, 0, 3, false)]
        [TestCase(true, float.NaN, 0, 3, false)]
        [TestCase(true, 1, -1, 3, false)]
        [TestCase(true, 1, 0, float.PositiveInfinity, false)]
        public void OnlyActualClosingTickCanRequestContacts(bool motion, float from, float to, float distance, bool expected)
        {
            var environment = new Contacts();
            var controller = new DoorCrushController(new DoorCrushModel(), environment);
            controller.Step(new DoorLeafStep(motion, from, to, new Vector2(distance, 0)));
            Assert.That(environment.Calls, Is.EqualTo(expected ? 1 : 0));
        }

        [Test]
        public void SeparateDoorsDoNotRetainTargetsOrPreviousMotion()
        {
            var first = new Contacts();
            var second = new Contacts();
            var a = new DoorCrushController(new DoorCrushModel(), first);
            var b = new DoorCrushController(new DoorCrushModel(), second);
            a.Step(new DoorLeafStep(true, 1, 0, Vector2.down));
            b.Step(new DoorLeafStep(false, 1, 0, Vector2.down));
            a.Step(new DoorLeafStep(true, 0, 1, Vector2.up));
            Assert.That(first.Calls, Is.EqualTo(1));
            Assert.That(second.Calls, Is.Zero);
        }

        private sealed class Contacts : IDoorCrushEnvironment
        {
            public int Calls;
            public void CrushContacts(Vector2 displacement) => Calls++;
        }
    }
}
