using UnityEngine;

namespace EmergencyShift.Features.Scorpion.View
{
    /// <summary>Registers perceived Scorpion cues with the shared scene atmosphere.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Scorpion2D), typeof(ScorpionAudio2D))]
    public sealed class ScorpionAudioThreat2D : AudioThreatEmitter2D
    {
        private Scorpion2D actor;
        private ScorpionAudio2D feedback;
        public override bool IsAlive => actor != null && actor.isActiveAndEnabled && actor.IsAlive;
        public override bool IsEngaged => IsAlive && actor.HasAggro;
        public override bool IsAttacking => IsAlive && actor.IsAttacking;
        public override Transform Target => actor != null ? actor.Target : null;
        public override LayerMask Obstacles => actor != null ? actor.BlockingGeometry : 0;

        private void Awake()
        {
            actor = GetComponent<Scorpion2D>();
            feedback = GetComponent<ScorpionAudio2D>();
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
