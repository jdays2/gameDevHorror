using EmergencyShift.Features.Enemies.Config;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class CreatureAttackSpritePlaybackTests
    {
        private CreatureAttackSpriteSet sprites;
        private CreatureAttackSpritePlayback playback;

        [SetUp]
        public void SetUp()
        {
            sprites = ScriptableObject.CreateInstance<CreatureAttackSpriteSet>();
            playback = new CreatureAttackSpritePlayback();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(sprites);
        }

        [Test]
        public void FirstRetractionSampleKeepsTheActualDisplayedFrameFromEveryAttackSegment()
        {
            int entryCount = sprites.Phases * sprites.BridgeFrames;
            int returnStart = entryCount + sprites.StrikeFrames;
            int[] interrupted = { 12 * sprites.BridgeFrames + 5, entryCount + 4,
                entryCount + 10, returnStart + 12 * sprites.BridgeFrames + 3 };
            foreach (int prior in interrupted)
            {
                playback.RecordAttackFrame(sprites, prior);
                Assert.That(playback.TrySelectRetractionFrame(sprites, .8f, 1, out int selected), Is.True);
                Assert.That(selected, Is.EqualTo(prior), "A changed gait phase or large first dt must not replace the visible pose or mouth.");
            }
        }

        [Test]
        public void InterruptedWindupReversesEntryWithoutPlayingAFutureStrike()
        {
            playback.RecordAttackFrame(sprites, 12 * sprites.BridgeFrames + 5);
            int previousSample = 5;
            int last = -1;
            float lastPhase = 0;
            for (int tick = 0; tick < 60; tick++)
            {
                float phase = .3f + tick * .004f;
                if (!playback.TrySelectRetractionFrame(sprites, phase, 1f / 60, out int frame))
                {
                    break;
                }
                Assert.That(frame, Is.LessThan(sprites.Phases * sprites.BridgeFrames), "Windup cancellation must never enter the strike segment.");
                int sample = frame % sprites.BridgeFrames;
                Assert.That(sample, Is.LessThanOrEqualTo(previousSample));
                previousSample = sample;
                last = frame;
                lastPhase = phase;
            }
            Assert.That(last, Is.EqualTo(sprites.FrameIndex(lastPhase, 0)), "The entry's walking endpoint follows the actor's current gait column.");
            Assert.That(playback.TrySelectRetractionFrame(sprites, lastPhase, 1f / 60, out _), Is.False);
        }

        [TestCase(4, 0)]
        [TestCase(10, 23)]
        public void InterruptedStrikeRetractsThroughItsOwnChamberThenJoinsTheCurrentGait(int initial, int chamber)
        {
            int entryCount = sprites.Phases * sprites.BridgeFrames;
            int returnStart = entryCount + sprites.StrikeFrames;
            playback.RecordAttackFrame(sprites, entryCount + initial);
            int previousStrike = initial;
            bool chamberSeen = false;
            bool bridgeSeen = false;
            int last = -1;
            float lastPhase = 0;
            int activeTicks = 0;
            for (int tick = 0; tick < 60; tick++)
            {
                float phase = .2f + tick * .01f;
                if (!playback.TrySelectRetractionFrame(sprites, phase, 1f / 60, out int frame))
                {
                    break;
                }
                activeTicks++;
                Assert.That(frame, Is.GreaterThanOrEqualTo(entryCount));
                if (frame < returnStart)
                {
                    Assert.That(bridgeSeen, Is.False, "The return bridge cannot reopen the mouth by revisiting strike frames.");
                    int sample = frame - entryCount;
                    if (chamber == 0)
                    {
                        Assert.That(sample, Is.LessThanOrEqualTo(previousStrike), "A cancelled preparation must not advance toward a future hit.");
                    }
                    else
                    {
                        Assert.That(sample, Is.GreaterThanOrEqualTo(previousStrike));
                    }
                    previousStrike = sample;
                    chamberSeen |= sample == chamber;
                }
                else
                {
                    bridgeSeen = true;
                    int sample = (frame - returnStart) % sprites.BridgeFrames;
                    chamberSeen |= sample == 0;
                    int column = Mathf.FloorToInt(Mathf.Repeat(phase, 1) * sprites.Phases);
                    Assert.That(frame, Is.EqualTo(returnStart + column * sprites.BridgeFrames + sample));
                }
                last = frame;
                lastPhase = phase;
            }
            Assert.That(chamberSeen, Is.True);
            Assert.That(bridgeSeen, Is.True);
            Assert.That(activeTicks, Is.LessThanOrEqualTo(Mathf.CeilToInt(sprites.InterruptionReturnSeconds * 60) + 2));
            Assert.That(last, Is.EqualTo(sprites.FrameIndex(lastPhase, 1)), "The last return frame aliases the exact current-phase walking pose.");
        }

        [Test]
        public void PartlyCompletedReturnKeepsItsFirstFrameThenFinishesTheRemainingBridge()
        {
            int returnStart = sprites.Phases * sprites.BridgeFrames + sprites.StrikeFrames;
            int original = returnStart + 3 * sprites.BridgeFrames + 4;
            playback.RecordAttackFrame(sprites, original);
            Assert.That(playback.TrySelectRetractionFrame(sprites, .7f, 1f / 60, out int first), Is.True);
            Assert.That(first, Is.EqualTo(original));
            int previousSample = 4;
            int last = first;
            float lastPhase = 0;
            for (int tick = 0; tick < 30; tick++)
            {
                float phase = .7f + tick * .005f;
                if (!playback.TrySelectRetractionFrame(sprites, phase, 1f / 60, out int frame))
                {
                    break;
                }
                Assert.That(frame, Is.GreaterThanOrEqualTo(returnStart));
                int sample = (frame - returnStart) % sprites.BridgeFrames;
                Assert.That(sample, Is.GreaterThanOrEqualTo(previousSample));
                previousSample = sample;
                last = frame;
                lastPhase = phase;
            }
            Assert.That(last, Is.EqualTo(sprites.FrameIndex(lastPhase, 1)));
        }

        [Test]
        public void AnAlreadyCompletedAttackAddsNoExtraRetraction()
        {
            playback.RecordAttackFrame(sprites, sprites.FrameIndex(.25f, 1));
            Assert.That(playback.TrySelectRetractionFrame(sprites, .7f, 1f / 60, out _), Is.False);
        }

        [Test]
        public void ResetClearsOldPlaybackAndANewAttackReplacesAnInterruptedOne()
        {
            int entryCount = sprites.Phases * sprites.BridgeFrames;
            playback.RecordAttackFrame(sprites, entryCount + 4);
            Assert.That(playback.TrySelectRetractionFrame(sprites, .2f, 1f / 60, out _), Is.True);
            playback.Reset();
            Assert.That(playback.TrySelectRetractionFrame(sprites, .2f, 1f / 60, out _), Is.False);

            playback.RecordAttackFrame(sprites, 2);
            Assert.That(playback.TrySelectRetractionFrame(sprites, .2f, 1f / 60, out int preparation), Is.True);
            Assert.That(preparation, Is.EqualTo(2));
            playback.RecordAttackFrame(sprites, entryCount + 12);
            Assert.That(playback.TrySelectRetractionFrame(sprites, .2f, 1f / 60, out int strike), Is.True);
            Assert.That(strike, Is.EqualTo(entryCount + 12));
        }

        [Test]
        public void TwoActorsNeverShareRetractionHistory()
        {
            var other = new CreatureAttackSpritePlayback();
            playback.RecordAttackFrame(sprites, sprites.Phases * sprites.BridgeFrames + 10);
            Assert.That(other.TrySelectRetractionFrame(sprites, .5f, 1f / 60, out _), Is.False);
            Assert.That(playback.TrySelectRetractionFrame(sprites, .5f, 1f / 60, out _), Is.True);
        }
    }
}
