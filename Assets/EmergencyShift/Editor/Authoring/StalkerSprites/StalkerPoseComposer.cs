using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using UnityEngine;

namespace EmergencyShift.Editor.Authoring
{
    /// <summary>Authoring poses for whole sprites; never runs in the Player.</summary>
    public static class StalkerPoseComposer
    {
        // These lengths describe the baked artwork. Installers copy them into
        // the runtime playback profiles only after the matching art is ready.
        public const float CycleDistance = 1.25f;
        public const float CompactStrideScale = .8f;
        public const float FrontSupportFraction = .65f;
        public const float HindSupportFraction = .56f;
        public const float MiddleSupportFraction = .35f;
        public const float MiddlePhaseShift = .35f;
        private const float FoldedBodyHeight = .2f;

        public static StalkerGaitModel Pose(StalkerRigProfile profile, StalkerSpriteSet sprites, float phase, float fold)
        {
            fold = Mathf.Clamp01(fold);
            float walkingFold = Mathf.Min(fold, sprites.CompactWalkingFold);
            float tuck = Mathf.InverseLerp(sprites.CompactWalkingFold, 1, fold);
            float strideScale = Mathf.Lerp(1, CompactStrideScale, walkingFold / sprites.CompactWalkingFold) * (1 - tuck);
            float height = Mathf.Lerp(profile.BodyHeight, FoldedBodyHeight, fold);
            var pose = new StalkerGaitModel(profile.Limbs, 1, CycleDistance, FrontSupportFraction, profile.LiftHeight, height, 0);
            for (int leg = 0; leg < 6; leg++)
            {
                StalkerLimb limb = profile.Limbs[leg];
                float side = leg % 2 == 0 ? -1 : 1;
                float supportFraction = FrontSupportFraction;
                if (leg >= 2 && leg < 4)
                {
                    supportFraction = MiddleSupportFraction;
                }
                else if (leg >= 4)
                {
                    supportFraction = HindSupportFraction;
                }
                // During support, local foot displacement equals root travel.
                float legAmplitude = supportFraction * .5f * CycleDistance * strideScale;
                float phaseShift = leg >= 2 && leg < 4 ? MiddlePhaseShift : 0;
                float localPhase = Mathf.Repeat(phase + limb.PhaseOffset + phaseShift, 1);
                float offset;
                float lift = 0;
                if (localPhase < supportFraction)
                {
                    offset = Mathf.Lerp(legAmplitude, -legAmplitude, localPhase / supportFraction);
                }
                else
                {
                    float swing = (localPhase - supportFraction) / (1 - supportFraction);
                    offset = Mathf.Lerp(-legAmplitude, legAmplitude, Mathf.SmoothStep(0, 1, swing));
                    lift = Mathf.Sin(swing * Mathf.PI) * profile.LiftHeight * (1 - walkingFold) * (1 - tuck);
                }

                Vector2 mount = Attachment(limb.Hip, leg);
                FootLane(leg, mount, side, out Vector2 extended, out Vector2 guarded);
                Vector3 hip = (Vector3)mount + Vector3.forward * height;
                Vector3 foot = Vector2.Lerp(extended, guarded, walkingFold) + Vector2.up * offset;
                foot.z = lift;
                Vector3 knee = GroundedKnee(limb, hip, ref foot);
                if (tuck > 0)
                {
                    Vector3 groundedUpper = (knee - hip).normalized;
                    Vector3 groundedLower = (foot - knee).normalized;
                    FoldedDirections(leg, side, limb, out Vector3 upper, out Vector3 lower);
                    // The middle pair clears the hind pair before folding inward.
                    float limbTuck = tuck;
                    if (leg >= 2 && leg < 4)
                    {
                        limbTuck = tuck * (2 - tuck);
                    }
                    knee = hip + Vector3.Slerp(groundedUpper, upper, limbTuck) * limb.UpperLength;
                    foot = knee + Vector3.Slerp(groundedLower, lower, limbTuck) * limb.LowerLength;
                }
                pose.Hips[leg] = hip;
                pose.Knees[leg] = knee;
                pose.Feet[leg] = foot;
            }
            return pose;
        }

        private static void FootLane(int leg, Vector2 hip, float side, out Vector2 extended, out Vector2 guarded)
        {
            if (leg < 2)
            {
                extended = hip + new Vector2(-side * .04f, .48f);
                guarded = hip + new Vector2(-side * .035f, .30f);
            }
            else if (leg < 4)
            {
                extended = hip + new Vector2(side * .56f, -.30f);
                guarded = hip + new Vector2(side * .50f, -.30f);
            }
            else
            {
                extended = hip + new Vector2(-side * .11f, -.48f);
                guarded = hip + new Vector2(-side * .09f, -.30f);
            }
        }

        private static Vector2 Attachment(Vector2 sourceHip, int leg)
        {
            // Version 05 places the articulation beneath the torso. The old
            // anchor sat at the exposed skin bulb rather than its inner mount.
            if (leg < 2)
            {
                return sourceHip * .4f;
            }
            if (leg < 4)
            {
                return sourceHip * .4f;
            }
            return sourceHip * .72f;
        }

        private static Vector3 GroundedKnee(StalkerLimb limb, Vector3 hip, ref Vector3 foot)
        {
            // A free spatial pole can lift the knee above the toe, visually
            // deleting its distal segment. Keep loaded knees between hip/toe heights.
            float kneeHeight = (hip.z + foot.z) * .5f;
            float upperProjection = Mathf.Sqrt(limb.UpperLength * limb.UpperLength - Mathf.Pow(hip.z - kneeHeight, 2));
            float lowerProjection = Mathf.Sqrt(limb.LowerLength * limb.LowerLength - Mathf.Pow(foot.z - kneeHeight, 2));
            Vector2 delta = foot - hip;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(upperProjection - lowerProjection) + .0001f,
                upperProjection + lowerProjection - .0001f);
            Vector2 axis = delta.normalized;
            Vector2 projectedFoot = (Vector2)hip + axis * distance;
            foot.x = projectedFoot.x;
            foot.y = projectedFoot.y;
            float along = (upperProjection * upperProjection - lowerProjection * lowerProjection + distance * distance) / (2 * distance);
            float radius = Mathf.Sqrt(Mathf.Max(0, upperProjection * upperProjection - along * along));
            Vector2 sourceFoot = limb.Tip - limb.Hip;
            Vector2 sourceKnee = limb.Knee - limb.Hip;
            float bendSide = Mathf.Sign(sourceFoot.x * sourceKnee.y - sourceFoot.y * sourceKnee.x);
            Vector2 point = (Vector2)hip + axis * along + new Vector2(-axis.y, axis.x) * (radius * bendSide);
            return new Vector3(point.x, point.y, kneeHeight);
        }

        private static void FoldedDirections(int leg, float side, StalkerLimb limb, out Vector3 upper, out Vector3 lower)
        {
            // The native closed pose keeps the fore spikes pointing forward
            // and the hind spikes backward. Reversing those directions makes
            // the lower material look severed beside the knee.
            Vector2 knee = new Vector2(side * .34f, .20f);
            Vector2 toe = new Vector2(side * .068f, .411f);
            if (leg >= 2 && leg < 4)
            {
                knee = new Vector2(side * .38f, .06f);
                toe = new Vector2(side * .13f, -.10f);
            }
            else if (leg >= 4)
            {
                knee = new Vector2(side * .34f, -.154f);
                toe = new Vector2(side * .06f, -.349f);
            }
            Vector2 hip = Attachment(limb.Hip, leg);
            Vector2 proximal = knee - hip;
            float rise = Mathf.Sqrt(limb.UpperLength * limb.UpperLength - proximal.sqrMagnitude);
            upper = new Vector3(proximal.x, proximal.y, rise) / limb.UpperLength;
            Vector2 distal = toe - knee;
            float drop = Mathf.Sqrt(limb.LowerLength * limb.LowerLength - distal.sqrMagnitude);
            lower = new Vector3(distal.x, distal.y, -drop) / limb.LowerLength;
        }
    }
}
