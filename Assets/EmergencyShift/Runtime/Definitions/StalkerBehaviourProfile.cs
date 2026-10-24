using System;
using UnityEngine;
using EmergencyShift.Features.Enemies.Config;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Enemies/Stalker behaviour")]
    public sealed class StalkerBehaviourProfile : ScriptableObject
    {
        [SerializeField, Min(.1f)]
        private float retreatSpeed = 3.8f;
        [SerializeField, Min(.1f)]
        private float standOffDistance = 1.05f;
        [SerializeField, Min(.1f)]
        private float recoilSeconds = .2f;
        [SerializeField, Min(.1f)]
        private float retreatSeconds = 1.05f;
        [SerializeField, Min(.1f)]
        private float cooldownSeconds = 3;
        [SerializeField, Min(.1f)]
        private float lightFearDistance = 3.2f;
        [SerializeField, Min(.05f)]
        private float reactionDelay = .45f;
        [Header("Flashlight — approach warning, close burn, twitch and retreat")]
        [SerializeField, Min(.1f)]
        private float lightBurnDistance = 1.9f;
        [SerializeField, Min(.01f)]
        private float eyeWarningSeconds = .3f;
        [SerializeField, Range(0, .95f), Tooltip("Fraction of the stopped reaction with the eye visibly open.")]
        private float eyeOpenRecoilFraction = .7f;
        [SerializeField, Min(.01f), Tooltip("Seconds of close illumination after the burn threshold to reach full clouding.")]
        private float eyeBurnSeconds = .3f;
        [SerializeField, Min(.1f)]
        private float eyeRecoverySeconds = 10;

        [SerializeField, Min(0)]
        private float interruptedAttackCooldown = .35f;

        public float RetreatSpeed => retreatSpeed;
        public float CooldownSeconds => cooldownSeconds;
        public float LightFearDistance => lightFearDistance;
        public float ReactionDelay => reactionDelay;

        public StalkerSettings ToSettings(EnemyDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            definition.Validate();
            return new StalkerSettings(definition.PursuitSpeed, retreatSpeed, standOffDistance, recoilSeconds,
                retreatSeconds, cooldownSeconds, lightFearDistance, reactionDelay, definition.AttackDuration,
                definition.HitStaggerSeconds, definition.HitPushSpeed, lightBurnDistance, eyeBurnSeconds,
                eyeRecoverySeconds, interruptedAttackCooldown, eyeOpenRecoilFraction, eyeWarningSeconds);
        }
    }
}
