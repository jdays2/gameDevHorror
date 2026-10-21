using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class WeaponCatalogConfigTests
    {
        [Serializable]
        private sealed class CatalogFixture
        {
            public WeaponDefinition[] weapons;
        }

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

        [Test]
        public void DuplicateIdsCannotSelectAnArbitraryFirstEntry()
        {
            WeaponDefinition first = Definition("same");
            WeaponDefinition second = Definition("same");
            WeaponCatalog catalog = Catalog(first, second);
            Assert.Throws<InvalidOperationException>(() => catalog.Validate());
            Assert.Throws<InvalidOperationException>(() => catalog.Find("same"));
            Assert.Throws<InvalidOperationException>(() => catalog.ToSettings());
        }

        [Test]
        public void ConflictingSlotsKeepTheExistingSimulationCatalogRule()
        {
            WeaponDefinition first = Definition("pistol-a");
            WeaponDefinition second = Definition("pistol-b");
            WeaponCatalog catalog = Catalog(first, second);
            Assert.Throws<InvalidOperationException>(() => catalog.Validate());
            Assert.Throws<InvalidOperationException>(() => catalog.Find("pistol-b"));
            Assert.Throws<InvalidOperationException>(() => catalog.ToSettings());
        }

        [Test]
        public void MissingDefinitionOrIdIsRejectedBeforeLookup()
        {
            WeaponCatalog missing = Catalog((WeaponDefinition)null);
            Assert.Throws<InvalidOperationException>(() => missing.Find("unknown"));
            WeaponCatalog emptyId = Catalog(Definition(" "));
            Assert.Throws<InvalidOperationException>(() => emptyId.Validate());
        }

        [Test]
        public void OriginalArrayNameAndObjectReferencesSupportTransientJsonFixtures()
        {
            var catalog = Own(ScriptableObject.CreateInstance<WeaponCatalog>());
            WeaponDefinition first = Definition("first");
            WeaponDefinition second = Definition("second");
            JsonUtility.FromJsonOverwrite("{\"selectionSlot\":1}", second);
            string json = JsonUtility.ToJson(new CatalogFixture { weapons = new[] { first, second } });
            JsonUtility.FromJsonOverwrite(json, catalog);
            Assert.DoesNotThrow(() => catalog.Validate());
            Assert.That(catalog.DefinitionCount, Is.EqualTo(2));
            Assert.That(catalog.DefinitionAt(0), Is.SameAs(first));
            Assert.That(catalog.DefinitionAt(1), Is.SameAs(second));
        }

        [Test]
        public void NullAndEmptyCatalogsKeepTheirDocumentedLookupPolicyAndIndexBounds()
        {
            var catalog = Own(ScriptableObject.CreateInstance<WeaponCatalog>());
            Assert.That(catalog.DefinitionCount, Is.Zero);
            Assert.That(catalog.Find("unknown"), Is.Null);
            Assert.Throws<InvalidOperationException>(() => catalog.ToSettings());
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.DefinitionAt(0));
            WeaponCatalog empty = Catalog();
            JsonUtility.FromJsonOverwrite("{\"weapons\":[]}", empty);
            Assert.DoesNotThrow(() => empty.ToSettings());
            Assert.That(empty.Find(null), Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => empty.DefinitionAt(-1));
        }

        [Test]
        public void AuthoredCatalogsKeepValidIdsAndReferencesWithoutMutation()
        {
            string[] assets = AssetDatabase.FindAssets("t:WeaponCatalog", new[] { "Assets/EmergencyShift" });
            Assert.That(assets, Is.Not.Empty);
            foreach (string guid in assets)
            {
                var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalog>(AssetDatabase.GUIDToAssetPath(guid));
                string before = EditorJsonUtility.ToJson(catalog);
                Assert.DoesNotThrow(() => catalog.ToSettings(), catalog.name);
                for (int index = 0; index < catalog.DefinitionCount; index++)
                {
                    WeaponDefinition definition = catalog.DefinitionAt(index);
                    Assert.That(catalog.Find(definition.ContentId), Is.SameAs(definition));
                }

                Assert.That(EditorJsonUtility.ToJson(catalog), Is.EqualTo(before));
            }
        }

        private WeaponCatalog Catalog(params WeaponDefinition[] definitions)
        {
            var catalog = Own(ScriptableObject.CreateInstance<WeaponCatalog>());
            var data = new SerializedObject(catalog);
            SerializedProperty values = data.FindProperty("weapons");
            values.arraySize = definitions.Length;
            for (int index = 0; index < definitions.Length; index++)
            {
                values.GetArrayElementAtIndex(index).objectReferenceValue = definitions[index];
            }

            data.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        private WeaponDefinition Definition(string id)
        {
            var definition = Own(ScriptableObject.CreateInstance<WeaponDefinition>());
            JsonUtility.FromJsonOverwrite("{\"contentId\":\"" + id + "\"}", definition);
            return definition;
        }

        private T Own<T>(T value) where T : Object
        {
            owned.Add(value);
            return value;
        }
    }
}
