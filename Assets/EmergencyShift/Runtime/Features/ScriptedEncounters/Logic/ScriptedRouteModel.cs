using System;
using EmergencyShift.Features.ScriptedEncounters.Config;
using UnityEngine;

namespace EmergencyShift.Features.ScriptedEncounters.Logic
{
    public sealed class ScriptedRouteModel
    {
        private readonly ScriptedRouteSettings settings;
        private readonly Vector2[] points;
        private float reactionLeft;
        private float stalled;
        private float bestDistance = float.PositiveInfinity;

        public ScriptedRouteState State { get; private set; } = ScriptedRouteState.WaitingForTrigger;
        public int PointIndex { get; private set; }
        public bool ControlsActor => State != ScriptedRouteState.Completed && State != ScriptedRouteState.Aborted;
        public bool IsMoving => State == ScriptedRouteState.Moving || State == ScriptedRouteState.Blocked;
        public Vector2 Goal => points[Mathf.Min(PointIndex, points.Length - 1)];
        public float Speed => settings.Speed;
        public float Closure
        {
            get
            {
                if (State == ScriptedRouteState.Reacting && settings.ReactionSeconds > 0)
                {
                    return 1 - reactionLeft / settings.ReactionSeconds;
                }

                return IsMoving ? 1 : 0;
            }
        }

        public event Action<ScriptedRouteState> StateChanged;

        public ScriptedRouteModel(ScriptedRouteSettings settings, Vector2[] route)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (route == null || route.Length == 0)
            {
                throw new ArgumentException("A scripted route requires at least one point.", nameof(route));
            }

            points = (Vector2[])route.Clone();
            foreach (Vector2 point in points)
            {
                if (!Finite(point))
                {
                    throw new ArgumentException("Route points must be finite.", nameof(route));
                }
            }
        }

        public void Step(float deltaTime, Vector2 position, bool canMove)
        {
            if (!float.IsFinite(deltaTime) || deltaTime <= 0 || !Finite(position) || !ControlsActor || !canMove)
            {
                return;
            }

            if (State == ScriptedRouteState.Reacting)
            {
                reactionLeft = Mathf.Max(0, reactionLeft - deltaTime);
                if (reactionLeft == 0)
                {
                    Enter(ScriptedRouteState.Moving);
                }

                return;
            }

            if (!IsMoving)
            {
                return;
            }

            float distance = Vector2.Distance(position, Goal);
            if (distance <= settings.ArrivalDistance)
            {
                PointIndex++;
                stalled = 0;
                bestDistance = float.PositiveInfinity;
                Enter(PointIndex == points.Length ? ScriptedRouteState.Completed : ScriptedRouteState.Moving);
                return;
            }

            if (distance <= bestDistance - settings.ProgressDistance)
            {
                bestDistance = distance;
                stalled = 0;
                Enter(ScriptedRouteState.Moving);
            }
            else
            {
                stalled += deltaTime;
                if (stalled >= settings.BlockedSeconds)
                {
                    Enter(ScriptedRouteState.Blocked);
                }
            }
        }

        internal void Start()
        {
            reactionLeft = settings.ReactionSeconds;
            Enter(reactionLeft > 0 ? ScriptedRouteState.Reacting : ScriptedRouteState.Moving);
        }

        internal void Abort() => Enter(ScriptedRouteState.Aborted);

        private void Enter(ScriptedRouteState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            StateChanged?.Invoke(state);
        }

        private static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.sqrMagnitude);
    }
}
