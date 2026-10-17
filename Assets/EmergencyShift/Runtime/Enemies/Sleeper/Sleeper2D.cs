using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Light-sensitive sleeper. Shared components execute combat; this brain owns decisions and instance state.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth2D), typeof(EnemyPerception2D), typeof(EnemyMotor2D))]
    [RequireComponent(typeof(EnemyMeleeAttack2D))]
    public sealed class Sleeper2D : Damageable2D
    {
        public enum BehaviourState { Sleeping, Waking, Hunting, Attacking, Recovering, TargetDead, Dead }

        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private SleeperBehaviourProfile behaviourProfile;
        [SerializeField] private Transform player;
        [SerializeField] private Flashlight2D flashlight;
        [SerializeField] private LayerMask blockingGeometry;
        [SerializeField] private bool attacksEnabled = true;
        private EnemyHealth2D health;
        private EnemyPerception2D perception;
        private EnemyMotor2D motor;
        private EnemyMeleeAttack2D melee;
        private EnemyHitResponse2D hitResponse;
        private CrowdAgent2D crowd;
        private Rigidbody2D body;
        private CircleCollider2D shape;
        private SleeperAudio2D feedback;
        private float stateTime;
        private float exposure;
        private float staggerUntil;
        private Vector2 hitPush;
        private Vector2 attackHeading;

        public event System.Action<BehaviourState> StateChanged;
        public event System.Action Damaged;
        public event System.Action AttackLunged;
        public event System.Action<bool> AttackResolved;
        public BehaviourState State { get; private set; } = BehaviourState.Sleeping;
        public EnemyDefinition Definition => definition;
        public SleeperBehaviourProfile BehaviourProfile => behaviourProfile;
        public int Health => health != null ? health.Current : 0;
        public override bool IsAlive => State != BehaviourState.Dead;
        public override bool OwnsHitAudio => feedback != null && feedback.isActiveAndEnabled;
        public bool HasAggro { get; private set; }
        public bool IsStaggered => IsAlive && Time.time < staggerUntil;
        public float ExposureProgress => Mathf.Clamp01(exposure / behaviourProfile.lightExposureSeconds);
        public float Closure => State == BehaviourState.Sleeping ? 1 : WakingClosure;
        private float WakingClosure => State == BehaviourState.Waking ? 1 - Mathf.Clamp01(stateTime / behaviourProfile.wakingSeconds) : 0;
        public float AttackProgress => State == BehaviourState.Attacking ? Mathf.Clamp01(stateTime / definition.attackDuration) : 0;
        public float DeathTime => State == BehaviourState.Dead ? stateTime : 0;
        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;
        public Vector2 AttackHeading => attackHeading;
        public Transform Player => player;
        public Flashlight2D LightSource => flashlight;
        public LayerMask BlockingGeometry => blockingGeometry;
        public bool AttacksEnabled { get => attacksEnabled; set => attacksEnabled = value; }

        public void Configure(Transform target, Flashlight2D lamp, LayerMask obstacles)
        {
            player = target;
            flashlight = lamp;
            blockingGeometry = obstacles;
            if (perception != null)
            {
                perception.Configure(target, obstacles, lamp);
                motor.Configure(obstacles);
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<CircleCollider2D>();
            health = GetComponent<EnemyHealth2D>();
            perception = GetComponent<EnemyPerception2D>();
            motor = GetComponent<EnemyMotor2D>();
            melee = GetComponent<EnemyMeleeAttack2D>();
            hitResponse = GetComponent<EnemyHitResponse2D>();
            crowd = GetComponent<CrowdAgent2D>();
            feedback = GetComponent<SleeperAudio2D>();
            if (definition == null || behaviourProfile == null || !behaviourProfile.IsValid || definition.maximumHealth <= 0 ||
                definition.attackDuration <= 0 || definition.lungeStart > definition.strikeAt || definition.strikeAt > definition.lungeEnd)
            {
                Debug.LogError("Missing or invalid Sleeper profiles: " + name, this);
                enabled = false;
                return;
            }

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
            perception.Configure(player, blockingGeometry, flashlight);
            motor.Configure(blockingGeometry);
            melee.Configure(definition, perception, motor);
            health.Initialize(definition.maximumHealth);
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            melee.Lunged += OnLunged;
            melee.Resolved += OnResolved;
        }

        private void FixedUpdate()
        {
            stateTime += Time.fixedDeltaTime;
            if (State == BehaviourState.Dead)
            {
                if (stateTime >= 3)
                {
                    enabled = false;
                }
                return;
            }
            if (!perception.TargetAlive)
            {
                if (HasAggro)
                {
                    Enter(BehaviourState.TargetDead);
                }
                motor.Move(Vector2.zero, false);
                return;
            }
            if (State == BehaviourState.Sleeping)
            {
                exposure = perception.Exposed(body.position) ? exposure + Time.fixedDeltaTime : Mathf.Max(0, exposure - behaviourProfile.exposureDecay * Time.fixedDeltaTime);
                motor.Move(Vector2.zero, false);
                if (exposure >= behaviourProfile.lightExposureSeconds)
                {
                    Wake();
                }
                return;
            }
            if (IsStaggered)
            {
                motor.Move(hitPush * Mathf.Clamp01((staggerUntil - Time.time) / Mathf.Max(.01f, definition.hitStaggerSeconds)), false);
                return;
            }

            Vector2 velocity = Vector2.zero;
            switch (State)
            {
                case BehaviourState.Waking:
                    if (stateTime >= behaviourProfile.wakingSeconds)
                    {
                        Enter(BehaviourState.Hunting);
                    }
                    break;
                case BehaviourState.Hunting:
                    if (attacksEnabled && melee.CanStart(body.position))
                    {
                        attackHeading = ((Vector2)player.position - body.position).normalized;
                        melee.Begin();
                        Enter(BehaviourState.Attacking);
                    }
                    else
                    {
                        velocity = motor.Follow(player.position, definition.pursuitSpeed);
                    }
                    break;
                case BehaviourState.Attacking:
                    velocity = melee.Step(AttackProgress, body.position, attackHeading, true);
                    if (AttackProgress >= 1)
                    {
                        melee.Finish();
                        Enter(BehaviourState.Recovering);
                        velocity = Vector2.zero;
                    }
                    break;
                case BehaviourState.Recovering:
                    if (stateTime >= behaviourProfile.recoverySeconds)
                    {
                        Enter(BehaviourState.Hunting);
                    }
                    break;
            }
            motor.Move(velocity, State == BehaviourState.Hunting);
        }

        private void Wake()
        {
            if (HasAggro || !IsAlive)
            {
                return;
            }
            HasAggro = true;
            exposure = 0;
            Enter(BehaviourState.Waking);
        }

        private void Enter(BehaviourState next)
        {
            if (State == next)
            {
                return;
            }
            State = next;
            stateTime = 0;
            StateChanged?.Invoke(next);
        }

        public override void ReceiveBulletHit(int damage, Vector2 point, Vector2 direction)
        {
            if (!IsAlive || damage <= 0 || health == null)
            {
                return;
            }
            health.TakeDamage(damage);
            if (hitResponse != null)
            {
                hitResponse.Play(point, direction, !IsAlive);
            }
            if (!IsAlive)
            {
                return;
            }
            staggerUntil = Time.time + definition.hitStaggerSeconds;
            hitPush = direction.normalized * definition.hitPushSpeed;
            if (State == BehaviourState.Attacking)
            {
                melee.Interrupt(definition.attackCooldown);
                Enter(BehaviourState.Recovering);
            }
        }

        private void OnDamaged()
        {
            Wake();
            Damaged?.Invoke();
        }
        private void OnLunged() => AttackLunged?.Invoke();
        private void OnResolved(bool landed) => AttackResolved?.Invoke(landed);

        private void OnDied()
        {
            Enter(BehaviourState.Dead);
            SetLivingPhysics(false);
        }

        private void SetLivingPhysics(bool alive)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = alive;
            shape.enabled = alive;
            if (crowd != null)
            {
                crowd.enabled = alive;
            }
        }

        [System.Serializable]
        public sealed class Snapshot
        {
            public int health;
            public BehaviourState state;
            public bool aggro;
            public float age;
            public float exposure;
            public float staggerLeft;
            public Vector2 push;
            public Vector2 heading;
            public EnemyMeleeAttack2D.Snapshot melee;
        }

        public Snapshot CaptureState()
        {
            return new Snapshot
            {
                health = Health,
                state = State,
                aggro = HasAggro,
                age = stateTime,
                exposure = exposure,
                staggerLeft = Mathf.Max(0, staggerUntil - Time.time),
                push = hitPush,
                heading = attackHeading,
                melee = melee.CaptureState()
            };
        }

        public void RestoreState(Snapshot saved)
        {
            health.Restore(saved.health);
            State = saved.state;
            HasAggro = saved.aggro;
            stateTime = saved.age;
            exposure = saved.exposure;
            staggerUntil = Time.time + saved.staggerLeft;
            hitPush = saved.push;
            attackHeading = saved.heading;
            melee.RestoreState(saved.melee);
            SetLivingPhysics(saved.health > 0);
            enabled = saved.health > 0;
            if (feedback != null)
            {
                feedback.RestoreSilently();
            }
            if (saved.health == 0)
            {
                GetComponent<CreatureSpriteAnimator2D>().RestoreDead();
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
    }
}
