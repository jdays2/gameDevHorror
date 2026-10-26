using System;
using UnityEngine;

namespace EmergencyShift.Features.ClawEnemy.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/Enemies/Timed sprite motion")]
    public sealed class TimedSpriteMotion : ScriptableObject
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private int[] durationsMs;
        [SerializeField] private bool loop;
        [SerializeField, Min(0)] private float referenceSpeed;

        public float ReferenceSpeed => referenceSpeed;
        public float Duration
        {
            get
            {
                int duration = 0;
                foreach (int milliseconds in durationsMs)
                {
                    duration += milliseconds;
                }
                return duration * .001f;
            }
        }

        public void Validate()
        {
            if (frames == null || durationsMs == null || frames.Length == 0 || frames.Length != durationsMs.Length ||
                !float.IsFinite(referenceSpeed) || referenceSpeed < 0 || (loop && referenceSpeed <= 0))
            {
                throw new InvalidOperationException("Invalid timed sprite motion: " + name);
            }
            for (int index = 0; index < frames.Length; index++)
            {
                if (frames[index] == null || durationsMs[index] < 1)
                {
                    throw new InvalidOperationException("Missing frame or duration: " + name);
                }
            }
        }

        public Sprite FrameAt(float seconds)
        {
            float age = loop ? Mathf.Repeat(Mathf.Max(0, seconds), Duration) : Mathf.Max(0, seconds);
            float end = 0;
            for (int index = 0; index < frames.Length; index++)
            {
                end += durationsMs[index] * .001f;
                if (age < end)
                {
                    return frames[index];
                }
            }
            return frames[frames.Length - 1];
        }
    }
}
