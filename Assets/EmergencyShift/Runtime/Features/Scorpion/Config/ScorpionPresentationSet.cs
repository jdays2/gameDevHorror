using System;
using EmergencyShift.Features.ClawEnemy.Config;
using UnityEngine;

namespace EmergencyShift.Features.Scorpion.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/Enemies/Scorpion presentation")]
    public sealed class ScorpionPresentationSet : ScriptableObject
    {
        [SerializeField] private TimedSpriteMotion run;
        [SerializeField] private TimedSpriteMotion runEyes;
        [SerializeField] private TimedSpriteMotion attack;
        [SerializeField] private TimedSpriteMotion attackEyes;
        [SerializeField] private Sprite idle;
        [SerializeField] private Sprite idleEyes;
        [SerializeField] private AudioClip[] steps;
        [SerializeField] private AudioClip hit;
        [SerializeField] private AudioClip presence;
        [SerializeField] private AudioClip grasp;
        [SerializeField] private AudioClip alarm;
        [SerializeField] private EnemyDeathAudioSet deathSounds;
        [SerializeField, Range(0, 1)] private float detectionVolume = .45f;
        [SerializeField, Min(1)] private float turnDegrees = 360;
        [SerializeField, Range(0, 1)] private float voiceVolume = .6f;
        [SerializeField, Range(0, 1)] private float stepVolume = .24f;
        [SerializeField, Min(.1f)] private float soundRadius = 12;
        [SerializeField, Min(.1f)] private float contactDistance = .265625f;
        public TimedSpriteMotion Run => run;
        public TimedSpriteMotion RunEyes => runEyes;
        public TimedSpriteMotion Attack => attack;
        public TimedSpriteMotion AttackEyes => attackEyes;
        public Sprite Idle => idle;
        public Sprite IdleEyes => idleEyes;
        public int StepCount => steps.Length;
        public AudioClip Step(int index) => steps[index];
        public AudioClip Hit => hit;
        public AudioClip Presence => presence;
        public AudioClip Grasp => grasp;
        public AudioClip Alarm => alarm;
        public EnemyDeathAudioSet DeathSounds => deathSounds;
        public float DetectionVolume => detectionVolume;
        public float TurnDegrees => turnDegrees;
        public float VoiceVolume => voiceVolume;
        public float StepVolume => stepVolume;
        public float SoundRadius => soundRadius;
        public float ContactDistance => contactDistance;
        public float DistancePerCycle => run.Duration * run.ReferenceSpeed;
        public void Validate()
        {
            run.Validate();
            runEyes.Validate();
            attack.Validate();
            attackEyes.Validate();
            if (idle == null || idleEyes == null || steps == null || steps.Length == 0 ||
                hit == null || presence == null || grasp == null || alarm == null ||
                deathSounds == null || deathSounds.Clips.Count == 0 ||
                !float.IsFinite(detectionVolume) || detectionVolume < 0 || detectionVolume > 1 ||
                !float.IsFinite(attack.ReferenceSpeed) || attack.ReferenceSpeed <= 0 ||
                !float.IsFinite(turnDegrees) || turnDegrees < 1 ||
                !float.IsFinite(soundRadius) || soundRadius <= 0 ||
                !float.IsFinite(contactDistance) || contactDistance <= 0 ||
                Mathf.Abs(run.Duration - runEyes.Duration) > .0001f ||
                Mathf.Abs(attack.Duration - attackEyes.Duration) > .0001f)
            {
                throw new InvalidOperationException("Incomplete scorpion presentation: " + name);
            }
            deathSounds.Validate();
            foreach (AudioClip step in steps)
            {
                if (step == null)
                {
                    throw new InvalidOperationException("Missing scorpion contact clip: " + name);
                }
            }
        }
    }
}
