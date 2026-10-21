using System;
using EmergencyShift.Features.Enemies;
using EmergencyShift.Features.Enemies.Logic;
using EmergencyShift.Features.Enemies.View;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Scene bounds, DTO and gizmo adapter for the pure personal patrol controller.</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyPatrol2D : MonoBehaviour
    {
        [Tooltip("Выключено: ждать на текущем месте. Включено: иногда передвигаться до первого обнаружения игрока.")]
        [SerializeField]
        private bool patrolEnabled;
        [SerializeField]
        private bool usePatrolArea = true;
        [Header("World-aligned area relative to spawn")]
        [SerializeField]
        private Vector2 areaOffset;
        [SerializeField]
        private Vector2 areaSize = new Vector2(3, 3);
        [SerializeField]
        private EnemyPatrolProfile profile;
        private EnemyPatrolControllerFactory factory;
        private EnemyPatrolController controller;
        private EnemyPatrolProfile configuredProfile;

        public EnemyPatrolProfile Profile => profile;
        public bool PatrolEnabled => isActiveAndEnabled && patrolEnabled;
        public bool UsePatrolArea => usePatrolArea;
        public Vector2 AreaOrigin => Application.isPlaying && controller != null && controller.IsInitialized
            ? controller.Origin : (Vector2)transform.position;
        public Rect Area => Bounds.Area(AreaOrigin);
        public bool HasTarget => controller != null && controller.HasTarget;
        public Vector2 Target => controller?.Target ?? Vector2.zero;
        public bool IsPausing => controller != null && controller.IsPausing;
        private EnemyPatrolBounds Bounds => new EnemyPatrolBounds(usePatrolArea, areaOffset, areaSize);

        [Serializable]
        public sealed class Snapshot
        {
            public Vector2 origin;
            public bool hasTarget;
            public Vector2 target;
            public float pause;
            public float stalled;
            public float previousDistance;
        }

        [Inject]
        public void Construct(EnemyPatrolControllerFactory controllers)
        {
            factory = controllers ?? throw new ArgumentNullException(nameof(controllers));
        }

        private void OnValidate()
        {
            areaSize = Vector2.Max(areaSize, Vector2.one * .2f);
            if (controller != null && configuredProfile != profile)
            {
                RequireController();
            }

            ResetRoute();
        }

        private void OnDrawGizmosSelected()
        {
            Rect area = Area;
            Gizmos.color = new Color(.95f, .72f, .2f, .8f);
            if (usePatrolArea)
            {
                Gizmos.DrawWireCube(new Vector3(area.center.x, area.center.y, transform.position.z),
                    new Vector3(area.width, area.height, 0));
            }

            if (Application.isPlaying && HasTarget)
            {
                Gizmos.DrawLine(transform.position, Target);
                Gizmos.DrawWireSphere(Target, .09f);
            }
        }

        public bool ContainsBody(Vector2 position, float radius) =>
            Bounds.ContainsBody(AreaOrigin, position, radius, profile != null ? profile.BodySkin : .04f);

        public Snapshot CaptureState()
        {
            EnemyPatrolState state = controller != null ? controller.Capture() : default;
            return new Snapshot
            {
                origin = state.Origin,
                hasTarget = state.HasTarget,
                target = state.Target,
                pause = state.Pause,
                stalled = state.Stalled,
                previousDistance = state.PreviousDistance
            };
        }

        public void RestoreState(Snapshot saved)
        {
            if (saved == null)
            {
                throw new ArgumentNullException(nameof(saved));
            }

            var result = RequireController().Restore(new EnemyPatrolState(saved.origin, saved.hasTarget, saved.target,
                saved.pause, saved.stalled, saved.previousDistance));
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Reason);
            }
        }

        public void Initialize(Vector2 spawn)
        {
            var result = RequireController().Initialize(spawn);
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Reason);
            }
        }

        public void ResetRoute() => controller?.Reset();

        public Vector2 Step(Vector2 position, float radius, LayerMask solids, float deltaTime)
        {
            if (!PatrolEnabled)
            {
                return Vector2.zero;
            }

            EnemyPatrolController route = RequireController();
            if (!route.IsInitialized)
            {
                route.Initialize(position);
            }

            return route.Step(new EnemyPatrolStep(true, position, radius, Bounds, solids.value, deltaTime));
        }

        private EnemyPatrolController RequireController()
        {
            if (factory == null || (patrolEnabled && profile == null))
            {
                throw new InvalidOperationException("Patrol requires level composition and an authored profile when enabled.");
            }

            if (controller == null || configuredProfile != profile)
            {
                EnemyPatrolController replacement = factory.Create(profile != null ? profile.ToSettings() : null,
                    new EnemyPatrolEnvironment2D());
                if (controller != null && controller.IsInitialized)
                {
                    var result = replacement.Restore(controller.Capture());
                    if (!result.Success)
                    {
                        throw new InvalidOperationException(result.Reason);
                    }
                }

                controller = replacement;
                configuredProfile = profile;
            }

            return controller;
        }
    }
}