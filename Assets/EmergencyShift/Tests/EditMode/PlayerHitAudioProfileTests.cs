using System;
using EmergencyShift.Features.PlayerHitAudio.Config;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class PlayerHitAudioProfileTests
    {
        private const string Path = "Assets/EmergencyShift/Data/Audio/PlayerHitAudio.asset";

        [TestCase("volume", -.01f)]
        [TestCase("volume", 1.01f)]
        [TestCase("volume", float.NaN)]
        [TestCase("volume", float.PositiveInfinity)]
        [TestCase("pitchVariation", -.01f)]
        [TestCase("pitchVariation", .051f)]
        [TestCase("pitchVariation", float.NaN)]
        [TestCase("overlapFadeSeconds", 0)]
        [TestCase("overlapFadeSeconds", .101f)]
        [TestCase("overlapFadeSeconds", float.PositiveInfinity)]
        public void InvalidTuningIsRejectedBeforePlayback(string field, float value)
        {
            var original = AssetDatabase.LoadAssetAtPath<PlayerHitAudioProfile>(Path);
            Assert.That(original, Is.Not.Null);
            var copy = Object.Instantiate(original);
            try
            {
                using (var data = new SerializedObject(copy))
                {
                    data.FindProperty(field).floatValue = value;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                Assert.Throws<InvalidOperationException>(copy.Validate);
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }

        [Test]
        public void AuthoredBankIsReadOnlyCompleteAndDoesNotStorePlaybackHistory()
        {
            var profile = AssetDatabase.LoadAssetAtPath<PlayerHitAudioProfile>(Path);
            Assert.That(profile, Is.Not.Null);
            string before = JsonUtility.ToJson(profile);
            profile.Validate();
            Assert.That(profile.Clips.Count, Is.EqualTo(4));
            Assert.That(profile.Clips, Is.Not.InstanceOf<AudioClip[]>());
            foreach (AudioClip clip in profile.Clips)
            {
                Assert.That(clip.channels, Is.EqualTo(1));
                Assert.That(clip.frequency, Is.EqualTo(48000));
                Assert.That(clip.length, Is.LessThan(.4f));
            }
            Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(before));
            Assert.That(typeof(PlayerHitAudioProfile).GetFields(System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance), Is.Empty);
        }

        [Test]
        public void MissingAndRepeatedClipReferencesFailExplicitly()
        {
            var profile = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PlayerHitAudioProfile>(Path));
            try
            {
                using (var data = new SerializedObject(profile))
                {
                    SerializedProperty bank = data.FindProperty("clips");
                    bank.GetArrayElementAtIndex(1).objectReferenceValue = bank.GetArrayElementAtIndex(0).objectReferenceValue;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    Assert.Throws<InvalidOperationException>(profile.Validate);
                    bank.GetArrayElementAtIndex(1).objectReferenceValue = null;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    Assert.Throws<InvalidOperationException>(profile.Validate);
                }
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }
    }
}
