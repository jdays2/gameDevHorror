using System;
using NUnit.Framework;
using UnityEditor;
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
                Assert.That(shortExtent - 4.2f, Is.EqualTo(profile.LightRangeMargin).Within(.0001f));
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
                var serialized = new SerializedObject(profile);
                serialized.FindProperty("lightRangeMargin").floatValue = -.1f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
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
                JsonUtility.FromJsonOverwrite("{\"cycleSeconds\":10,\"minimumFactor\":0.75,\"darkIntervals\":[{\"x\":0.2,\"y\":0.3},{\"x\":0.7,\"y\":0.72}]}", profile);
                profile.Validate();
                string before = JsonUtility.ToJson(profile);
                Assert.That(profile.Evaluate(2.5f), Is.Zero);
                Assert.That(profile.Evaluate(7.1f), Is.Zero);
                Assert.That(profile.Evaluate(3.1f), Is.InRange(.75f, 1));
                Assert.That(profile.Evaluate(12.5f), Is.Zero);
                Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(before));
                var serialized = new SerializedObject(profile);
                serialized.FindProperty("darkIntervals").GetArrayElementAtIndex(0).vector2Value = new Vector2(.8f, .2f);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<InvalidOperationException>(() => profile.Validate());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }
    }
}
