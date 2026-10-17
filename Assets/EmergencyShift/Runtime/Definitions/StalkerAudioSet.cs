using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Shared authored audio data: one asset instead of 49 references on every creature.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Stalker audio set")]
    public sealed class StalkerAudioSet : ScriptableObject
    {
        public enum Cue
        {
            Step,
            Scrape,
            Idle,
            Notice,
            Windup,
            Lunge,
            Bite,
            LightFear,
            RetreatPain,
            Cower,
            Hit,
            Death,
            Debris,
            EyeBurn
        }

        [SerializeField]
        private AudioClip[] step;
        [SerializeField]
        private AudioClip[] scrape;
        [SerializeField]
        private AudioClip[] idle;
        [SerializeField]
        private AudioClip[] notice;
        [SerializeField]
        private AudioClip[] windup;
        [SerializeField]
        private AudioClip[] lunge;
        [SerializeField]
        private AudioClip[] bite;
        [SerializeField]
        private AudioClip[] lightFear;
        [SerializeField]
        private AudioClip[] retreatPain;
        [SerializeField]
        private AudioClip[] cower;
        [SerializeField]
        private AudioClip[] hit;
        [SerializeField]
        private AudioClip[] death;
        [SerializeField]
        private AudioClip[] debris;
        [SerializeField]
        private AudioClip[] eyeBurn;
        [SerializeField, Range(0, 1)]
        private float eyeBurnGain = .8f;
        public float EyeBurnGain => eyeBurnGain;
        [Header("Shared simultaneous voice limits")]
        [SerializeField, Min(1)]
        private int movementVoices = 8;
        [SerializeField, Min(1)]
        private int idleVoices = 2;
        [SerializeField, Min(1)]
        private int deathVoices = 3;
        public int MovementVoices => movementVoices;
        public int IdleVoices => idleVoices;
        public int DeathVoices => deathVoices;

        public AudioClip[] Clips(Cue cue)
        {
            switch (cue)
            {
                case Cue.Step:
                    return step;
                case Cue.Scrape:
                    return scrape;
                case Cue.Idle:
                    return idle;
                case Cue.Notice:
                    return notice;
                case Cue.Windup:
                    return windup;
                case Cue.Lunge:
                    return lunge;
                case Cue.Bite:
                    return bite;
                case Cue.LightFear:
                    return lightFear;
                case Cue.RetreatPain:
                    return retreatPain;
                case Cue.Cower:
                    return cower;
                case Cue.Hit:
                    return hit;
                case Cue.Death:
                    return death;
                case Cue.Debris:
                    return debris;
                case Cue.EyeBurn:
                    return eyeBurn;
                default:
                    return null;
            }
        }
    }
}
