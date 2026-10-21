using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class LightingConfigTests
    {
        private readonly List<Object> owned = new List<Object>();

        [TearDown]
        public void Cleanup()
        {
            foreach (Object value in owned)
            {
                Object.DestroyImmediate(value);
            }

            owned.Clear();
        }

        [TestCase("lightRangeMargin", float.NaN)]
        [TestCase("lightRangeMargin", -1)]
        [TestCase("zoomSmoothTime", float.PositiveInfinity)]
        [TestCase("zoomSmoothTime", 0)]
        [TestCase("unarmedViewRadius", float.NaN)]
        [TestCase("unarmedViewRadius", .1f)]
        public void InvalidCameraTuningIsRejected(string field, float value)
        {
            var profile = Own(ScriptableObject.CreateInstance<CameraFramingProfile>());
            Set(profile, field, value);
            Assert.Throws<InvalidOperationException>(() => profile.Validate());
        }

        [Test]
        public void FiniteInputsCannotOverflowTheCameraProjection()
        {
            var profile = Own(ScriptableObject.CreateInstance<CameraFramingProfile>());
            Assert.Throws<ArgumentOutOfRangeException>(() => profile.OrthographicSize(4, float.Epsilon));
            Assert.Throws<ArgumentOutOfRangeException>(() => profile.OrthographicSize(4, float.Epsilon, false));
            Set(profile, "lightRangeMargin", float.MaxValue);
            Assert.Throws<ArgumentOutOfRangeException>(() => profile.OrthographicSize(float.MaxValue, 1));
        }

        [TestCase(0, float.NaN)]
        [TestCase(1, float.PositiveInfinity)]
        [TestCase(2, float.NegativeInfinity)]
        [TestCase(3, float.NaN)]
        [TestCase(3, 1.1f)]
        [TestCase(3, -.1f)]
        public void InvalidLightColorCannotReachRenderer(int channel, float value)
        {
            var profile = Own(ScriptableObject.CreateInstance<LocalLightProfile>());
            var data = new SerializedObject(profile);
            SerializedProperty property = data.FindProperty("color");
            Color color = property.colorValue;
            color[channel] = value;
            property.colorValue = color;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => profile.Validate());
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidFlickerClockIsRejected(float clock)
        {
            var profile = Own(ScriptableObject.CreateInstance<LocalLightProfile>());
            Assert.Throws<ArgumentOutOfRangeException>(() => profile.Evaluate(clock));
        }

        [Test]
        public void FiniteClockCannotOverflowCycleOrNoiseArithmetic()
        {
            var profile = Own(ScriptableObject.CreateInstance<LocalLightProfile>());
            Set(profile, "cycleSeconds", .1f);
            Assert.Throws<ArgumentOutOfRangeException>(() => profile.Evaluate(float.MaxValue));
            Set(profile, "cycleSeconds", 6);
            Set(profile, "noiseFrequency", 10);
            Assert.Throws<ArgumentOutOfRangeException>(() => profile.Evaluate(float.MaxValue));
        }

        [Test]
        public void OldLightFieldsAndDropoutIntervalsRemainSerializedAndHdrIsRetained()
        {
            var profile = Own(ScriptableObject.CreateInstance<LocalLightProfile>());
            JsonUtility.FromJsonOverwrite("{\"color\":{\"r\":2,\"g\":3,\"b\":4,\"a\":0.5},\"radius\":3,\"intensity\":1.2,\"cycleSeconds\":10,\"darkIntervals\":[{\"x\":0.2,\"y\":0.3}]}", profile);
            Assert.DoesNotThrow(() => profile.Validate());
            Assert.That(profile.Color, Is.EqualTo(new Color(2, 3, 4, .5f)));
            Assert.That(profile.Radius, Is.EqualTo(3));
            Assert.That(profile.Intensity, Is.EqualTo(1.2f));
            Assert.That(profile.Evaluate(2.5f), Is.Zero);
        }

        [Test]
        public void LampClipArrayIsReadByIndexAndPriorPanDefaultIsRetained()
        {
            var sounds = Own(ScriptableObject.CreateInstance<LocalLampAudioSet>());
            var clip = Own(AudioClip.Create("Transient lamp config", 16, 1, 8000, false));
            var data = new SerializedObject(sounds);
            data.FindProperty("hum").objectReferenceValue = clip;
            SerializedProperty contacts = data.FindProperty("contacts");
            contacts.arraySize = 1;
            contacts.GetArrayElementAtIndex(0).objectReferenceValue = clip;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.DoesNotThrow(() => sounds.Validate());
            Assert.That(sounds.Hum, Is.SameAs(clip));
            Assert.That(sounds.ContactCount, Is.EqualTo(1));
            Assert.That(sounds.ContactAt(0), Is.SameAs(clip));
            Assert.That(sounds.MaximumPan, Is.EqualTo(.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => sounds.ContactAt(1));
            Set(sounds, "maximumPan", float.NaN);
            Assert.Throws<InvalidOperationException>(() => sounds.Validate());
        }

        [Test]
        public void EveryAuthoredCameraLightAndLampAudioAssetIsValidAndUnchanged()
        {
            ValidateAssets<CameraFramingProfile>("CameraFramingProfile", profile => profile.Validate());
            ValidateAssets<LocalLightProfile>("LocalLightProfile", profile => profile.Validate());
            ValidateAssets<LocalLampAudioSet>("LocalLampAudioSet", profile => profile.Validate());
        }

        private static void ValidateAssets<T>(string type, Action<T> validate) where T : Object
        {
            string[] assets = AssetDatabase.FindAssets("t:" + type, new[] { "Assets/EmergencyShift" });
            Assert.That(assets, Is.Not.Empty, type);
            foreach (string guid in assets)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                string before = EditorJsonUtility.ToJson(asset);
                Assert.DoesNotThrow(() => validate(asset), asset.name);
                Assert.That(EditorJsonUtility.ToJson(asset), Is.EqualTo(before));
            }
        }

        private static void Set(Object target, string field, float value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).floatValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private T Own<T>(T value) where T : Object
        {
            owned.Add(value);
            return value;
        }
    }
}
