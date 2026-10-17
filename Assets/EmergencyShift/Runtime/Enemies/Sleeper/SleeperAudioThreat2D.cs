using UnityEngine;

namespace EmergencyShift
{
    [DisallowMultipleComponent, RequireComponent(typeof(Sleeper2D), typeof(SleeperAudio2D))]
    public sealed class SleeperAudioThreat2D : AudioThreatEmitter2D
    {
        private Sleeper2D actor;
        private SleeperAudio2D feedback;
        public override bool IsAlive => actor != null && actor.isActiveAndEnabled && actor.IsAlive;
        public override bool IsEngaged => IsAlive && actor.HasAggro;
        public override bool IsAttacking => IsAlive && actor.State == Sleeper2D.BehaviourState.Attacking;
        public override Transform Target => actor != null ? actor.Player : null;
        public override LayerMask Obstacles => actor != null ? actor.BlockingGeometry : 0;

        private void Awake()
        {
            actor = GetComponent<Sleeper2D>();
            feedback = GetComponent<SleeperAudio2D>();
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            feedback.PerceptibleCue += ReportHeard;
        }
        protected override void OnDisable()
        {
            if (feedback != null)
            {
                feedback.PerceptibleCue -= ReportHeard;
            }
            base.OnDisable();
        }
    }
}
