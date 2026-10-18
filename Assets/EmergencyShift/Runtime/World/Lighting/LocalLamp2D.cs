using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift
{
    /// <summary>Слабый локальный свет. Помогает видеть/подбирать, но не заменяет защитный луч фонаря.</summary>
    [RequireComponent(typeof(Light2D)), DisallowMultipleComponent]
    public sealed class LocalLamp2D : MonoBehaviour
    {
        private static readonly List<LocalLamp2D> active = new List<LocalLamp2D>();
        public static IReadOnlyList<LocalLamp2D> Active => active;

        [SerializeField]
        private Light2D source;
        [SerializeField]
        private SpriteRenderer bulb;
        [SerializeField, Tooltip("Optional shared appearance. Legacy scene tuning is retained when absent.")]
        private LocalLightProfile profile;
        [SerializeField]
        private Color color = new Color(.70f, .53f, .31f);
        [SerializeField, Min(.2f)]
        private float radius = 3.4f;
        [SerializeField, Min(0)]
        private float intensity = 1.2f;
        [SerializeField]
        private bool castsShadows = true;
        [SerializeField]
        private bool startsOn = true;
        [SerializeField]
        private bool flickers;
        [SerializeField]
        private float phase;
        private bool powered;
        private LocalLampSceneSettings sceneSettings;
        public Vector2 Origin => transform.position;
        public float Radius
        {
            get
            {
                if (sceneSettings != null)
                {
                    return sceneSettings.lightRadius;
                }

                return profile != null ? profile.radius : radius;
            }
        }
        public float CurrentIntensity => source != null && source.enabled ? source.intensity : 0;

        public void ConfigureScene(LocalLampSceneSettings settings)
        {
            settings.Validate();
            sceneSettings = settings;
            if (!Application.isPlaying)
            {
                powered = settings.startsOn;
            }

            Apply();
        }

        public void RefreshAppearance()
        {
            Apply();
        }

        public void SetProfile(LocalLightProfile appearance)
        {
            appearance.Validate();
            profile = appearance;
            Apply();
        }

        public void Configure(Light2D light, SpriteRenderer marker, Color tint, bool unstable)
        {
            source = light;
            bulb = marker;
            color = tint;
            flickers = unstable;
            Apply();
        }

        private void Awake()
        {
            if (profile != null)
            {
                profile.Validate();
            }

            powered = sceneSettings != null ? sceneSettings.startsOn : startsOn;
            Apply();
        }

        private void OnEnable()
        {
            if (!active.Contains(this))
            {
                active.Add(this);
            }

            if (!Application.isPlaying)
            {
                powered = sceneSettings != null ? sceneSettings.startsOn : startsOn;
            }

            Apply();
        }

        private void OnDisable()
        {
            active.Remove(this);
            if (source != null)
            {
                source.enabled = false;
            }

            if (bulb != null)
            {
                bulb.enabled = false;
            }
        }

        private void OnValidate()
        {
            radius = Mathf.Max(.2f, radius);
            intensity = Mathf.Max(0, intensity);
            if (!Application.isPlaying)
            {
                powered = sceneSettings != null ? sceneSettings.startsOn : startsOn;
            }

            Apply();
        }

        private void Apply()
        {
            if (source == null)
            {
                source = GetComponent<Light2D>();
            }

            if (source == null)
            {
                return;
            }

            source.lightType = Light2D.LightType.Point;
            source.color = profile != null ? profile.color : color;
            source.pointLightOuterAngle = 360;
            source.pointLightInnerAngle = 360;
            source.pointLightOuterRadius = Radius;
            source.pointLightInnerRadius = .12f;
            float strength = profile != null ? profile.intensity : intensity;
            if (sceneSettings != null)
            {
                source.color = sceneSettings.color;
                strength = sceneSettings.intensity;
            }

            source.intensity = powered ? strength : 0;
            source.enabled = powered;
            if (sceneSettings != null)
            {
                source.shadowsEnabled = sceneSettings.castsShadows;
            }
            else if (profile != null)
            {
                source.shadowsEnabled = profile.castsShadows;
            }
            else
            {
                source.shadowsEnabled = castsShadows;
            }
            if (bulb != null)
            {
                Color tint = source.color;
                if (sceneSettings != null)
                {
                    tint.a = Mathf.Clamp01(strength / .28f);
                }

                bulb.color = tint;
                bulb.enabled = powered && strength > .001f;
            }
        }

        public void SetPower(bool on)
        {
            powered = on;
            Apply();
        }

        private void Update()
        {
            if (source == null || !powered)
            {
                return;
            }

            float offset = sceneSettings != null ? sceneSettings.phase : phase;
            float clock = Time.time + offset + transform.position.x * .37f + transform.position.y * .19f;
            if (sceneSettings != null)
            {
                float brightness = sceneSettings.Brightness(clock);
                source.intensity = sceneSettings.intensity * brightness;
                if (bulb != null)
                {
                    Color tint = sceneSettings.color;
                    tint.a = brightness * Mathf.Clamp01(sceneSettings.intensity / .28f);
                    bulb.color = tint;
                    bulb.enabled = brightness > .01f && sceneSettings.intensity > .001f;
                }

                return;
            }

            if (profile != null)
            {
                float brightness = profile.Evaluate(clock);
                source.intensity = profile.intensity * brightness;
                if (bulb != null)
                {
                    Color tint = profile.color;
                    tint.a = brightness;
                    bulb.color = tint;
                    bulb.enabled = brightness > .01f;
                }

                return;
            }

            if (!flickers)
            {
                return;
            }

            float t = Mathf.Repeat(clock, 6.4f);
            float factor = (t > 4.9f && t < 5.02f) || (t > 5.16f && t < 5.25f) ? 0 : Mathf.Lerp(.82f, 1, Mathf.PerlinNoise(clock * 1.4f, 3.1f));
            source.intensity = intensity * factor;
            if (bulb != null)
            {
                bulb.enabled = factor > .1f;
            }
        }

        public bool Illuminates(Vector2 point, LayerMask opaque)
        {
            float distance = Vector2.Distance(Origin, point);
            return isActiveAndEnabled && CurrentIntensity * (1 - distance / Radius) > .10f && distance < Radius * .9f && Physics2D.Linecast(Origin, point, opaque).collider == null;
        }

        public static bool AnyIlluminates(Vector2 point, LayerMask opaque)
        {
            foreach (var lamp in active)
            {
                if (lamp != null && lamp.Illuminates(point, opaque))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
