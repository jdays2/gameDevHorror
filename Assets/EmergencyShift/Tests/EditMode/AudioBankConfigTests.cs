using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class AudioBankConfigTests
    {
        [Test]
        public void BankCannotBeCastBackToMutableArrayOrList()
        {
            AudioClipBank bank = new AudioClip[1];
            IReadOnlyList<AudioClip> view = bank;
            Assert.That(view, Is.Not.InstanceOf<AudioClip[]>());
            Assert.That(view, Is.Not.InstanceOf<IList<AudioClip>>());
            Assert.That(view.Count, Is.EqualTo(1));
            Assert.That(typeof(AudioClipBank).GetProperty("Item").CanWrite, Is.False);
        }

        [Test]
        public void BankIdentityKeepsHistoryForTheSameArrayAndSeparatesEqualContent()
        {
            var array = new AudioClip[1];
            AudioClipBank first = array;
            AudioClipBank same = array;
            AudioClipBank separate = new AudioClip[1];
            var history = new Dictionary<AudioClipBank, int> { [first] = 7 };
            Assert.That(history[same], Is.EqualTo(7));
            Assert.That(history.ContainsKey(separate), Is.False);
        }

        [Test]
        public void DefaultEmptyBankAndEnumeratorKeepCollectionContract()
        {
            AudioClipBank bank = default;
            Assert.That(bank.Count, Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => { AudioClip ignored = bank[0]; });
            AudioClipBank.Enumerator enumerator = bank.GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => { AudioClip ignored = enumerator.Current; });
            Assert.That(enumerator.MoveNext(), Is.False);
            Assert.That(enumerator.MoveNext(), Is.False);
            enumerator.Reset();
            Assert.That(enumerator.MoveNext(), Is.False);
        }

        [Test]
        public void WarmedBankEnumerationAndDictionaryLookupAllocateNothing()
        {
            AudioClipBank bank = new AudioClip[3];
            var history = new Dictionary<AudioClipBank, int> { [bank] = 1 };
            bank.GetHashCode();
            int count = 0;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                foreach (AudioClip clip in bank)
                {
                    count++;
                }

                count += history[bank];
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(count, Is.EqualTo(4000));
            Assert.That(allocated, Is.Zero);
        }

        [TestCase(typeof(WeaponAudioProfile))]
        [TestCase(typeof(EnemyDeathAudioSet))]
        [TestCase(typeof(PlayerAudioSet))]
        [TestCase(typeof(DoorAudioSet))]
        [TestCase(typeof(SleeperAudioProfile))]
        [TestCase(typeof(StalkerAudioSet))]
        [TestCase(typeof(PlayerWorldAudioSet))]
        public void ProfileRuntimeApiDoesNotExposeWritableFieldsOrArrays(Type type)
        {
            Assert.That(type.GetFields(BindingFlags.Instance | BindingFlags.Public), Is.Empty);
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                Assert.That(property.CanWrite, Is.False, property.Name);
                Assert.That(property.PropertyType.IsArray, Is.False, property.Name);
            }

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                Assert.That(method.ReturnType.IsArray, Is.False, method.Name);
            }
        }

        [TestCase(typeof(WeaponAudioProfile))]
        [TestCase(typeof(EnemyDeathAudioSet))]
        [TestCase(typeof(PlayerAudioSet))]
        [TestCase(typeof(DoorAudioSet))]
        [TestCase(typeof(SleeperAudioProfile))]
        [TestCase(typeof(StalkerAudioSet))]
        [TestCase(typeof(PlayerWorldAudioSet))]
        public void AllAuthoredProfilesLoadAndValidateWithoutMutation(Type type)
        {
            string[] identifiers = AssetDatabase.FindAssets("t:" + type.Name, new[] { "Assets/EmergencyShift" });
            Assert.That(identifiers.Length, Is.GreaterThan(0), type.Name);
            foreach (string id in identifiers)
            {
                string path = AssetDatabase.GUIDToAssetPath(id);
                var profile = (ScriptableObject)AssetDatabase.LoadAssetAtPath(path, type);
                string before = JsonUtility.ToJson(profile);
                MethodInfo validate = type.GetMethod("Validate");
                Assert.DoesNotThrow(() => validate?.Invoke(profile, null), path);
                Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(before), path);
            }
        }

        [TestCase(typeof(WeaponAudioProfile), "shotGain")]
        [TestCase(typeof(PlayerWorldAudioSet), "stepVolume")]
        [TestCase(typeof(SleeperAudioProfile), "hearingDistance")]
        [TestCase(typeof(StalkerAudioSet), "eyeBurnGain")]
        [TestCase(typeof(EnemyDeathAudioSet), "volume")]
        public void NonFiniteAuthoringFailsBeforeAudioConsumption(Type type, string field)
        {
            ScriptableObject profile = ScriptableObject.CreateInstance(type);
            try
            {
                var data = new SerializedObject(profile);
                data.FindProperty(field).floatValue = float.NaN;
                data.ApplyModifiedPropertiesWithoutUndo();
                var error = Assert.Throws<TargetInvocationException>(() => type.GetMethod("Validate").Invoke(profile, null));
                Assert.That(error.InnerException, Is.InstanceOf<InvalidOperationException>());
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void OldSerializedClipNamesOrderNullSlotsAndSurfaceFallbackArePreserved()
        {
            var world = ScriptableObject.CreateInstance<PlayerWorldAudioSet>();
            var pickup = ScriptableObject.CreateInstance<PlayerAudioSet>();
            AudioClip first = AudioClip.Create("First clip", 64, 1, 44100, false);
            AudioClip second = AudioClip.Create("Second clip", 64, 1, 44100, false);
            try
            {
                var data = new SerializedObject(world);
                SetClips(data, "concreteSteps", first, null, second);
                SetClips(data, "woodSteps");
                SetClips(data, "tileSteps", second);
                data.FindProperty("stepVolume").floatValue = .24f;
                data.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(world.Steps(GroundSoundSurface.Wood).Equals(world.ConcreteSteps), Is.True);
                Assert.That(world.Steps(GroundSoundSurface.Concrete).Count, Is.EqualTo(3));
                Assert.That(world.Steps(GroundSoundSurface.Concrete)[1], Is.Null);
                Assert.That(world.Steps(GroundSoundSurface.Tile)[0], Is.SameAs(second));
                Assert.That(world.StepVolume, Is.EqualTo(.24f));
                var pickups = new SerializedObject(pickup);
                SetClips(pickups, "ammoPickup", first, second);
                SetClips(pickups, "upgradePickup", second);
                SetClips(pickups, "concreteCasings", first);
                pickups.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(pickup.Pickup(PickupKind.Ammo)[0], Is.SameAs(first));
                Assert.That(pickup.Pickup((PickupKind)99)[0], Is.SameAs(second));
                Assert.That(pickup.Casings(GroundSoundSurface.Wood)[0], Is.SameAs(first));
            }
            finally
            {
                Object.DestroyImmediate(world);
                Object.DestroyImmediate(pickup);
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }

        private static void SetClips(SerializedObject data, string field, params AudioClip[] clips)
        {
            SerializedProperty list = data.FindProperty(field);
            list.arraySize = clips.Length;
            for (int index = 0; index < clips.Length; index++)
            {
                list.GetArrayElementAtIndex(index).objectReferenceValue = clips[index];
            }
        }
    }
}
