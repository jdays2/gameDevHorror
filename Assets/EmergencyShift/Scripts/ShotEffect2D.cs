using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift
{
    /// <summary>Короткая вспышка/попадание: одноразовый свет и простые геометрические штрихи.</summary>
    public sealed class ShotEffect2D : MonoBehaviour
    {
        [SerializeField] private Light2D flash;
        [SerializeField] private SpriteRenderer[] strokes;
        private float duration;
        private float elapsed;
        private float initialIntensity;
        private Color tint;
        private bool configured;

        public void Configure(Light2D light, SpriteRenderer[] shapes) { flash = light; strokes = shapes; }

        public void Play(ShotVisualProfile profile, bool impact, Vector2 direction)
        {
            configured = true; elapsed = 0;
            duration = impact ? profile.impactSeconds : profile.flashSeconds;
            duration = Mathf.Max(0.02f, duration);
            tint = profile.flashColor;
            initialIntensity = Mathf.Max(0, profile.flashIntensity) * (impact ? 0.5f : 1);
            flash.color = tint; flash.intensity = initialIntensity;
            flash.pointLightOuterRadius = Mathf.Max(0.1f, profile.flashRadius) * (impact ? 0.35f : 1);
            flash.enabled = true;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            transform.localScale = Vector3.one * (impact ? 0.65f : 1);
            foreach (SpriteRenderer stroke in strokes) stroke.color = tint;
        }

        private void Update()
        {
            if (!configured) return;
            elapsed += Time.deltaTime;
            if (elapsed >= duration) { Destroy(gameObject); return; }
            float fade = 1 - elapsed / duration;
            flash.intensity = initialIntensity * fade * fade;
            Color color = tint; color.a = fade;
            foreach (SpriteRenderer stroke in strokes) stroke.color = color;
        }
    }
}
