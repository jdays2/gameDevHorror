using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class EnemyConfigTests
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

        [TestCase("pursuitSpeed", float.NaN)]
        [TestCase("pursuitSpeed", -1)]
        [TestCase("hitStaggerSeconds", float.PositiveInfinity)]
        [TestCase("hitStaggerSeconds", -1)]
        [TestCase("hitPushSpeed", float.NegativeInfinity)]
        [TestCase("hitPushSpeed", -1)]
        public void InvalidSharedMovementCannotEnterEitherBrain(string field, float value)
        {
            var definition = Own(ScriptableObject.CreateInstance<EnemyDefinition>());
            var sleeper = Own(ScriptableObject.CreateInstance<SleeperBehaviourProfile>());
            var stalker = Own(ScriptableObject.CreateInstance<StalkerBehaviourProfile>());
            Set(definition, field, value);
            Assert.Throws<InvalidOperationException>(() => definition.Validate());
            Assert.Throws<InvalidOperationException>(() => sleeper.ToSettings(definition));
            Assert.Throws<InvalidOperationException>(() => stalker.ToSettings(definition));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void InvalidCapacityIsRejectedBeforeBrainInitialization(int capacity)
        {
            var definition = Own(ScriptableObject.CreateInstance<EnemyDefinition>());
            JsonUtility.FromJsonOverwrite("{\"maximumHealth\":" + capacity + "}", definition);
            Assert.Throws<InvalidOperationException>(() => definition.Validate());
        }

        [Test]
        public void OldFieldsStillDeserializeAndZeroPursuitRemainsSupported()
        {
            var definition = Own(ScriptableObject.CreateInstance<EnemyDefinition>());
            JsonUtility.FromJsonOverwrite("{\"maximumHealth\":8,\"pursuitSpeed\":0,\"attackDamage\":3,\"attackDuration\":1.3,\"attackCooldown\":2}", definition);
            var sleeper = Own(ScriptableObject.CreateInstance<SleeperBehaviourProfile>());
            var stalker = Own(ScriptableObject.CreateInstance<StalkerBehaviourProfile>());
            Assert.That(definition.MaximumHealth, Is.EqualTo(8));
            Assert.That(definition.ToMeleeSettings().Damage, Is.EqualTo(3));
            Assert.That(sleeper.ToSettings(definition).PursuitSpeed, Is.Zero);
            Assert.That(stalker.ToSettings(definition).PursuitSpeed, Is.Zero);
            Assert.That(definition.AttackDuration, Is.EqualTo(1.3f));
            Assert.That(definition.AttackCooldown, Is.EqualTo(2));
        }

        [TestCase("lightExposureSeconds", float.NaN)]
        [TestCase("lightExposureSeconds", 0)]
        [TestCase("exposureDecay", float.PositiveInfinity)]
        [TestCase("wakingSeconds", float.NegativeInfinity)]
        [TestCase("recoverySeconds", -1)]
        public void InvalidSleeperProfileIsReportedAndRejected(string field, float value)
        {
            var profile = Own(ScriptableObject.CreateInstance<SleeperBehaviourProfile>());
            var definition = Own(ScriptableObject.CreateInstance<EnemyDefinition>());
            Set(profile, field, value);
            Assert.That(profile.IsValid, Is.False);
            Assert.Throws<InvalidOperationException>(() => profile.ToSettings(definition));
        }

        [TestCase("retreatSpeed", float.NaN)]
        [TestCase("reactionDelay", 0)]
        [TestCase("eyeBurnSeconds", float.PositiveInfinity)]
        [TestCase("cooldownSeconds", -1)]
        public void InvalidStalkerProfileCannotEnterItsModel(string field, float value)
        {
            var profile = Own(ScriptableObject.CreateInstance<StalkerBehaviourProfile>());
            var definition = Own(ScriptableObject.CreateInstance<EnemyDefinition>());
            Set(profile, field, value);
            Assert.Throws<ArgumentException>(() => profile.ToSettings(definition));
        }

        [TestCase("speed", float.NaN)]
        [TestCase("wanderStepDistance", -1)]
        public void InvalidPatrolProfileCannotEnterItsModel(string field, float value)
        {
            var profile = Own(ScriptableObject.CreateInstance<EnemyPatrolProfile>());
            Set(profile, field, value);
            Assert.Throws<ArgumentException>(() => profile.ToSettings());
        }

        [Test]
        public void EveryAuthoredEnemyProfileIsValidWithoutSharedAssetMutation()
        {
            ValidateAssets<EnemyDefinition>(profile => profile.Validate());
            ValidateAssets<EnemyPatrolProfile>(profile => profile.ToSettings());
            var definition = Own(ScriptableObject.CreateInstance<EnemyDefinition>());
            ValidateAssets<SleeperBehaviourProfile>(profile => profile.ToSettings(definition));
            ValidateAssets<StalkerBehaviourProfile>(profile => profile.ToSettings(definition));
        }

        private static void ValidateAssets<T>(Action<T> validate) where T : Object
        {
            string[] assets = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets/EmergencyShift" });
            Assert.That(assets, Is.Not.Empty, typeof(T).Name);
            foreach (string guid in assets)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                string before = EditorJsonUtility.ToJson(asset);
                Assert.DoesNotThrow(() => validate(asset), asset.name);
                Assert.That(EditorJsonUtility.ToJson(asset), Is.EqualTo(before));
            }
        }

        private static void Set(Object target, string field, float value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).floatValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private T Own<T>(T value) where T : Object
        {
            owned.Add(value);
            return value;
        }
    }
}
