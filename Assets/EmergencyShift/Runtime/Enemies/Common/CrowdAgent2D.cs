using EmergencyShift.Core.Composition;
using UnityEngine;
using Unity.Profiling;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Small-population local separation. Static walls still decide whether a step is safe.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D)), DisallowMultipleComponent]
    public sealed class CrowdAgent2D : MonoBehaviour
    {
        [SerializeField, Min(0)]
        private float clearance = .16f;
        [SerializeField, Range(0, 2)]
        private float strength = .85f;
        private Rigidbody2D body;
        private CircleCollider2D shape;
        private Damageable2D life;
        private int ordinal;
        private SceneRegistry<CrowdAgent2D> registry;
        private static readonly ProfilerMarker marker = new ProfilerMarker("EmergencyShift.Crowd.Separation");
        public float Radius => shape.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));

        [Inject]
        private void Construct(SceneRegistry<CrowdAgent2D> sceneRegistry)
        {
            registry?.Remove(this);
            registry = sceneRegistry;
            if (isActiveAndEnabled)
            {
                Register();
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<CircleCollider2D>();
            life = GetComponent<Damageable2D>();
        }

        private void OnEnable()
        {
            if (registry != null)
            {
                Register();
            }
        }

        private void OnDisable()
        {
            registry?.Remove(this);
        }

        private void Register()
        {
            int registration = registry.Register(this);
            if (ordinal == 0)
            {
                ordinal = registration;
            }
        }

        public Vector2 Adjust(Vector2 desired, LayerMask solids)
        {
            using (marker.Auto())
            {
                float speed = desired.magnitude;
                if (speed < .01f)
                {
                    return desired;
                }

                Vector2 repel = Vector2.zero;
                foreach (CrowdAgent2D other in registry.Members)
                {
                    if (other == this || other.body == null || (other.life != null && !other.life.IsAlive) || other.gameObject.scene != gameObject.scene)
                    {
                        continue;
                    }

                    Vector2 delta = body.position - other.body.position;
                    float range = Radius + other.Radius + clearance;
                    if (delta.sqrMagnitude >= range * range)
                    {
                        continue;
                    }

                    if (Physics2D.Linecast(body.position, other.body.position, solids).collider != null)
                    {
                        continue;
                    }

                    float distance = delta.magnitude;
                    Vector2 away = distance > .001f ? delta / distance : new Vector2(Mathf.Cos(ordinal * 2.39996f), Mathf.Sin(ordinal * 2.39996f));
                    repel += away * (1 - distance / range);
                }

                Vector2 next = Vector2.ClampMagnitude(desired + repel * speed * strength, speed);
                if (next.sqrMagnitude > .0001f && Physics2D.CircleCast(body.position, Radius * .95f, next.normalized, Mathf.Max(.08f, speed * Time.fixedDeltaTime), solids).collider != null)
                {
                    return Vector2.zero;
                }

                return next;
            }
        }

    }
}
