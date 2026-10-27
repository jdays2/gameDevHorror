using System;
using EmergencyShift.Features.Enemies.Config;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Retracts the last displayed whole attack pose without changing combat state.</summary>
    public sealed class CreatureAttackSpritePlayback
    {
        private enum Segment
        {
            Entry,
            Strike,
            Return
        }

        private CreatureAttackSpriteSet recordedSet;
        private int lastAttackFrame;
        private bool hasAttackFrame;
        private bool retracting;
        private Segment segment;
        private int initialSample;
        private int chamberSample;
        private float strikeFraction;
        private float elapsed;
        private float duration;

        public void RecordAttackFrame(CreatureAttackSpriteSet sprites, int frame)
        {
            if (sprites == null)
            {
                throw new ArgumentNullException(nameof(sprites));
            }
            if (frame < 0 || frame >= sprites.FrameCount)
            {
                throw new ArgumentOutOfRangeException(nameof(frame));
            }

            recordedSet = sprites;
            lastAttackFrame = frame;
            retracting = false;
            elapsed = 0;
            int returnStart = sprites.Phases * sprites.BridgeFrames + sprites.StrikeFrames;
            bool completed = frame >= returnStart && (frame - returnStart) % sprites.BridgeFrames == sprites.BridgeFrames - 1;
            hasAttackFrame = !completed;
        }

        public bool TrySelectRetractionFrame(CreatureAttackSpriteSet sprites, float phase, float deltaTime, out int frame)
        {
            frame = 0;
            if (sprites == null || sprites != recordedSet)
            {
                Reset();
                return false;
            }
            if (!hasAttackFrame)
            {
                return false;
            }
            if (!retracting)
            {
                BeginRetraction(sprites);
                // Preserve the pose actually seen before the state cut, including its mouth.
                frame = lastAttackFrame;
                return true;
            }

            elapsed += Mathf.Max(0, deltaTime);
            float progress = Mathf.Clamp01(elapsed / duration);
            int column = Mathf.Clamp(Mathf.FloorToInt(Mathf.Repeat(phase, 1) * sprites.Phases), 0, sprites.Phases - 1);
            int entryCount = sprites.Phases * sprites.BridgeFrames;
            int returnStart = entryCount + sprites.StrikeFrames;
            int sample;
            switch (segment)
            {
                case Segment.Entry:
                    sample = Sample(initialSample, 0, progress);
                    frame = column * sprites.BridgeFrames + sample;
                    break;
                case Segment.Strike:
                    if (progress < strikeFraction)
                    {
                        sample = Sample(initialSample, chamberSample, progress / strikeFraction);
                        frame = entryCount + sample;
                    }
                    else
                    {
                        float returning = (progress - strikeFraction) / (1 - strikeFraction);
                        sample = Sample(0, sprites.BridgeFrames - 1, returning);
                        frame = returnStart + column * sprites.BridgeFrames + sample;
                    }
                    break;
                default:
                    sample = Sample(initialSample, sprites.BridgeFrames - 1, progress);
                    frame = returnStart + column * sprites.BridgeFrames + sample;
                    break;
            }

            if (progress >= 1)
            {
                // Emit the exact current-phase walking endpoint once before body folding resumes.
                hasAttackFrame = false;
            }
            return true;
        }

        public void Reset()
        {
            recordedSet = null;
            hasAttackFrame = false;
            retracting = false;
            elapsed = 0;
        }

        private void BeginRetraction(CreatureAttackSpriteSet sprites)
        {
            int entryCount = sprites.Phases * sprites.BridgeFrames;
            int returnStart = entryCount + sprites.StrikeFrames;
            int bridgeTravel = sprites.BridgeFrames - 1;
            duration = sprites.ReturnSeconds;
            strikeFraction = 0;
            if (lastAttackFrame < entryCount)
            {
                segment = Segment.Entry;
                initialSample = lastAttackFrame % sprites.BridgeFrames;
                duration *= initialSample / (float)bridgeTravel;
            }
            else if (lastAttackFrame < returnStart)
            {
                segment = Segment.Strike;
                initialSample = lastAttackFrame - entryCount;
                chamberSample = initialSample < sprites.StrikePeakFrame ? 0 : sprites.StrikeFrames - 1;
                int strikeTravel = Mathf.Abs(chamberSample - initialSample);
                int fullStrikeTravel = Mathf.Max(sprites.StrikePeakFrame, sprites.StrikeFrames - 1 - sprites.StrikePeakFrame);
                float strikeDuration = (sprites.InterruptionReturnSeconds - sprites.ReturnSeconds) * strikeTravel / fullStrikeTravel;
                duration = sprites.ReturnSeconds + strikeDuration;
                strikeFraction = strikeDuration / duration;
            }
            else
            {
                segment = Segment.Return;
                initialSample = (lastAttackFrame - returnStart) % sprites.BridgeFrames;
                duration *= (bridgeTravel - initialSample) / (float)bridgeTravel;
            }
            duration = Mathf.Max(.001f, duration);
            elapsed = 0;
            retracting = true;
        }

        private static int Sample(int from, int to, float progress) =>
            Mathf.RoundToInt(Mathf.Lerp(from, to, progress));
    }
}
