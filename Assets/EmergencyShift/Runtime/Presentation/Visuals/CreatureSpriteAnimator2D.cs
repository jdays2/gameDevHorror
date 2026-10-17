using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Перелистывает спрайты, поворачивает изображение и ведёт отдельный зрачок.</summary>
    public class CreatureSpriteAnimator2D : MonoBehaviour
    {
        [SerializeField]
        private EnemyPresentationSource2D presentation;
        [SerializeField]
        private Transform visualRoot;
        [SerializeField]
        private SpriteRenderer bodyRenderer;
        [SerializeField]
        private SpriteRenderer iris;
        [SerializeField]
        private SpriteRenderer pupil;
        [SerializeField, Tooltip("Opaque backing covers the eye baked into body frames; not a second pupil.")]
        private SpriteRenderer eyeUnderlay;
        [SerializeField]
        private Color damagedEyeTint = new Color(.64f, .60f, .60f, 1);
        [SerializeField, Tooltip("Optional textured damage treatment; null keeps the tint fallback.")]
        private Material damagedEyeMaterial;
        [SerializeField]
        private Sprite[] eyeFrames;
        [SerializeField]
        private Sprite[] walkFrames;
        [SerializeField]
        private Sprite[] recoilFrames;
        [SerializeField]
        private Sprite[] attackFrames;
        [SerializeField]
        private Sprite[] deathFrames;
        [SerializeField]
        private StalkerLocomotionProfile locomotionProfile;
        [SerializeField, Min(1)]
        private float walkFramesPerSecond = 7f;
        [SerializeField, Min(0.001f)]
        private float pupilTravel = 0.028f;
        [SerializeField, Min(1)]
        private float turnSpeed = 160f;
        private float walkClock;
        private float compactClock;
        private float walkPhase;
        private float movementWeight;
        private float supportYaw;
        private float turnVelocity;
        private Vector2 previousPosition;
        private bool hasPreviousPosition;
        private bool deathStyleApplied;
        private Color restingIrisColor = Color.white;
        private Color restingUnderlayColor = Color.white;
        private Material restingIrisMaterial;
        public int CurrentFrame
        {
            get; private set;
        }
        public bool EyeOpen => pupil != null && pupil.enabled;
        public Vector2 PupilOffset => pupil != null ? (Vector2)pupil.transform.localPosition : Vector2.zero;

        public void Configure(EnemyPresentationSource2D actor, Transform root, SpriteRenderer renderer, SpriteRenderer irisRenderer, SpriteRenderer pupilRenderer, Sprite[] walk, Sprite[] recoil)
        {
            presentation = actor;
            visualRoot = root;
            bodyRenderer = renderer;
            iris = irisRenderer;
            pupil = pupilRenderer;
            walkFrames = walk;
            recoilFrames = recoil;
        }

        public void ConfigureCombat(Sprite[] attack, Sprite[] death)
        {
            attackFrames = attack;
            deathFrames = death;
        }

        protected virtual void Awake()
        {
            presentation = GetComponent<EnemyPresentationSource2D>();
            if (iris != null)
            {
                restingIrisColor = iris.color;
                restingIrisMaterial = iris.sharedMaterial;
            }
            if (eyeUnderlay != null)
            {
                restingUnderlayColor = eyeUnderlay.color;
            }
            walkClock = (float)new System.Random(System.Guid.NewGuid().GetHashCode()).NextDouble() / Mathf.Max(1, walkFramesPerSecond) * 8;
            walkPhase = Mathf.Repeat(walkClock * Mathf.Max(1, walkFramesPerSecond) / 8f, 1f);
        }

        protected virtual void OnEnable()
        {
            hasPreviousPosition = false;
        }

        public Quaternion VisualRotation => visualRoot.rotation;
        public void RestoreVisualRotation(Quaternion rotation)
        {
            visualRoot.rotation = rotation;
            supportYaw = 0;
            turnVelocity = 0;
            movementWeight = 0;
            hasPreviousPosition = false;
        }

        public void RestoreDead()
        {
            RemainsVisualStyle.Apply(visualRoot);
            deathStyleApplied = true;
            if (deathFrames != null && deathFrames.Length > 0)
            {
                CurrentFrame = deathFrames.Length - 1;
                bodyRenderer.sprite = deathFrames[CurrentFrame];
            }

            if (iris != null)
            {
                iris.enabled = false;
            }

            if (pupil != null)
            {
                pupil.enabled = false;
            }
            if (eyeUnderlay != null)
            {
                eyeUnderlay.enabled = false;
            }

            enabled = false;
        }

        protected virtual void LateUpdate()
        {
            if (presentation == null || visualRoot == null || bodyRenderer == null || walkFrames == null || walkFrames.Length == 0)
            {
                return;
            }

            bool creeping = presentation.IsWalking;
            bool attacking = presentation.IsAttacking;
            bool dead = presentation.IsDead;
            if (dead && !deathStyleApplied)
            {
                RemainsVisualStyle.Apply(visualRoot);
                deathStyleApplied = true;
            }

            Vector2 position = transform.position;
            Vector2 translation = Vector2.zero;
            if (hasPreviousPosition)
            {
                translation = position - previousPosition;
            }

            previousPosition = position;
            hasPreviousPosition = true;

            if (locomotionProfile != null)
            {
                bool walking = creeping && !presentation.IsStaggered && presentation.Closure < 0.05f;
                UpdateLocomotion(walking, translation);
            }
            else if (creeping && presentation.Velocity.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(presentation.Velocity.y, presentation.Velocity.x) * Mathf.Rad2Deg - 90;
                visualRoot.rotation = Quaternion.RotateTowards(visualRoot.rotation, Quaternion.Euler(0, 0, angle), turnSpeed * Time.deltaTime);
                walkClock += Time.deltaTime * Mathf.Clamp(presentation.MovementSpeed / 1.1f, 0.35f, 1.5f);
            }

            if (presentation.IsOrienting && presentation.Target != null)
            {
                Vector2 target = presentation.AttackHeading;
                float angle = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg - 90;
                visualRoot.rotation = Quaternion.RotateTowards(visualRoot.rotation, Quaternion.Euler(0, 0, angle), turnSpeed * Time.deltaTime);
            }

            int eyeFrame = 0;
            if (dead && deathFrames != null && deathFrames.Length > 0)
            {
                CurrentFrame = Mathf.Min(deathFrames.Length - 1, (int)(presentation.DeathTime * 6f));
                bodyRenderer.sprite = deathFrames[CurrentFrame];
                if (presentation.DeathTime >= 1)
                {
                    enabled = false;
                }
            }
            else if (attacking && attackFrames != null && attackFrames.Length > 0)
            {
                CurrentFrame = Mathf.Min(attackFrames.Length - 1, (int)(presentation.AttackProgress * attackFrames.Length));
                bodyRenderer.sprite = attackFrames[CurrentFrame];
            }
            else if (presentation.Closure < 0.05f || recoilFrames == null || recoilFrames.Length == 0)
            {
                CurrentFrame = WalkFrame();
                bodyRenderer.sprite = walkFrames[CurrentFrame];
            }
            else
            {
                CurrentFrame = Mathf.Clamp(Mathf.RoundToInt(presentation.Closure * (recoilFrames.Length - 1)), 0, recoilFrames.Length - 1);
                if (presentation.IsCompactWalking)
                {
                    compactClock += Time.deltaTime;
                    CurrentFrame = recoilFrames.Length - 1 - ((int)(compactClock * 5) % 2);
                }

                bodyRenderer.sprite = recoilFrames[CurrentFrame];
                eyeFrame = CurrentFrame;
            }

            bool open = !dead && presentation.Closure < 0.166f;
            Color eyeTint = presentation.HasEyeDamage ? damagedEyeTint : Color.white;
            if (eyeUnderlay != null)
            {
                eyeUnderlay.enabled = !dead;
                eyeUnderlay.color = restingUnderlayColor * eyeTint;
            }
            if (iris != null)
            {
                bool texturedDamage = presentation.HasEyeDamage && damagedEyeMaterial != null;
                Material eyeMaterial = texturedDamage ? damagedEyeMaterial : restingIrisMaterial;
                if (iris.sharedMaterial != eyeMaterial)
                {
                    iris.sharedMaterial = eyeMaterial;
                }
                iris.color = texturedDamage ? restingIrisColor : restingIrisColor * eyeTint;
                if (eyeFrames != null && eyeFrames.Length > 0)
                {
                    int eyeFrameIndex = Mathf.Clamp(eyeFrame, 0, eyeFrames.Length - 1);
                    iris.sprite = eyeFrames[eyeFrameIndex];
                    iris.enabled = !dead;
                }
                else
                {
                    iris.enabled = open;
                }
            }

            if (pupil == null)
            {
                return;
            }

            pupil.enabled = open;
            if (!open || presentation.Target == null)
            {
                return;
            }

            Vector2 localTarget = visualRoot.InverseTransformPoint(presentation.Target.position);
            Vector2 offset = localTarget.normalized * pupilTravel * Mathf.Clamp01(localTarget.magnitude / 0.4f);
            Vector2 next = Vector2.MoveTowards(pupil.transform.localPosition, offset, Time.deltaTime * 0.25f);
            pupil.transform.localPosition = new Vector3(next.x, next.y, -0.002f);
        }

        private int WalkFrame()
        {
            if (locomotionProfile == null)
            {
                return (int)(walkClock * walkFramesPerSecond) % walkFrames.Length;
            }

            // Two support phases remain monotonic; the same distance always advances the same cycle.
            float supportPhase = walkPhase * Mathf.PI * 4f;
            float hold = Mathf.Clamp(locomotionProfile.supportHold, 0, 0.6f);
            float framePhase = walkPhase - Mathf.Sin(supportPhase) * hold / (Mathf.PI * 4f);
            return Mathf.Clamp(Mathf.FloorToInt(framePhase * walkFrames.Length), 0, walkFrames.Length - 1);
        }

        private void UpdateLocomotion(bool walking, Vector2 translation)
        {
            float heading = visualRoot.eulerAngles.z - supportYaw;
            if (!walking)
            {
                visualRoot.rotation = Quaternion.Euler(0, 0, heading);
                supportYaw = 0;
                turnVelocity = 0;
                movementWeight = 0;
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0)
            {
                return;
            }

            float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            float cycleDistance = Mathf.Max(0.01f, locomotionProfile.distancePerCycle * scale);
            float distance = translation.magnitude;
            float acceptedDistance = Mathf.Max(cycleDistance, presentation.MovementSpeed * dt * 3f);
            if (distance > acceptedDistance)
            {
                // Teleports/restores are not a burst of footwork.
                distance = 0;
            }

            float speed = distance / dt;
            bool moving = speed >= Mathf.Max(0.001f, locomotionProfile.minimumMovingSpeed);
            if (moving)
            {
                walkPhase = Mathf.Repeat(walkPhase + distance / cycleDistance, 1f);
                float targetHeading = Mathf.Atan2(translation.y, translation.x) * Mathf.Rad2Deg - 90;
                heading = Mathf.SmoothDampAngle(heading, targetHeading, ref turnVelocity,
                    Mathf.Max(0.01f, locomotionProfile.turnSmoothSeconds), turnSpeed, dt);
            }
            else
            {
                turnVelocity = 0;
            }

            float targetWeight = Mathf.Clamp01(speed / Mathf.Max(0.1f, locomotionProfile.fullWeightSpeed));
            if (!moving)
            {
                targetWeight = 0;
            }

            float response = targetWeight > movementWeight ? locomotionProfile.startSeconds : locomotionProfile.settleSeconds;
            movementWeight = Mathf.MoveTowards(movementWeight, targetWeight, dt / Mathf.Max(0.01f, response));
            supportYaw = Mathf.Sin(walkPhase * Mathf.PI * 2f) * Mathf.Clamp(locomotionProfile.supportYawDegrees, 0, 3) * movementWeight;
            visualRoot.rotation = Quaternion.Euler(0, 0, heading + supportYaw);
        }
    }
}
