using System;
using EmergencyShift.Core.Composition;
using EmergencyShift.Features.Projectiles;
using EmergencyShift.Features.Projectiles.Logic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zenject;
using Random = UnityEngine.Random;

namespace EmergencyShift
{
    /// <summary>Летящая пуля с проверкой всего отрезка пути, чтобы не проскочить тонкую стену.</summary>
    public sealed class BulletProjectile2D : MonoBehaviour, IProjectileFlightEnvironment
    {
        [SerializeField]
        private SpriteRenderer trail;
        [SerializeField]
        private SpriteRenderer tip;
        [SerializeField]
        private Light2D glow;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
        private Transform owner;
        private PlayerWeapon2D weapon;
        private ShotVisualProfile profile;
        private ProjectileVisualProfile presentation;
        private ShotEffect2D effectPrefab;
        private LayerMask collisionMask;
        private ProjectileFlightControllerFactory factory;
        private ScenePrefabFactory prefabs;
        private ProjectileFlightController controller;
        private RaycastHit2D collision;
        private int volleyId;
        private bool visible;
        private float trailScale;
        public float DistanceTravelled => controller?.Model.Distance ?? 0;
        public Vector2 FlightDirection => controller?.Model.Direction ?? Vector2.zero;

        [Inject]
        private void Construct(ProjectileFlightControllerFactory flightFactory, ScenePrefabFactory scenePrefabs)
        {
            factory = flightFactory ?? throw new ArgumentNullException(nameof(flightFactory));
            prefabs = scenePrefabs ?? throw new ArgumentNullException(nameof(scenePrefabs));
        }

        public void ConfigureVisual(SpriteRenderer streak, SpriteRenderer head, Light2D light)
        {
            trail = streak;
            tip = head;
            glow = light;
        }

        public void Launch(Transform shooter, Vector2 heading, ShotVisualProfile style, ShotEffect2D impact, LayerMask mask, int hitDamage, float range, int shotId = 0)
        {
            if (style == null || style.projectile == null)
            {
                throw new System.InvalidOperationException("A projectile visual profile is required.");
            }

            if (factory == null)
            {
                throw new InvalidOperationException("Projectile requires its level composition before launch.");
            }

            ProjectileFlightController replacement = factory.Create(style.projectile.ToFlightSettings(range, hitDamage),
                transform.position, heading, this);
            owner = shooter;
            profile = style;
            presentation = style.projectile;
            effectPrefab = impact;
            weapon = shooter != null ? shooter.GetComponent<PlayerWeapon2D>() : null;
            collisionMask = mask;
            volleyId = shotId;
            controller = replacement;
            Vector2 direction = controller.Model.Direction;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            visible = Random.value < presentation.projectileVisibility;
            float variation = presentation.trailVariation;
            trailScale = Random.Range(1 - variation, 1);
            var tint = presentation.bulletColor;
            tint.a *= Random.Range(1 - variation, 1);
            tip.color = tint;
            tip.enabled = visible;
            trail.color = tint;
            glow.enabled = visible && presentation.bulletLightRadius > 0 && presentation.bulletLightIntensity > 0;
            glow.color = presentation.bulletColor;
            glow.intensity = Mathf.Max(0, presentation.bulletLightIntensity);
            glow.pointLightOuterRadius = Mathf.Max(0.01f, presentation.bulletLightRadius);
            UpdateTrail();
        }

        private void FixedUpdate() => controller?.Step(Time.fixedDeltaTime);

        bool IProjectileFlightEnvironment.Sweep(Vector2 start, Vector2 direction, float radius, float step)
        {
            var filter = new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = true,
                layerMask = collisionMask
            };
            int count = Physics2D.CircleCast(start, radius, direction, filter, hits, step);
            int closest = -1;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider2D collider = hits[i].collider;
                if (collider == null || collider.transform.IsChildOf(owner))
                {
                    continue;
                }

                if (hits[i].distance < nearest)
                {
                    nearest = hits[i].distance;
                    closest = i;
                }
            }

            if (closest < 0)
            {
                return false;
            }

            collision = hits[closest];
            return true;
        }

        void IProjectileFlightEnvironment.Impact(int damage, Vector2 direction)
        {
            if (weapon != null)
            {
                weapon.ReportImpact(collision.collider, collision.point, volleyId);
            }

            var receiver = collision.collider.GetComponentInParent<Damageable2D>();
            if (receiver != null && receiver.IsAlive)
            {
                receiver.ReceiveBulletHit(damage, collision.point, direction);
            }

            Vector2 point = collision.centroid - direction * .01f;
            if (ImpactSurface2D.Resolve(collision.collider) != ImpactMaterial.Flesh)
            {
                var effect = prefabs.Instantiate(effectPrefab.gameObject,
                    new Vector3(point.x, point.y, -.1f), Quaternion.identity).GetComponent<ShotEffect2D>();
                effect.Play(profile, true, -direction);
            }
        }

        void IProjectileFlightEnvironment.Move(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, -.1f);
            UpdateTrail();
        }

        void IProjectileFlightEnvironment.Release() => Destroy(gameObject);

        private void UpdateTrail()
        {
            float length = Mathf.Min(presentation.trailLength * trailScale, DistanceTravelled);
            trail.enabled = visible && length > 0.01f;
            trail.transform.localPosition = new Vector3(0, -length * 0.5f, 0.001f);
            trail.transform.localScale = new Vector3(presentation.trailWidth, length, 1);
        }
    }
}
