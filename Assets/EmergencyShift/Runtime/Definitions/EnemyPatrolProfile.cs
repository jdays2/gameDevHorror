using UnityEngine;
using EmergencyShift.Features.Enemies.Config;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Enemies/Patrol profile")]
    public sealed class EnemyPatrolProfile : ScriptableObject
    {
        [SerializeField, Min(.1f)]
        private float speed = .6f;
        [SerializeField, Min(.4f)]
        private float wanderStepDistance = 1.6f;
        [SerializeField]
        private Vector2 pauseSeconds = new Vector2(.6f, 1.8f);
        [Header("Route probing — previous defaults")]
        [SerializeField, Range(1, 64)]
        private int candidateAttempts = 12;
        [SerializeField, Min(0)]
        private float bodySkin = .04f;
        [SerializeField, Min(.001f)]
        private float arrivalDistance = .12f;
        [SerializeField, Min(.001f)]
        private float minimumCandidateDistance = .3f;
        [SerializeField, Min(0)]
        private float failedSearchPause = .4f;
        [SerializeField, Min(.001f)]
        private float stalledSeconds = .9f;
        [SerializeField, Range(0, 1)]
        private float progressFraction = .15f;

        public float BodySkin => bodySkin;
        public float Speed => speed;
        public EnemyPatrolSettings ToSettings() => new EnemyPatrolSettings(speed, wanderStepDistance, pauseSeconds,
            candidateAttempts, bodySkin, arrivalDistance, minimumCandidateDistance, failedSearchPause, stalledSeconds, progressFraction);
    }
}
