using System;
using EmergencyShift.Features.Flashlight;
using EmergencyShift.Features.Flashlight.Config;
using EmergencyShift.Features.Flashlight.Logic;
using EmergencyShift.Features.Flashlight.View;
using EmergencyShift.Features.PlayerInput.Logic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Input/View bridge; state, interpolation and Unity obstruction are separate boundaries.</summary>
    public sealed class Flashlight2D : MonoBehaviour, IThreatLight2D, IFlashlightOpticsSource, IFlashlightView
    {
        [SerializeField]
        private Camera worldCamera;
        [SerializeField]
        private PlayerAim2D aim;
        [SerializeField]
        private Transform aimPivot;
        [SerializeField]
        private Light2D beam;
        [SerializeField]
        private Light2D nearGlow;
        [SerializeField]
        private bool startsOn = false;
        [SerializeField, Tooltip("Disable in scenes where the player must first find a flashlight.")]
        private bool startsOwned = true;
        [Header("Optics — world units and degrees")]
        [SerializeField, Min(.5f)]
        private float range = 4.2f;
        [SerializeField, Range(20f, 140f)]
        private float beamAngle = 80f;
        [Header("Aim — player input")]
        [SerializeField, Min(.01f)]
        private float turnSmoothTime = .075f;
        [SerializeField, Min(30)]
        private float maximumTurnSpeed = 720f;
        [SerializeField, Min(0)]
        private float cursorDeadZone = .25f;
        private IPlayerInput input;
        private GameplayInputBlock inputBlock;
        private FlashlightControllerFactory factory;
        private FlashlightController controller;
        private FlashlightAimController aiming;
        private readonly FlashlightObstruction2D obstruction = new FlashlightObstruction2D();

        public bool IsOwned => controller?.IsOwned ?? startsOwned;
        public bool IsOn => controller != null ? controller.IsOn : beam != null && beam.enabled;
        public float Range => controller?.Range ?? range;
        public float BeamAngle => controller?.BeamAngle ?? beamAngle;
        public Camera WorldCamera => worldCamera;
        public Vector2 Origin => beam != null ? (Vector2)beam.transform.position : (Vector2)transform.position;
        public Vector2 Direction => aimPivot != null ? (Vector2)aimPivot.up : Vector2.up;
        bool IFlashlightView.HasBeam => beam != null;

        public event Action OwnershipChanged;
        public event Action OpticsChanged;
        public event Action<bool> PowerChanged;

        [Inject]
        public void Construct(IPlayerInput playerInput, GameplayInputBlock block, FlashlightControllerFactory controllers)
        {
            input = playerInput;
            inputBlock = block;
            factory = controllers ?? throw new ArgumentNullException(nameof(controllers));
        }

        private void Awake()
        {
            RequireController();
            SetPower(startsOn);
        }

        private void OnValidate()
        {
            aiming = null;
            if (controller != null)
            {
                controller.SetOptics(new Vector2(range, beamAngle));
            }
            else if (FlashlightStateModel.CanSetOptics(new Vector2(range, beamAngle)).Success)
            {
                RenderOptics(new Vector2(Mathf.Max(.5f, range), Mathf.Clamp(beamAngle, 20, 140)));
            }
        }

        private void Update()
        {
            if (inputBlock.IsBlocked)
            {
                return;
            }

            if (input.ToggleLightPressed)
            {
                Toggle();
            }

            if (aim != null)
            {
                if (aim.HasTarget)
                {
                    RequireAim().Smooth(aim.WorldPoint, Time.deltaTime);
                }

                return;
            }

            if (input.HasPointer && worldCamera != null && aimPivot != null)
            {
                RequireAim().Smooth(worldCamera.ScreenToWorldPoint(input.PointerPosition), Time.deltaTime);
            }
        }

        public void Configure(Camera camera, Transform pivot, Light2D light, Light2D spillLight = null)
        {
            worldCamera = camera;
            aimPivot = pivot;
            beam = light;
            nearGlow = spillLight;
            aiming = null;
            if (controller != null)
            {
                controller.RefreshPresentation();
            }
            else
            {
                OnValidate();
            }
        }

        public void ConfigureAim(PlayerAim2D target) => aim = target;

        public void SetOptics(float newRange, float newAngle)
        {
            var result = RequireController().SetOptics(new Vector2(newRange, newAngle));
            if (!result.Success)
            {
                throw new ArgumentException(result.Reason);
            }
        }

        public void SetPower(bool on) => RequireController().SetPower(on);
        public void Toggle() => SetPower(!IsOn);
        public bool Acquire() => RequireController().Acquire();
        public void RestoreOwnership(bool owned) => RequireController().RestoreOwnership(owned);

        /// <summary>Scenario input uses the same interpolation as normal aim intent.</summary>
        public void AimTowards(Vector2 worldPoint, float deltaTime)
        {
            aim?.SetScriptedTarget(worldPoint);
            RequireAim().Smooth(worldPoint, deltaTime);
        }

        public void AimAt(Vector2 worldPoint)
        {
            aim?.SetScriptedTarget(worldPoint);
            RequireAim().Snap(worldPoint);
        }

        public bool Illuminates(Vector2 worldPoint, LayerMask blockingGeometry)
        {
            Vector2 origin = Origin;
            return RequireController().Covers(origin, Direction, worldPoint) &&
                obstruction.IsUnblocked(origin, worldPoint, blockingGeometry);
        }

        void IFlashlightView.RenderPower(bool on)
        {
            if (beam != null)
            {
                beam.enabled = on;
            }

            if (nearGlow != null)
            {
                nearGlow.enabled = on;
            }
        }

        void IFlashlightView.RenderOptics(Vector2 optics) => RenderOptics(optics);
        void IFlashlightView.OwnershipChanged() => OwnershipChanged?.Invoke();
        void IFlashlightView.PowerChanged(bool on) => PowerChanged?.Invoke(on);
        void IFlashlightView.OpticsChanged() => OpticsChanged?.Invoke();

        private void RenderOptics(Vector2 optics)
        {
            range = optics.x;
            beamAngle = optics.y;
            if (beam != null)
            {
                beam.pointLightOuterRadius = range;
                beam.pointLightInnerAngle = beamAngle * .6f;
                beam.pointLightOuterAngle = beamAngle;
            }
        }

        private FlashlightController RequireController()
        {
            if (factory == null)
            {
                throw new InvalidOperationException("Flashlight requires level composition.");
            }

            if (controller == null)
            {
                controller = factory.Create(startsOwned, beam != null && beam.enabled,
                    new Vector2(range, beamAngle), this);
                controller.RefreshPresentation();
            }

            return controller;
        }

        private FlashlightAimController RequireAim()
        {
            RequireController();
            return aiming ??= factory.CreateAim(new FlashlightAimSettings(turnSmoothTime, maximumTurnSpeed, cursorDeadZone),
                new FlashlightAimEnvironment2D(transform, aimPivot));
        }
    }
}