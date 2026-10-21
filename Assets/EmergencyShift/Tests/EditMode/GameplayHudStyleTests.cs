using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class GameplayHudStyleTests
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

        [TestCase("woundedFraction", float.NaN)]
        [TestCase("woundedFraction", -.1f)]
        [TestCase("woundedFraction", 1.1f)]
        [TestCase("criticalFraction", float.NaN)]
        [TestCase("criticalFraction", .7f)]
        [TestCase("trailDelay", -1)]
        [TestCase("trailDelay", float.PositiveInfinity)]
        [TestCase("trailDuration", 0)]
        [TestCase("hitDuration", float.NaN)]
        [TestCase("spareOpacity", 1.1f)]
        [TestCase("hitVolume", -1)]
        public void InvalidNumbersAndReversedHealthThresholdsAreRejected(string field, float value)
        {
            GameplayHudStyle style = Style();
            var serialized = new SerializedObject(style);
            serialized.FindProperty(field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => style.Validate());
        }

        private static IEnumerable<TestCaseData> InvalidColors()
        {
            foreach (string name in new[] { "healthy", "wounded", "critical", "lostHealth" })
            {
                for (int channel = 0; channel < 4; channel++)
                {
                    yield return new TestCaseData(name, channel, float.NaN);
                }

                yield return new TestCaseData(name, 3, 1.1f);
                yield return new TestCaseData(name, 3, -.1f);
            }
        }

        [TestCaseSource(nameof(InvalidColors))]
        public void InvalidColorComponentsAreRejected(string field, int channel, float value)
        {
            GameplayHudStyle style = Style();
            var serialized = new SerializedObject(style);
            SerializedProperty property = serialized.FindProperty(field);
            Color color = property.colorValue;
            color[channel] = value;
            property.colorValue = color;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => style.Validate());
        }

        [Test]
        public void FiniteHdrColorsAreNotClamped()
        {
            GameplayHudStyle style = Style();
            var serialized = new SerializedObject(style);
            serialized.FindProperty("healthy").colorValue = new Color(2, 3, 4, .5f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.DoesNotThrow(() => style.Validate());
            Assert.That(style.Healthy, Is.EqualTo(new Color(2, 3, 4, .5f)));
        }

        [Test]
        public void LegacySerializedValuesRemainVisibleThroughReadonlyApi()
        {
            GameplayHudStyle style = Style();
            JsonUtility.FromJsonOverwrite("{\"healthy\":{\"r\":0.1,\"g\":0.2,\"b\":0.3,\"a\":1},\"woundedFraction\":0.65,\"criticalFraction\":0.3,\"trailDelay\":0.21}", style);
            Assert.DoesNotThrow(() => style.Validate());
            Assert.That(style.Healthy, Is.EqualTo(new Color(.1f, .2f, .3f, 1)));
            Assert.That(style.WoundedFraction, Is.EqualTo(.65f));
            Assert.That(style.CriticalFraction, Is.EqualTo(.3f));
            Assert.That(style.TrailDelay, Is.EqualTo(.21f));
        }

        [Test]
        public void HitClipsCanBeReadByIndexWithoutExposingMutableArray()
        {
            GameplayHudStyle style = Style();
            Assert.That(style.HitSoundCount, Is.Zero);
            var clip = AudioClip.Create("Transient HUD config fixture", 16, 1, 8000, false);
            owned.Add(clip);
            var serialized = new SerializedObject(style);
            SerializedProperty clips = serialized.FindProperty("hitSounds");
            clips.arraySize = 1;
            clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(style.HitSoundCount, Is.EqualTo(1));
            Assert.That(style.HitSoundAt(0), Is.SameAs(clip));
            Assert.Throws<ArgumentOutOfRangeException>(() => style.HitSoundAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => style.HitSoundAt(1));
        }

        [Test]
        public void AllAuthoredStylesValidateWithoutMutation()
        {
            string[] assets = AssetDatabase.FindAssets("t:GameplayHudStyle", new[] { "Assets/EmergencyShift" });
            Assert.That(assets, Is.Not.Empty);
            foreach (string guid in assets)
            {
                var style = AssetDatabase.LoadAssetAtPath<GameplayHudStyle>(AssetDatabase.GUIDToAssetPath(guid));
                string before = EditorJsonUtility.ToJson(style);
                Assert.DoesNotThrow(() => style.Validate(), style.name);
                Assert.That(EditorJsonUtility.ToJson(style), Is.EqualTo(before));
            }
        }

        private GameplayHudStyle Style()
        {
            var result = ScriptableObject.CreateInstance<GameplayHudStyle>();
            owned.Add(result);
            return result;
        }
    }
}
