using System;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Config
{
    public sealed class EnemyPatrolSettings
    {
        public EnemyPatrolSettings(float speed, float wanderDistance, Vector2 pauseSeconds, int attempts,
            float skin, float arrivalDistance, float minimumCandidateDistance, float retryPause,
            float stalledSeconds, float progressFraction)
        {
            if (!Nonnegative(speed) || !Nonnegative(wanderDistance) || !Nonnegative(pauseSeconds.x) ||
                !Nonnegative(pauseSeconds.y) || pauseSeconds.y < pauseSeconds.x || attempts < 1 || attempts > 64 ||
                !Nonnegative(skin) || !Positive(arrivalDistance) || !Positive(minimumCandidateDistance) ||
                !Nonnegative(retryPause) || !Positive(stalledSeconds) || !Nonnegative(progressFraction) || progressFraction > 1)
            {
                throw new ArgumentException("Invalid patrol values, pause ordering or bounded candidate count.");
            }

            Speed = speed;
            WanderDistance = wanderDistance;
            PauseSeconds = pauseSeconds;
            Attempts = attempts;
            Skin = skin;
            ArrivalDistance = arrivalDistance;
            MinimumCandidateDistance = minimumCandidateDistance;
            RetryPause = retryPause;
            StalledSeconds = stalledSeconds;
            ProgressFraction = progressFraction;
        }

        public float Speed { get; }
        public float WanderDistance { get; }
        public Vector2 PauseSeconds { get; }
        public int Attempts { get; }
        public float Skin { get; }
        public float ArrivalDistance { get; }
        public float MinimumCandidateDistance { get; }
        public float RetryPause { get; }
        public float StalledSeconds { get; }
        public float ProgressFraction { get; }

        private static bool Nonnegative(float value) => float.IsFinite(value) && value >= 0;
        private static bool Positive(float value) => float.IsFinite(value) && value > 0;
    }
}
