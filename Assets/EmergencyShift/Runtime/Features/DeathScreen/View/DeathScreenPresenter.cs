using System;
using EmergencyShift.Features.DeathScreen.Logic;
using EmergencyShift.Features.PlayerInput.Logic;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace EmergencyShift.Features.DeathScreen.View
{
    [DefaultExecutionOrder(-400), DisallowMultipleComponent]
    public sealed class DeathScreenPresenter : MonoBehaviour
    {
        [SerializeField] private DeathScreenView view;
        private DeathScreenController controller;
        private IPlayerInput input;
        private PlayerLife2D player;
        private InputAction held;
        private InputAction cancel;
        private bool started;
        private bool checkedStartupRestore;

        public DeathScreenController Controller => controller;

        [Inject]
        private void Construct(DeathScreenController controller, IPlayerInput input, PlayerLife2D player)
        {
            this.controller = controller;
            this.input = input;
            this.player = player;
        }

        private void Awake()
        {
            held = new InputAction("Death screen confirmation held", InputActionType.Button);
            held.AddBinding("<Mouse>/leftButton");
            held.AddBinding("<Keyboard>/enter");
            held.AddBinding("<Keyboard>/space");
            held.AddBinding("<Keyboard>/e");
            held.AddBinding("<Keyboard>/r");
            held.AddBinding("<Gamepad>/buttonSouth");
            held.wantsInitialStateCheck = true;
            cancel = new InputAction("Death screen cancel quit", InputActionType.Button);
            cancel.AddBinding("<Keyboard>/escape");
            cancel.AddBinding("<Gamepad>/buttonEast");
        }

        private void OnEnable()
        {
            if (controller == null || view == null || player == null)
            {
                throw new InvalidOperationException("Death UI needs its authored view and level composition.");
            }
            held.Enable();
            cancel.Enable();
            controller.Model.Changed += Present;
            player.HealthChanged += OnHealthChanged;
            view.PrimaryRequested += OnPrimary;
            view.SecondaryRequested += OnSecondary;
            controller.BeginStartupFence();
            if (started)
            {
                OnHealthChanged(player.Health);
            }
        }

        private void Start()
        {
            started = true;
            OnHealthChanged(player.Health);
            Present();
        }

        private void Update()
        {
            controller.Tick(Time.unscaledDeltaTime, held.IsPressed());
            if (input.RestartPressed)
            {
                controller.Execute(DeathScreenIntent.Retry);
            }
            if (cancel.WasPressedThisFrame())
            {
                controller.Execute(DeathScreenIntent.CancelQuit);
            }
            Present();
        }

        private void LateUpdate()
        {
            if (!checkedStartupRestore)
            {
                checkedStartupRestore = true;
                controller.Execute(DeathScreenIntent.ShowRestoreError);
            }
        }

        private void OnDisable()
        {
            held?.Disable();
            cancel?.Disable();
            if (player != null)
            {
                player.HealthChanged -= OnHealthChanged;
            }
            if (view != null)
            {
                view.PrimaryRequested -= OnPrimary;
                view.SecondaryRequested -= OnSecondary;
            }
            if (controller != null)
            {
                controller.Model.Changed -= Present;
                controller.Cancel();
            }
            if (view != null)
            {
                view.Hide();
            }
        }

        private void OnDestroy()
        {
            held?.Dispose();
            cancel?.Dispose();
        }

        private void OnHealthChanged(int health)
        {
            if (health <= 0)
            {
                controller.Execute(DeathScreenIntent.Enter);
            }
            else
            {
                controller.ObserveLivingPlayer();
            }
        }

        private void OnPrimary() => controller.Execute(controller.Model.Phase == DeathScreenPhase.ConfirmQuit
            ? DeathScreenIntent.CancelQuit : DeathScreenIntent.Retry);

        private void OnSecondary() => controller.Execute(controller.Model.Phase == DeathScreenPhase.ConfirmQuit
            ? DeathScreenIntent.ConfirmQuit : DeathScreenIntent.AskQuit);

        private void Present()
        {
            if (view != null)
            {
                view.Render(controller.Model);
            }
        }
    }
}
