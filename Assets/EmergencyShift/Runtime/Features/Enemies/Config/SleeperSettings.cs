using System;

namespace EmergencyShift.Features.Enemies.Config
{
    public sealed class SleeperSettings
    {
        public SleeperSettings(float pursuitSpeed, float attackDuration, float attackCooldown, float staggerSeconds,
            float pushSpeed, float exposureSeconds, float exposureDecay, float wakingSeconds, float recoverySeconds)
        {
            if (!Nonnegative(pursuitSpeed) || !Positive(attackDuration) || !Nonnegative(attackCooldown) ||
                !Nonnegative(staggerSeconds) || !Nonnegative(pushSpeed) || !Positive(exposureSeconds) ||
                !Positive(exposureDecay) || !Positive(wakingSeconds) || !Nonnegative(recoverySeconds))
            {
                throw new ArgumentException("Sleeper requires finite speeds, positive durations and nonnegative recovery/stagger.");
            }

            PursuitSpeed = pursuitSpeed;
            AttackDuration = attackDuration;
            AttackCooldown = attackCooldown;
            StaggerSeconds = staggerSeconds;
            PushSpeed = pushSpeed;
            ExposureSeconds = exposureSeconds;
            ExposureDecay = exposureDecay;
            WakingSeconds = wakingSeconds;
            RecoverySeconds = recoverySeconds;
        }

        public float PursuitSpeed { get; }
        public float AttackDuration { get; }
        public float AttackCooldown { get; }
        public float StaggerSeconds { get; }
        public float PushSpeed { get; }
        public float ExposureSeconds { get; }
        public float ExposureDecay { get; }
        public float WakingSeconds { get; }
        public float RecoverySeconds { get; }
        private static bool Nonnegative(float value) => float.IsFinite(value) && value >= 0;
        private static bool Positive(float value) => float.IsFinite(value) && value > 0;
    }
}
