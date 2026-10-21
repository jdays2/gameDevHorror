using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public readonly struct SleeperStateSnapshot
    {
        public SleeperStateSnapshot(SleeperState state, bool aggro, float age, float exposure, float staggerLeft,
            Vector2 push, Vector2 heading)
        {
            State = state;
            Aggro = aggro;
            Age = age;
            Exposure = exposure;
            StaggerLeft = staggerLeft;
            Push = push;
            Heading = heading;
        }

        public SleeperState State { get; }
        public bool Aggro { get; }
        public float Age { get; }
        public float Exposure { get; }
        public float StaggerLeft { get; }
        public Vector2 Push { get; }
        public Vector2 Heading { get; }
    }
}
