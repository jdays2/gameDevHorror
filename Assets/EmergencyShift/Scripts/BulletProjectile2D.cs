using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EmergencyShift
{
    /// <summary>Летящая пуля с проверкой всего отрезка пути, чтобы не проскочить тонкую стену.</summary>
    public sealed class BulletProjectile2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer trail;
        [SerializeField] private SpriteRenderer tip;
        [SerializeField] private Light2D glow;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
        private Transform owner;
        private PlayerWeapon2D weapon;
        private ShotVisualProfile profile;
        private ShotEffect2D effectPrefab;
        private LayerMask collisionMask;
        private Vector2 direction;
        private float distance;
        private float maxDistance;
        private float age;
        private int damage;
        private bool active;
        public float DistanceTravelled => distance;

        public void ConfigureVisual(SpriteRenderer streak, SpriteRenderer head, Light2D light)
        { trail = streak; tip = head; glow = light; }

        public void Launch(Transform shooter, Vector2 heading, ShotVisualProfile style, ShotEffect2D impact, LayerMask mask, int hitDamage, float range)
        {
            owner = shooter; direction = heading.normalized; profile = style; effectPrefab = impact;
            weapon = shooter != null ? shooter.GetComponent<PlayerWeapon2D>() : null;
            collisionMask = mask; damage = hitDamage; maxDistance = Mathf.Max(0.1f, range);
            active = true; age = 0; distance = 0;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            tip.color = style.bulletColor; trail.color = style.bulletColor;
            glow.enabled = style.bulletLightRadius > 0 && style.bulletLightIntensity > 0;
            glow.color = style.bulletColor; glow.intensity = Mathf.Max(0, style.bulletLightIntensity);
            glow.pointLightOuterRadius = Mathf.Max(0.01f, style.bulletLightRadius);
            UpdateTrail();
        }

        private void FixedUpdate()
        {
            if (!active) return;
            age += Time.fixedDeltaTime;
            if (age > 3 || distance >= maxDistance) { Destroy(gameObject); return; }
            float step = Mathf.Min(Mathf.Max(1, profile.bulletSpeed) * Time.fixedDeltaTime, maxDistance - distance);
            Vector2 start = transform.position;
            var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = collisionMask };
            int count = Physics2D.CircleCast(start, 0.035f, direction, filter, hits, step);
            int closest = -1; float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider2D collider = hits[i].collider;
                if (collider == null || collider.transform.IsChildOf(owner)) continue;
                if (hits[i].distance < nearest) { nearest = hits[i].distance; closest = i; }
            }
            if (closest >= 0)
            {
                RaycastHit2D hit = hits[closest];
                if (weapon != null) weapon.ReportImpact(hit.collider, hit.point);
                var receiver = hit.collider.GetComponentInParent<Damageable2D>();
                if (receiver != null && receiver.IsAlive) receiver.ReceiveBulletHit(damage,hit.point,direction);
                Vector2 point = hit.centroid - direction * 0.01f;
                if(ImpactSurface2D.Resolve(hit.collider)!=ImpactMaterial.Flesh)
                {
                    var effect = Instantiate(effectPrefab, new Vector3(point.x, point.y, -0.1f), Quaternion.identity);
                    effect.Play(profile, true, -direction);
                }
                active = false; Destroy(gameObject); return;
            }
            Vector2 next = start + direction * step;
            transform.position = new Vector3(next.x, next.y, -0.1f);
            distance += step;
            UpdateTrail();
        }

        private void UpdateTrail()
        {
            float length = Mathf.Min(Mathf.Max(0.02f, profile.trailLength), distance);
            trail.enabled = length > 0.01f;
            trail.transform.localPosition = new Vector3(0, -length * 0.5f, 0.001f);
            trail.transform.localScale = new Vector3(Mathf.Max(0.01f, profile.trailWidth), length, 1);
        }
    }
}
