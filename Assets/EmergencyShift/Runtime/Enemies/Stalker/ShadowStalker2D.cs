using System;
using EmergencyShift.Features.Enemies;
using EmergencyShift.Features.Enemies.Logic;
using EmergencyShift.Features.Enemies.View;
using UnityEngine;
using Zenject;

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
        private Rigidbody2D body;
        private CircleCollider2D bodyCollider;
        private EnemyHitResponse2D hitResponse;
        private EnemyPatrol2D patrol;
        private CrowdAgent2D crowd;
        private StalkerAudio2D audioFeedback;
        private StalkerControllerFactory factory;
        private StalkerController controller;
        private static readonly Unity.Profiling.ProfilerMarker tickMarker = new Unity.Profiling.ProfilerMarker("EmergencyShift.Stalker.Tick");

        public event Action<BehaviourState> StateChanged;
        public event Action Damaged;
        public event Action EyeBurnStarted;
        public event Action AttackLunged;
        public event Action<bool> AttackResolved;
        public EnemyDefinition Definition => definition;
        public StalkerBehaviourProfile BehaviourProfile => behaviourProfile;
        public BehaviourState State => controller != null ? (BehaviourState)controller.Model.State : BehaviourState.Sneaking;
        public override bool IsAlive => State != BehaviourState.Dead;
        public override bool OwnsHitAudio => audioFeedback != null && audioFeedback.isActiveAndEnabled;
        public bool HasAggro => controller != null && controller.Model.HasAggro;
        public float EyeDamageProgress => controller?.Model.EyeDamageProgress ?? 0;
        public bool EyeBurned => EyeDamageProgress > 0;
        public float PursuitSpeed => controller?.Model.PursuitSpeed ?? (definition != null ? definition.PursuitSpeed : 0);
        public float RetreatSpeed => controller?.Model.RetreatSpeed ?? (behaviourProfile != null ? behaviourProfile.RetreatSpeed : 0);
        public int ChaseWaypointCount => motor != null ? motor.WaypointCount : 0;
        public Vector2 Home => controller?.Model.Home ?? Vector2.zero;
        public float HabitatRadius => habitatRadius;
        public bool IsStaggered => controller != null && controller.Model.IsStaggered;
        public float Closure => controller?.Model.Closure ?? 0;
        public bool Exposed => controller != null && controller.Model.Exposed;
        public float MovementSpeed => body != null ? body.linearVelocity.magnitude : 0;
        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;
        public Transform Player => player;
        public Flashlight2D LightSource => flashlight;
        public LayerMask BlockingGeometry => blockingGeometry;
        public float LightFearDistance => controller?.Model.FearDistance ?? (behaviourProfile != null ? behaviourProfile.LightFearDistance : 0);
        public float ReactionDelay => controller?.Model.ReactionDelay ?? (behaviourProfile != null ? behaviourProfile.ReactionDelay : 0);
        public float LightReactionProgress => controller?.Model.ReactionProgress ?? 0;
        public float AttackProgress => controller?.Model.AttackProgress ?? 0;
        public float DeathTime => controller?.Model.DeathTime ?? 0;
        public int Health => health != null ? health.Current : 0;
        public bool AttacksEnabled
        {
            get => attacksEnabled;
            set => attacksEnabled = value;
        }

        public override void ReceiveBulletHit(int damage, Vector2 point, Vector2 direction) => ApplyBulletHit(damage, point, direction);

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

        [Inject]
        private void Construct(PlayerLife2D scenePlayer, Flashlight2D sceneLight, StalkerControllerFactory controllers)
        {
            factory = controllers ?? throw new ArgumentNullException(nameof(controllers));
            if (player == null && scenePlayer != null)
            {
                player = scenePlayer.transform;
            }

            if (flashlight == null)
            {
                flashlight = sceneLight;
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CircleCollider2D>();
            crowd = GetComponent<CrowdAgent2D>();
            audioFeedback = GetComponent<StalkerAudio2D>();
            if (definition == null || behaviourProfile == null)
            {
                Debug.LogError("Missing enemy profiles: " + name, this);
                enabled = false;
                return;
            }

            if (factory == null)
            {
                throw new InvalidOperationException("Stalker requires level composition before Awake.");
            }

            health = GetComponent<EnemyHealth2D>();
            perception = GetComponent<EnemyPerception2D>();
            motor = GetComponent<EnemyMotor2D>();
            melee = GetComponent<EnemyMeleeAttack2D>();
            patrol = GetComponent<EnemyPatrol2D>();
            controller = factory.Create(behaviourProfile.ToSettings(definition), body.position,
                new StalkerLocomotion2D(motor, patrol), new EnemyBrainMelee2D(melee));
            controller.Model.StateChanged += ForwardState;
            controller.Model.EyeBurnStarted += ForwardBurn;
            perception.Configure(player, blockingGeometry, flashlight);
            motor.Configure(blockingGeometry);
            melee.Configure(definition, perception, motor);
            health.Initialize(definition.MaximumHealth);
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            melee.Lunged += OnLunged;
            melee.Resolved += OnResolved;
            hitResponse = GetComponent<EnemyHitResponse2D>();
            if (patrol != null)
            {
                patrol.Initialize(Home);
            }
        }

        private void FixedUpdate()
        {
            using (tickMarker.Auto())
            {
                bool ready = player != null && flashlight != null;
                bool targetAlive = IsAlive && ready && perception.TargetAlive;
                bool visible = targetAlive && perception.CanSeeTarget(body.position);
                bool exposed = targetAlive && perception.Exposed(body.position);
                var frame = new StalkerObservation(Time.fixedDeltaTime, body.position, body.linearVelocity,
                    player != null ? (Vector2)player.position : body.position,
                    flashlight != null ? flashlight.Origin : body.position,
                    flashlight != null ? flashlight.Direction : Vector2.zero,
                    ready, targetAlive, visible, exposed, patrol != null && patrol.PatrolEnabled,
                    attacksEnabled, habitatRadius, motor.Radius, blockingGeometry.value | (1 << 9));
                controller.Step(frame);
                if (controller.Model.CanStopTicking)
                {
                    enabled = false;
                }
            }
        }

        public void ApplyBulletHit(int amount, Vector2 point, Vector2 direction)
        {
            if (!IsAlive || amount <= 0)
            {
                return;
            }

            TakeDamage(amount);
            if (hitResponse != null)
            {
                hitResponse.Play(point, direction, !IsAlive);
            }

            if (IsAlive)
            {
                controller.Stagger(direction, perception.TargetAlive);
            }
        }

        public void TakeDamage(int amount) => health?.TakeDamage(amount);
        private void OnDamaged() => Damaged?.Invoke();
        private void OnLunged() => AttackLunged?.Invoke();
        private void OnResolved(bool landed) => AttackResolved?.Invoke(landed);
        private void ForwardState(StalkerState state) => StateChanged?.Invoke((BehaviourState)state);
        private void ForwardBurn() => EyeBurnStarted?.Invoke();

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
            controller.Kill();
            if (crowd != null)
            {
                crowd.enabled = false;
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
            public bool eyeBurned;
            public bool hasEyeDamageProgress;
            public float eyeDamage;
            public float burnContact;
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
            StalkerStateSnapshot state = controller.Model.Capture();
            return new Snapshot
            {
                health = Health,
                state = (BehaviourState)state.State,
                aggro = state.Aggro,
                eyeBurned = EyeBurned,
                hasEyeDamageProgress = true,
                eyeDamage = state.EyeDamage,
                burnContact = state.BurnContact,
                age = state.Age,
                exposure = state.Exposure,
                closure = state.Closure,
                escape = state.Escape,
                approach = state.Approach,
                staggerLeft = state.StaggerLeft,
                push = state.Push,
                melee = melee.CaptureState(),
                patrol = patrol != null ? patrol.CaptureState() : null
            };
        }

        public void RestoreState(Snapshot saved)
        {
            if (saved == null || saved.melee == null)
            {
                throw new ArgumentException("Stalker snapshot requires actor and melee state.", nameof(saved));
            }

            float eyeDamage = saved.eyeBurned ? 1 : 0;
            if (saved.hasEyeDamageProgress)
            {
                eyeDamage = Mathf.Clamp01(saved.eyeDamage);
            }
            var state = new StalkerStateSnapshot((StalkerState)saved.state, saved.aggro, eyeDamage,
                saved.hasEyeDamageProgress ? Mathf.Max(0, saved.burnContact) : 0, saved.age, saved.exposure,
                saved.closure, saved.escape, saved.approach, saved.staggerLeft, saved.push);
            var validation = StalkerModel.ValidateSnapshot(state);
            var meleeValidation = EnemyMeleeModel.ValidateSnapshot(new EnemyMeleeState(saved.melee.struck, saved.melee.lunged, saved.melee.cooldown));
            if (!validation.Success || !meleeValidation.Success)
            {
                throw new InvalidOperationException("Invalid Stalker snapshot values.");
            }

            if (patrol != null && saved.patrol != null)
            {
                var route = saved.patrol;
                if (!EnemyPatrolModel.ValidateSnapshot(new EnemyPatrolState(route.origin, route.hasTarget, route.target,
                    route.pause, route.stalled, route.previousDistance)).Success)
                {
                    throw new InvalidOperationException("Invalid Stalker patrol snapshot values.");
                }
            }

            health.Restore(saved.health);
            var restored = controller.Restore(state);
            if (!restored.Success)
            {
                throw new InvalidOperationException(restored.Reason);
            }

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
            if (controller != null)
            {
                controller.Model.StateChanged -= ForwardState;
                controller.Model.EyeBurnStarted -= ForwardBurn;
                controller.Dispose();
            }

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
