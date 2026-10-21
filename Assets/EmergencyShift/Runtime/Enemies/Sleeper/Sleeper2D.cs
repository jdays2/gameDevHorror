using System;
using EmergencyShift.Features.Enemies;
using EmergencyShift.Features.Enemies.Logic;
using EmergencyShift.Features.Enemies.View;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Authored references, native lifetime, presentation events and legacy save adapter.</summary>
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
        private SleeperControllerFactory factory;
        private SleeperController controller;

        public event Action<BehaviourState> StateChanged;
        public event Action Damaged;
        public event Action AttackLunged;
        public event Action<bool> AttackResolved;
        public BehaviourState State => controller != null ? (BehaviourState)controller.Model.State : BehaviourState.Sleeping;
        public EnemyDefinition Definition => definition;
        public SleeperBehaviourProfile BehaviourProfile => behaviourProfile;
        public int Health => health != null ? health.Current : 0;
        public override bool IsAlive => State != BehaviourState.Dead;
        public override bool OwnsHitAudio => feedback != null && feedback.isActiveAndEnabled;
        public bool HasAggro => controller != null && controller.Model.HasAggro;
        public bool IsStaggered => controller != null && controller.Model.IsStaggered;
        public float ExposureProgress => controller?.Model.ExposureProgress ?? 0;
        public float Closure => controller?.Model.Closure ?? 1;
        public float AttackProgress => controller?.Model.AttackProgress ?? 0;
        public float DeathTime => controller?.Model.DeathTime ?? 0;
        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;
        public Vector2 AttackHeading => controller?.Model.AttackHeading ?? Vector2.zero;
        public Transform Player => player;
        public Flashlight2D LightSource => flashlight;
        public LayerMask BlockingGeometry => blockingGeometry;
        public bool AttacksEnabled
        {
            get => attacksEnabled;
            set => attacksEnabled = value;
        }

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

        [Inject]
        private void Construct(PlayerLife2D scenePlayer, Flashlight2D sceneLight, SleeperControllerFactory controllers)
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
            shape = GetComponent<CircleCollider2D>();
            health = GetComponent<EnemyHealth2D>();
            perception = GetComponent<EnemyPerception2D>();
            motor = GetComponent<EnemyMotor2D>();
            melee = GetComponent<EnemyMeleeAttack2D>();
            hitResponse = GetComponent<EnemyHitResponse2D>();
            crowd = GetComponent<CrowdAgent2D>();
            feedback = GetComponent<SleeperAudio2D>();
            if (definition == null || behaviourProfile == null || !behaviourProfile.IsValid || definition.MaximumHealth <= 0 ||
                definition.AttackDuration <= 0 || definition.LungeStart > definition.StrikeAt || definition.StrikeAt > definition.LungeEnd)
            {
                Debug.LogError("Missing or invalid Sleeper profiles: " + name, this);
                enabled = false;
                return;
            }

            if (factory == null)
            {
                throw new InvalidOperationException("Sleeper requires level composition before Awake.");
            }

            controller = factory.Create(behaviourProfile.ToSettings(definition), new EnemyBrainMotor2D(motor), new EnemyBrainMelee2D(melee));
            controller.Model.StateChanged += ForwardState;
            perception.Configure(player, blockingGeometry, flashlight);
            motor.Configure(blockingGeometry);
            melee.Configure(definition, perception, motor);
            health.Initialize(definition.MaximumHealth);
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            melee.Lunged += OnLunged;
            melee.Resolved += OnResolved;
        }

        private void FixedUpdate()
        {
            bool targetAlive = IsAlive && perception.TargetAlive;
            bool exposed = targetAlive && State == BehaviourState.Sleeping && perception.Exposed(body.position);
            Vector2 target = player != null ? (Vector2)player.position : body.position;
            controller.Step(Time.fixedDeltaTime, targetAlive, exposed, attacksEnabled, body.position, target);
            if (controller.Model.CanStopTicking)
            {
                enabled = false;
            }
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

            if (IsAlive)
            {
                controller.Stagger(direction);
            }
        }

        private void OnDamaged()
        {
            controller.Wake();
            Damaged?.Invoke();
        }

        private void ForwardState(SleeperState state) => StateChanged?.Invoke((BehaviourState)state);
        private void OnLunged() => AttackLunged?.Invoke();
        private void OnResolved(bool landed) => AttackResolved?.Invoke(landed);
        private void OnDied()
        {
            controller.Kill();
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

        [Serializable]
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
            SleeperStateSnapshot state = controller.Model.Capture();
            return new Snapshot
            {
                health = Health,
                state = (BehaviourState)state.State,
                aggro = state.Aggro,
                age = state.Age,
                exposure = state.Exposure,
                staggerLeft = state.StaggerLeft,
                push = state.Push,
                heading = state.Heading,
                melee = melee.CaptureState()
            };
        }

        public void RestoreState(Snapshot saved)
        {
            if (saved == null || saved.melee == null)
            {
                throw new ArgumentException("Sleeper snapshot requires actor and melee state.", nameof(saved));
            }

            var state = new SleeperStateSnapshot((SleeperState)saved.state, saved.aggro, saved.age, saved.exposure,
                saved.staggerLeft, saved.push, saved.heading);
            var validation = SleeperModel.ValidateSnapshot(state);
            var meleeValidation = EnemyMeleeModel.ValidateSnapshot(new EnemyMeleeState(saved.melee.struck, saved.melee.lunged, saved.melee.cooldown));
            if (!validation.Success || !meleeValidation.Success)
            {
                throw new InvalidOperationException("Invalid sleeper snapshot values.");
            }

            health.Restore(saved.health);
            var restored = controller.Restore(state);
            if (!restored.Success)
            {
                throw new InvalidOperationException(restored.Reason);
            }

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
            if (controller != null)
            {
                controller.Model.StateChanged -= ForwardState;
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
    }
}
