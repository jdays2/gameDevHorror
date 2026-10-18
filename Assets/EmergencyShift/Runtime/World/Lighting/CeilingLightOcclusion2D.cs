using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift
{
    /// <summary>Top-down ceiling illumination: only authored full-height walls and doors occlude it.</summary>
    [ExecuteAlways, DefaultExecutionOrder(100), DisallowMultipleComponent]
    [RequireComponent(typeof(LocalLamp2D), typeof(Light2D))]
    public sealed class CeilingLightOcclusion2D : MonoBehaviour
    {
        private const int Resolution = 128;
        private const int RayCount = 512;
        [SerializeField, Tooltip("Full-height rectangular walls and moving door leaves. Exclude desks and other low furniture.")]
        private BoxCollider2D[] fullHeightBlockers = new BoxCollider2D[0];
        private LocalLamp2D lamp;
        private Light2D source;
        private Texture2D texture;
        private Sprite cookie;
        private Sprite originalCookie;
        private readonly float[] distances = new float[RayCount];
        private readonly Color32[] pixels = new Color32[Resolution * Resolution];
        private int previousGeometry;
        private bool initialized;

        public void Configure(BoxCollider2D[] blockers)
        {
            fullHeightBlockers = blockers;
            initialized = false;
            Refresh();
        }

        private void OnEnable()
        {
            lamp = GetComponent<LocalLamp2D>();
            source = GetComponent<Light2D>();
            originalCookie = source.lightCookieSprite;
            initialized = false;
            Refresh();
        }

        private void LateUpdate()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (source == null || lamp == null || fullHeightBlockers == null || fullHeightBlockers.Length == 0)
            {
                return;
            }

            int geometry = GeometryHash();
            if (!initialized || previousGeometry != geometry)
            {
                RebuildCookie();
                previousGeometry = geometry;
                initialized = true;
            }

            // LocalLamp owns power, colour and flicker. The cookie replaces planar furniture shadows.
            source.shadowsEnabled = false;
            source.lightCookieSprite = cookie;
        }

        private int GeometryHash()
        {
            unchecked
            {
                int hash = transform.localToWorldMatrix.GetHashCode() * 31 + lamp.Radius.GetHashCode();
                foreach (var blocker in fullHeightBlockers)
                {
                    if (blocker == null)
                    {
                        continue;
                    }

                    hash = hash * 31 + blocker.transform.localToWorldMatrix.GetHashCode();
                    hash = hash * 31 + blocker.size.GetHashCode();
                    hash = hash * 31 + blocker.offset.GetHashCode();
                    hash = hash * 31 + blocker.isActiveAndEnabled.GetHashCode();
                }

                return hash;
            }
        }

        private void RebuildCookie()
        {
            float radius = lamp.Radius;
            for (int i = 0; i < RayCount; i++)
            {
                float angle = i * Mathf.PI * 2 / RayCount;
                Vector3 direction = transform.rotation * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                distances[i] = VisibleDistance(transform.position, direction, radius, fullHeightBlockers);
            }

            float texelSize = radius * 2 / Resolution;
            for (int y = 0; y < Resolution; y++)
            {
                for (int x = 0; x < Resolution; x++)
                {
                    Vector2 offset = new Vector2(x + .5f - Resolution * .5f, y + .5f - Resolution * .5f) * texelSize;
                    float angle = Mathf.Repeat(Mathf.Atan2(offset.y, offset.x), Mathf.PI * 2);
                    float ray = angle * RayCount / (Mathf.PI * 2);
                    int first = Mathf.FloorToInt(ray) % RayCount;
                    float limit = Mathf.Min(distances[first], distances[(first + 1) % RayCount]);
                    byte value = (byte)(255 * Mathf.Clamp01((limit - offset.magnitude) / texelSize));
                    pixels[y * Resolution + x] = new Color32(value, value, value, 255);
                }
            }

            if (texture == null)
            {
                texture = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false, true);
                texture.name = "Ceiling wall occlusion - transient";
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                cookie = Sprite.Create(texture, new Rect(0, 0, Resolution, Resolution), Vector2.one * .5f, Resolution);
                cookie.name = texture.name;
                cookie.hideFlags = HideFlags.HideAndDontSave;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        public static float VisibleDistance(Vector2 origin, Vector2 direction, float range, BoxCollider2D[] blockers)
        {
            float nearest = range;
            foreach (var blocker in blockers)
            {
                if (blocker == null || !blocker.isActiveAndEnabled)
                {
                    continue;
                }

                Vector2 localOrigin = blocker.transform.InverseTransformPoint(origin);
                Vector2 localDirection = blocker.transform.InverseTransformVector(direction);
                Vector2 minimum = blocker.offset - blocker.size * .5f;
                Vector2 maximum = blocker.offset + blocker.size * .5f;
                float enter = 0;
                float exit = nearest;
                if (ClipAxis(localOrigin.x, localDirection.x, minimum.x, maximum.x, ref enter, ref exit) &&
                    ClipAxis(localOrigin.y, localDirection.y, minimum.y, maximum.y, ref enter, ref exit))
                {
                    nearest = enter;
                }
            }

            return nearest;
        }

        private static bool ClipAxis(float origin, float direction, float minimum, float maximum, ref float enter, ref float exit)
        {
            if (Mathf.Abs(direction) < .000001f)
            {
                return origin >= minimum && origin <= maximum;
            }

            float first = (minimum - origin) / direction;
            float second = (maximum - origin) / direction;
            enter = Mathf.Max(enter, Mathf.Min(first, second));
            exit = Mathf.Min(exit, Mathf.Max(first, second));
            return enter <= exit;
        }

        private void OnDisable()
        {
            if (source != null && source.lightCookieSprite == cookie)
            {
                source.lightCookieSprite = originalCookie;
            }

            UnityEngine.Rendering.CoreUtils.Destroy(cookie);
            UnityEngine.Rendering.CoreUtils.Destroy(texture);
            cookie = null;
            texture = null;
            initialized = false;
        }
    }
}
