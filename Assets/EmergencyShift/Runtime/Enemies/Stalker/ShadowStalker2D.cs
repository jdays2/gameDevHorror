using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Species brain: permanent aggro, delayed light fear, retreat and melee.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    [RequireComponent(typeof(EnemyHealth2D), typeof(EnemyPerception2D), typeof(EnemyMotor2D))]
    [RequireComponent(typeof(EnemyMeleeAttack2D))]
    public sealed class ShadowStalker2D : Damageable2D
    {
        public enum BehaviourState
        {
            Sneaking,
            Recoiling,
            Retreating,
            Waiting,
            Attacking,
            Dead,
            Dormant,
            Returning,
            Patrolling,
            TargetDead
        }

        [SerializeField]
        private Transform player;
        [SerializeField]
        private Flashlight2D flashlight;
        [SerializeField]
        private LayerMask blockingGeometry;
        [SerializeField]
        private bool attacksEnabled = true;
        [Header("First detection — proximity and line of sight")]
        [SerializeField, Min(.5f), InspectorName("Detection Radius")]
        private float habitatRadius = 3.2f;
        [Header("Shared immutable tuning — duplicate asset for a variant")]
        [SerializeField]
        private EnemyDefinition definition;
        [SerializeField]
        private StalkerBehaviourProfile behaviourProfile;
        private EnemyHealth2D health;
        private EnemyPerception2D perception;
        private EnemyMotor2D motor;
        private EnemyMeleeAttack2D melee;
        public EnemyDefinition Definition => definition;
        public StalkerBehaviourProfile BehaviourProfile => behaviourProfile;
        private float creepSpeed => definition.pursuitSpeed;
        private float attackDistance => definition.attackDistance;
        private float attackDuration => definition.attackDuration;
        private float attackCooldown => definition.attackCooldown;
        private int maximumHealth => definition.maximumHealth;
        private float hitStaggerSeconds => definition.hitStaggerSeconds;
        private float hitPushSpeed => definition.hitPushSpeed;
        private float retreatSpeed => behaviourProfile.retreatSpeed;
        private float standOffDistance => behaviourProfile.standOffDistance;
        private float recoilSeconds => behaviourProfile.recoilSeconds;
        private float retreatSeconds => behaviourProfile.retreatSeconds;
        private float cooldownSeconds => behaviourProfile.cooldownSeconds;
        private float lightFearDistance => behaviourProfile.lightFearDistance;
        private float reactionDelay => behaviourProfile.reactionDelay;

        private Rigidbody2D body;
        private CircleCollider2D bodyCollider;
        private float stateTime;
        private Vector2 escapeDirection;
        private float exposureTime;
        private Vector2 approachBeforeLight;
        private Vector2 home;
        private float staggerUntil;
        private Vector2 hitPush;
        private EnemyHitResponse2D hitResponse;
        private EnemyPatrol2D patrol;
        private CrowdAgent2D crowd;
        private StalkerAudio2D audioFeedback;
        private static readonly Unity.Profiling.ProfilerMarker tickMarker = new Unity.Profiling.ProfilerMarker("EmergencyShift.Stalker.Tick");
        public override bool IsAlive => State != BehaviourState.Dead;
        public override bool OwnsHitAudio => audioFeedback != null && audioFeedback.isActiveAndEnabled;

        public override void ReceiveBulletHit(int damage, Vector2 point, Vector2 direction)
        {
            ApplyBulletHit(damage, point, direction);
        }

        // Presentation observes these events; damage, timing and state ownership stay here.
        public event System.Action<BehaviourState> StateChanged;
        public event System.Action Damaged;
        public event System.Action AttackLunged;
        public event System.Action<bool> AttackResolved;
        public bool HasAggro
        {
            get; private set;
        }
        public float PursuitSpeed => creepSpeed;
        public float RetreatSpeed => retreatSpeed;
        public int ChaseWaypointCount => motor != null ? motor.WaypointCount : 0;
        public Vector2 Home => home;
        public float HabitatRadius => habitatRadius;
        public bool IsStaggered => State != BehaviourState.Dead && Time.time < staggerUntil;
        public BehaviourState State
        {
            get; private set;
        }
        public float Closure
        {
            get; private set;
        }
        public bool Exposed
        {
            get; private set;
        }
        public float MovementSpeed => body != null ? body.linearVelocity.magnitude : 0;
        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;
        public Transform Player => player;
        public Flashlight2D LightSource => flashlight;
        public LayerMask BlockingGeometry => blockingGeometry;
        public float LightFearDistance => lightFearDistance;
        public float ReactionDelay => reactionDelay;
        public float LightReactionProgress => Mathf.Clamp01(exposureTime / reactionDelay);
        public float AttackProgress => State == BehaviourState.Attacking ? Mathf.Clamp01(stateTime / attackDuration) : 0;
        public float DeathTime => State == BehaviourState.Dead ? stateTime : 0;
        public int Health => health != null ? health.Current : 0;
        public bool AttacksEnabled
        {
            get => attacksEnabled; set => attacksEnabled = value;
        }

        public void Configure(Transform target, Flashlight2D lamp, LayerMask obstacles)
        {
            player = target;
            flashlight = lamp;
            blockingGeometry = obstacles;
            if (perception != null)
            {
                perception.Configure(target, obstacles, lamp);
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CircleCollider2D>();
            var context = LevelContext2D.For(this);
            if (context != null)
            {
                if (player == null && context.Player != null)
                {
                    player = context.Player.transform;
                }

                if (flashlight == null)
                {
                    flashlight = context.Flashlight;
                }
            }

            crowd = GetComponent<CrowdAgent2D>();
            audioFeedback = GetComponent<StalkerAudio2D>();
            if (definition == null || behaviourProfile == null)
            {
                Debug.LogError("Missing enemy profiles: " + name, this);
                enabled = false;
                return;
            }

            health = GetComponent<EnemyHealth2D>();
            perception = GetComponent<EnemyPerception2D>();
            motor = GetComponent<EnemyMotor2D>();
            melee = GetComponent<EnemyMeleeAttack2D>();
            perception.Configure(player, blockingGeometry, flashlight);
            motor.Configure(blockingGeometry);
            melee.Configure(definition, perception, motor);
            health.Initialize(maximumHealth);
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            melee.Lunged += OnLunged;
            melee.Resolved += OnResolved;
            hitResponse = GetComponent<EnemyHitResponse2D>();
            home = body.position;
            State = BehaviourState.Dormant;
            patrol = GetComponent<EnemyPatrol2D>();
            if (patrol != null)
            {
                patrol.Initialize(home);
            }
        }

        private void FixedUpdate()
        {
            using (tickMarker.Auto())
            {
                Tick();
            }
        }

        private void Tick()
        {
            stateTime += Time.fixedDeltaTime;
            if (State == BehaviourState.Dead)
            {
                body.linearVelocity = Vector2.zero;
                if (stateTime >= 3)
                {
                    enabled = false;
                }

                return;
            }

            if (player == null || flashlight == null)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            bool aliveTarget = perception.TargetAlive;
            if (!aliveTarget)
            {
                if (patrol != null)
                {
                    patrol.ResetRoute();
                }

                Exposed = false;
                exposureTime = 0;
                Enter(BehaviourState.TargetDead);
                body.linearVelocity = Vector2.zero;
                return;
            }

            bool lineOfSight = perception.CanSeeTarget(body.position);
            if (!HasAggro && Vector2.Distance(body.position, player.position) <= habitatRadius && lineOfSight)
            {
                ActivateAggro();
            }

            if (IsStaggered)
            {
                body.linearVelocity = hitPush * Mathf.Clamp01((staggerUntil - Time.time) / Mathf.Max(.01f, hitStaggerSeconds));
                return;
            }

            Exposed = perception.Exposed(body.position);
            bool closeLight = Exposed && Vector2.Distance(body.position, flashlight.Origin) <= lightFearDistance;
            bool idle = State == BehaviourState.Dormant || State == BehaviourState.Patrolling;
            if ((idle || State == BehaviourState.Sneaking || State == BehaviourState.Attacking) && closeLight)
            {
                if (exposureTime == 0)
                {
                    approachBeforeLight = body.linearVelocity.sqrMagnitude > 0.01f ? body.linearVelocity.normalized : ((Vector2)player.position - body.position).normalized;
                }

                exposureTime += Time.fixedDeltaTime;
                if (exposureTime >= reactionDelay)
                {
                    escapeDirection = (body.position - flashlight.Origin).normalized;
                    if (escapeDirection.sqrMagnitude < 0.001f)
                    {
                        escapeDirection = -flashlight.Direction;
                    }

                    Enter(BehaviourState.Recoiling);
                }
            }
            else
            {
                exposureTime = 0;
            }

            Vector2 velocity = Vector2.zero;
            switch (State)
            {
                case BehaviourState.Dormant:
                case BehaviourState.Patrolling:
                    velocity = StepIdle();
                    break;
                case BehaviourState.Sneaking:
                    velocity = StepPursuit(closeLight, lineOfSight);
                    break;
                case BehaviourState.Recoiling:
                    velocity = StepRecoil();
                    break;
                case BehaviourState.Retreating:
                    velocity = StepRetreat();
                    break;
                case BehaviourState.Waiting:
                    velocity = StepWait();
                    break;
                case BehaviourState.Attacking:
                    velocity = StepAttack();
                    break;
            }

            motor.Move(velocity, State != BehaviourState.Attacking);
        }

        private Vector2 StepIdle()
        {
            Vector2 velocity = Vector2.zero;
            Closure = Mathf.MoveTowards(Closure, 0, Time.fixedDeltaTime * 3);
            if (patrol != null && patrol.PatrolEnabled)
            {
                if (State != BehaviourState.Patrolling)
                {
                    Enter(BehaviourState.Patrolling);
                }

                float radius = bodyCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
                velocity = patrol.Step(body.position, radius, blockingGeometry.value | (1 << 9), Time.fixedDeltaTime);
            }
            else if (State != BehaviourState.Dormant)
            {
                if (patrol != null)
                {
                    patrol.ResetRoute();
                }

                Enter(BehaviourState.Dormant);
            }

            return velocity;
        }

        private Vector2 StepPursuit(bool closeLight, bool lineOfSight)
        {
            Vector2 velocity = Vector2.zero;
            Closure = Mathf.MoveTowards(Closure, 0, Time.fixedDeltaTime * 3);
            if (closeLight)
            {
                velocity = motor.ClearDirection(approachBeforeLight) * creepSpeed;
                return velocity;
            }

            Vector2 toPlayer = (Vector2)player.position - body.position;
            if (HasAggro)
            {
                if (attacksEnabled && melee.CanStart(body.position) && Closure < 0.1f)
                {
                    Enter(BehaviourState.Attacking);
                    melee.Begin();
                    escapeDirection = toPlayer.normalized;
                    return velocity;
                }

                Vector2 behindPlayer = (Vector2)player.position - flashlight.Direction * standOffDistance;
                float radius = bodyCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
                int solids = blockingGeometry.value | (1 << 9);
                Vector2 goal = lineOfSight ? behindPlayer : (Vector2)player.position;
                if (lineOfSight && (!motor.CanStand(goal) || !motor.Clear(player.position, goal)))
                {
                    goal = player.position;
                }

                Vector2 toBehind = goal - body.position;
                if (toBehind.magnitude > 0.15f && toPlayer.magnitude > standOffDistance * 0.8f)
                {
                    velocity = motor.Follow(goal, creepSpeed);
                }
            }

            return velocity;
        }

        private Vector2 StepRecoil()
        {
            Vector2 velocity = Vector2.zero;
            Closure = Mathf.Clamp01(stateTime / recoilSeconds);
            if (stateTime >= recoilSeconds)
            {
                Enter(BehaviourState.Retreating);
            }

            return velocity;
        }

        private Vector2 StepRetreat()
        {
            Vector2 velocity = Vector2.zero;
            Closure = 1;
            velocity = motor.ClearDirection(escapeDirection) * retreatSpeed;
            if (stateTime >= retreatSeconds)
            {
                Enter(BehaviourState.Waiting);
                velocity = Vector2.zero;
            }

            return velocity;
        }

        private Vector2 StepWait()
        {
            Vector2 velocity = Vector2.zero;
            Closure = 1;
            if (stateTime >= cooldownSeconds && !Exposed)
            {
                Enter(HasAggro ? BehaviourState.Sneaking : BehaviourState.Dormant);
            }

            return velocity;
        }

        private Vector2 StepAttack()
        {
            Vector2 velocity = Vector2.zero;
            Closure = 0;
            velocity = melee.Step(AttackProgress, body.position, escapeDirection);
            if (AttackProgress >= 1)
            {
                melee.Finish();
                Enter(BehaviourState.Sneaking);
                velocity = Vector2.zero;
            }

            return velocity;
        }

        private void Enter(BehaviourState state)
        {
            bool changed = State != state;
            State = state;
            stateTime = 0;
            if (changed)
            {
                StateChanged?.Invoke(state);
            }
        }

        private void ActivateAggro()
        {
            if (HasAggro)
            {
                return;
            }

            HasAggro = true;
            if (patrol != null)
            {
                patrol.ResetRoute();
            }

            if (State == BehaviourState.Dormant || State == BehaviourState.Patrolling)
            {
                Enter(BehaviourState.Sneaking);
            }
        }

        public void ApplyBulletHit(int amount, Vector2 point, Vector2 direction)
        {
            if (State == BehaviourState.Dead || amount <= 0)
            {
                return;
            }

            TakeDamage(amount);
            if (hitResponse != null)
            {
                hitResponse.Play(point, direction, State == BehaviourState.Dead);
            }

            if (State == BehaviourState.Dead)
            {
                return;
            }

            if (perception.TargetAlive)
            {
                ActivateAggro();
            }

            staggerUntil = Time.time + hitStaggerSeconds;
            hitPush = direction.normalized * hitPushSpeed;
            if (State == BehaviourState.Attacking)
            {
                Enter(BehaviourState.Sneaking);
                melee.Interrupt(.35f);
            }
        }

        public void TakeDamage(int amount)
        {
            if (health != null)
            {
                health.TakeDamage(amount);
            }
        }

        private void OnDamaged() => Damaged?.Invoke();
        private void OnLunged() => AttackLunged?.Invoke();
        private void OnResolved(bool landed) => AttackResolved?.Invoke(landed);
        [ContextMenu("Preview/Kill creature (Play only)")]
        public void Kill()
        {
            if (Application.isPlaying && health != null)
            {
                health.Kill();
            }
        }

        private void OnDied()
        {
            Closure = 1;
            Enter(BehaviourState.Dead);
            if (crowd != null)
            {
                crowd.enabled = false;
            }

            if (patrol != null)
            {
                patrol.ResetRoute();
            }

            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            bodyCollider.enabled = false;
        }

        [System.Serializable]
        public sealed class Snapshot
        {
            public int health;
            public BehaviourState state;
            public bool aggro;
            public float age;
            public float exposure;
            public float closure;
            public Vector2 escape;
            public Vector2 approach;
            public float staggerLeft;
            public Vector2 push;
            public EnemyMeleeAttack2D.Snapshot melee;
            public EnemyPatrol2D.Snapshot patrol;
        }

        public Snapshot CaptureState()
        {
            return new Snapshot
            {
                health = Health,
                state = State,
                aggro = HasAggro,
                age = stateTime,
                exposure = exposureTime,
                closure = Closure,
                escape = escapeDirection,
                approach = approachBeforeLight,
                staggerLeft = Mathf.Max(0, staggerUntil - Time.time),
                push = hitPush,
                melee = melee.CaptureState(),
                patrol = patrol != null ? patrol.CaptureState() : null
            };
        }

        public void RestoreState(Snapshot saved)
        {
            health.Restore(saved.health);
            State = saved.state;
            HasAggro = saved.aggro;
            stateTime = saved.age;
            exposureTime = saved.exposure;
            Closure = saved.closure;
            escapeDirection = saved.escape;
            approachBeforeLight = saved.approach;
            staggerUntil = Time.time + saved.staggerLeft;
            hitPush = saved.push;
            melee.RestoreState(saved.melee);
            if (patrol != null && saved.patrol != null)
            {
                patrol.RestoreState(saved.patrol);
            }

            bool alive = saved.health > 0;
            body.linearVelocity = Vector2.zero;
            body.simulated = alive;
            bodyCollider.enabled = alive;
            if (crowd != null)
            {
                crowd.enabled = alive;
            }

            enabled = alive;
            if (audioFeedback != null)
            {
                audioFeedback.enabled = alive;
            }

            var sprites = GetComponent<StalkerSpriteAnimator>();
            if (!alive && sprites != null)
            {
                sprites.RestoreDead();
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }

            if (melee != null)
            {
                melee.Lunged -= OnLunged;
                melee.Resolved -= OnResolved;
            }
        }

        private void OnDisable()
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(.3f, .75f, .85f, .7f);
            Gizmos.DrawWireSphere(transform.position, habitatRadius);
        }
    }
}
