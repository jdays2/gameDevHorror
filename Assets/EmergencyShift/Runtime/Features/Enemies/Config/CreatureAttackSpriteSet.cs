using System;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Config
{
    /// <summary>Immutable whole-body attack frames with gait-phase entry and return bridges.</summary>
    [CreateAssetMenu(menuName = "Emergency Shift/Enemies/Creature attack sprite set")]
    public sealed class CreatureAttackSpriteSet : ScriptableObject
    {
        [SerializeField, Min(2)] private int phases = 48;
        [SerializeField, Min(2)] private int bridgeFrames = 15;
        [SerializeField, Min(2)] private int strikeFrames = 24;
        [SerializeField, Range(.01f, .4f)] private float entryFraction = .15f;
        [SerializeField, Range(.6f, .98f)] private float returnStartFraction = .85f;
        [SerializeField, Range(.8f, .999f)] private float returnCompleteFraction = .97f;
        [SerializeField, Range(.01f, .99f)] private float strikePeakFraction = .4f;
        [SerializeField, Min(.01f), Tooltip("Time reserved inside the actual attack to settle support and raise the front pair.")]
        private float entrySeconds = .24f;
        [SerializeField, Min(.01f), Tooltip("Time reserved inside the actual attack for the whole-body return to walking.")]
        private float returnSeconds = .24f;
        [SerializeField, Min(.01f), Tooltip("Visual retraction after an interrupted attack; does not delay combat, eye protection or movement.")]
        private float interruptionReturnSeconds = .36f;
        [SerializeField] private Sprite[] frames;

        public int Phases => phases;
        public int BridgeFrames => bridgeFrames;
        public int StrikeFrames => strikeFrames;
        public int StrikePeakFrame => Mathf.RoundToInt(strikePeakFraction * (strikeFrames - 1));
        public float EntrySeconds => entrySeconds;
        public float ReturnSeconds => returnSeconds;
        public float InterruptionReturnSeconds => interruptionReturnSeconds;
        public int FrameCount => phases * bridgeFrames * 2 + strikeFrames;

        public int FrameIndex(float phase, float progress, float strikeAt = .43f, float attackDuration = .8f)
        {
            int column = Mathf.Clamp(Mathf.FloorToInt(Mathf.Repeat(phase, 1) * phases), 0, phases - 1);
            progress = Mathf.Clamp01(progress);
            float duration = Mathf.Max(.01f, attackDuration);
            float entryEnd = Mathf.Clamp(Mathf.Max(entryFraction, entrySeconds / duration), entryFraction, Mathf.Max(entryFraction, strikeAt - .01f));
            float returnStart = Mathf.Min(returnStartFraction, returnCompleteFraction - returnSeconds / duration);
            returnStart = Mathf.Clamp(returnStart, Mathf.Max(entryEnd + .01f, strikeAt + .01f), returnCompleteFraction - .01f);
            if (progress < entryEnd)
            {
                int sample = Mathf.Clamp(Mathf.RoundToInt(progress / entryEnd * (bridgeFrames - 1)), 0, bridgeFrames - 1);
                return column * bridgeFrames + sample;
            }
            if (progress < returnStart)
            {
                float strikeProgress = Mathf.InverseLerp(entryEnd, returnStart, progress);
                float actualPeak = Mathf.Clamp(Mathf.InverseLerp(entryEnd, returnStart, strikeAt), .01f, .99f);
                float sourceProgress;
                if (strikeProgress <= actualPeak)
                {
                    sourceProgress = strikeProgress / actualPeak * strikePeakFraction;
                }
                else
                {
                    sourceProgress = Mathf.Lerp(strikePeakFraction, 1, Mathf.InverseLerp(actualPeak, 1, strikeProgress));
                }
                int sample = Mathf.Clamp(Mathf.RoundToInt(sourceProgress * (strikeFrames - 1)), 0, strikeFrames - 1);
                return phases * bridgeFrames + sample;
            }
            float returnProgress = Mathf.InverseLerp(returnStart, returnCompleteFraction, progress);
            int returning = Mathf.Clamp(Mathf.RoundToInt(returnProgress * (bridgeFrames - 1)), 0, bridgeFrames - 1);
            return phases * bridgeFrames + strikeFrames + column * bridgeFrames + returning;
        }

        public Sprite Frame(int index) => frames[index];

        public void Validate()
        {
            if (phases < 2 || bridgeFrames < 2 || strikeFrames < 2 || !float.IsFinite(entryFraction) ||
                !float.IsFinite(returnStartFraction) || !float.IsFinite(returnCompleteFraction) ||
                !float.IsFinite(strikePeakFraction) || strikePeakFraction <= 0 || strikePeakFraction >= 1 ||
                !float.IsFinite(entrySeconds) || entrySeconds < .01f ||
                !float.IsFinite(returnSeconds) || returnSeconds < .01f ||
                !float.IsFinite(interruptionReturnSeconds) || interruptionReturnSeconds < returnSeconds ||
                entryFraction <= 0 || entryFraction >= returnStartFraction || returnStartFraction >= returnCompleteFraction ||
                returnCompleteFraction >= 1 || frames == null || frames.Length != FrameCount)
            {
                throw new InvalidOperationException("Incomplete creature attack sprite set: " + name);
            }
            foreach (Sprite frame in frames)
            {
                if (frame == null)
                {
                    throw new InvalidOperationException("Missing creature attack frame: " + name);
                }
            }
        }
    }
}
