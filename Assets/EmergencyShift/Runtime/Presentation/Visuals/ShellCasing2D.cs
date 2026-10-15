using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Small top-down casing: planar motion plus virtual height, one first-floor-contact sound.</summary>
    public sealed class ShellCasing2D : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer body;
        [SerializeField]
        private SpriteRenderer contact;
        private PlayerAudio2D audioOwner;
        private Vector2 floor;
        private Vector2 velocity;
        private float height;
        private float lift;
        private float gravity;
        private float spin;
        private Vector3 bodyScale;
        private LayerMask obstacles;
        private bool landed;
        public bool Settled
        {
            get; private set;
        }
        public bool Landed => landed;
        public float Height => height;
        public Vector2 FloorPosition => floor;

        public event System.Action<ShellCasing2D> FirstLanded;
        public void Launch(Vector2 origin, Vector2 impulse, float verticalSpeed, float angularSpeed, PlayerAudio2D owner, LayerMask walls)
        {
            floor = origin;
            velocity = impulse;
            height = .04f;
            lift = verticalSpeed;
            gravity = 12;
            spin = angularSpeed;
            audioOwner = owner;
            obstacles = walls;
            landed = Settled = false;
            bodyScale = new Vector3(.075f, .028f, 1);
            body.transform.localScale = bodyScale;
            body.color = new Color(.63f, .47f, .23f);
            enabled = true;
            gameObject.SetActive(true);
            ApplyVisual();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float distance = velocity.magnitude * dt;
            var hit = distance > 0 ? Physics2D.CircleCast(floor, .015f, velocity.normalized, distance, obstacles) : default(RaycastHit2D);
            if (hit.collider != null)
            {
                floor = hit.centroid + hit.normal * .008f;
                velocity = Vector2.Reflect(velocity, hit.normal) * .3f;
            }
            else
            {
                floor += velocity * dt;
            }

            lift -= gravity * dt;
            height += lift * dt;
            if (height <= 0 && lift < 0)
            {
                height = 0;
                if (!landed)
                {
                    landed = true;
                    lift = -lift * .22f;
                    velocity *= .20f;
                    spin *= .35f;
                    if (audioOwner != null)
                    {
                        audioOwner.PlayCasingAt(floor, GroundAudioSurface2D.At(floor));
                    }

                    FirstLanded?.Invoke(this);
                }
                else
                {
                    Settled = true;
                    velocity = Vector2.zero;
                    spin = 0;
                    enabled = false;
                }
            }

            velocity = Vector2.MoveTowards(velocity, Vector2.zero, (landed ? 12 : .45f) * dt);
            body.transform.Rotate(0, 0, spin * dt);
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            transform.position = new Vector3(floor.x, floor.y, -.04f);
            body.transform.localScale = bodyScale * (1 + height * .6f);
            contact.transform.localScale = new Vector3(.082f, .032f, 1) / (1 + height);
            contact.color = new Color(0, 0, 0, Mathf.Lerp(.42f, .18f, Mathf.Clamp01(height)));
        }
    }
}
