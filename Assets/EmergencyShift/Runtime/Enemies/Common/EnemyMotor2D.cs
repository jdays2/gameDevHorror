using UnityEngine;

namespace EmergencyShift
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class EnemyMotor2D : MonoBehaviour
    {
        private Rigidbody2D body;
        private CircleCollider2D shape;
        private CrowdAgent2D crowd;
        private readonly EnemyChasePath2D path = new EnemyChasePath2D();
        private static readonly float[] angles =
        {
            45,
            -45,
            90,
            -90
        };
        private int solids;
        public float Radius => shape.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        public int WaypointCount => path.WaypointCount;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<CircleCollider2D>();
            crowd = GetComponent<CrowdAgent2D>();
        }

        public void Configure(LayerMask opaque)
        {
            solids = opaque.value | (1 << 9);
        }

        public void Move(Vector2 velocity, bool separation = true)
        {
            body.linearVelocity = separation && crowd != null ? crowd.Adjust(velocity, solids) : velocity;
        }

        public Vector2 Follow(Vector2 goal, float speed) => path.Direction(body.position, goal, Radius, solids) * speed;
        public bool CanStand(Vector2 goal) => Physics2D.OverlapCircle(goal, Radius, solids) == null;
        public bool Clear(Vector2 from, Vector2 to) => Physics2D.Linecast(from, to, solids).collider == null;
        public Vector2 ClearDirection(Vector2 preferred)
        {
            float probe = shape.radius * .9f;
            if (Physics2D.CircleCast(body.position, probe, preferred, .3f, solids).collider == null)
            {
                return preferred;
            }

            foreach (float angle in angles)
            {
                Vector2 alternative = Quaternion.Euler(0, 0, angle) * preferred;
                if (Physics2D.CircleCast(body.position, probe, alternative, .3f, solids).collider == null)
                {
                    return alternative;
                }
            }

            return Vector2.zero;
        }

        private void OnDisable()
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
