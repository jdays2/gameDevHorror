using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift
{
    /// <summary>Короткая проекция силуэта капсулы от настоящего света дула; плоское приближение высоты.</summary>
    [DefaultExecutionOrder(40)]
    public sealed class PlayerShotShadow2D : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer body;
        [SerializeField]
        private SpriteRenderer projection;
        [SerializeField, Range(0, 1)]
        private float opacity = .88f;
        [SerializeField, Min(.1f)]
        private float maximumLength = 1.65f;
        [SerializeField]
        private LayerMask walls = 1 << 8;
        private Light2D source;
        private float peak;
        private MaterialPropertyBlock properties;
        public bool Visible => projection != null && projection.enabled;
        public Vector2 Away
        {
            get; private set;
        }

        public void Configure(SpriteRenderer capsule, SpriteRenderer shadow)
        {
            body = capsule;
            projection = shadow;
        }

        public void Follow(Light2D light)
        {
            source = light;
            peak = light != null ? Mathf.Max(.01f, light.intensity) : 1;
        }

        private void LateUpdate()
        {
            if (projection == null || body == null)
            {
                return;
            }

            projection.enabled = source != null && source.enabled && source.intensity > .02f;
            if (!projection.enabled)
            {
                return;
            }

            Vector2 center = body.transform.position;
            Away = (center - (Vector2)source.transform.position).normalized;
            projection.sprite = body.sprite;
            projection.transform.rotation = body.transform.rotation;
            Vector3 parent = projection.transform.parent.lossyScale, scale = body.transform.lossyScale;
            projection.transform.localScale = new Vector3(scale.x / parent.x, scale.y / parent.y, 1);
            projection.transform.position = new Vector3(center.x, center.y, -.065f);
            projection.color = new Color(0, 0, 0, opacity * Mathf.Clamp01(source.intensity / peak));
            if (properties == null)
            {
                properties = new MaterialPropertyBlock();
            }

            properties.Clear();
            properties.SetVector("_Pivot", new Vector4(center.x, center.y, 0, 0));
            properties.SetVector("_Away", new Vector4(Away.x, Away.y, 0, 0));
            Vector3 extents = body.sprite.bounds.extents;
            float radius = Mathf.Abs(Vector2.Dot(body.transform.right, Away)) * extents.x * Mathf.Abs(scale.x) + Mathf.Abs(Vector2.Dot(body.transform.up, Away)) * extents.y * Mathf.Abs(scale.y);
            float shift = Mathf.Min(.28f, Mathf.Max(0, maximumLength - radius));
            float stretch = Mathf.Min(1.3f, Mathf.Max(0, maximumLength - radius - shift) / Mathf.Max(.001f, radius));
            properties.SetFloat("_Offset", shift);
            properties.SetFloat("_Stretch", stretch);
            properties.SetFloat("_UseLightClip", 1);
            properties.SetVector("_Light", new Vector4(source.transform.position.x, source.transform.position.y, source.pointLightOuterRadius, -1));
            properties.SetVector("_LightDirection", new Vector4(Away.x, Away.y, 0, 0));
            RaycastHit2D hit = Physics2D.Linecast(center, center + Away * maximumLength, walls);
            if (hit.collider != null)
            {
                Vector2 p = hit.point + hit.normal * .02f;
                properties.SetFloat("_UseWallClip", 1);
                properties.SetVector("_Wall", new Vector4(p.x, p.y, hit.normal.x, hit.normal.y));
            }

            projection.SetPropertyBlock(properties);
        }

        private void OnDisable()
        {
            source = null;
            if (projection != null)
            {
                projection.enabled = false;
            }
        }
    }
}
