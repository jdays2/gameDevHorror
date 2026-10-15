using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Adapter only: it observes AI and actual audible playback; it never activates or resets aggro.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(ShadowStalker2D))]
    public sealed class StalkerAudioThreat2D : AudioThreatEmitter2D
    {
        private ShadowStalker2D stalker;
        private StalkerAudio2D feedback;
        public override bool IsAlive => stalker != null && stalker.isActiveAndEnabled && stalker.IsAlive;
        public override bool IsEngaged => IsAlive && stalker.HasAggro;
        public override bool IsAttacking => IsAlive && stalker.State == ShadowStalker2D.BehaviourState.Attacking;
        public override Transform Target => stalker != null ? stalker.Player : null;
        public override LayerMask Obstacles => stalker != null ? stalker.BlockingGeometry : 0;
        private void Awake() { stalker = GetComponent<ShadowStalker2D>(); feedback = GetComponent<StalkerAudio2D>(); }
        protected override void OnEnable()
        {
            base.OnEnable();
            if (feedback != null) feedback.PerceptibleCue += OnHeard;
        }
        protected override void OnDisable()
        {
            if (feedback != null) feedback.PerceptibleCue -= OnHeard;
            base.OnDisable();
        }
        private void OnHeard(float duration, float gain, bool urgent) { ReportHeard(duration, gain, urgent); }
    }
}
