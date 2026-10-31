using System;
using EmergencyShift.Features.Enemies.View;
using EmergencyShift.Features.Scorpion.Config;
using EmergencyShift.Features.Scorpion.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Features.Scorpion.View
{
    [RequireComponent(typeof(EnemyHealth2D), typeof(EnemyMotor2D), typeof(EnemyPerception2D))]
    [RequireComponent(typeof(EnemyMeleeAttack2D), typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class Scorpion2D : Damageable2D
    {
        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private ScorpionPresentationSet presentation;
        [SerializeField] private LayerMask blockingGeometry;
        [SerializeField, Min(.1f)] private float detectionRadius = 5;
        private ScorpionControllerFactory factory;
        private ScorpionController controller;
        private Transform target;
        private Flashlight2D lamp;
        private EnemyHealth2D health;
        private EnemyMotor2D motor;
        private EnemyPerception2D perception;
        private EnemyMeleeAttack2D melee;
        private Rigidbody2D body;
        private CircleCollider2D shape;
        private CrowdAgent2D crowd;
        private EnemyHitResponse2D hitResponse;
        private ScorpionPack2D pack;
        private Vector2 previousPosition;
        public event Action Hit;
        public event Action Died;
        public override bool IsAlive => health != null && health.IsAlive;
        public int Health => health != null ? health.Current : 0;
        public bool HasAggro => controller != null && controller.Model.HasAggro;
        public bool IsAttacking => controller != null && controller.Model.State == ScorpionState.Attacking;
        public bool IsGathering => controller != null && controller.Model.State == ScorpionState.Gathering;
        public float AttackProgress => controller?.Model.AttackProgress ?? 0;
        public float GaitPhase => controller?.Model.GaitPhase ?? 0;
        public float DeathTime => controller?.Model.DeathTime ?? 0;
        public Vector2 AttackHeading => controller?.Model.Heading ?? Vector2.up;
        public Vector2 Facing => controller?.Model.Facing ?? (Vector2)transform.up;
        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;
        public Transform Target => target;
        public IThreatLight2D LightSource => lamp;
        public LayerMask BlockingGeometry => blockingGeometry;
        public ScorpionPresentationSet Presentation => presentation;
        public ScorpionPack2D Pack => pack;
        public bool HasTargetLineOfSight { get; private set; }

        [Inject]
        private void Construct(PlayerLife2D player, Flashlight2D flashlight, ScorpionControllerFactory controllers)
        {
            target = player != null ? player.transform : null;
            lamp = flashlight;
            factory = controllers;
        }

        private void Awake()
        {
            if (factory == null || definition == null || presentation == null)
            {
                throw new InvalidOperationException("Scorpion requires level composition and configured profiles.");
            }
            presentation.Validate();
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<CircleCollider2D>();
            health = GetComponent<EnemyHealth2D>();
            motor = GetComponent<EnemyMotor2D>();
            perception = GetComponent<EnemyPerception2D>();
            melee = GetComponent<EnemyMeleeAttack2D>();
            crowd = GetComponent<CrowdAgent2D>();
            hitResponse = GetComponent<EnemyHitResponse2D>();
            // Light belongs only to the shadow presenter; it is never a threat input.
            perception.Configure(target, blockingGeometry, null);
            motor.Configure(blockingGeometry);
            melee.Configure(definition, perception, motor);
            controller = factory.Create(definition, new ScorpionMotionAdapter(motor), new EnemyBrainMelee2D(melee), transform.up);
            health.Initialize(definition.MaximumHealth);
            health.Died += OnDied;
            previousPosition = body.position;
        }

        private void FixedUpdate()
        {
            float travelled = Vector2.Distance(body.position, previousPosition);
            previousPosition = body.position;
            // Separation, walls and contacts can reduce the requested speed to zero.
            // Start the next acceleration from the movement that physics actually allowed.
            controller.Model.CommitMotion(body.linearVelocity.magnitude);
            controller.Model.AdvanceTravel(travelled, presentation.DistancePerCycle);
            HasTargetLineOfSight = perception.CanSeeTarget(body.position);
            if (!HasAggro && perception.Within(body.position, detectionRadius) && HasTargetLineOfSight)
            {
                RequestAlarm();
            }
            controller.Step(body.position, target != null ? (Vector2)target.position : body.position,
                perception.TargetAlive, definition.PursuitSpeed, presentation.Attack.ReferenceSpeed / definition.PursuitSpeed,
                presentation.DistancePerCycle / presentation.Attack.ReferenceSpeed, presentation.TurnDegrees * Time.fixedDeltaTime,
                presentation.Acceleration * Time.fixedDeltaTime, presentation.Braking * Time.fixedDeltaTime);
        }

        public void BindPack(ScorpionPack2D owner)
        {
            if (pack != null && pack != owner)
            {
                throw new InvalidOperationException("A scorpion cannot belong to two room packs.");
            }
            pack = owner;
        }

        public void Alert() => controller?.Alert();

        public override void ReceiveBulletHit(int damage, Vector2 point, Vector2 direction)
        {
            if (!IsAlive)
            {
                return;
            }
            if (health.TakeDamage(damage))
            {
                hitResponse?.Play(point, direction, !IsAlive);
                Hit?.Invoke();
                if (IsAlive)
                {
                    RequestAlarm();
                }
            }
        }

        private void RequestAlarm()
        {
            if (pack != null)
            {
                pack.Alert(this);
            }
            else
            {
                bool wasAlerted = HasAggro;
                controller.Alert();
                if (!wasAlerted && HasAggro)
                {
                    GetComponent<ScorpionAudio2D>().PlayAlarm();
                }
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
            Died?.Invoke();
            enabled = false;
        }

        private void OnDisable()
        {
            if (controller != null && controller.Model.IsAlive)
            {
                controller.Suspend();
            }
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }
        }
    }
}
