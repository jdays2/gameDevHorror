using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class WeaponDefinitionConfigTests
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

        [TestCase(typeof(MagazineWeaponDefinition))]
        [TestCase(typeof(PumpShotgunDefinition))]
        public void OriginalNamesDeserializeIntoPrivateBaseAndSubtypeFields(Type type)
        {
            var definition = (WeaponDefinition)Own(ScriptableObject.CreateInstance(type));
            JsonUtility.FromJsonOverwrite("{\"contentId\":\"legacy-test\",\"damage\":3,\"maximumAmmo\":72,\"range\":8,\"secondsBetweenShots\":0.9,\"selectionSlot\":1,\"magazineCapacity\":4}", definition);
            WeaponSettings settings = definition.ToSettings();
            Assert.That(definition.ContentId, Is.EqualTo("legacy-test"));
            Assert.That(definition.Damage, Is.EqualTo(3));
            Assert.That(definition.MaximumAmmo, Is.EqualTo(72));
            Assert.That(definition.Range, Is.EqualTo(8));
            Assert.That(settings.SelectionSlot, Is.EqualTo(1));
            WeaponCycle cycle = settings.CreateCycle(12);
            Assert.That(cycle.MagazineCapacity, Is.EqualTo(4));
        }

        [Test]
        public void EditorAuthoringStillAssignsBaseVisualAndAudioReferencesByTheirOriginalNames()
        {
            var definition = Own(ScriptableObject.CreateInstance<PumpShotgunDefinition>());
            var visuals = Own(ScriptableObject.CreateInstance<ShotVisualProfile>());
            var audio = Own(ScriptableObject.CreateInstance<WeaponAudioProfile>());
            var data = new SerializedObject(definition);
            data.FindProperty("visuals").objectReferenceValue = visuals;
            data.FindProperty("audioProfile").objectReferenceValue = audio;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(definition.Visuals, Is.SameAs(visuals));
            Assert.That(definition.AudioProfile, Is.SameAs(audio));
            Assert.DoesNotThrow(() => definition.ToSettings());
        }

        [Test]
        public void EveryAuthoredWeaponDefinitionBuildsSettingsWithoutMutation()
        {
            string[] assets = AssetDatabase.FindAssets("t:WeaponDefinition", new[] { "Assets/EmergencyShift" });
            Assert.That(assets, Is.Not.Empty);
            foreach (string guid in assets)
            {
                var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                string before = EditorJsonUtility.ToJson(definition);
                Assert.DoesNotThrow(() => definition.ToSettings(), definition.name);
                Assert.That(EditorJsonUtility.ToJson(definition), Is.EqualTo(before));
            }
        }

        private T Own<T>(T value) where T : Object
        {
            owned.Add(value);
            return value;
        }
    }
}
