using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift
{
    /// <summary>Управляет направлением, оптикой и обоими источниками света фонаря.</summary>
    public sealed class Flashlight2D : MonoBehaviour, IThreatLight2D
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
        [Header("Optics — world units and degrees")]
        [SerializeField, Min(0.5f)]
        private float range = 4.2f;
        [SerializeField, Range(20f, 140f)]
        private float beamAngle = 80f;
        [Header("Aim — player input")]
        [SerializeField, Min(0.01f)]
        private float turnSmoothTime = .075f;
        [SerializeField, Min(30)]
        private float maximumTurnSpeed = 720f;
        [SerializeField, Min(0)]
        private float cursorDeadZone = .25f;
        [Header("Source placement")]
        [SerializeField, Min(.005f)]
        private float surfaceClearance = .04f;
        [SerializeField, Min(0)]
        private float maximumOriginRetraction = .45f;
        [SerializeField]
        private LayerMask originBlockingLayers = (1 << 8) | (1 << 9);
        private Vector3 authoredBeamPosition;
        private float spillZ;
        private bool placementInitialized;
        private float angularVelocity;
        public bool IsOn => beam != null && beam.enabled;
        public float Range => range;
        public float BeamAngle => beamAngle;
        public Camera WorldCamera => worldCamera;
        public Vector2 Origin => beam != null ? (Vector2)beam.transform.position : (Vector2)transform.position;
        public Vector2 Direction => aimPivot != null ? (Vector2)aimPivot.up : Vector2.up;

        public event System.Action<bool> PowerChanged;
        public void Configure(Camera camera, Transform pivot, Light2D light, Light2D spillLight = null)
        {
            worldCamera = camera;
            aimPivot = pivot;
            beam = light;
            nearGlow = spillLight;
            placementInitialized = false;
            ApplyOptics();
        }

        public void ConfigureAim(PlayerAim2D target)
        {
            aim = target;
        }

        private void Awake()
        {
            ApplyOptics();
            SetPower(startsOn);
        }

        private void OnValidate() => ApplyOptics();

        private void LateUpdate()
        {
            UpdateLightPlacement();
        }

        private void UpdateLightPlacement()
        {
            if (!Application.isPlaying || beam == null || beam.transform.parent == null)
            {
                return;
            }

            if (!placementInitialized)
            {
                authoredBeamPosition = beam.transform.localPosition;
                spillZ = nearGlow != null ? nearGlow.transform.position.z : transform.position.z;
                placementInitialized = true;
            }

            Vector3 desired = beam.transform.parent.TransformPoint(authoredBeamPosition);
            Vector2 safe = LightOriginPlacement2D.Clamp(transform.position, desired, gameObject.scene,
                surfaceClearance, maximumOriginRetraction, originBlockingLayers);
            beam.transform.position = new Vector3(safe.x, safe.y, desired.z);
            if (nearGlow != null)
            {
                Vector2 forward = aimPivot != null ? aimPivot.up : beam.transform.up;
                Vector2 probe = (Vector2)transform.position + forward * .001f;
                Vector2 spill = LightOriginPlacement2D.Clamp(transform.position, probe, gameObject.scene,
                    surfaceClearance, maximumOriginRetraction, originBlockingLayers);
                nearGlow.transform.position = new Vector3(spill.x, spill.y, spillZ);
            }
        }
        /// <summary>Меняет оптику, сохраняя состояние питания; используется модулями улучшения.</summary>
        public void SetOptics(float newRange, float newAngle)
        {
            range = Mathf.Max(0.5f, newRange);
            beamAngle = Mathf.Clamp(newAngle, 20f, 140f);
            ApplyOptics();
        }

        private void ApplyOptics()
        {
            range = Mathf.Max(0.5f, range);
            beamAngle = Mathf.Clamp(beamAngle, 20f, 140f);
            if (beam == null)
            {
                return;
            }

            beam.pointLightOuterRadius = range;
            beam.pointLightInnerAngle = beamAngle * 0.6f;
            beam.pointLightOuterAngle = beamAngle;
        }

        public void SetPower(bool on)
        {
            bool previous = IsOn;
            if (beam != null)
            {
                beam.enabled = on;
            }

            if (nearGlow != null)
            {
                nearGlow.enabled = on;
            }

            if (previous != IsOn)
            {
                PowerChanged?.Invoke(IsOn);
            }
        }

        private void Update()
        {
            if (GameplayInputBlock.IsBlocked)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                Toggle();
            }

            if (aim != null)
            {
                if (aim.HasTarget)
                {
                    RotateTowards(aim.WorldPoint, Time.deltaTime);
                }

                return;
            }

            if (Mouse.current == null || worldCamera == null || aimPivot == null)
            {
                return;
            }

            Vector3 world = worldCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RotateTowards(world, Time.deltaTime);
        }

        /// <summary>Scenario/test input; ordinary pointer input comes from PlayerAim2D.</summary>
        public void AimTowards(Vector2 worldPoint, float deltaTime)
        {
            if (aim != null)
            {
                aim.SetScriptedTarget(worldPoint);
            }

            RotateTowards(worldPoint, deltaTime);
        }

        private void RotateTowards(Vector2 worldPoint, float deltaTime)
        {
            if (aimPivot == null || deltaTime <= 0)
            {
                return;
            }

            Vector2 offset = worldPoint - (Vector2)transform.position;
            if (offset.sqrMagnitude < cursorDeadZone * cursorDeadZone)
            {
                angularVelocity = 0;
                return;
            }

            float target = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg - 90;
            float angle = Mathf.SmoothDampAngle(aimPivot.eulerAngles.z, target, ref angularVelocity, turnSmoothTime, maximumTurnSpeed, deltaTime);
            aimPivot.rotation = Quaternion.Euler(0, 0, angle);
        }

        /// <summary>Явная установка позы для сценарных демонстраций и телепорта; ввод использует AimTowards.</summary>
        public void AimAt(Vector2 worldPoint)
        {
            if (aim != null)
            {
                aim.SetScriptedTarget(worldPoint);
            }

            if (aimPivot == null)
            {
                return;
            }

            Vector2 direction = worldPoint - (Vector2)transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                aimPivot.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
                angularVelocity = 0;
            }

            UpdateLightPlacement();
        }

        /// <summary>Игровое освещение основным лучом. Слабый ближний свет не отпугивает врага.</summary>
        public bool Illuminates(Vector2 worldPoint, LayerMask blockingGeometry)
        {
            if (!IsOn)
            {
                return false;
            }

            Vector2 offset = worldPoint - Origin;
            if (offset.sqrMagnitude > range * range * 0.81f)
            {
                return false;
            }

            if (offset.sqrMagnitude > 0.0001f && Vector2.Angle(Direction, offset) > beamAngle * 0.45f)
            {
                return false;
            }

            // Коллайдеры тех же стен и двери перекрывают свет и игровую реакцию.
            return Physics2D.Linecast(Origin, worldPoint, blockingGeometry).collider == null;
        }

        public void Toggle() => SetPower(!IsOn);
    }
}
