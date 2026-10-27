using System.Collections.Generic;
using EmergencyShift.Editor.Authoring;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class CreatureAttackSpriteSetTests
    {
        [Test]
        public void AttackSelectionKeepsTheCapturedGaitColumnAtBothEdges()
        {
            var set = ScriptableObject.CreateInstance<CreatureAttackSpriteSet>();
            try
            {
                for (int phase = 0; phase < 48; phase++)
                {
                    int returnStart = set.Phases * set.BridgeFrames + set.StrikeFrames;
                    int walkingEnd = returnStart + phase * set.BridgeFrames + set.BridgeFrames - 1;
                    Assert.That(set.FrameIndex(phase / 48f, 0), Is.EqualTo(phase * set.BridgeFrames));
                    Assert.That(set.FrameIndex(phase / 48f, 1), Is.EqualTo(walkingEnd));
                    Assert.That(set.FrameIndex(phase / 48f, 47 / 48f), Is.EqualTo(walkingEnd),
                        "The last sampled .8-second attack frame must already have completed its return.");
                }
                Assert.That(set.FrameIndex(1.25f, 0), Is.EqualTo(set.FrameIndex(.25f, 0)));
                Assert.That(set.FrameIndex(.25f, .42f, .42f), Is.EqualTo(set.FrameIndex(.25f, .62f, .62f)),
                    "The same painted strike peak follows each species' actual hit time.");
            }
            finally
            {
                Object.DestroyImmediate(set);
            }
        }

        [TestCase(.8f, .42f)]
        [TestCase(1.05f, .62f)]
        public void RealAttackDurationReservesEntryAndReturnWithoutMovingTheHit(float duration, float strikeAt)
        {
            var set = ScriptableObject.CreateInstance<CreatureAttackSpriteSet>();
            try
            {
                int entryCount = set.Phases * set.BridgeFrames;
                int returnStart = entryCount + set.StrikeFrames;
                float entryEnd = set.EntrySeconds / duration;
                float returnBegin = .97f - set.ReturnSeconds / duration;
                Assert.That(set.FrameIndex(0, entryEnd - .001f, strikeAt, duration), Is.EqualTo(set.BridgeFrames - 1));
                Assert.That(set.FrameIndex(0, returnBegin, strikeAt, duration), Is.EqualTo(returnStart));
                Assert.That(set.FrameIndex(0, strikeAt, strikeAt, duration), Is.EqualTo(entryCount + set.StrikePeakFrame));
                int prior = -1;
                for (int tick = 0; tick <= Mathf.CeilToInt(duration * 60); tick++)
                {
                    int frame = set.FrameIndex(0, tick / (duration * 60), strikeAt, duration);
                    if (frame < entryCount || frame >= returnStart)
                    {
                        bool sameEntry = frame < entryCount && prior >= 0 && prior < entryCount;
                        bool sameReturn = frame >= returnStart && prior >= returnStart;
                        if (sameEntry || sameReturn)
                        {
                            Assert.That(frame - prior, Is.InRange(0, 1), "At 60Hz the dense support bridge must not skip its adjacent drawings.");
                        }
                    }
                    prior = frame;
                }
            }
            finally
            {
                Object.DestroyImmediate(set);
            }
        }

        [Test]
        public void EveryPhaseJoinsTheExactWalkPoseAndSharedAttackEndpoints()
        {
            Load(out StalkerRigProfile profile, out StalkerSpriteSet sprites);
            StalkerGaitModel attackStart = StalkerAttackPoseComposer.Strike(profile, sprites, 0);
            StalkerGaitModel attackEnd = StalkerAttackPoseComposer.Strike(profile, sprites, 1);
            for (int phase = 0; phase < 48; phase++)
            {
                float position = phase / 48f;
                StalkerGaitModel walk = StalkerPoseComposer.Pose(profile, sprites, position, 0);
                SamePose(walk, StalkerAttackPoseComposer.Entry(profile, sprites, position, 0));
                SamePose(walk, StalkerAttackPoseComposer.Return(profile, sprites, position, 1));
                SamePose(attackStart, StalkerAttackPoseComposer.Entry(profile, sprites, position, 1));
                SamePose(attackEnd, StalkerAttackPoseComposer.Return(profile, sprites, position, 0));
            }
        }

        [Test]
        public void AllAttackChainsStayAboveTheFloorAndKeepTheirPaintedBend()
        {
            Load(out StalkerRigProfile profile, out StalkerSpriteSet sprites);
            foreach (StalkerGaitModel pose in Poses(profile, sprites))
            {
                int supportFeet = 0;
                for (int leg = 0; leg < 6; leg++)
                {
                    StalkerLimb limb = profile.Limbs[leg];
                    Assert.That(Vector3.Distance(pose.Hips[leg], pose.Knees[leg]), Is.EqualTo(limb.UpperLength).Within(.00001f));
                    Assert.That(Vector3.Distance(pose.Knees[leg], pose.Feet[leg]), Is.EqualTo(limb.LowerLength).Within(.00001f));
                    Assert.That(pose.Knees[leg].z, Is.GreaterThanOrEqualTo(-.000001f));
                    Assert.That(pose.Feet[leg].z, Is.GreaterThanOrEqualTo(-.000001f));
                    if (pose.Feet[leg].z <= .000001f)
                    {
                        supportFeet++;
                    }
                    Assert.That(Vector2.Distance(pose.Knees[leg], pose.Feet[leg]), Is.GreaterThan(.25f), "Distal spikes must remain visibly long.");
                    Vector2 sourceFoot = limb.Tip - limb.Hip;
                    Vector2 sourceKnee = limb.Knee - limb.Hip;
                    Vector2 foot = pose.Feet[leg] - pose.Hips[leg];
                    Vector2 knee = pose.Knees[leg] - pose.Hips[leg];
                    float original = sourceFoot.x * sourceKnee.y - sourceFoot.y * sourceKnee.x;
                    float actual = foot.x * knee.y - foot.y * knee.x;
                    Assert.That(original * actual, Is.GreaterThan(0));
                }
                Assert.That(supportFeet, Is.GreaterThanOrEqualTo(3),
                    "The front pair must wait for the non-attacking feet to land before lifting.");
            }
        }

        private static IEnumerable<StalkerGaitModel> Poses(StalkerRigProfile profile, StalkerSpriteSet sprites)
        {
            for (int phase = 0; phase < 48; phase++)
            {
                for (int sample = 0; sample < 15; sample++)
                {
                    yield return StalkerAttackPoseComposer.Entry(profile, sprites, phase / 48f, sample / 14f);
                    yield return StalkerAttackPoseComposer.Return(profile, sprites, phase / 48f, sample / 14f);
                }
            }
            for (int sample = 0; sample < 24; sample++)
            {
                yield return StalkerAttackPoseComposer.Strike(profile, sprites, sample / 23f);
            }
        }

        private static void SamePose(StalkerGaitModel expected, StalkerGaitModel actual)
        {
            for (int leg = 0; leg < 6; leg++)
            {
                Assert.That(actual.Hips[leg], Is.EqualTo(expected.Hips[leg]));
                Assert.That(actual.Knees[leg], Is.EqualTo(expected.Knees[leg]));
                Assert.That(actual.Feet[leg], Is.EqualTo(expected.Feet[leg]));
            }
        }

        private static void Load(out StalkerRigProfile profile, out StalkerSpriteSet sprites)
        {
            profile = AssetDatabase.LoadAssetAtPath<StalkerRigProfile>("Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset");
            sprites = AssetDatabase.LoadAssetAtPath<StalkerSpriteSet>("Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset");
        }
    }
}
