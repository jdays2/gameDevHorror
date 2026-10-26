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
            var profile=AssetDatabase.LoadAssetAtPath<StalkerRigProfile>("Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset");
            var sprites=AssetDatabase.LoadAssetAtPath<StalkerSpriteSet>("Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset");
            for (int row=0;row<9;row++)
            {
                for (int phase=0;phase<48;phase++)
                {
                    var pose=StalkerPoseComposer.Pose(profile,sprites,phase/48f,row/8f);
                    for (int leg=0;leg<6;leg++)
                    {
                        Assert.That(Vector3.Distance(pose.Hips[leg],pose.Knees[leg]),Is.EqualTo(profile.Limbs[leg].UpperLength).Within(.00001f));
                        Assert.That(Vector3.Distance(pose.Knees[leg],pose.Feet[leg]),Is.EqualTo(profile.Limbs[leg].LowerLength).Within(.00001f));
                        Assert.That(pose.Knees[leg].z,Is.GreaterThanOrEqualTo(0));
                        Vector2 sourceFoot=profile.Limbs[leg].Tip-profile.Limbs[leg].Hip;
                        Vector2 sourceKnee=profile.Limbs[leg].Knee-profile.Limbs[leg].Hip;
                        Vector2 foot=pose.Feet[leg]-pose.Hips[leg];
                        Vector2 knee=pose.Knees[leg]-pose.Hips[leg];
                        float sourceCross=sourceFoot.x*sourceKnee.y-sourceFoot.y*sourceKnee.x;
                        float actualCross=foot.x*knee.y-foot.y*knee.x;
                        Assert.That(sourceCross*actualCross,Is.GreaterThan(0),"A fold must not flip the painted surface inside out.");
                    }
                    for (int side=0;side<2;side++)
                    {
                        Assert.That(pose.Knees[side].y,Is.GreaterThan(pose.Knees[side+2].y+.1f),"Front and middle knees must occupy separate sectors.");
                        Assert.That(pose.Knees[side+4].y,Is.LessThan(pose.Knees[side+2].y-.1f),"Hind and middle knees must occupy separate sectors.");
                    }
                }
            }
        }

        [Test]
        public void FullyFoldedBodyHasNoResidualWalkingPhase()
        {
            var profile=AssetDatabase.LoadAssetAtPath<StalkerRigProfile>("Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset");
            var sprites=AssetDatabase.LoadAssetAtPath<StalkerSpriteSet>("Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset");
            var first=StalkerPoseComposer.Pose(profile,sprites,0,1);
            for (int phase=1;phase<48;phase++)
            {
                var actual=StalkerPoseComposer.Pose(profile,sprites,phase/48f,1);
                for (int leg=0;leg<6;leg++)
                {
                    Assert.That(Vector3.Distance(actual.Feet[leg],first.Feet[leg]),Is.LessThan(.00001f));
                    Assert.That(Vector3.Distance(actual.Knees[leg],first.Knees[leg]),Is.LessThan(.00001f));
                }
            }
        }
    }
}
