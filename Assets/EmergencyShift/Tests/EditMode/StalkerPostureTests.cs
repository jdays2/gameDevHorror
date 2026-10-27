using EmergencyShift.Editor.Authoring;
using EmergencyShift.Features.Enemies.Config;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class StalkerPostureTests
    {
        [Test]
        public void AllBodyPosesKeepTheirSixBoneChainsAboveTheFloor()
        {
            var profile = AssetDatabase.LoadAssetAtPath<StalkerRigProfile>("Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset");
            var sprites = AssetDatabase.LoadAssetAtPath<StalkerSpriteSet>("Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset");
            for (int row = 0; row < 9; row++)
            {
                for (int phase = 0; phase < 48; phase++)
                {
                    var pose = StalkerPoseComposer.Pose(profile, sprites, phase / 48f, row / 8f);
                    for (int leg = 0; leg < 6; leg++)
                    {
                        Assert.That(Vector3.Distance(pose.Hips[leg], pose.Knees[leg]),
                            Is.EqualTo(profile.Limbs[leg].UpperLength).Within(.00001f));
                        Assert.That(Vector3.Distance(pose.Knees[leg], pose.Feet[leg]),
                            Is.EqualTo(profile.Limbs[leg].LowerLength).Within(.00001f));
                        Assert.That(pose.Knees[leg].z, Is.GreaterThanOrEqualTo(0));
                        Assert.That(pose.Feet[leg].z, Is.GreaterThanOrEqualTo(0));
                        Assert.That(Vector2.Distance(pose.Knees[leg], pose.Feet[leg]) / profile.Limbs[leg].LowerLength,
                            Is.GreaterThan(.5f), "The distal projection must retain at least half of its anatomical length.");
                        Vector2 sourceFoot = profile.Limbs[leg].Tip - profile.Limbs[leg].Hip;
                        Vector2 sourceKnee = profile.Limbs[leg].Knee - profile.Limbs[leg].Hip;
                        Vector2 foot = pose.Feet[leg] - pose.Hips[leg];
                        Vector2 knee = pose.Knees[leg] - pose.Hips[leg];
                        float sourceCross = sourceFoot.x * sourceKnee.y - sourceFoot.y * sourceKnee.x;
                        float actualCross = foot.x * knee.y - foot.y * knee.x;
                        Assert.That(sourceCross * actualCross, Is.GreaterThan(0),
                            "A fold must not flip the painted surface inside out.");
                    }
                    for (int side = 0; side < 2; side++)
                    {
                        Assert.That(pose.Knees[side].y, Is.GreaterThan(pose.Knees[side + 2].y + .1f),
                            "Front and middle knees must occupy separate sectors.");
                        Assert.That(pose.Knees[side + 4].y, Is.LessThan(pose.Knees[side + 2].y - .1f),
                            "Hind and middle knees must occupy separate sectors.");
                    }
                }
            }
        }

        [Test]
        public void SupportFeetStayFixedWhenTheRootTravelsOneBakedCycle()
        {
            var profile = AssetDatabase.LoadAssetAtPath<StalkerRigProfile>("Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset");
            var sprites = AssetDatabase.LoadAssetAtPath<StalkerSpriteSet>("Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset");
            const float step = .0001f;
            foreach (float fold in new[] { 0f, sprites.CompactWalkingFold })
            {
                float stride = fold == 0 ? 1 : StalkerPoseComposer.CompactStrideScale;
                for (int sample = 0; sample < 48; sample++)
                {
                    float phase = (sample + .5f) / 48;
                    var before = StalkerPoseComposer.Pose(profile, sprites, phase - step, fold);
                    var after = StalkerPoseComposer.Pose(profile, sprites, phase + step, fold);
                    for (int leg = 0; leg < 6; leg++)
                    {
                        bool middle = leg >= 2 && leg < 4;
                        float shift = middle ? StalkerPoseComposer.MiddlePhaseShift : 0;
                        float support = StalkerPoseComposer.FrontSupportFraction;
                        if (middle)
                        {
                            support = StalkerPoseComposer.MiddleSupportFraction;
                        }
                        else if (leg >= 4)
                        {
                            support = StalkerPoseComposer.HindSupportFraction;
                        }
                        float local = Mathf.Repeat(phase + profile.Limbs[leg].PhaseOffset + shift, 1);
                        if (local <= step || local >= support - step)
                        {
                            continue;
                        }
                        float rootTravel = 2 * step * StalkerPoseComposer.CycleDistance * stride;
                        Vector3 drift = after.Feet[leg] - before.Feet[leg] + Vector3.up * rootTravel;
                        Assert.That(drift.magnitude, Is.LessThan(.00002f),
                            "Changing cadence must not make planted feet slide through the world.");
                        Assert.That(after.Feet[leg].z, Is.EqualTo(0).Within(.000001f));
                    }
                }
            }
        }

        [Test]
        public void FullyFoldedBodyHasNoResidualWalkingPhase()
        {
            var profile = AssetDatabase.LoadAssetAtPath<StalkerRigProfile>("Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset");
            var sprites = AssetDatabase.LoadAssetAtPath<StalkerSpriteSet>("Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset");
            var first = StalkerPoseComposer.Pose(profile, sprites, 0, 1);
            for (int leg = 0; leg < 6; leg++)
            {
                if (leg < 2)
                {
                    Assert.That(first.Feet[leg].y, Is.GreaterThan(first.Knees[leg].y),
                        "The native closed fore spike points forward from its knee.");
                }
                else if (leg >= 4)
                {
                    Assert.That(first.Feet[leg].y, Is.LessThan(first.Knees[leg].y),
                        "The native closed hind spike points backward from its knee.");
                }
            }
            for (int phase = 1; phase < 48; phase++)
            {
                var actual = StalkerPoseComposer.Pose(profile, sprites, phase / 48f, 1);
                for (int leg = 0; leg < 6; leg++)
                {
                    Assert.That(Vector3.Distance(actual.Feet[leg], first.Feet[leg]), Is.LessThan(.00001f));
                    Assert.That(Vector3.Distance(actual.Knees[leg], first.Knees[leg]), Is.LessThan(.00001f));
                }
            }
        }
    }
}
