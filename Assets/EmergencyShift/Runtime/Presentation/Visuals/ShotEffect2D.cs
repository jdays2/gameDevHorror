using System;
using EmergencyShift.Core.Composition;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zenject;
using Random = UnityEngine.Random;

namespace EmergencyShift
{
    /// <summary>Огонь и свет дула затухают вместе; штрихи используются для попаданий.</summary>
    public sealed class ShotEffect2D : MonoBehaviour
    {
        private SceneRegistry<ShotEffect2D> registry;

        [Inject]
        public void Construct(SceneRegistry<ShotEffect2D> sceneEffects)
        {
            registry?.Remove(this);
            registry = sceneEffects ?? throw new ArgumentNullException(nameof(sceneEffects));
            if (isActiveAndEnabled)
            {
                registry.Register(this);
            }
        }

        private void OnEnable() => registry?.Register(this);

        private void OnDisable()
        {
            registry?.Remove(this);
        }

        public bool Illuminates(Vector2 point, LayerMask opaque)
        {
            return isActiveAndEnabled && configured && flash != null && flash.enabled && flash.intensity > .12f &&
                Vector2.Distance(point, flash.transform.position) < flash.pointLightOuterRadius * .9f &&
                Physics2D.Linecast(flash.transform.position, point, opaque).collider == null;
        }

        [SerializeField]
        private Light2D flash;
        [SerializeField]
        private SpriteRenderer[] strokes;
        [SerializeField]
        private SpriteRenderer whiteCore;
        [SerializeField]
        private SpriteRenderer[] flames;
        [SerializeField]
        private SpriteRenderer plume;
        private MaterialPropertyBlock plumeProperties;
        private static readonly int AgeId = Shader.PropertyToID("_Age");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");
        private float plumeDuration;
        private Vector3[] shapeScales;
        private Vector3[] shapePositions;
        private float duration;
        private float elapsed;
        private float initialIntensity;
        private Color tint;
        private bool configured;
        private bool isImpact;
        public Light2D LightSource => flash;

        public void Configure(Light2D light, SpriteRenderer[] shapes)
        {
            flash = light;
            strokes = shapes;
        }

        public void ConfigureFlame(Light2D light, SpriteRenderer[] sparks, SpriteRenderer core, SpriteRenderer[] layers)
        {
            flash = light;
            strokes = sparks;
            whiteCore = core;
            flames = layers;
        }

        public void ConfigurePlume(SpriteRenderer renderer)
        {
            plume = renderer;
        }

        public void Play(ShotVisualProfile profile, bool impact, Vector2 direction)
        {
            configured = true;
            elapsed = 0;
            isImpact = impact;
            duration = impact ? profile.impactSeconds : profile.flashSeconds;
            duration = Mathf.Max(0.02f, duration);
            tint = profile.flashColor;
            initialIntensity = Mathf.Max(0, profile.flashIntensity) * (impact ? 0.5f : 1);
            flash.color = Color.Lerp(tint, Color.white, 0.35f);
            flash.intensity = initialIntensity;
            flash.pointLightOuterRadius = Mathf.Max(0.1f, profile.flashRadius) * (impact ? 0.35f : 1);
            flash.enabled = true;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            transform.localScale = Vector3.one * (impact ? 0.55f : Mathf.Max(0.1f, profile.muzzleSize) * Random.Range(0.92f, 1.08f));
            foreach (SpriteRenderer stroke in strokes)
            {
                stroke.color = tint;
                stroke.enabled = impact || plume == null;
            }

            shapeScales = new Vector3[strokes.Length];
            shapePositions = new Vector3[strokes.Length];
            for (int i = 0; i < strokes.Length; i++)
            {
                shapeScales[i] = strokes[i].transform.localScale;
                shapePositions[i] = strokes[i].transform.localPosition;
            }

            if (whiteCore != null)
            {
                whiteCore.color = Color.white;
                whiteCore.enabled = impact || plume == null;
            }

            if (flames != null)
            {
                for (int i = 0; i < flames.Length; i++)
                {
                    flames[i].enabled = !impact && plume == null;
                    flames[i].color = Color.Lerp(tint, Color.white, i == 0 ? 0.1f : 0.5f);
                }
            }

            if (plume != null)
            {
                plume.enabled = !impact;
                plumeDuration = Mathf.Clamp(profile.plumeSeconds, .02f, duration);
                // The muzzle light must never outlive its visible fire. Impacts retain their own timing.
                if (!impact)
                {
                    duration = plumeDuration;
                }

                plume.transform.localPosition = new Vector3(0, profile.plumeLength * .42f, -.003f);
                plume.transform.localScale = new Vector3(profile.plumeWidth, profile.plumeLength, 1);
                plume.color = Color.Lerp(Color.white, tint, .22f);
                plumeProperties = new MaterialPropertyBlock();
                plumeProperties.SetFloat(SeedId, Random.Range(0f, 100f));
                plumeProperties.SetFloat(AgeId, 0);
                plume.SetPropertyBlock(plumeProperties);
            }
        }

        private void Update()
        {
            if (!configured)
            {
                return;
            }

            elapsed += Time.deltaTime;
            if (elapsed >= duration)
            {
                flash.intensity = 0;
                flash.enabled = false;
                if (plume != null)
                {
                    plume.enabled = false;
                }

                Destroy(gameObject);
                return;
            }

            // Короткая удержанная вершина яркости, затем резкий спад, а не слабый первый кадр.
            float t = elapsed / duration;
            float fade = 1 - Mathf.InverseLerp(0.25f, 1, t);
            if (plume != null && !isImpact)
            {
                // Match the shader's smoothstep(.35, 1, age); squared light falloff removes a glowing tail.
                fade = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.35f, 1, t));
            }

            flash.intensity = initialIntensity * fade * fade;
            if (plume != null && !isImpact)
            {
                float age = Mathf.Clamp01(elapsed / plumeDuration);
                plume.enabled = age < 1;
                plumeProperties.SetFloat(AgeId, age);
                plume.SetPropertyBlock(plumeProperties);
            }

            Color color = tint;
            color.a = fade;
            for (int i = 0; i < strokes.Length; i++)
            {
                var stroke = strokes[i];
                stroke.color = color;
                Vector3 outward = shapePositions[i].normalized;
                stroke.transform.localPosition = shapePositions[i] + outward * t * (isImpact ? 0.28f : 0.13f);
                stroke.transform.localScale = shapeScales[i] * Mathf.Lerp(1, 0.35f, t);
            }

            if (whiteCore != null)
            {
                whiteCore.color = new Color(1, 1, 1, fade);
                whiteCore.transform.localScale = new Vector3(0.16f, 0.3f, 1) * Mathf.Lerp(1, 0.55f, t);
            }

            if (flames != null)
            {
                for (int i = 0; i < flames.Length; i++)
                {
                    Color flameColor = Color.Lerp(tint, Color.white, i == 0 ? 0.1f : 0.5f);
                    flameColor.a = fade;
                    flames[i].color = flameColor;
                }
            }
        }
    }
}
