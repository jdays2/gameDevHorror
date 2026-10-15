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
        [SerializeField]
        private Color color = new Color(.70f, .53f, .31f);
        [SerializeField, Min(.2f)]
        private float radius = 1.8f;
        [SerializeField, Min(0)]
        private float intensity = .5f;
        [SerializeField]
        private bool startsOn = true;
        [SerializeField]
        private bool flickers;
        [SerializeField]
        private float phase;
        private bool powered;
        public Vector2 Origin => transform.position;
        public float Radius => radius;
        public float CurrentIntensity => source != null && source.enabled ? source.intensity : 0;

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
            powered = startsOn;
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
                powered = startsOn;
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
                powered = startsOn;
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
            source.color = color;
            source.pointLightOuterAngle = 360;
            source.pointLightInnerAngle = 360;
            source.pointLightOuterRadius = radius;
            source.pointLightInnerRadius = .12f;
            source.intensity = powered ? intensity : 0;
            source.enabled = powered;
            if (bulb != null)
            {
                bulb.color = color;
                bulb.enabled = powered;
            }
        }

        public void SetPower(bool on)
        {
            powered = on;
            Apply();
        }

        private void Update()
        {
            if (source == null || !powered || !flickers)
            {
                return;
            }

            float clock = Time.time + phase + transform.position.x * .37f + transform.position.y * .19f;
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
            return isActiveAndEnabled && CurrentIntensity * (1 - distance / radius) > .10f && distance < radius * .9f && Physics2D.Linecast(Origin, point, opaque).collider == null;
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
