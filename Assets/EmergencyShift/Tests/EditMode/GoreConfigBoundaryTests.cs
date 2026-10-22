using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class GoreConfigBoundaryTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void InvalidGoreConfigCannotSelectADeathComposition(bool duplicateIds)
        {
            var profile = ScriptableObject.CreateInstance<GoreProfile>();
            try
            {
                if (duplicateIds)
                {
                    Set(profile, "variants", new[] { new GoreVariant("same"), new GoreVariant("same") });
                }
                else
                {
                    var data = new SerializedObject(profile);
                    data.FindProperty("gravity").floatValue = float.NaN;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }

                Assert.Throws<InvalidOperationException>(() => profile.ChooseVariant(new Core.Randomness.SystemRandomSource(new System.Random(15))));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [TestCase("poolSize", float.NaN)]
        [TestCase("poolSize", 0)]
        [TestCase("releaseDelay", float.PositiveInfinity)]
        [TestCase("releaseDelay", -1)]
        [TestCase("gravity", float.NegativeInfinity)]
        [TestCase("gravity", 0)]
        public void InvalidGoreScalarsRejectBeforeRandomConsumption(string field, float value)
        {
            var profile = ScriptableObject.CreateInstance<GoreProfile>();
            var random = new FixedRandom();
            try
            {
                Set(profile, field, value);
                Assert.Throws<InvalidOperationException>(() => profile.ChooseVariant(random));
                Assert.That(random.Reads, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void RestoredOnlyVariantsRemainAddressableAndDoNotParticipateInNewChoice()
        {
            var profile = ScriptableObject.CreateInstance<GoreProfile>();
            try
            {
                Set(profile, "variants", new[] { new GoreVariant("old", true), new GoreVariant("new") });
                var random = new FixedRandom();
                Assert.That(profile.ChooseVariant(random).Id, Is.EqualTo("new"));
                Assert.That(random.Maximum, Is.EqualTo(1));
                Assert.That(profile.FindVariant("old").RestorationOnly, Is.True);
                Assert.That(profile.FindVariant("OLD"), Is.Null);
                Assert.That(profile.VariantCount, Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void InvalidRandomIndexIsRejected(int value)
        {
            var profile = ScriptableObject.CreateInstance<GoreProfile>();
            try
            {
                Assert.Throws<InvalidOperationException>(() => profile.ChooseVariant(new FixedRandom { Index = value }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [TestCase(typeof(GoreProfile))]
        [TestCase(typeof(BloodPoolProfile))]
        public void EveryAuthoredProfileValidatesWithoutMutation(Type type)
        {
            string[] ids = AssetDatabase.FindAssets("t:" + type.Name, new[] { "Assets/EmergencyShift" });
            Assert.That(ids, Is.Not.Empty);
            foreach (string id in ids)
            {
                UnityEngine.Object profile = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(id), type);
                string before = EditorJsonUtility.ToJson(profile);
                type.GetMethod("Validate").Invoke(profile, null);
                Assert.That(EditorJsonUtility.ToJson(profile), Is.EqualTo(before));
            }
        }

        [TestCase(typeof(GoreProfile))]
        [TestCase(typeof(BloodPoolProfile))]
        [TestCase(typeof(GoreVariant))]
        [TestCase(typeof(GorePiece))]
        [TestCase(typeof(BloodPoolVariant))]
        public void ConfigApiDoesNotExposeWritableFieldsPropertiesOrArrays(Type type)
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly;
            Assert.That(type.GetFields(flags), Is.Empty);
            foreach (var property in type.GetProperties(flags))
            {
                Assert.That(property.GetSetMethod(), Is.Null, property.Name);
                Assert.That(property.PropertyType.IsArray, Is.False, property.Name);
            }
        }

        [Test]
        public void ChoosingGoreDoesNotConsumeGlobalUnityRandomState()
        {
            var profile = ScriptableObject.CreateInstance<GoreProfile>();
            UnityEngine.Random.State original = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(122);
                UnityEngine.Random.State before = UnityEngine.Random.state;
                float expected = UnityEngine.Random.value;
                UnityEngine.Random.state = before;
                profile.ChooseVariant(new Core.Randomness.SystemRandomSource(new System.Random(15)));
                Assert.That(UnityEngine.Random.value, Is.EqualTo(expected));
            }
            finally
            {
                UnityEngine.Random.state = original;
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void LevelCosmeticFactoriesAreSeparateFromGameplayAndOtherLevels()
        {
            var parent = new Zenject.DiContainer();
            parent.Bind<Core.Randomness.RandomSourceFactory>().AsSingle().WithArguments(new System.Random(93));
            var first = new Zenject.DiContainer(parent);
            var second = new Zenject.DiContainer(parent);
            LevelRegistriesInstaller.Install(first);
            LevelRegistriesInstaller.Install(second);
            var cosmetic = first.ResolveId<Core.Randomness.RandomSourceFactory>(Core.Randomness.RandomSourceIds.Cosmetic);
            var sibling = second.ResolveId<Core.Randomness.RandomSourceFactory>(Core.Randomness.RandomSourceIds.Cosmetic);
            var gameplay = parent.Resolve<Core.Randomness.RandomSourceFactory>();
            Assert.That(cosmetic, Is.Not.SameAs(sibling));
            Assert.That(cosmetic, Is.Not.SameAs(gameplay));
            for (int index = 0; index < 1000; index++)
            {
                cosmetic.Create().NextUnit();
            }

            var expected = new Core.Randomness.RandomSourceFactory(new System.Random(93));
            Assert.That(gameplay.Create().NextUnit(), Is.EqualTo(expected.Create().NextUnit()));
        }

        private static void Set(object target, string field, object value) => target.GetType().GetField(field,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(target, value);

        private sealed class FixedRandom : Core.Randomness.IRandomSource
        {
            public int Index;
            public int Reads;
            public int Maximum;
            public int NextInt(int minimumInclusive, int maximumExclusive)
            {
                Reads++;
                Maximum = maximumExclusive;
                return Index;
            }

            public double NextUnit() => 0;
        }
    }
}
