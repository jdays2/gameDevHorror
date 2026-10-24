using System;
using EmergencyShift.Features.Enemies.Logic;
using EmergencyShift.Features.Enemies.View;
using EmergencyShift.Features.ScriptedEncounters.Config;
using EmergencyShift.Features.ScriptedEncounters.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Features.ScriptedEncounters.View
{
    /// <summary>Scene authoring and species bridge. Ticked only by the actor's single decision loop.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(ShadowStalker2D))]
    public sealed class ScriptedStalkerRoute2D : MonoBehaviour, IDisposable
    {
        [SerializeField]
        private string encounterId;
        [SerializeField]
        private ScriptedRouteProfile profile;
        [SerializeField]
        private Transform[] routePoints;
        private ScriptedRouteControllerFactory factory;
        private ScriptedRouteController controller;
        private StalkerModel actor;

        public string EncounterId => encounterId;
        public ScriptedRouteState State => controller != null ? controller.Model.State : ScriptedRouteState.WaitingForLight;
        public int PointIndex => controller?.Model.PointIndex ?? 0;
        public bool ControlsActor => controller != null && controller.Model.ControlsActor;
        public float Closure => controller?.Model.Closure ?? 0;
        public bool Exposed { get; private set; }
        public Transform[] RoutePoints => routePoints != null ? (Transform[])routePoints.Clone() : Array.Empty<Transform>();

        public ShadowStalker2D.BehaviourState PresentationState
        {
            get
            {
                if (State == ScriptedRouteState.Reacting)
                {
                    return ShadowStalker2D.BehaviourState.Recoiling;
                }

                return controller != null && controller.Model.IsMoving
                    ? ShadowStalker2D.BehaviourState.Retreating : ShadowStalker2D.BehaviourState.Dormant;
            }
        }

        public event Action<ShadowStalker2D.BehaviourState> PresentationChanged;

        [Inject]
        private void Construct(ScriptedRouteControllerFactory controllers) => factory = controllers;

        public void Initialize(StalkerModel stalker, EnemyMotor2D motor, int blockingGeometry)
        {
            if (controller != null || factory == null || profile == null || string.IsNullOrWhiteSpace(encounterId) ||
                routePoints == null || routePoints.Length == 0)
            {
                throw new InvalidOperationException("Scripted Stalker requires level composition, ID, profile and route points: " + name);
            }

            var points = new Vector2[routePoints.Length];
            var shape = GetComponent<CircleCollider2D>();
            float radius = shape.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            for (int index = 0; index < points.Length; index++)
            {
                Transform point = routePoints[index];
                if (point == null || point.gameObject.scene != gameObject.scene || Physics2D.OverlapCircle(point.position, radius, blockingGeometry) != null)
                {
                    throw new InvalidOperationException("Scripted route has a missing, foreign or obstructed point: " + encounterId + "/" + index);
                }

                points[index] = point.position;
            }

            controller = factory.Create(profile.ToSettings(), points, new EnemyBrainMotor2D(motor));
            actor = stalker;
            var claim = new SetStalkerScriptedControlCommand(actor, true).Execute();
            if (!claim.Success)
            {
                throw new InvalidOperationException(claim.Reason);
            }

            controller.Model.StateChanged += OnStateChanged;
        }

        public bool TryStep(StalkerObservation frame)
        {
            if (!ControlsActor)
            {
                return false;
            }

            if (!actor.IsAlive)
            {
                new AbortScriptedRouteCommand(controller.Model).Execute();
                return false;
            }

            Exposed = frame.Exposed;
            controller.Step(frame.DeltaTime, frame.Position, frame.Exposed, frame.TargetReady && frame.TargetAlive,
                !actor.IsStaggered, actor.StaggerVelocity);
            if (!controller.Model.ControlsActor)
            {
                new SetStalkerScriptedControlCommand(actor, false).Execute();
            }

            return true;
        }

        public void Abort()
        {
            if (ControlsActor)
            {
                new AbortScriptedRouteCommand(controller.Model).Execute();
            }
        }

        public void Dispose()
        {
            if (controller == null)
            {
                return;
            }

            controller.Model.StateChanged -= OnStateChanged;
            if (actor.IsAlive && actor.IsScripted)
            {
                new SetStalkerScriptedControlCommand(actor, false).Execute();
            }

            controller = null;
            actor = null;
        }

        private void OnDestroy() => Dispose();

        private void OnStateChanged(ScriptedRouteState state)
        {
            if (state == ScriptedRouteState.Blocked)
            {
                Debug.LogWarning("Scripted route blocked; retrying without aggression: " + encounterId + "/" + PointIndex, this);
            }

            if (actor.IsAlive)
            {
                PresentationChanged?.Invoke(PresentationState);
            }
        }

        private void OnDrawGizmos()
        {
            if (routePoints == null)
            {
                return;
            }

            Gizmos.color = new Color(.9f, .3f, .15f, .9f);
            Vector3 previous = transform.position;
            foreach (Transform point in routePoints)
            {
                if (point == null)
                {
                    continue;
                }

                Gizmos.DrawLine(previous, point.position);
                Gizmos.DrawWireSphere(point.position, profile != null ? profile.ToSettings().ArrivalDistance : .16f);
                previous = point.position;
            }
        }
    }
}
