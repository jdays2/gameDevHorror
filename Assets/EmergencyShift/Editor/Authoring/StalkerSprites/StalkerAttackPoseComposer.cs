using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using UnityEngine;

namespace EmergencyShift.Editor.Authoring
{
    /// <summary>Offline whole-body attack poses from the same six anatomical chains as walking.</summary>
    public static class StalkerAttackPoseComposer
    {
        private const float SupportTransferFraction = .3f;

        public static StalkerGaitModel Entry(StalkerRigProfile profile, StalkerSpriteSet sprites, float phase, float progress)
        {
            StalkerGaitModel walking = StalkerPoseComposer.Pose(profile, sprites, phase, 0);
            StalkerGaitModel supported = SettleSupport(profile, walking);
            if (progress < SupportTransferFraction)
            {
                return Blend(profile, walking, supported, Mathf.SmoothStep(0, 1, progress / SupportTransferFraction));
            }
            StalkerGaitModel chambered = FrontPose(profile, sprites, false);
            float lift = (progress - SupportTransferFraction) / (1 - SupportTransferFraction);
            return Blend(profile, supported, chambered, Mathf.SmoothStep(0, 1, lift));
        }

        public static StalkerGaitModel Strike(StalkerRigProfile profile, StalkerSpriteSet sprites, float progress)
        {
            StalkerGaitModel chambered = FrontPose(profile, sprites, false);
            StalkerGaitModel extended = FrontPose(profile, sprites, true);
            float reach;
            if (progress <= .4f)
            {
                reach = Mathf.SmoothStep(0, 1, progress / .4f);
            }
            else
            {
                reach = Mathf.SmoothStep(1, 0, (progress - .4f) / .6f);
            }
            return Blend(profile, chambered, extended, reach);
        }

        public static StalkerGaitModel Return(StalkerRigProfile profile, StalkerSpriteSet sprites, float phase, float progress)
        {
            StalkerGaitModel chambered = FrontPose(profile, sprites, false);
            StalkerGaitModel walking = StalkerPoseComposer.Pose(profile, sprites, phase, 0);
            StalkerGaitModel supported = SettleSupport(profile, walking);
            float settledUntil = 1 - SupportTransferFraction;
            if (progress < settledUntil)
            {
                return Blend(profile, chambered, supported, Mathf.SmoothStep(0, 1, progress / settledUntil));
            }
            float release = (progress - settledUntil) / SupportTransferFraction;
            return Blend(profile, supported, walking, Mathf.SmoothStep(0, 1, release));
        }

        private static StalkerGaitModel SettleSupport(StalkerRigProfile profile, StalkerGaitModel source)
        {
            var pose = new StalkerGaitModel(profile.Limbs, 1, 1, .6f, profile.LiftHeight, profile.BodyHeight, 0);
            for (int leg = 0; leg < 6; leg++)
            {
                pose.Hips[leg] = source.Hips[leg];
                pose.Knees[leg] = source.Knees[leg];
                pose.Feet[leg] = source.Feet[leg];
                if (leg >= 2)
                {
                    Vector3 foot = source.Feet[leg];
                    foot.z = 0;
                    pose.Knees[leg] = Knee(profile.Limbs[leg], pose.Hips[leg], ref foot, pose.Hips[leg].z * .5f);
                    pose.Feet[leg] = foot;
                }
            }
            return pose;
        }

        private static StalkerGaitModel FrontPose(StalkerRigProfile profile, StalkerSpriteSet sprites, bool striking)
        {
            // Settle the four non-attacking feet before lifting the front pair.
            // The walking middle pair has a shorter support window, so its
            // sampled phase is not necessarily grounded at attack preparation.
            StalkerGaitModel pose = StalkerPoseComposer.Pose(profile, sprites, 0, 0);
            for (int leg = 2; leg < 6; leg++)
            {
                Vector3 foot = pose.Feet[leg];
                foot.z = 0;
                pose.Knees[leg] = Knee(profile.Limbs[leg], pose.Hips[leg], ref foot, pose.Hips[leg].z * .5f);
                pose.Feet[leg] = foot;
            }
            for (int leg = 0; leg < 2; leg++)
            {
                float side = leg == 0 ? -1 : 1;
                Vector3 upper = new Vector3(side * .30f, .34f, .04f);
                Vector3 lower = new Vector3(-side * .18f, .18f, -.02f);
                if (striking)
                {
                    upper = new Vector3(side * .24f, .38f, -.04f);
                    lower = new Vector3(-side * .18f, .39f, -.06f);
                }
                StalkerLimb limb = profile.Limbs[leg];
                pose.Knees[leg] = pose.Hips[leg] + upper.normalized * limb.UpperLength;
                pose.Feet[leg] = pose.Knees[leg] + lower.normalized * limb.LowerLength;
            }
            return pose;
        }

        private static StalkerGaitModel Blend(StalkerRigProfile profile, StalkerGaitModel from, StalkerGaitModel to, float progress)
        {
            if (progress <= 0)
            {
                return from;
            }
            if (progress >= 1)
            {
                return to;
            }
            var pose = new StalkerGaitModel(profile.Limbs, 1, 1, .6f, profile.LiftHeight, profile.BodyHeight, 0);
            for (int leg = 0; leg < 6; leg++)
            {
                StalkerLimb limb = profile.Limbs[leg];
                pose.Hips[leg] = Vector3.Lerp(from.Hips[leg], to.Hips[leg], progress);
                Vector3 foot = Vector3.Lerp(from.Feet[leg], to.Feet[leg], progress);
                float kneeHeight = Mathf.Lerp(from.Knees[leg].z, to.Knees[leg].z, progress);
                pose.Knees[leg] = Knee(limb, pose.Hips[leg], ref foot, kneeHeight);
                pose.Feet[leg] = foot;
            }
            return pose;
        }

        private static Vector3 Knee(StalkerLimb limb, Vector3 hip, ref Vector3 foot, float height)
        {
            // Interpolate grounded contacts and heights before reconstructing the two exact
            // segment lengths. Slerping independent segment directions can dip a toe below floor.
            float upper = Mathf.Sqrt(Mathf.Max(0, limb.UpperLength * limb.UpperLength - Mathf.Pow(hip.z - height, 2)));
            float lower = Mathf.Sqrt(Mathf.Max(0, limb.LowerLength * limb.LowerLength - Mathf.Pow(foot.z - height, 2)));
            Vector2 delta = foot - hip;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(upper - lower) + .0001f, upper + lower - .0001f);
            Vector2 axis = delta.normalized;
            Vector2 contact = (Vector2)hip + axis * distance;
            foot.x = contact.x;
            foot.y = contact.y;
            float along = (upper * upper - lower * lower + distance * distance) / (2 * distance);
            float radius = Mathf.Sqrt(Mathf.Max(0, upper * upper - along * along));
            Vector2 sourceFoot = limb.Tip - limb.Hip;
            Vector2 sourceKnee = limb.Knee - limb.Hip;
            float bend = Mathf.Sign(sourceFoot.x * sourceKnee.y - sourceFoot.y * sourceKnee.x);
            Vector2 point = (Vector2)hip + axis * along + new Vector2(-axis.y, axis.x) * (radius * bend);
            return new Vector3(point.x, point.y, height);
        }
    }
}
