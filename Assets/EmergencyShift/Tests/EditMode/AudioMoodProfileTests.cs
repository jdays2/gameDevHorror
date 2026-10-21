using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class AudioMoodProfileTests
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

        [TestCase("masterVolume", float.NaN)]
        [TestCase("masterVolume", 1.1f)]
        [TestCase("explorationGain", -1)]
        [TestCase("explorationGain", float.PositiveInfinity)]
        [TestCase("threatGain", 1.1f)]
        [TestCase("combatGain", float.NaN)]
        [TestCase("duckGain", -1)]
        [TestCase("lowHealthGain", float.PositiveInfinity)]
        [TestCase("riseSeconds", 0)]
        [TestCase("riseSeconds", float.NaN)]
        [TestCase("fallSeconds", -1)]
        [TestCase("threatHoldSeconds", 0)]
        [TestCase("combatHoldSeconds", float.PositiveInfinity)]
        [TestCase("awarenessDistance", .1f)]
        [TestCase("closeCombatDistance", 10)]
        [TestCase("observationInterval", 0)]
        [TestCase("perceptionMemorySeconds", -1)]
        [TestCase("perceptionMemorySeconds", float.NaN)]
        [TestCase("shotDuckSeconds", 0)]
        [TestCase("cueDuckSeconds", float.PositiveInfinity)]
        [TestCase("damageDuckSeconds", 0)]
        [TestCase("damageDuckSeconds", float.NaN)]
        [TestCase("combatAfterShotSeconds", 0)]
        [TestCase("deathFadeSeconds", 0)]
        [TestCase("duckResponseSeconds", .01f)]
        [TestCase("scheduleLeadSeconds", 0)]
        public void InvalidTuningAndReversedPerceptionRadiiAreRejected(string field, float value)
        {
            AudioMoodProfile profile = Profile();
            var serialized = new SerializedObject(profile);
            serialized.FindProperty(field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => profile.Validate());
        }

        [TestCase("crowdEnemiesForCombat", 0)]
        [TestCase("layerPriority", -1)]
        [TestCase("layerPriority", 257)]
        public void InvalidCountsAndVoicePrioritiesAreRejected(string field, int value)
        {
            AudioMoodProfile profile = Profile();
            var serialized = new SerializedObject(profile);
            serialized.FindProperty(field).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<InvalidOperationException>(() => profile.Validate());
        }

        [Test]
        public void ExistingSerializedNamesAndPriorHardcodedDefaultsAreRetained()
        {
            AudioMoodProfile profile = Profile();
            JsonUtility.FromJsonOverwrite("{\"masterVolume\":0.41,\"awarenessDistance\":12,\"closeCombatDistance\":3,\"crowdEnemiesForCombat\":4}", profile);
            Assert.DoesNotThrow(() => profile.Validate());
            Assert.That(profile.MasterVolume, Is.EqualTo(.41f));
            Assert.That(profile.AwarenessDistance, Is.EqualTo(12));
            Assert.That(profile.CloseCombatDistance, Is.EqualTo(3));
            Assert.That(profile.CrowdEnemiesForCombat, Is.EqualTo(4));
            Assert.That(profile.DamageDuckSeconds, Is.EqualTo(.6f));
            Assert.That(profile.CombatAfterShotSeconds, Is.EqualTo(2));
            Assert.That(profile.DeathFadeSeconds, Is.EqualTo(.55f));
            Assert.That(profile.DuckResponseSeconds, Is.EqualTo(.06f));
            Assert.That(profile.ScheduleLeadSeconds, Is.EqualTo(.15f));
            Assert.That(profile.LayerPriority, Is.EqualTo(120));
        }

        [Test]
        public void AllAuthoredProfilesValidateWithoutChangingThemOrLosingClips()
        {
            string[] assets = AssetDatabase.FindAssets("t:AudioMoodProfile", new[] { "Assets/EmergencyShift" });
            Assert.That(assets, Is.Not.Empty);
            bool anyClip = false;
            foreach (string guid in assets)
            {
                var profile = AssetDatabase.LoadAssetAtPath<AudioMoodProfile>(AssetDatabase.GUIDToAssetPath(guid));
                string before = EditorJsonUtility.ToJson(profile);
                Assert.DoesNotThrow(() => profile.Validate(), profile.name);
                Assert.That(EditorJsonUtility.ToJson(profile), Is.EqualTo(before));
                anyClip |= profile.Exploration != null || profile.Threat != null || profile.Combat != null;
            }

            Assert.That(anyClip, Is.True);
        }

        [Test]
        public void OptionalSilentLayersRemainValid()
        {
            AudioMoodProfile profile = Profile();
            Assert.That(profile.Exploration, Is.Null);
            Assert.That(profile.Threat, Is.Null);
            Assert.That(profile.Combat, Is.Null);
            Assert.DoesNotThrow(() => profile.Validate());
        }

        private AudioMoodProfile Profile()
        {
            var result = ScriptableObject.CreateInstance<AudioMoodProfile>();
            owned.Add(result);
            return result;
        }
    }
}
