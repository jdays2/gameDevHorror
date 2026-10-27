using System;
using System.Collections.Generic;
using System.IO;
using EmergencyShift.Editor.Authoring;
using EmergencyShift.Features.Enemies.Config;
using UnityEditor;
using UnityEngine;

public static class ExportStalkerPoses05
{
    public static object Export()
    {
        var profile = AssetDatabase.LoadAssetAtPath<StalkerRigProfile>("Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset");
        var sprites = AssetDatabase.LoadAssetAtPath<StalkerSpriteSet>("Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset");
        var poses = new List<object>();
        float maxBoneError = 0;
        float minDistalProjection = float.MaxValue;
        float minFootHeight = float.MaxValue;
        float minWinding = float.MaxValue;
        for (int row = 0; row < 9; row++)
        {
            for (int phase = 0; phase < 48; phase++)
            {
                var pose = StalkerPoseComposer.Pose(profile, sprites, phase / 48f, row / 8f);
                for (int leg = 0; leg < 6; leg++)
                {
                    StalkerLimb limb = profile.Limbs[leg];
                    maxBoneError = Mathf.Max(maxBoneError, Mathf.Abs(Vector3.Distance(pose.Hips[leg], pose.Knees[leg]) - limb.UpperLength));
                    maxBoneError = Mathf.Max(maxBoneError, Mathf.Abs(Vector3.Distance(pose.Knees[leg], pose.Feet[leg]) - limb.LowerLength));
                    minDistalProjection = Mathf.Min(minDistalProjection, Vector2.Distance(pose.Knees[leg], pose.Feet[leg]));
                    minFootHeight = Mathf.Min(minFootHeight, pose.Feet[leg].z);
                    Vector2 foot = pose.Feet[leg] - pose.Hips[leg];
                    Vector2 knee = pose.Knees[leg] - pose.Hips[leg];
                    Vector2 oldFoot = limb.Tip - limb.Hip;
                    Vector2 oldKnee = limb.Knee - limb.Hip;
                    minWinding = Mathf.Min(minWinding, (foot.x * knee.y - foot.y * knee.x) * (oldFoot.x * oldKnee.y - oldFoot.y * oldKnee.x));
                }
                poses.Add(new { row, phase,
                    hips = Array.ConvertAll(pose.Hips, p => new[] { p.x, p.y, p.z }),
                    knees = Array.ConvertAll(pose.Knees, p => new[] { p.x, p.y, p.z }),
                    feet = Array.ConvertAll(pose.Feet, p => new[] { p.x, p.y, p.z }) });
            }
        }
        const string folder = "Concepts/Used/Stalker/Postures05/";
        File.WriteAllText(folder + "poses.json", Newtonsoft.Json.JsonConvert.SerializeObject(poses));
        var result = new
        {
            count = poses.Count,
            maxBoneError,
            minDistalProjection,
            minFootHeight,
            minWinding,
            cycleDistance = StalkerPoseComposer.CycleDistance,
            compactStrideScale = StalkerPoseComposer.CompactStrideScale,
            frontSupportFraction = StalkerPoseComposer.FrontSupportFraction,
            hindSupportFraction = StalkerPoseComposer.HindSupportFraction,
            middleSupportFraction = StalkerPoseComposer.MiddleSupportFraction,
            middlePhaseShift = StalkerPoseComposer.MiddlePhaseShift,
            runtimeIk = false
        };
        File.WriteAllText(folder + "bones.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
