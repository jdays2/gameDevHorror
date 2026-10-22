#if UNITY_EDITOR || UNITY_INCLUDE_INSTRUMENTATION
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift
{
    /// <summary>Saved comparison-room controls. Owns only local material overrides and inspection poses.</summary>
    public sealed class FurnitureLightStudy2D : MonoBehaviour
    {
        public enum Variant { Original, DarkTop, DirectionalEdge }

        [Serializable]
        public sealed class Sample
        {
            public SpriteRenderer renderer;
            [Tooltip("Center XY and half size ZW of the main top surface, in sprite local units.")]
            public Vector4 surfaceRect;
            public int responseIndex;
        }

        [SerializeField] private FurnitureLightStudyProfile profile;
        [SerializeField] private Material studyMaterial;
        [SerializeField] private Flashlight2D flashlight;
        [SerializeField] private CameraFollow2D cameraFollow;
        [SerializeField] private Light2D[] ceilingLights;
        [SerializeField] private Light2D[] flashlightLights;
        [SerializeField] private Sample[] samples;
        [SerializeField] private UnityEngine.UI.Text status;
        private Material[] originalMaterials;
        private MaterialPropertyBlock[] originalProperties;
        private bool[] originalCeilingEnabled;
        private bool[] originalFlashlightShadows;
        private bool ownsOverrides;
        private MaterialPropertyBlock properties;
        private PlayerAim2D aim;
        private Rigidbody2D playerBody;
        private int selectedSample;
        private int selectedSide;
        private bool initialized;

        private static readonly int LightPosition = Shader.PropertyToID("_StudyLightLocal");
        private static readonly int SurfaceRect = Shader.PropertyToID("_StudySurfaceRect");
        private static readonly int Response = Shader.PropertyToID("_StudyResponse");
        private static readonly int WorldToSurface = Shader.PropertyToID("_StudyWorldToSurface");
        private static readonly Vector2[] Sides = { Vector2.down, Vector2.right, Vector2.up, Vector2.left };
        private static readonly string[] SideNames = { "снизу", "справа", "сверху", "слева" };
        private static readonly string[] VariantNames = { "A · ОБЫЧНЫЙ", "B · ТЁМНЫЙ ВЕРХ", "C · НАПРАВЛЕННАЯ КРОМКА" };

        public Variant CurrentVariant { get; private set; }
        public bool CeilingEnabled { get; private set; }
        public bool ShadowsEnabled { get; private set; } = true;
        public int SampleCount => samples.Length;
        public Sample[] Samples => samples;

        public void ReplaceRuntimeSamples(Sample[] replacement)
        {
            if (!initialized || replacement == null || replacement.Length == 0)
            {
                throw new InvalidOperationException("Runtime sample replacement requires an initialized study and samples.");
            }

            foreach (Sample sample in replacement)
            {
                if (sample == null || sample.renderer == null || sample.responseIndex < 0 ||
                    sample.responseIndex >= profile.SurfaceCount || sample.surfaceRect.z <= 0 || sample.surfaceRect.w <= 0)
                {
                    throw new InvalidOperationException("Invalid replacement study sample.");
                }
            }

            RestoreOverrides();
            samples = (Sample[])replacement.Clone();
            selectedSample = Mathf.Min(selectedSample, samples.Length - 1);
            if (isActiveAndEnabled)
            {
                CaptureOverrides();
                SetVariant((int)CurrentVariant);
                SetCeiling(CeilingEnabled);
                SetShadows(ShadowsEnabled);
            }
        }

        public void Configure(FurnitureLightStudyProfile settings, Material material, Flashlight2D playerLight,
            CameraFollow2D follow, Light2D[] ceiling, Light2D[] handheld, Sample[] furniture, UnityEngine.UI.Text label)
        {
            if (initialized)
            {
                throw new InvalidOperationException("Configure furniture study before its runtime lifetime starts.");
            }

            profile = settings;
            studyMaterial = material;
            flashlight = playerLight;
            cameraFollow = follow;
            ceilingLights = ceiling;
            flashlightLights = handheld;
            samples = furniture;
            status = label;
        }

        private void Start()
        {
            properties = new MaterialPropertyBlock();
            profile.Validate();
            if (studyMaterial == null || flashlight == null || cameraFollow == null || status == null
                || samples == null || samples.Length == 0 || ceilingLights == null || flashlightLights == null)
            {
                throw new InvalidOperationException("Furniture study is missing its scene dependencies.");
            }

            for (int i = 0; i < samples.Length; i++)
            {
                var sample = samples[i];
                if (sample.renderer == null || sample.responseIndex < 0 || sample.responseIndex >= profile.SurfaceCount
                    || sample.surfaceRect.z <= 0 || sample.surfaceRect.w <= 0)
                {
                    throw new InvalidOperationException("Invalid furniture sample geometry or response index.");
                }

            }

            foreach (Light2D light in ceilingLights)
            {
                if (light == null)
                {
                    throw new InvalidOperationException("Missing ceiling light in study.");
                }
            }

            foreach (Light2D light in flashlightLights)
            {
                if (light == null)
                {
                    throw new InvalidOperationException("Missing flashlight light in study.");
                }
            }

            aim = flashlight.GetComponent<PlayerAim2D>();
            playerBody = flashlight.GetComponent<Rigidbody2D>();
            initialized = true;
            CaptureOverrides();
            SetVariant((int)Variant.DirectionalEdge);
            SetCeiling(false);
            SetShadows(true);
            flashlight.SetPower(true);
            InspectSample(0, 0);
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            if (aim.UsesScriptedTarget && Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > .01f)
            {
                aim.ClearScriptedTarget();
            }

            var keys = Keyboard.current;
            if (keys == null)
            {
                return;
            }

            if (keys.digit1Key.wasPressedThisFrame)
            {
                SetVariant(0);
            }
            if (keys.digit2Key.wasPressedThisFrame)
            {
                SetVariant(1);
            }
            if (keys.digit3Key.wasPressedThisFrame)
            {
                SetVariant(2);
            }
            if (keys.lKey.wasPressedThisFrame)
            {
                ToggleCeiling();
            }
            if (keys.hKey.wasPressedThisFrame)
            {
                ToggleShadows();
            }
            if (keys.tabKey.wasPressedThisFrame)
            {
                NextSample();
            }
            if (keys.vKey.wasPressedThisFrame)
            {
                NextSide();
            }
        }

        private void LateUpdate()
        {
            if (!initialized || CurrentVariant == Variant.Original)
            {
                return;
            }

            foreach (var sample in samples)
            {
                var response = profile.SurfaceAt(sample.responseIndex);
                Vector3 localLight = sample.renderer.transform.InverseTransformPoint(flashlight.Origin);
                sample.renderer.GetPropertyBlock(properties);
                properties.SetVector(LightPosition, localLight);
                properties.SetVector(SurfaceRect, sample.surfaceRect);
                properties.SetMatrix(WorldToSurface, sample.renderer.transform.worldToLocalMatrix);
                float edgeEnabled = CurrentVariant == Variant.DirectionalEdge ? 1 : 0;
                properties.SetVector(Response, new Vector4(response.TopResponse, response.EdgeResponse,
                    response.EdgeWidth, edgeEnabled));
                sample.renderer.SetPropertyBlock(properties);
            }
        }

        public void SetVariant(int variant)
        {
            if (!initialized || !ownsOverrides || variant < 0 || variant > 2)
            {
                return;
            }

            CurrentVariant = (Variant)variant;
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i].renderer.SetPropertyBlock(originalProperties[i]);
                samples[i].renderer.sharedMaterial = variant == 0 ? originalMaterials[i] : studyMaterial;
            }

            RefreshStatus();
        }

        public void ToggleCeiling() => SetCeiling(!CeilingEnabled);
        public void SelectOriginal() => SetVariant(0);
        public void SelectDarkTop() => SetVariant(1);
        public void SelectDirectionalEdge() => SetVariant(2);

        public void SetCeiling(bool on)
        {
            if (!ownsOverrides)
            {
                return;
            }

            CeilingEnabled = on;
            foreach (var light in ceilingLights)
            {
                light.enabled = on;
            }

            RefreshStatus();
        }

        public void ToggleShadows()
        {
            SetShadows(!ShadowsEnabled);
        }

        public void SetShadows(bool enabled)
        {
            if (!ownsOverrides)
            {
                return;
            }

            ShadowsEnabled = enabled;
            foreach (var light in flashlightLights)
            {
                light.shadowsEnabled = ShadowsEnabled;
            }

            RefreshStatus();
        }

        public void NextSample() => InspectSample((selectedSample + 1) % samples.Length, selectedSide);
        public void NextSide() => InspectSample(selectedSample, (selectedSide + 1) % Sides.Length);

        public void InspectSample(int sampleIndex, int side)
        {
            if (!initialized || sampleIndex < 0 || sampleIndex >= samples.Length || side < 0 || side >= Sides.Length)
            {
                return;
            }

            selectedSample = sampleIndex;
            selectedSide = side;
            var renderer = samples[sampleIndex].renderer;
            Vector2 direction = Sides[side];
            Bounds bounds = renderer.bounds;
            float extent = Mathf.Abs(direction.x) * bounds.extents.x + Mathf.Abs(direction.y) * bounds.extents.y;
            Vector2 position = (Vector2)bounds.center + direction * (extent + profile.InspectionClearance);
            flashlight.transform.position = position;
            playerBody.position = position;
            playerBody.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            flashlight.AimAt(bounds.center);
            cameraFollow.SnapToTarget();
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (status == null || !initialized)
            {
                return;
            }

            string ceiling = CeilingEnabled ? "ВКЛ" : "ВЫКЛ";
            string shadows = ShadowsEnabled ? "ВКЛ" : "ВЫКЛ";
            string label = profile.SurfaceAt(samples[selectedSample].responseIndex).Label;
            status.text = VariantNames[(int)CurrentVariant] + "\n" + label + " · ракурс " + SideNames[selectedSide]
                + "     Потолок: " + ceiling + "     Тени: " + shadows;
        }

        private void OnEnable()
        {
            if (initialized)
            {
                CaptureOverrides();
                SetVariant((int)CurrentVariant);
                SetCeiling(CeilingEnabled);
                SetShadows(ShadowsEnabled);
            }
        }

        private void CaptureOverrides()
        {
            originalMaterials = new Material[samples.Length];
            originalProperties = new MaterialPropertyBlock[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                originalMaterials[i] = samples[i].renderer.sharedMaterial;
                originalProperties[i] = new MaterialPropertyBlock();
                samples[i].renderer.GetPropertyBlock(originalProperties[i]);
            }

            originalCeilingEnabled = new bool[ceilingLights.Length];
            for (int i = 0; i < ceilingLights.Length; i++)
            {
                originalCeilingEnabled[i] = ceilingLights[i].enabled;
            }

            originalFlashlightShadows = new bool[flashlightLights.Length];
            for (int i = 0; i < flashlightLights.Length; i++)
            {
                originalFlashlightShadows[i] = flashlightLights[i].shadowsEnabled;
            }

            ownsOverrides = true;
        }

        private void OnDisable() => RestoreOverrides();
        private void OnDestroy() => RestoreOverrides();

        private void RestoreOverrides()
        {
            if (!ownsOverrides)
            {
                return;
            }

            ownsOverrides = false;
            for (int i = 0; i < samples.Length; i++)
            {
                if (samples[i].renderer != null)
                {
                    samples[i].renderer.SetPropertyBlock(originalProperties[i]);
                    samples[i].renderer.sharedMaterial = originalMaterials[i];
                }
            }

            for (int i = 0; i < ceilingLights.Length; i++)
            {
                if (ceilingLights[i] != null)
                {
                    ceilingLights[i].enabled = originalCeilingEnabled[i];
                }
            }

            for (int i = 0; i < flashlightLights.Length; i++)
            {
                if (flashlightLights[i] != null)
                {
                    flashlightLights[i].shadowsEnabled = originalFlashlightShadows[i];
                }
            }
        }
    }
}
#endif
