using System;
using System.Collections.Generic;
using System.IO;
using EmergencyShift.Editor.Authoring;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class ExportCreatureAttackAdjacent05
{
    private const string Folder = "Concepts/Used/Stalker/Postures05/AttackAdjacent01/";
    private const int Phases = 48;
    private const int BridgeFrames = 15;
    private const int StrikeFrames = 24;

    public static string Export()
    {
        var profile = AssetDatabase.LoadAssetAtPath<StalkerRigProfile>("Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset");
        var sprites = AssetDatabase.LoadAssetAtPath<StalkerSpriteSet>("Assets/EmergencyShift/Data/Enemies/StalkerSprites.asset");
        profile.Validate();
        var poses = new List<object>();
        float maxBoneError = 0;
        float minimumHeight = float.MaxValue;
        int reversedProjectedBends = 0;
        for (int phase = 0; phase < Phases; phase++)
        {
            for (int sample = 0; sample < BridgeFrames; sample++)
            {
                StalkerGaitModel pose = StalkerAttackPoseComposer.Entry(profile, sprites, phase / (float)Phases, sample / (float)(BridgeFrames - 1));
                Add(poses, "entry-" + phase.ToString("D2") + "-" + sample.ToString("D2"), "entry", phase, sample, pose);
                ValidatePose(profile, pose, ref maxBoneError, ref minimumHeight, ref reversedProjectedBends);
            }
        }
        for (int sample = 0; sample < StrikeFrames; sample++)
        {
            StalkerGaitModel pose = StalkerAttackPoseComposer.Strike(profile, sprites, sample / (float)(StrikeFrames - 1));
            Add(poses, "strike-" + sample.ToString("D2"), "strike", -1, sample, pose);
            ValidatePose(profile, pose, ref maxBoneError, ref minimumHeight, ref reversedProjectedBends);
        }
        for (int phase = 0; phase < Phases; phase++)
        {
            for (int sample = 0; sample < BridgeFrames; sample++)
            {
                StalkerGaitModel pose = StalkerAttackPoseComposer.Return(profile, sprites, phase / (float)Phases, sample / (float)(BridgeFrames - 1));
                Add(poses, "return-" + phase.ToString("D2") + "-" + sample.ToString("D2"), "return", phase, sample, pose);
                ValidatePose(profile, pose, ref maxBoneError, ref minimumHeight, ref reversedProjectedBends);
            }
        }
        if (maxBoneError > .00001f || minimumHeight < -.00001f || reversedProjectedBends > 0)
        {
            throw new InvalidOperationException("Attack pose failed anatomy validation: length=" + maxBoneError + ", height=" + minimumHeight + ", projected bends=" + reversedProjectedBends);
        }
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "attack-poses.json", JsonConvert.SerializeObject(poses));
        File.WriteAllText(Folder + "attack-pose-validation.json", JsonConvert.SerializeObject(new
        {
            poses = poses.Count,
            phases = Phases,
            bridgeFrames = BridgeFrames,
            strikeFrames = StrikeFrames,
            maxBoneError,
            minimumHeight,
            reversedProjectedBends,
            torsoChanged = false,
            runtimeIk = false,
            entryEndpoint = "StalkerPoseComposer.Pose(profile,sprites,phase,0)",
            returnEndpoint = "StalkerPoseComposer.Pose(profile,sprites,phase,0)"
        }, Formatting.Indented));
        return poses.Count + " whole-body attack poses exported; bone error " + maxBoneError;
    }

    private static void Add(List<object> poses, string name, string group, int phase, int sample, StalkerGaitModel pose)
    {
        poses.Add(new
        {
            name,
            phase,
            group,
            sample,
            hips = Array.ConvertAll(pose.Hips, p => new[] { p.x, p.y, p.z }),
            knees = Array.ConvertAll(pose.Knees, p => new[] { p.x, p.y, p.z }),
            feet = Array.ConvertAll(pose.Feet, p => new[] { p.x, p.y, p.z })
        });
    }

    private static void ValidatePose(StalkerRigProfile profile, StalkerGaitModel pose, ref float lengthError, ref float height, ref int reversedBends)
    {
        for (int leg = 0; leg < 6; leg++)
        {
            StalkerLimb limb = profile.Limbs[leg];
            Vector3 hip = pose.Hips[leg];
            Vector3 knee = pose.Knees[leg];
            Vector3 foot = pose.Feet[leg];
            lengthError = Mathf.Max(lengthError, Mathf.Abs(Vector3.Distance(hip, knee) - limb.UpperLength));
            lengthError = Mathf.Max(lengthError, Mathf.Abs(Vector3.Distance(knee, foot) - limb.LowerLength));
            height = Mathf.Min(height, Mathf.Min(knee.z, foot.z));
            Vector2 sourceFoot = limb.Tip - limb.Hip;
            Vector2 sourceKnee = limb.Knee - limb.Hip;
            float original = sourceFoot.x * sourceKnee.y - sourceFoot.y * sourceKnee.x;
            Vector3 currentFoot = foot - hip;
            Vector3 currentKnee = knee - hip;
            float current = currentFoot.x * currentKnee.y - currentFoot.y * currentKnee.x;
            if (original * current <= 0)
            {
                reversedBends++;
            }
        }
    }
}
