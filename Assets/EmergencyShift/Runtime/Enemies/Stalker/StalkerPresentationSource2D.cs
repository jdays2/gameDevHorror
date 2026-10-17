using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Species adapter; a different AI can provide the same presentation facts.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(ShadowStalker2D))]
    public sealed class StalkerPresentationSource2D : EnemyPresentationSource2D
    {
        private ShadowStalker2D actor;
        private void Awake()
        {
            actor = GetComponent<ShadowStalker2D>();
        }

        public override bool IsDead => !actor.IsAlive;
        public override bool IsWalking => actor.State == ShadowStalker2D.BehaviourState.Sneaking || actor.State == ShadowStalker2D.BehaviourState.Patrolling;
        public override bool IsAttacking => actor.State == ShadowStalker2D.BehaviourState.Attacking;
        public override bool IsCompactWalking => actor.State == ShadowStalker2D.BehaviourState.Retreating;
        public override bool IsStaggered => actor.IsStaggered;
        public override float Closure => actor.Closure;
        public override Vector2 Velocity => actor.Velocity;
        public override Transform Target => actor.Player;
        public override float AttackProgress => actor.AttackProgress;
        public override float DeathTime => actor.DeathTime;
        public override IThreatLight2D LightSource => actor.LightSource;
        public override LayerMask BlockingGeometry => actor.BlockingGeometry;
    }
}
