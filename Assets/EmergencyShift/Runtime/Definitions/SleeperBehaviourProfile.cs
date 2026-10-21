using System;
using UnityEngine;
using EmergencyShift.Features.Enemies.Config;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Enemies/Sleeper behaviour")]
    public sealed class SleeperBehaviourProfile : ScriptableObject
    {
        [SerializeField, Min(.05f)]
        private float lightExposureSeconds = .65f;
        [SerializeField, Min(.01f), Tooltip("Exposure seconds lost per second outside the directed beam.")]
        private float exposureDecay = .75f;
        [SerializeField, Min(.05f)]
        private float wakingSeconds = .9f;
        [SerializeField, Min(0)]
        private float recoverySeconds = .65f;

        public bool IsValid => float.IsFinite(lightExposureSeconds) && lightExposureSeconds > 0 &&
            float.IsFinite(exposureDecay) && exposureDecay > 0 && float.IsFinite(wakingSeconds) && wakingSeconds > 0 &&
            float.IsFinite(recoverySeconds) && recoverySeconds >= 0;

        public SleeperSettings ToSettings(EnemyDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            definition.Validate();
            if (!IsValid)
            {
                throw new InvalidOperationException("Invalid Sleeper behaviour profile: " + name);
            }

            return new SleeperSettings(definition.PursuitSpeed, definition.AttackDuration, definition.AttackCooldown,
                definition.HitStaggerSeconds, definition.HitPushSpeed, lightExposureSeconds, exposureDecay, wakingSeconds, recoverySeconds);
        }
    }
}
