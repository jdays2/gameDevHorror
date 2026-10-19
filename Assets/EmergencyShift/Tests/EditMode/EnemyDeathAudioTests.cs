using System.Collections.Generic;
using EmergencyShift.Core.Randomness;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class EnemyDeathAudioTests
    {
        private readonly List<Object> temporary = new List<Object>();
        private EnemyDeathAudioVariants variants;

        [SetUp]
        public void CreateWorldChoiceHistory()
        {
            variants = new EnemyDeathAudioVariants(new SystemRandomSource(new System.Random(1234)));
        }

        private AudioClip Clip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 64, 1, 48000, false);
            temporary.Add(clip);
            return clip;
        }

        private EnemyDeathAudioSet Bank(params AudioClip[] clips)
        {
            var bank = ScriptableObject.CreateInstance<EnemyDeathAudioSet>();
            temporary.Add(bank);
            var serialized = new SerializedObject(bank);
            var entries = serialized.FindProperty("clips");
            entries.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++)
            {
                entries.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return bank;
        }

        [TearDown]
        public void Cleanup()
        {
            variants.Dispose();
            foreach (Object item in temporary)
            {
                Object.DestroyImmediate(item);
            }
            temporary.Clear();
        }

        [Test]
        public void ConsecutiveChoicesAcrossConsumersNeverRepeat()
        {
            var bank = Bank(Clip("A"), Clip("B"), Clip("C"));
            string before = EditorJsonUtility.ToJson(bank);
            AudioClip previous = null;
            var seen = new HashSet<AudioClip>();
            for (int i = 0; i < 128; i++)
            {
                AudioClip selected = variants.Choose(bank);
                Assert.That(selected, Is.Not.Null);
                Assert.That(selected, Is.Not.SameAs(previous));
                seen.Add(selected);
                previous = selected;
            }
            Assert.That(seen.Count, Is.EqualTo(3));
            Assert.That(EditorJsonUtility.ToJson(bank), Is.EqualTo(before));
        }

        [Test]
        public void SeparateBanksHaveIndependentHistories()
        {
            AudioClip a = Clip("A");
            AudioClip b = Clip("B");
            var first = Bank(a, b);
            var second = Bank(a);
            AudioClip previous = variants.Choose(first);
            for (int i = 0; i < 16; i++)
            {
                variants.Choose(second);
                AudioClip next = variants.Choose(first);
                Assert.That(next, Is.Not.SameAs(previous));
                previous = next;
            }
        }

        [Test]
        public void MissingOrEmptyBankStaysSilent()
        {
            Assert.That(variants.Choose(null), Is.Null);
            Assert.That(variants.Choose(Bank()), Is.Null);
            Assert.That(variants.Choose(Bank(null, null)), Is.Null);
        }

        [Test]
        public void SingleRecordingAndDuplicateEntriesRemainUsable()
        {
            AudioClip clip = Clip("Only recording");
            var bank = Bank(null, clip, clip);
            for (int i = 0; i < 8; i++)
            {
                Assert.That(variants.Choose(bank), Is.SameAs(clip));
            }
        }

        [Test]
        public void ChoiceHistoryBelongsToTheWorldAndEndsWithIt()
        {
            var bank = Bank(Clip("A"), Clip("B"));
            using (var otherWorld = new EnemyDeathAudioVariants(new SystemRandomSource(new System.Random(1234))))
            {
                AudioClip first = variants.Choose(bank);
                Assert.That(variants.Choose(bank), Is.Not.SameAs(first));
                Assert.That(otherWorld.Choose(bank), Is.SameAs(first));
                variants.Dispose();
                variants.Dispose();
                Assert.Throws<System.ObjectDisposedException>(() => variants.Choose(bank));
                Assert.That(otherWorld.Choose(bank), Is.Not.Null);
            }
        }
    }
}
