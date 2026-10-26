using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using UnityEngine;

namespace EmergencyShift.Editor.Authoring
{
    /// <summary>Offline key poses. Feet remain in their own lanes; a folded body has no walking cycle.</summary>
    public static class StalkerPoseComposer
    {
        private const float SupportFraction = .6f;

        public static StalkerGaitModel Pose(StalkerRigProfile profile, StalkerSpriteSet sprites, float phase, float fold)
        {
            var pose = new StalkerGaitModel(profile.Limbs, 1, 1, SupportFraction, .075f, .13f, 0);
            float height = Mathf.Lerp(profile.BodyHeight, .2f, fold);
            float amplitude = .3f * sprites.StrideScale(fold);
            if (fold > sprites.CompactWalkingFold)
            {
                amplitude *= 1 - Mathf.InverseLerp(sprites.CompactWalkingFold, 1, fold);
            }
            for (int leg = 0; leg < 6; leg++)
            {
                StalkerLimb limb = profile.Limbs[leg];
                float local = Mathf.Repeat(phase + limb.PhaseOffset, 1);
                float offset;
                float lift = 0;
                if (local < SupportFraction)
                {
                    offset = Mathf.Lerp(amplitude, -amplitude, local / SupportFraction);
                }
                else
                {
                    float swing = (local - SupportFraction) / (1 - SupportFraction);
                    offset = Mathf.Lerp(-amplitude, amplitude, Mathf.SmoothStep(0, 1, swing));
                    lift = Mathf.Sin(swing * Mathf.PI) * profile.LiftHeight * (1 - fold);
                }
                Vector2 extended;
                Vector2 closed;
                Vector2 kneePole;
                float side = leg % 2 == 0 ? -1 : 1;
                if (leg < 2)
                {
                    extended = limb.Hip + new Vector2(-side * .08f, .59f);
                    closed = limb.Hip + new Vector2(-side * .07f, .07f);
                    kneePole = new Vector2(side, 1);
                }
                else if (leg < 4)
                {
                    extended = new Vector2(limb.Hip.x + side * .49f, limb.Hip.y - .33f);
                    closed = limb.Hip + new Vector2(0, -.08f);
                    kneePole = new Vector2(side, 0);
                }
                else
                {
                    extended = limb.Hip + new Vector2(-side * .07f, -.59f);
                    closed = limb.Hip + new Vector2(-side * .07f, -.07f);
                    kneePole = new Vector2(side, -1);
                }
                Vector3 hip = (Vector3)limb.Hip + Vector3.forward * height;
                Vector3 foot = Vector2.Lerp(extended, closed, fold) + Vector2.up * offset;
                foot.z = lift;
                // Each pair bends into its own outward sector even when the
                // foot passes under the torso. A foot-relative pole lets the
                // middle knee swing forward across the front leg.
                pose.Hips[leg] = hip;
                pose.Knees[leg] = StalkerGaitModel.SolveKnee(hip, ref foot, kneePole, limb.UpperLength, limb.LowerLength);
                pose.Feet[leg] = foot;
            }
            return pose;
        }
    }
}
