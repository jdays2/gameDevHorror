using UnityEngine;

namespace EmergencyShift
{
    /// <summary>One bounded cosmetic cloud; its scene-owned shot effect supplies the scaled clock.</summary>
    public sealed class MuzzleSmoke2D : MonoBehaviour
    {
        private static readonly int AgeId = Shader.PropertyToID("_Age");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");
        private static readonly int SideBurstId = Shader.PropertyToID("_SideBurst");
        [SerializeField] private SpriteRenderer cloud;
        [SerializeField] private SpriteRenderer sideBurst;
        private MaterialPropertyBlock properties;
        private MaterialPropertyBlock sideProperties;
        private Vector2 size;
        private Vector2 sideSize;
        private float elapsed;
        private float duration;
        private float dispersion;
        private float drift;
        private float sideDuration;
        public bool IsVisible => (cloud != null && cloud.enabled) || (sideBurst != null && sideBurst.enabled);
        public bool HasSideBurst => sideBurst != null && sideBurst.enabled;

        public void Configure(SpriteRenderer renderer) => cloud = renderer;
        public void ConfigureSideBurst(SpriteRenderer renderer) => sideBurst = renderer;

        public void Begin(ShotVisualProfile profile)
        {
            elapsed = 0;
            duration = profile.SmokeSeconds;
            dispersion = profile.SmokeDispersion;
            size = profile.SmokeSize;
            sideSize = profile.SideSmokeSize;
            sideDuration = profile.SideSmokeSeconds;
            drift = Random.Range(-.16f, .16f);
            if (properties == null)
            {
                properties = new MaterialPropertyBlock();
            }
            properties.SetFloat(SeedId, Random.Range(0f, 100f));
            properties.SetFloat(AgeId, 0);
            properties.SetFloat(SideBurstId, 0);
            cloud.SetPropertyBlock(properties);
            Color tint = profile.SmokeColor;
            tint.a *= profile.SmokeOpacity;
            cloud.color = tint;
            cloud.enabled = tint.a > 0;
            if (sideBurst != null)
            {
                if (sideProperties == null)
                {
                    sideProperties = new MaterialPropertyBlock();
                }
                sideProperties.SetFloat(SeedId, Random.Range(0f, 100f));
                sideProperties.SetFloat(SideBurstId, 1);
                tint = profile.SmokeColor;
                tint.a *= profile.SideSmokeOpacity;
                sideBurst.color = tint;
                sideBurst.enabled = tint.a > 0;
            }
            Present();
        }

        public bool Tick(float seconds)
        {
            if (!IsVisible)
            {
                return false;
            }
            elapsed += seconds;
            if (elapsed >= duration)
            {
                Stop();
                return false;
            }
            Present();
            return true;
        }

        public void Stop()
        {
            if (cloud != null)
            {
                cloud.enabled = false;
            }
            if (sideBurst != null)
            {
                sideBurst.enabled = false;
            }
        }

        private void Present()
        {
            float age = Mathf.Clamp01(elapsed / duration);
            float expansion = 1 + dispersion * age;
            cloud.transform.localPosition = new Vector3(drift * age, size.y * .45f + elapsed * dispersion * .35f, .001f);
            cloud.transform.localScale = new Vector3(size.x * expansion, size.y * expansion, 1);
            properties.SetFloat(AgeId, age);
            cloud.SetPropertyBlock(properties);
            if (sideBurst != null && sideBurst.enabled)
            {
                float sideAge = Mathf.Clamp01(elapsed / sideDuration);
                sideBurst.enabled = sideAge < 1;
                sideBurst.transform.localPosition = new Vector3(0, .045f, .002f);
                sideBurst.transform.localScale = new Vector3(sideSize.x, sideSize.y, 1);
                sideProperties.SetFloat(AgeId, sideAge);
                sideBurst.SetPropertyBlock(sideProperties);
            }
        }

        private void OnDisable() => Stop();
    }
}
