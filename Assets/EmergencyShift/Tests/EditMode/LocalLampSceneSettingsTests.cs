using System;
using NUnit.Framework;

namespace EmergencyShift.Tests
{
    public sealed class LocalLampSceneSettingsTests
    {
        [Test]
        public void DifferentSceneRadiiAndMuteStayIndependent()
        {
            var first = new LocalLampSceneSettings { soundRadius = 1 };
            var second = new LocalLampSceneSettings { soundRadius = 3 };
            Assert.That(first.SoundGain(1.2f), Is.Zero);
            Assert.That(second.SoundGain(1.2f), Is.GreaterThan(0));
            first.soundRadius = 0;
            Assert.That(first.SoundGain(0), Is.Zero);
            Assert.That(second.soundRadius, Is.EqualTo(3));
        }

        [Test]
        public void SteadyLightDoesNotEnterFlickerWindows()
        {
            var settings = new LocalLampSceneSettings();
            Assert.That(settings.Brightness(4.95f), Is.Zero);
            Assert.That(settings.Brightness(5.2f), Is.Zero);
            settings.flickers = false;
            Assert.That(settings.Brightness(4.95f), Is.EqualTo(1));
            Assert.That(settings.Brightness(5.2f), Is.EqualTo(1));
        }

        [Test]
        public void InvalidDesignerValuesAreRejected()
        {
            var settings = new LocalLampSceneSettings { soundRadius = float.NaN };
            Assert.Throws<InvalidOperationException>(() => settings.Validate());
            settings.soundRadius = 2;
            settings.fixtureLength = 0;
            Assert.Throws<InvalidOperationException>(() => settings.Validate());
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.SoundGain(-1));
        }
    }
}
