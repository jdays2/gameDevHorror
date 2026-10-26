using System;
using EmergencyShift.Features.ClawEnemy.Logic;
using EmergencyShift.Features.Enemies.View;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Features.ClawEnemy.View
{
    /// <summary>Prototype actor adapter. Shared health, routes and melee remain species independent.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(EnemyHealth2D))]
    [RequireComponent(typeof(EnemyMotor2D), typeof(EnemyPerception2D), typeof(EnemyMeleeAttack2D))]
    public sealed class ClawEnemy2D : Damageable2D
    {
        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private LayerMask blockingGeometry;
        [SerializeField, Min(.1f)] private float detectionRadius = 3.2f;
        [SerializeField] private bool stationary;
        private ClawEnemyControllerFactory factory;
        private ClawEnemyController controller;
        private Transform target;
        private Rigidbody2D body;
        private CircleCollider2D shape;
        private EnemyHealth2D health;
        private EnemyMotor2D motor;
        private EnemyPerception2D perception;
        private EnemyMeleeAttack2D melee;
        private EnemyPatrol2D patrol;
        private CrowdAgent2D crowd;
        private EnemyHitResponse2D feedback;
        private Vector2 hitDirection;

        public override bool IsAlive => health != null && health.IsAlive;
        public int Health => health != null ? health.Current : 0;
        public bool HasAggro => controller != null && controller.Model.HasAggro;
        public bool IsAttacking => controller != null && controller.Model.State == ClawEnemyState.Attacking;
        public bool IsStaggered => controller != null && controller.Model.State == ClawEnemyState.Staggered;
        public float AttackProgress => controller != null ? controller.Model.AttackProgress : 0;
        public Vector2 AttackHeading => controller != null ? controller.Model.Heading : Vector2.left;
        public Vector2 Velocity => body != null && !stationary ? body.linearVelocity : Vector2.zero;
        public bool Stationary => stationary;

        [Inject]
        private void Construct(PlayerLife2D player, ClawEnemyControllerFactory controllers)
        {
            target = player != null ? player.transform : null;
            factory = controllers;
        }

        private void Awake()
        {
            if (factory == null || definition == null)
            {
                throw new InvalidOperationException("Claw prototype requires level composition and its definition.");
            }
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<CircleCollider2D>();
            health = GetComponent<EnemyHealth2D>();
            motor = GetComponent<EnemyMotor2D>();
            perception = GetComponent<EnemyPerception2D>();
            melee = GetComponent<EnemyMeleeAttack2D>();
            patrol = GetComponent<EnemyPatrol2D>();
            crowd = GetComponent<CrowdAgent2D>();
            feedback = GetComponent<EnemyHitResponse2D>();
            perception.Configure(target, blockingGeometry, null);
            motor.Configure(blockingGeometry);
            melee.Configure(definition, perception, motor);
            controller = factory.Create(definition, new ClawLocomotion2D(motor, patrol), new EnemyBrainMelee2D(melee));
            health.Initialize(definition.MaximumHealth);
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            if (patrol != null)
            {
                patrol.Initialize(body.position);
            }
            if (stationary)
            {
                body.constraints = RigidbodyConstraints2D.FreezeAll;
                if (crowd != null)
                {
                    crowd.enabled = false;
                }
            }
        }

        private void FixedUpdate()
        {
            bool targetAlive = perception.TargetAlive;
            bool detected = targetAlive && perception.Within(body.position, detectionRadius) && perception.CanSeeTarget(body.position);
            var frame = new ClawEnemyObservation(Time.fixedDeltaTime, body.position,
                target != null ? (Vector2)target.position : body.position, targetAlive, detected,
                patrol != null && patrol.PatrolEnabled, !stationary, stationary, motor.Radius,
                blockingGeometry.value | (1 << 9));
            controller.Step(frame);
        }

        public override void ReceiveBulletHit(int damage, Vector2 point, Vector2 direction)
        {
            if (!IsAlive)
            {
                return;
            }
            hitDirection = direction;
            if (health.TakeDamage(damage) && feedback != null)
            {
                feedback.Play(point, direction, !IsAlive);
            }
        }

        private void OnDamaged()
        {
            if (health.IsAlive)
            {
                controller.Hit(hitDirection);
            }
        }

        private void OnDied()
        {
            controller.Kill();
            shape.enabled = false;
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            if (crowd != null)
            {
                crowd.enabled = false;
            }
            enabled = false;
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }
    }
}
