using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift
{
    /// <summary>Управляет направлением, оптикой и обоими источниками света фонаря.</summary>
    public sealed class Flashlight2D : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Transform aimPivot;
        [SerializeField] private Light2D beam;
        [SerializeField] private Light2D nearGlow;
        [SerializeField] private bool startsOn = false;
        [Header("Optics — world units and degrees")]
        [SerializeField, Min(0.5f)] private float range = 4.2f;
        [SerializeField, Range(20f, 140f)] private float beamAngle = 80f;
        public bool IsOn => beam != null && beam.enabled;
        public float Range => range;
        public float BeamAngle => beamAngle;
        public Vector2 Origin => beam != null ? (Vector2)beam.transform.position : (Vector2)transform.position;
        public Vector2 Direction => aimPivot != null ? (Vector2)aimPivot.up : Vector2.up;

        public void Configure(Camera camera, Transform pivot, Light2D light, Light2D spillLight = null)
        { worldCamera = camera; aimPivot = pivot; beam = light; nearGlow = spillLight; ApplyOptics(); }

        private void Awake() { ApplyOptics(); SetPower(startsOn); }
        private void OnValidate() => ApplyOptics();

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
            if (beam == null) return;
            beam.pointLightOuterRadius = range;
            beam.pointLightInnerAngle = beamAngle * 0.6f;
            beam.pointLightOuterAngle = beamAngle;
        }

        public void SetPower(bool on)
        {
            if (beam != null) beam.enabled = on;
            if (nearGlow != null) nearGlow.enabled = on;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) Toggle();
            if (Mouse.current == null || worldCamera == null || aimPivot == null) return;
            Vector3 world = worldCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            AimAt(world);
        }

        public void AimAt(Vector2 worldPoint)
        {
            if (aimPivot == null) return;
            Vector2 direction = worldPoint - (Vector2)transform.position;
            if (direction.sqrMagnitude > 0.001f)
                aimPivot.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
        }

        /// <summary>Игровое освещение основным лучом. Слабый ближний свет не отпугивает врага.</summary>
        public bool Illuminates(Vector2 worldPoint, LayerMask blockingGeometry)
        {
            if (!IsOn) return false;
            Vector2 offset = worldPoint - Origin;
            if (offset.sqrMagnitude > range * range * 0.81f) return false;
            if (offset.sqrMagnitude > 0.0001f && Vector2.Angle(Direction, offset) > beamAngle * 0.45f) return false;
            // Коллайдеры тех же стен и двери перекрывают свет и игровую реакцию.
            return Physics2D.Linecast(Origin, worldPoint, blockingGeometry).collider == null;
        }

        public void Toggle() => SetPower(!IsOn);
    }
}
