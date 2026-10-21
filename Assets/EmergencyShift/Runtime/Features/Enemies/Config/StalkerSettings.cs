using System;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Config
{
    public sealed class StalkerSettings
    {
        public StalkerSettings(float pursuitSpeed, float retreatSpeed, float standOffDistance, float recoilSeconds,
            float retreatSeconds, float cooldownSeconds, float fearDistance, float reactionDelay, float attackDuration,
            float staggerSeconds, float pushSpeed, float eyeBurnLead, float eyeBurnSeconds, float eyeBurnCue, float interruptDelay)
        {
            if (!Nonnegative(pursuitSpeed) || !Positive(retreatSpeed) || !Positive(standOffDistance) ||
                !Positive(recoilSeconds) || !Positive(retreatSeconds) || !Nonnegative(cooldownSeconds) ||
                !Positive(fearDistance) || !Positive(reactionDelay) || !Positive(attackDuration) ||
                !Nonnegative(staggerSeconds) || !Nonnegative(pushSpeed) || !Nonnegative(eyeBurnLead) ||
                !Nonnegative(eyeBurnSeconds) || !Nonnegative(eyeBurnCue) || !Nonnegative(interruptDelay))
            {
                throw new ArgumentException("Invalid Stalker movement, reaction, attack or eye-burn settings.");
            }

            PursuitSpeed = pursuitSpeed;
            RetreatSpeed = retreatSpeed;
            StandOffDistance = standOffDistance;
            RecoilSeconds = recoilSeconds;
            RetreatSeconds = retreatSeconds;
            CooldownSeconds = cooldownSeconds;
            FearDistance = fearDistance;
            ReactionDelay = reactionDelay;
            AttackDuration = attackDuration;
            StaggerSeconds = staggerSeconds;
            PushSpeed = pushSpeed;
            EyeBurnLead = Mathf.Max(.05f, eyeBurnLead);
            EyeBurnSeconds = Mathf.Max(.1f, eyeBurnSeconds);
            EyeBurnCue = Mathf.Max(.1f, eyeBurnCue);
            InterruptDelay = interruptDelay;
        }

        public float PursuitSpeed { get; }
        public float RetreatSpeed { get; }
        public float StandOffDistance { get; }
        public float RecoilSeconds { get; }
        public float RetreatSeconds { get; }
        public float CooldownSeconds { get; }
        public float FearDistance { get; }
        public float ReactionDelay { get; }
        public float AttackDuration { get; }
        public float StaggerSeconds { get; }
        public float PushSpeed { get; }
        public float EyeBurnLead { get; }
        public float EyeBurnSeconds { get; }
        public float EyeBurnCue { get; }
        public float InterruptDelay { get; }
        private static bool Nonnegative(float value) => float.IsFinite(value) && value >= 0;
        private static bool Positive(float value) => float.IsFinite(value) && value > 0;
    }
}
