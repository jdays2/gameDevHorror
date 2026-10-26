using System;
using UnityEngine;

namespace EmergencyShift.Features.ClawEnemy.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/Enemies/Claw motion set")]
    public sealed class ClawMotionSet : ScriptableObject
    {
        [SerializeField] private TimedSpriteMotion walk;
        [SerializeField] private TimedSpriteMotion run;
        [SerializeField] private TimedSpriteMotion attack;
        [SerializeField, Min(1)] private float turnDegreesPerSecond = 360;

        public TimedSpriteMotion Walk => walk;
        public TimedSpriteMotion Run => run;
        public TimedSpriteMotion Attack => attack;
        public float TurnSpeed => turnDegreesPerSecond;

        public void Validate()
        {
            if (walk == null || run == null || attack == null || !float.IsFinite(turnDegreesPerSecond) || turnDegreesPerSecond <= 0)
            {
                throw new InvalidOperationException("Claw motion set is incomplete.");
            }
            walk.Validate();
            run.Validate();
            attack.Validate();
        }
    }
}
