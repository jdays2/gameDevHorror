using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift
{
    /// <summary>Короткая вспышка/попадание: одноразовый свет и простые геометрические штрихи.</summary>
    public sealed class ShotEffect2D : MonoBehaviour
    {
        [SerializeField] private Light2D flash;
        [SerializeField] private SpriteRenderer[] strokes;
        [SerializeField] private SpriteRenderer whiteCore;
        [SerializeField] private SpriteRenderer[] flames;
        private Vector3[] shapeScales;
        private Vector3[] shapePositions;
        private float duration;
        private float elapsed;
        private float initialIntensity;
        private Color tint;
        private bool configured;
        private bool isImpact;

        public void Configure(Light2D light, SpriteRenderer[] shapes) { flash = light; strokes = shapes; }
        public void ConfigureFlame(Light2D light, SpriteRenderer[] sparks, SpriteRenderer core, SpriteRenderer[] layers)
        { flash = light; strokes = sparks; whiteCore = core; flames = layers; }

        public void Play(ShotVisualProfile profile, bool impact, Vector2 direction)
        {
            configured = true; elapsed = 0;
            isImpact = impact;
            duration = impact ? profile.impactSeconds : profile.flashSeconds;
            duration = Mathf.Max(0.02f, duration);
            tint = profile.flashColor;
            initialIntensity = Mathf.Max(0, profile.flashIntensity) * (impact ? 0.5f : 1);
            flash.color = Color.Lerp(tint, Color.white, 0.35f); flash.intensity = initialIntensity;
            flash.pointLightOuterRadius = Mathf.Max(0.1f, profile.flashRadius) * (impact ? 0.35f : 1);
            flash.enabled = true;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            transform.localScale = Vector3.one * (impact ? 0.55f : Mathf.Max(0.1f, profile.muzzleSize) * Random.Range(0.92f, 1.08f));
            foreach (SpriteRenderer stroke in strokes) stroke.color = tint;
            shapeScales = new Vector3[strokes.Length]; shapePositions = new Vector3[strokes.Length];
            for (int i = 0; i < strokes.Length; i++)
            {
                shapeScales[i] = strokes[i].transform.localScale;
                shapePositions[i] = strokes[i].transform.localPosition;
            }
            if (whiteCore != null) whiteCore.color = Color.white;
            if (flames != null)
                for (int i = 0; i < flames.Length; i++)
                { flames[i].enabled = !impact; flames[i].color = Color.Lerp(tint, Color.white, i == 0 ? 0.1f : 0.5f); }
        }

        private void Update()
        {
            if (!configured) return;
            elapsed += Time.deltaTime;
            if (elapsed >= duration) { Destroy(gameObject); return; }
            // Короткая удержанная вершина яркости, затем резкий спад, а не слабый первый кадр.
            float t = elapsed / duration;
            float fade = 1 - Mathf.InverseLerp(0.25f, 1, t);
            flash.intensity = initialIntensity * fade * fade;
            Color color = tint; color.a = fade;
            for (int i = 0; i < strokes.Length; i++)
            {
                var stroke = strokes[i]; stroke.color = color;
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
                for (int i = 0; i < flames.Length; i++)
                {
                    Color flameColor = Color.Lerp(tint, Color.white, i == 0 ? 0.1f : 0.5f);
                    flameColor.a = fade; flames[i].color = flameColor;
                }
        }
    }
}
