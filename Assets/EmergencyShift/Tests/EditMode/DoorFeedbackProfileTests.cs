using System;
using System.Collections.Generic;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.AccessControl;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class DoorFeedbackProfileTests
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

        [TestCase("confirmationDelay", float.NaN)]
        [TestCase("confirmationDelay", -1)]
        [TestCase("denialCooldown", float.PositiveInfinity)]
        [TestCase("denialCooldown", 0)]
        [TestCase("volume", float.NaN)]
        [TestCase("volume", -1)]
        [TestCase("volume", 1.01f)]
        [TestCase("hearingDistance", float.NaN)]
        [TestCase("hearingDistance", 2)]
        [TestCase("hearingDistance", 1)]
        [TestCase("fullVolumeRadius", -1)]
        [TestCase("fullVolumeRadius", float.PositiveInfinity)]
        [TestCase("fullVolumeRadius", 7)]
        [TestCase("panDistance", 0)]
        [TestCase("panDistance", float.NaN)]
        [TestCase("maximumPan", float.PositiveInfinity)]
        [TestCase("maximumPan", -1)]
        [TestCase("maximumPan", 1.01f)]
        public void InvalidNumbersAndReversedFalloffAreRejectedBeforeUse(string field, float value)
        {
            DoorAccessFeedbackProfile profile = Profile();
            var serialized = new SerializedObject(profile);
            SerializedProperty property = serialized.FindProperty(field);
            Assert.That(property, Is.Not.Null, "Existing serialized field names must remain stable.");
            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => profile.Validate());
        }

        [Test]
        public void LegacySerializedNamesRetainValuesAndNewTuningUsesPriorConstants()
        {
            DoorAccessFeedbackProfile profile = Profile();
            JsonUtility.FromJsonOverwrite("{\"confirmationDelay\":0.37,\"denialCooldown\":0.9,\"volume\":0.41,\"hearingDistance\":9}", profile);
            Assert.DoesNotThrow(() => profile.Validate());
            Assert.That(profile.ConfirmationDelay, Is.EqualTo(.37f));
            Assert.That(profile.DenialCooldown, Is.EqualTo(.9f));
            Assert.That(profile.Volume, Is.EqualTo(.41f));
            Assert.That(profile.HearingDistance, Is.EqualTo(9));
            Assert.That(profile.FullVolumeRadius, Is.EqualTo(2));
            Assert.That(profile.PanDistance, Is.EqualTo(6));
            Assert.That(profile.MaximumPan, Is.EqualTo(.75f));
        }

        [Test]
        public void AllAuthoredProfilesRemainValidAndUnchanged()
        {
            string[] assets = AssetDatabase.FindAssets("t:DoorAccessFeedbackProfile", new[] { "Assets/EmergencyShift" });
            Assert.That(assets, Is.Not.Empty);
            foreach (string guid in assets)
            {
                var profile = AssetDatabase.LoadAssetAtPath<DoorAccessFeedbackProfile>(AssetDatabase.GUIDToAssetPath(guid));
                string before = EditorJsonUtility.ToJson(profile);
                Assert.DoesNotThrow(() => profile.Validate(), profile.name);
                Assert.That(EditorJsonUtility.ToJson(profile), Is.EqualTo(before));
                Assert.That(profile.Denied, Is.Not.Null, profile.name);
                Assert.That(profile.Granted, Is.Not.Null, profile.name);
            }
        }

        [Test]
        public void InvalidReplacementCannotChangeAnExistingReaderProfile()
        {
            DoorAccessFeedbackProfile valid = Profile();
            DoorAccessFeedbackProfile invalid = Profile();
            var serialized = new SerializedObject(invalid);
            serialized.FindProperty("confirmationDelay").floatValue = float.NaN;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var owner = new GameObject("Inactive feedback configuration fixture");
            owner.SetActive(false);
            owned.Add(owner);
            var reader = owner.AddComponent<DoorAccess2D>();
            reader.Construct(new DoorAccessControllerFactory(new UnityGameClock()));
            reader.ConfigureFeedback(valid);
            Assert.Throws<InvalidOperationException>(() => reader.ConfigureFeedback(invalid));
            Assert.That(reader.FeedbackProfile, Is.SameAs(valid));
        }

        private DoorAccessFeedbackProfile Profile()
        {
            var result = ScriptableObject.CreateInstance<DoorAccessFeedbackProfile>();
            owned.Add(result);
            return result;
        }
    }
}
