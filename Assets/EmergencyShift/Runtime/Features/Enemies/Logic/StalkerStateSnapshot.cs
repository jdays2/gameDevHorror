using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public readonly struct StalkerStateSnapshot
    {
        public StalkerStateSnapshot(StalkerState state, bool aggro, float eyeDamage, float burnContact, float age,
            float exposure, float closure, Vector2 escape, Vector2 approach, float staggerLeft, Vector2 push)
        {
            State = state;
            Aggro = aggro;
            EyeDamage = eyeDamage;
            BurnContact = burnContact;
            Age = age;
            Exposure = exposure;
            Closure = closure;
            Escape = escape;
            Approach = approach;
            StaggerLeft = staggerLeft;
            Push = push;
        }

        public StalkerState State { get; }
        public bool Aggro { get; }
        public float EyeDamage { get; }
        public float BurnContact { get; }
        public float Age { get; }
        public float Exposure { get; }
        public float Closure { get; }
        public Vector2 Escape { get; }
        public Vector2 Approach { get; }
        public float StaggerLeft { get; }
        public Vector2 Push { get; }
    }
}
