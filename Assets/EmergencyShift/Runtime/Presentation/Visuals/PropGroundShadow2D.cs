using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift
{
    /// <summary>Short sprite-silhouette shadows for low props; reuses the existing projection material.</summary>
    [DisallowMultipleComponent]
    public sealed class PropGroundShadow2D : MonoBehaviour
    {
        [SerializeField]
        private PropShadowProfile profile;
        [SerializeField]
        private SpriteRenderer body;
        [SerializeField]
        private Collider2D footprint;
        [SerializeField]
        private SpriteRenderer contact;
        [SerializeField]
        private SpriteRenderer projection;
        [SerializeField]
        private Flashlight2D flashlight;
        [SerializeField]
        private Light2D[] lamps;
        [SerializeField]
        private LayerMask blockers = 1 << 8;

        private MaterialPropertyBlock properties;
        public float CurrentExtension { get; private set; }
        public Vector2 CurrentDirection { get; private set; }

        public void Configure(PropShadowProfile style, SpriteRenderer source, Collider2D collider,
            SpriteRenderer contactRenderer, SpriteRenderer projectionRenderer)
        {
            profile = style;
            body = source;
            footprint = collider;
            contact = contactRenderer;
            projection = projectionRenderer;
        }

        public void ConfigureLights(Flashlight2D playerLight, Light2D[] roomLights)
        {
            flashlight = playerLight;
            lamps = roomLights;
        }

        private void Awake()
        {
            if (profile == null || body == null || footprint == null || contact == null || projection == null)
            {
                Debug.LogError("Missing prop shadow configuration: " + name, this);
                enabled = false;
                return;
            }

            profile.Validate();
            properties = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            Vector2 center = footprint.bounds.center;
            Sync(contact);
            contact.transform.localScale = Vector3.one * (1 + profile.contactExpansion);
            contact.color = new Color(0, 0, 0, profile.contactOpacity);
            contact.enabled = body.enabled;
            projection.enabled = false;
            CurrentExtension = 0;
            float strength = 0;
            Vector2 origin = center;
            Vector2 direction = Vector2.up;
            float range = 0;
            float cosine = -1;

            if (flashlight != null && flashlight.IsOn)
            {
                float angle = Mathf.Cos(flashlight.BeamAngle * .5f * Mathf.Deg2Rad);
                if (TryLight(center, flashlight.Origin, flashlight.Direction, flashlight.Range, angle, profile.flashlightStrength, out float score))
                {
                    strength = score;
                    origin = flashlight.Origin;
                    direction = flashlight.Direction;
                    range = flashlight.Range;
                    cosine = angle;
                }
            }

            if (lamps != null)
            {
                foreach (var lamp in lamps)
                {
                    if (lamp == null || !lamp.isActiveAndEnabled || lamp.intensity <= 0)
                    {
                        continue;
                    }

                    Vector2 at = lamp.transform.position;
                    if (TryLight(center, at, Vector2.up, lamp.pointLightOuterRadius, -1, lamp.intensity, out float score)
                        && score > strength)
                    {
                        strength = score;
                        origin = at;
                        direction = Vector2.up;
                        range = lamp.pointLightOuterRadius;
                        cosine = -1;
                    }
                }
            }

            if (strength <= 0 || !body.enabled)
            {
                return;
            }

            Vector2 away = (center - origin).normalized;
            float extension = Mathf.Min(profile.maximumExtension, Vector2.Distance(center, origin) * profile.extensionPerDistance);
            Vector2 extents = body.bounds.extents;
            float radius = Mathf.Abs(away.x) * extents.x + Mathf.Abs(away.y) * extents.y
                + Vector2.Distance(body.bounds.center, center);
            Sync(projection);
            projection.color = new Color(0, 0, 0, profile.projectionOpacity * Mathf.Clamp01(strength));
            properties.Clear();
            properties.SetVector("_Pivot", new Vector4(center.x, center.y, 0, 0));
            properties.SetVector("_Away", new Vector4(away.x, away.y, 0, 0));
            properties.SetFloat("_Offset", extension * .8f);
            properties.SetFloat("_Stretch", extension * .2f / Mathf.Max(.001f, radius));
            properties.SetFloat("_UseLightClip", 1);
            properties.SetVector("_Light", new Vector4(origin.x, origin.y, range, cosine));
            properties.SetVector("_LightDirection", new Vector4(direction.x, direction.y, 0, 0));
            projection.SetPropertyBlock(properties);
            projection.enabled = true;
            CurrentExtension = extension;
            CurrentDirection = away;
        }

        private bool TryLight(Vector2 center, Vector2 origin, Vector2 direction, float range,
            float cosine, float intensity, out float strength)
        {
            strength = 0;
            Vector2 offset = center - origin;
            float distance = offset.magnitude;
            if (distance <= .01f || distance >= range)
            {
                return false;
            }

            if (cosine > -1 && Vector2.Dot(offset / distance, direction) < cosine)
            {
                return false;
            }

            Collider2D first = Physics2D.Linecast(origin, center, blockers).collider;
            if (first != null && first != footprint)
            {
                return false;
            }

            strength = intensity * (1 - distance / range);
            return strength > 0;
        }

        private void Sync(SpriteRenderer target)
        {
            target.sprite = body.sprite;
            target.flipX = body.flipX;
            target.flipY = body.flipY;
            target.transform.localPosition = Vector3.zero;
            target.transform.localRotation = Quaternion.identity;
            target.transform.localScale = Vector3.one;
        }

        private void OnDisable()
        {
            CurrentExtension = 0;
            if (contact != null)
            {
                contact.enabled = false;
            }

            if (projection != null)
            {
                projection.enabled = false;
            }
        }
    }
}
