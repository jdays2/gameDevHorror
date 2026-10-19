using EmergencyShift.Features.PlayerInput.View;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace EmergencyShift.Features.PlayerInput.Tests
{
    public sealed class PlayerInputTests
    {
        private const string ControlsPath = "Assets/EmergencyShift/Settings/EmergencyShiftControls.inputactions";
        private InputSettings originalSettings;
        private InputSettings testSettings;

        [SetUp]
        public void EnablePlayerInputUpdatesInEditMode()
        {
            originalSettings = InputSystem.settings;
            testSettings = Object.Instantiate(originalSettings);
            InputSystem.settings = testSettings;
            testSettings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
        }

        [TearDown]
        public void RestoreInputSettings()
        {
            InputSystem.settings = originalSettings;
            Object.DestroyImmediate(testSettings);
        }

        [Test]
        public void Movement_WasdDiagonal_IsNormalizedWithoutMutatingTemplate()
        {
            InputActionAsset template = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            var input = new InputActionPlayerInput(template);
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
                InputSystem.Update();
                Assert.That(input.Movement.x, Is.EqualTo(1 / Mathf.Sqrt(2)).Within(0.001));
                Assert.That(input.Movement.y, Is.EqualTo(1 / Mathf.Sqrt(2)).Within(0.001));
                Assert.That(template.enabled, Is.False);
            }
            finally
            {
                input.Dispose();
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [Test]
        public void Actions_ExistingKeyboardBindings_KeepTheirOriginalIntents()
        {
            InputActionAsset template = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            var input = new InputActionPlayerInput(template);
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E, Key.X, Key.F, Key.Digit1, Key.R, Key.T));
                InputSystem.Update();
                Assert.That(input.InteractPressed, Is.True);
                Assert.That(input.ReloadPressed, Is.True);
                Assert.That(input.ToggleLightPressed, Is.True);
                Assert.That(input.SelectPistolPressed, Is.True);
                Assert.That(input.SelectShotgunPressed, Is.False);
                Assert.That(input.RestartPressed, Is.True);
                Assert.That(input.RefillAmmoPressed, Is.True);
                Assert.That(input.ConfirmationHeld, Is.True);
                Assert.That(input.DialogueAdvancePressed, Is.False);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter, Key.Digit2));
                InputSystem.Update();
                Assert.That(input.DialogueAdvancePressed, Is.True);
                Assert.That(input.SelectShotgunPressed, Is.True);
            }
            finally
            {
                input.Dispose();
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [Test]
        public void Dispose_OneInputScope_DoesNotDisableAnotherScopeOrSharedAsset()
        {
            InputActionAsset template = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            var first = new InputActionPlayerInput(template);
            var second = new InputActionPlayerInput(template);
            try
            {
                first.Dispose();
                first.Dispose();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                InputSystem.Update();
                Assert.That(second.Movement, Is.EqualTo(Vector2.up));
                Assert.That(template.enabled, Is.False);
            }
            finally
            {
                first.Dispose();
                second.Dispose();
                InputSystem.RemoveDevice(keyboard);
            }
        }
    }
}
