using System;
using EmergencyShift.Core.Composition;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Health.Logic;
using EmergencyShift.Features.Interaction;
using EmergencyShift.Features.Interaction.Config;
using EmergencyShift.Features.Interaction.Logic;
using EmergencyShift.Features.PlayerInput.Logic;
using EmergencyShift.Features.PlayerMovement;
using EmergencyShift.Features.PlayerRules.Config;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Tests
{
    public sealed class PlayerRulesTests
    {
        [Test]
        public void SharedChangesApplyToLegacyDefaultsWhileAuthoredDifferencesRemain()
        {
            PlayerRulesSettings rules = Changed();
            Assert.That(rules.ProjectHealth(2, .7f).Maximum, Is.EqualTo(9));
            Assert.That(rules.ProjectHealth(2, .7f).ProtectionSeconds, Is.EqualTo((double).9f));
            Assert.That(rules.ProjectHealth(3, .2f).Maximum, Is.EqualTo(3));
            Assert.That(rules.ProjectHealth(3, .2f).ProtectionSeconds, Is.EqualTo((double).2f));
            Assert.That(rules.ProjectMoveSpeed(3.5f), Is.EqualTo(5));
            Assert.That(rules.ProjectMoveSpeed(4), Is.EqualTo(4));
            PlayerInteractionSettings ordinary = rules.ProjectInteraction(LegacyInteraction());
            Assert.That(ordinary.Reach, Is.EqualTo(2));
            Assert.That(ordinary.PickupRadius, Is.EqualTo(.9f));
            Assert.That(ordinary.NoticeSeconds, Is.EqualTo(2.4f));
            Assert.That(ordinary.MaximumNotices, Is.EqualTo(12));
            PlayerInteractionSettings manual = rules.ProjectInteraction(new PlayerInteractionSettings(1, .01f, .6f, 2, .4f));
            Assert.That(manual.Reach, Is.EqualTo(1));
            Assert.That(manual.PickupRadius, Is.EqualTo(.4f));
            Assert.That(manual.NoticeSeconds, Is.EqualTo(.6f));
            Assert.That(manual.MaximumNotices, Is.EqualTo(2));
        }

        [Test]
        public void ExplicitOverrideCanKeepValuesThatEqualTheOldDefault()
        {
            PlayerRulesSettings rules = Changed();
            Assert.That(rules.ProjectHealth(2, .7f, true).Maximum, Is.EqualTo(2));
            Assert.That(rules.ProjectMoveSpeed(3.5f, true), Is.EqualTo(3.5f));
            PlayerInteractionSettings resolved = rules.ProjectInteraction(LegacyInteraction(), true);
            Assert.That(resolved.PickupRadius, Is.EqualTo(.65f));
            Assert.That(resolved.Reach, Is.EqualTo(1.6f));
        }

        [Test]
        public void PersonalHealthAndCapacityNeverMutateSharedSettings()
        {
            PlayerRulesSettings rules = Changed();
            var factory = new HealthModelFactory(new Clock());
            HealthModel first = factory.Create(rules.ProjectHealth(2, .7f));
            HealthModel second = factory.Create(rules.ProjectHealth(2, .7f));
            Assert.That(new SetHealthCapacityCommand(first, 20, 15).Execute().Success, Is.True);
            Assert.That(new DamageHealthCommand(first, 2).Execute().Success, Is.True);
            Assert.That(first.Maximum, Is.EqualTo(20));
            Assert.That(first.Current, Is.EqualTo(13));
            Assert.That(second.Maximum, Is.EqualTo(9));
            Assert.That(second.Current, Is.EqualTo(9));
            Assert.That(rules.MaximumHealth, Is.EqualTo(9));
        }

        [Test]
        public void FactoriesUseTheInjectedProfileRatherThanHardcodedDefaults()
        {
            PlayerRulesSettings rules = Changed();
            var input = new Input { Movement = Vector2.right };
            var movement = new PlayerMovementControllerFactory(input, new GameplayInputBlock(), rules).Create(3.5f);
            movement.SampleInput();
            Assert.That(movement.Velocity, Is.EqualTo(Vector2.right * 5));
            var notices = new PlayerInteractionControllerFactory(new Clock(), rules).CreateNotices(LegacyInteraction());
            for (int index = 0; index < 30; index++)
            {
                new ShowPickupNoticeCommand(notices, "Notice " + index).Execute();
            }

            Assert.That(notices.PendingCount, Is.EqualTo(12));
        }

        [TestCase("maximumHealth", 0)]
        [TestCase("protectionSeconds", -1)]
        [TestCase("protectionSeconds", float.NaN)]
        [TestCase("moveSpeed", float.NaN)]
        [TestCase("moveSpeed", -1)]
        [TestCase("reach", .01f)]
        [TestCase("pickupRadius", .01f)]
        [TestCase("pickupRadius", float.PositiveInfinity)]
        [TestCase("tieDistance", -1)]
        [TestCase("noticeSeconds", .1f)]
        [TestCase("maximumNotices", 0)]
        [TestCase("maximumNotices", 65)]
        public void InvalidProfileFailsBeforeWorldBindings(string field, float value)
        {
            var owner = new GameObject("Invalid player rules world");
            var profile = ScriptableObject.CreateInstance<PlayerRulesProfile>();
            try
            {
                var data = new SerializedObject(profile);
                SerializedProperty property = data.FindProperty(field);
                if (property.propertyType == SerializedPropertyType.Integer)
                {
                    property.intValue = (int)value;
                }
                else
                {
                    property.floatValue = value;
                }

                data.ApplyModifiedPropertiesWithoutUndo();
                var installer = owner.AddComponent<WorldInstaller>();
                EmergencyShift.Editor.ThreatSignalAuthoring.ConfigureInstaller(installer);
                data = new SerializedObject(installer);
                data.FindProperty("playerRules").objectReferenceValue = profile;
                data.ApplyModifiedPropertiesWithoutUndo();
                var scope = new DiContainer();
                scope.Inject(installer);
                Assert.Catch<ArgumentException>(() => installer.InstallBindings());
                Assert.That(scope.HasBinding<PlayerRulesSettings>(), Is.False);
                Assert.That(scope.HasBinding<EmergencyShift.Features.AudioMood.Config.AudioThreatSignalSettings>(), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void MissingPlayerProfileFailsBeforeAnyWorldBinding()
        {
            var owner = new GameObject("Missing player rules world");
            try
            {
                var installer = owner.AddComponent<WorldInstaller>();
                EmergencyShift.Editor.ThreatSignalAuthoring.ConfigureInstaller(installer);
                var scope = new DiContainer();
                scope.Inject(installer);
                Assert.Throws<InvalidOperationException>(() => installer.InstallBindings());
                Assert.That(scope.HasBinding<PlayerRulesSettings>(), Is.False);
                Assert.That(scope.HasBinding<EmergencyShift.Features.AudioMood.Config.AudioThreatSignalSettings>(), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void ProductionProfileIsReadOnlyAndKeepsCurrentBalance()
        {
            var profile = AssetDatabase.LoadAssetAtPath<PlayerRulesProfile>(EmergencyShift.Editor.PlayerRulesAuthoring.ProfilePath);
            Assert.That(profile, Is.Not.Null);
            string before = JsonUtility.ToJson(profile);
            PlayerRulesSettings rules = profile.ToSettings();
            Assert.That(rules.MaximumHealth, Is.EqualTo(2));
            Assert.That(rules.MoveSpeed, Is.EqualTo(3.5f));
            Assert.That(rules.Reach, Is.EqualTo(1.6f));
            Assert.That(rules.PickupRadius, Is.EqualTo(.65f));
            Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(before));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EmergencyShift.Editor.ThreatSignalAuthoring.ContextPath);
            var data = new SerializedObject(prefab.GetComponent<WorldInstaller>());
            Assert.That(data.FindProperty("playerRules").objectReferenceValue, Is.SameAs(profile));
        }

        private static PlayerRulesSettings Changed() => new PlayerRulesSettings(9, .9f, 5, 2, .9f, .1f, 2.4f, 12);
        private static PlayerInteractionSettings LegacyInteraction() => new PlayerInteractionSettings(1.6f, .05f, 1.8f, 8);

        private sealed class Clock : IGameClock
        {
            public double Time => 0;
        }

        private sealed class Input : IPlayerInput
        {
            public Vector2 Movement { get; set; }
            public Vector2 PointerPosition => Vector2.zero;
            public bool HasPointer => false;
            public bool FirePressed => false;
            public bool FireHeld => false;
            public bool InteractPressed => false;
            public bool ToggleLightPressed => false;
            public bool DialogueAdvancePressed => false;
            public bool ReloadPressed => false;
            public bool SelectPistolPressed => false;
            public bool SelectShotgunPressed => false;
            public bool RestartPressed => false;
            public bool RefillAmmoPressed => false;
            public bool ConfirmationHeld => false;
            public bool PausePressed => false;
        }
    }
}
