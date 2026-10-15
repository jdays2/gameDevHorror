using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Чёткие тени полного текущего силуэта, включая ноги, в плоской 2D-сцене.</summary>
    [DefaultExecutionOrder(30)]
    public sealed class CreatureGroundShadow2D : MonoBehaviour
    {
        [SerializeField]
        private EnemyPresentationSource2D creature;
        [SerializeField]
        private Transform visual;
        [SerializeField]
        private SpriteRenderer bodyRenderer;
        [SerializeField]
        private SpriteRenderer contact;
        [SerializeField]
        private SpriteRenderer projection;
        [SerializeField, Range(1, 1.15f)]
        private float contactScale = 1.04f;
        [SerializeField, Range(0, 1)]
        private float contactOpacity = 0.55f;
        [SerializeField, Range(0, 1)]
        private float projectionOpacity = 0.62f;
        [SerializeField, Range(0, 0.6f)]
        private float projectionStretch = 0.30f;
        [SerializeField, Min(0.05f)]
        private float maximumLength = 1.35f;
        private bool settledDead;
        private MaterialPropertyBlock contactProperties;
        private MaterialPropertyBlock projectionProperties;
        private static readonly int PivotId = Shader.PropertyToID("_Pivot");
        private static readonly int AwayId = Shader.PropertyToID("_Away");
        private static readonly int OffsetId = Shader.PropertyToID("_Offset");
        private static readonly int StretchId = Shader.PropertyToID("_Stretch");
        private static readonly int LightClipId = Shader.PropertyToID("_UseLightClip");
        private static readonly int LightId = Shader.PropertyToID("_Light");
        private static readonly int LightDirectionId = Shader.PropertyToID("_LightDirection");
        private static readonly int WallClipId = Shader.PropertyToID("_UseWallClip");
        private static readonly int WallId = Shader.PropertyToID("_Wall");
        public float ProjectionLength
        {
            get; private set;
        }
        public Vector2 ProjectionDirection
        {
            get; private set;
        }

        public void Configure(EnemyPresentationSource2D actor, Transform bodyVisual, SpriteRenderer contactRenderer, SpriteRenderer projectionRenderer)
        {
            creature = actor;
            visual = bodyVisual;
            contact = contactRenderer;
            projection = projectionRenderer;
            bodyRenderer = bodyVisual.Find("Body").GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            settledDead = false;
        }

        private void LateUpdate()
        {
            if (creature == null || bodyRenderer == null || visual == null || contact == null || projection == null || settledDead)
            {
                return;
            }

            if (contactProperties == null)
            {
                contactProperties = new MaterialPropertyBlock();
            }

            if (projectionProperties == null)
            {
                projectionProperties = new MaterialPropertyBlock();
            }

            bool dead = creature.IsDead;
            // Размер и поза уже содержатся в текущем кадре: дополнительное сжатие силуэта не нужно.
            float leap = creature.AttackProgress > 0 ? Mathf.Sin(creature.AttackProgress * Mathf.PI) : 0f;
            float size = dead ? Mathf.Lerp(1f, 0.95f, Mathf.Clamp01(creature.DeathTime * 2f)) : contactScale - leap * 0.02f;
            Vector2 center = bodyRenderer.transform.position;
            SyncSilhouette(contact, size);
            SyncSilhouette(projection, 1f);
            contact.enabled = true;
            contact.transform.position = new Vector3(center.x + 0.02f, center.y - 0.03f, dead ? -0.015f : -0.04f);
            contact.color = new Color(0, 0, 0, dead ? contactOpacity * 0.45f : contactOpacity * (1f - leap * 0.25f));
            contactProperties.Clear();
            contact.SetPropertyBlock(contactProperties);
            IThreatLight2D lamp = creature.LightSource;
            ProjectionLength = 0;
            projection.enabled = false;
            if (!dead && lamp != null && lamp.Illuminates(center, creature.BlockingGeometry))
            {
                Vector2 offset = center - lamp.Origin;
                float distance = offset.magnitude;
                if (distance > 0.05f)
                {
                    ProjectionDirection = offset / distance;
                    float reach = Mathf.Clamp01(1f - distance / (lamp.Range * 0.9f));
                    float shift = Mathf.Lerp(0.07f, maximumLength * 0.35f, reach);
                    float stretch = projectionStretch * reach;
                    Vector3 bodyScale = bodyRenderer.transform.lossyScale;
                    Vector3 extents = bodyRenderer.sprite.bounds.extents;
                    float radius = Mathf.Abs(Vector2.Dot(bodyRenderer.transform.right, ProjectionDirection)) * extents.x * Mathf.Abs(bodyScale.x) + Mathf.Abs(Vector2.Dot(bodyRenderer.transform.up, ProjectionDirection)) * extents.y * Mathf.Abs(bodyScale.y);
                    float available = Mathf.Max(0, maximumLength - radius);
                    shift = Mathf.Min(shift, available);
                    stretch = Mathf.Min(stretch, Mathf.Max(0, available - shift) / Mathf.Max(radius, 0.001f));
                    float length = radius * (1f + stretch) + shift;
                    RaycastHit2D hit = Physics2D.Linecast(center, center + ProjectionDirection * length, creature.BlockingGeometry);
                    if (length > 0.12f)
                    {
                        ProjectionLength = Mathf.Min(length, Mathf.Max(0, lamp.Range * 0.9f - distance));
                        projection.transform.position = new Vector3(center.x, center.y, -0.035f);
                        projection.color = new Color(0, 0, 0, projectionOpacity * Mathf.SmoothStep(0, 1, reach * 2f));
                        projectionProperties.Clear();
                        projectionProperties.SetVector(PivotId, new Vector4(center.x, center.y, 0, 0));
                        projectionProperties.SetVector(AwayId, new Vector4(ProjectionDirection.x, ProjectionDirection.y, 0, 0));
                        projectionProperties.SetFloat(OffsetId, shift);
                        projectionProperties.SetFloat(StretchId, stretch);
                        projectionProperties.SetFloat(LightClipId, 1);
                        projectionProperties.SetVector(LightId, new Vector4(lamp.Origin.x, lamp.Origin.y, lamp.Range * 0.9f, Mathf.Cos(lamp.BeamAngle * 0.45f * Mathf.Deg2Rad)));
                        projectionProperties.SetVector(LightDirectionId, new Vector4(lamp.Direction.x, lamp.Direction.y, 0, 0));
                        if (hit.collider != null)
                        {
                            ProjectionLength = Mathf.Min(ProjectionLength, Mathf.Max(0, hit.distance - 0.05f));
                            Vector2 point = hit.point + hit.normal * 0.03f;
                            projectionProperties.SetFloat(WallClipId, 1);
                            projectionProperties.SetVector(WallId, new Vector4(point.x, point.y, hit.normal.x, hit.normal.y));
                        }

                        projection.SetPropertyBlock(projectionProperties);
                        projection.enabled = true;
                    }
                }
            }

            // После оседания трупа настройки больше не пересчитываются.
            if (dead && creature.DeathTime >= 0.8f)
            {
                settledDead = true;
            }
        }

        private void SyncSilhouette(SpriteRenderer shadow, float scale)
        {
            shadow.sprite = bodyRenderer.sprite;
            shadow.flipX = bodyRenderer.flipX;
            shadow.flipY = bodyRenderer.flipY;
            shadow.transform.rotation = bodyRenderer.transform.rotation;
            Vector3 world = bodyRenderer.transform.lossyScale * scale;
            Vector3 parent = shadow.transform.parent.lossyScale;
            shadow.transform.localScale = new Vector3(world.x / parent.x, world.y / parent.y, 1);
        }

        private void OnDisable()
        {
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
