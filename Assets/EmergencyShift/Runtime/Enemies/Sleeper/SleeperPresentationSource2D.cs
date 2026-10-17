using UnityEngine;

namespace EmergencyShift
{
    [DisallowMultipleComponent, RequireComponent(typeof(Sleeper2D))]
    public sealed class SleeperPresentationSource2D : EnemyPresentationSource2D
    {
        private Sleeper2D actor;
        private void Awake()
        {
            actor = GetComponent<Sleeper2D>();
        }
        public override bool IsDead => !actor.IsAlive;
        public override bool IsWalking => actor.State == Sleeper2D.BehaviourState.Hunting;
        public override bool IsAttacking => actor.State == Sleeper2D.BehaviourState.Attacking;
        public override bool IsOrienting => IsAttacking || actor.State == Sleeper2D.BehaviourState.Waking;
        public override bool IsStaggered => actor.IsStaggered;
        public override float Closure => actor.Closure;
        public override Vector2 Velocity => actor.Velocity;
        public override Transform Target => actor.Player;
        public override Vector2 AttackHeading => IsAttacking ? actor.AttackHeading : base.AttackHeading;
        public override float AttackProgress => actor.AttackProgress;
        public override float DeathTime => actor.DeathTime;
        public override IThreatLight2D LightSource => actor.LightSource;
        public override LayerMask BlockingGeometry => actor.BlockingGeometry;
    }
}
