using UnityEngine;

namespace EmergencyShift.Features.Scorpion.View
{
    [RequireComponent(typeof(Scorpion2D))]
    public sealed class ScorpionPresentationSource2D : EnemyPresentationSource2D
    {
        private Scorpion2D actor;
        private void Awake() => actor = GetComponent<Scorpion2D>();
        public override bool IsWalking => actor.IsAlive && actor.HasAggro;
        public override bool IsAttacking => actor.IsAttacking;
        public override float AttackElevation => 0;
        public override bool IsDead => !actor.IsAlive;
        public override float AttackProgress => actor.AttackProgress;
        public override float DeathTime => actor.DeathTime;
        public override IThreatLight2D LightSource => actor.LightSource;
        public override LayerMask BlockingGeometry => actor.BlockingGeometry;
        public override Vector2 Velocity => actor.Velocity;
        public override Transform Target => actor.Target;
        public override Vector2 AttackHeading => actor.AttackHeading;
    }
}
