using System;
using System.Collections.Generic;
using System.Reflection;
using EmergencyShift.Features.LevelObjectives.Logic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class LevelConfigTests
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

        [Test]
        public void UnknownObjectiveConditionDoesNotMatchAsAlways()
        {
            Assert.That(ObjectiveRuleMatcher.Matches((ObjectiveCondition)99, null, new Facts()), Is.False);
            Assert.Throws<InvalidOperationException>(() => new LevelObjectiveRule((ObjectiveCondition)99, null, 0, ""));
        }

        [Test]
        public void MissingCardIdOrNegativePhaseIsRejected()
        {
            Assert.Throws<InvalidOperationException>(() => new LevelObjectiveRule(ObjectiveCondition.HasCard, " ", 0, ""));
            Assert.Throws<InvalidOperationException>(() => new LevelObjectiveRule(ObjectiveCondition.Always, null, -1, ""));
        }

        [TestCase(ObjectiveCondition.Always, null, false, true)]
        [TestCase(ObjectiveCondition.HasCard, "service", false, true)]
        [TestCase(ObjectiveCondition.HasCard, "evacuation", false, false)]
        [TestCase(ObjectiveCondition.MissingCard, "service", false, false)]
        [TestCase(ObjectiveCondition.MissingCard, "evacuation", false, true)]
        [TestCase(ObjectiveCondition.WeaponMissing, null, false, true)]
        [TestCase(ObjectiveCondition.WeaponMissing, null, true, false)]
        public void KnownConditionsUseOnlyInventoryFacts(ObjectiveCondition condition, string id, bool weapon, bool expected)
        {
            Assert.That(ObjectiveRuleMatcher.Matches(condition, id, new Facts { HasWeapon = weapon }), Is.EqualTo(expected));
        }

        [TestCase(ObjectiveCondition.HasCard)]
        [TestCase(ObjectiveCondition.MissingCard)]
        [TestCase(ObjectiveCondition.WeaponMissing)]
        public void UnavailableFactsDoNotSatisfyAConditionalGoal(ObjectiveCondition condition)
        {
            Assert.That(ObjectiveRuleMatcher.Matches(condition, "service", null), Is.False);
        }

        [Test]
        public void DuplicateLevelIdsRejectLookupInsteadOfReturningFirstMatch()
        {
            LevelCatalog catalog = Catalog(Definition("same-level"), Definition("same-level"));
            Assert.Throws<InvalidOperationException>(() => catalog.Find("same-level"));
        }

        [Test]
        public void MissingDefinitionIsNotSilentlySkipped()
        {
            LevelCatalog catalog = Catalog(Definition("first"), null);
            Assert.Throws<InvalidOperationException>(() => catalog.Find("first"));
        }

        [Test]
        public void FirstMatchCannotHideALaterInvalidDefinition()
        {
            LevelDefinition second = Definition("second");
            JsonUtility.FromJsonOverwrite("{\"saveRevision\":0}", second);
            LevelCatalog catalog = Catalog(Definition("first"), second);
            Assert.Throws<InvalidOperationException>(() => catalog.Find("first"));
        }

        [Test]
        public void LegacyNullCatalogArrayIsAnEmptyCatalog()
        {
            LevelCatalog catalog = Create<LevelCatalog>();
            Assert.That(catalog.DefinitionCount, Is.Zero);
            Assert.That(catalog.Find("unknown"), Is.Null);
        }

        [TestCase(typeof(LevelDefinition))]
        [TestCase(typeof(LevelCatalog))]
        public void RuntimeConfigApiDoesNotExposeMutableArraysOrFields(Type type)
        {
            Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly), Is.Empty);
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Assert.That(property.CanWrite, Is.False);
                Assert.That(property.PropertyType.IsArray, Is.False);
            }
        }

        [TestCase(typeof(LevelDefinition))]
        [TestCase(typeof(LevelCatalog))]
        public void CurrentAuthoredAssetsValidateWithoutMutation(Type type)
        {
            string[] ids = AssetDatabase.FindAssets("t:" + type.Name, new[] { "Assets/EmergencyShift" });
            // FirstShift's definition was deliberately removed; do not recreate content for a test.
            TestContext.Out.WriteLine(type.Name + " authored assets: " + ids.Length);
            foreach (string id in ids)
            {
                Object asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(id), type);
                string before = JsonUtility.ToJson(asset);
                Assert.DoesNotThrow(() => type.GetMethod("Validate").Invoke(asset, null), asset.name);
                Assert.That(JsonUtility.ToJson(asset), Is.EqualTo(before));
            }
        }

        [Test]
        public void OldInlineRuleFieldsAndPriorityOrderRemainSerialized()
        {
            LevelDefinition definition = Definition("ordered");
            JsonUtility.FromJsonOverwrite("{\"objectives\":[{\"condition\":1,\"cardId\":\"service\",\"phase\":3,\"text\":\"First\"},{\"condition\":0,\"phase\":1,\"text\":\"Fallback\"}]}", definition);
            definition.Validate();
            Assert.That(definition.ObjectiveCount, Is.EqualTo(2));
            Assert.That(definition.ObjectiveAt(0).Condition, Is.EqualTo(ObjectiveCondition.HasCard));
            Assert.That(definition.ObjectiveAt(0).CardId, Is.EqualTo("service"));
            Assert.That(definition.ObjectiveAt(0).Phase, Is.EqualTo(3));
            Assert.That(definition.ObjectiveAt(0).Text, Is.EqualTo("First"));
            Assert.That(definition.ObjectiveAt(1).Text, Is.EqualTo("Fallback"));
            Assert.That((int)ObjectiveCondition.Always, Is.Zero);
            Assert.That((int)ObjectiveCondition.WeaponMissing, Is.EqualTo(3));
        }

        [TestCase("{\"levelId\":\" \"}")]
        [TestCase("{\"saveRevision\":0}")]
        [TestCase("{\"exitCardId\":\" \"}")]
        [TestCase("{\"objectives\":[{\"condition\":99}]}")]
        public void InvalidHeaderOrRuleCannotEnterCatalog(string patch)
        {
            LevelDefinition definition = Definition("valid");
            JsonUtility.FromJsonOverwrite(patch, definition);
            Assert.Throws<InvalidOperationException>(() => Catalog(definition).Find("valid"));
        }

        [Test]
        public void RejectedEditorIdentityChangeDoesNotPartiallyMutateAsset()
        {
            LevelDefinition definition = Definition("before");
            string before = JsonUtility.ToJson(definition);
            Assert.Throws<InvalidOperationException>(() => EmergencyShift.Editor.LevelPersistenceAuthoring.ConfigureIdentity(definition, "after", "invalid"));
            Assert.That(JsonUtility.ToJson(definition), Is.EqualTo(before));
        }

        [Test]
        public void RejectedEditorCatalogReplacementKeepsOldEntries()
        {
            LevelDefinition first = Definition("first");
            LevelCatalog catalog = Catalog(first);
            Assert.Throws<InvalidOperationException>(() => EmergencyShift.Editor.LevelPersistenceAuthoring.ReplaceCatalogEntries(catalog, new[] { first, Definition("first") }));
            Assert.That(catalog.DefinitionCount, Is.EqualTo(1));
            Assert.That(catalog.DefinitionAt(0), Is.SameAs(first));
        }

        [Test]
        public void ExactIdsAndReservedSceneMetadataAreRetained()
        {
            LevelDefinition first = Definition("Exact");
            LevelDefinition second = Definition("exact");
            JsonUtility.FromJsonOverwrite("{\"scenePath\":\"Assets/EmergencyShift/Scenes/NotBuiltYet.unity\",\"nextLevelId\":\"reserved-next\"}", first);
            LevelCatalog catalog = Catalog(first, second);
            Assert.That(catalog.Find("Exact"), Is.SameAs(first));
            Assert.That(catalog.Find("exact"), Is.SameAs(second));
            Assert.That(catalog.Find("unknown"), Is.Null);
            Assert.That(first.NextLevelId, Is.EqualTo("reserved-next"));
        }

        private T Create<T>() where T : ScriptableObject
        {
            T value = ScriptableObject.CreateInstance<T>();
            owned.Add(value);
            return value;
        }

        private LevelDefinition Definition(string id)
        {
            LevelDefinition definition = Create<LevelDefinition>();
            EmergencyShift.Editor.LevelPersistenceAuthoring.ConfigureIdentity(definition, id);
            return definition;
        }

        private LevelCatalog Catalog(params LevelDefinition[] definitions)
        {
            LevelCatalog catalog = Create<LevelCatalog>();
            var serialized = new SerializedObject(catalog);
            SerializedProperty entries = serialized.FindProperty("levels");
            entries.arraySize = definitions.Length;
            for (int index = 0; index < definitions.Length; index++)
            {
                entries.GetArrayElementAtIndex(index).objectReferenceValue = definitions[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        private sealed class Facts : IObjectiveFacts
        {
            public bool HasWeapon { get; set; }
            public bool HasCard(string id) => id == "service";
        }
    }
}
