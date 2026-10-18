using System;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class EnvironmentPresentationTests
    {
        [TestCase(16f / 9)]
        [TestCase(1f)]
        [TestCase(9f / 16)]
        public void BeamFitsShortViewportWithTheConfiguredMargin(float aspect)
        {
            var profile = ScriptableObject.CreateInstance<CameraFramingProfile>();
            try
            {
                float size = profile.OrthographicSize(4.2f, aspect);
                float shortExtent = Mathf.Min(size, size * aspect);
                Assert.That(shortExtent - 4.2f, Is.EqualTo(profile.lightRangeMargin).Within(.0001f));
                Assert.That(profile.OrthographicSize(7, aspect), Is.GreaterThan(size));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void InvalidFramingCannotCreateAnInvisibleOrNonFiniteCamera()
        {
            var profile = ScriptableObject.CreateInstance<CameraFramingProfile>();
            try
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => profile.OrthographicSize(4.2f, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => profile.OrthographicSize(float.NaN, 1));
                profile.lightRangeMargin = -.1f;
                Assert.Throws<InvalidOperationException>(() => profile.Validate());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void LightDropoutTurnsOffAndReturnsWithoutMutatingSharedTuning()
        {
            var profile = ScriptableObject.CreateInstance<LocalLightProfile>();
            try
            {
                profile.cycleSeconds = 10;
                profile.minimumFactor = .75f;
                profile.darkIntervals = new[] { new Vector2(.2f, .3f), new Vector2(.7f, .72f) };
                profile.Validate();
                string before = JsonUtility.ToJson(profile);
                Assert.That(profile.Evaluate(2.5f), Is.Zero);
                Assert.That(profile.Evaluate(7.1f), Is.Zero);
                Assert.That(profile.Evaluate(3.1f), Is.InRange(.75f, 1));
                Assert.That(profile.Evaluate(12.5f), Is.Zero);
                Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(before));
                profile.darkIntervals[0] = new Vector2(.8f, .2f);
                Assert.Throws<InvalidOperationException>(() => profile.Validate());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }
    }
}
