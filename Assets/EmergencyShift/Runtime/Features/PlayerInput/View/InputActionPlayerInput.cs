using System;
using EmergencyShift.Features.PlayerInput.Logic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EmergencyShift.Features.PlayerInput.View
{
    /// <summary>Owns one player's action instance; the shared authored asset remains unchanged.</summary>
    public sealed class InputActionPlayerInput : IPlayerInput, IDisposable
    {
        private readonly InputActionAsset actions;
        private readonly InputAction movement;
        private readonly InputAction pointer;
        private readonly InputAction fire;
        private readonly InputAction interact;
        private readonly InputAction reload;
        private readonly InputAction pistol;
        private readonly InputAction shotgun;
        private readonly InputAction light;
        private readonly InputAction restart;
        private readonly InputAction refill;
        private readonly InputAction advance;
        private readonly InputAction confirmation;
        private bool disposed;

        public InputActionPlayerInput(InputActionAsset template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            actions = UnityEngine.Object.Instantiate(template);
            try
            {
                movement = actions.FindAction("Gameplay/Move", true);
                pointer = actions.FindAction("Gameplay/Pointer", true);
                fire = actions.FindAction("Gameplay/Fire", true);
                interact = actions.FindAction("Gameplay/Interact", true);
                reload = actions.FindAction("Gameplay/Reload", true);
                pistol = actions.FindAction("Gameplay/SelectPistol", true);
                shotgun = actions.FindAction("Gameplay/SelectShotgun", true);
                light = actions.FindAction("Gameplay/ToggleLight", true);
                restart = actions.FindAction("Gameplay/Restart", true);
                refill = actions.FindAction("Gameplay/RefillAmmo", true);
                advance = actions.FindAction("Dialogue/Advance", true);
                confirmation = actions.FindAction("Dialogue/ConfirmationHeld", true);
                actions.Enable();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public Vector2 Movement => movement.ReadValue<Vector2>();
        public Vector2 PointerPosition => pointer.ReadValue<Vector2>();
        public bool HasPointer => !disposed && Application.isFocused && pointer.controls.Count > 0;
        public bool FirePressed => fire.WasPressedThisFrame();
        public bool FireHeld => fire.IsPressed();
        public bool InteractPressed => interact.WasPressedThisFrame();
        public bool ReloadPressed => reload.WasPressedThisFrame();
        public bool SelectPistolPressed => pistol.WasPressedThisFrame();
        public bool SelectShotgunPressed => shotgun.WasPressedThisFrame();
        public bool ToggleLightPressed => light.WasPressedThisFrame();
        public bool RestartPressed => restart.WasPressedThisFrame();
        public bool RefillAmmoPressed => refill.WasPressedThisFrame();
        public bool DialogueAdvancePressed => advance.WasPressedThisFrame();
        public bool ConfirmationHeld => confirmation.IsPressed();

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            actions.Disable();
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(actions);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(actions);
            }
        }
    }
}
