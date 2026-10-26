using System;
using System.Collections.Generic;
using EmergencyShift.Features.Enemies.Config;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    /// <summary>Instance-owned presentation kinematics. Never moves the authoritative actor.</summary>
    public sealed class StalkerGaitModel
    {
        private const float ReachSafetyFraction = .88f;
        private readonly StalkerLimb[] anatomy;
        private readonly float scale;
        private readonly float cycleDistance;
        private readonly float supportFraction;
        private readonly float liftHeight;
        private readonly float bodyHeight;
        private readonly float weightTransfer;
        private readonly float initialPhase;
        private readonly Vector3[] takeoff = new Vector3[6];
        private readonly Vector3[] landing = new Vector3[6];
        private readonly float[] swingTime = new float[6];
        private readonly float[] swingDuration = new float[6];
        private readonly bool[] previousSwingWindow = new bool[6];
        private Vector2 previousPosition;
        private float previousHeading;
        private bool initialized;
        private float phase;
        private float weight;

        public Vector3[] Hips { get; } = new Vector3[6];
        public Vector3[] Knees { get; } = new Vector3[6];
        public Vector3[] Feet { get; } = new Vector3[6];
        public bool[] Swinging { get; } = new bool[6];
        public Vector2 BodyOffset { get; private set; }
        public int ReachCorrections { get; private set; }
        public float Phase => phase;

        public StalkerGaitModel(IReadOnlyList<StalkerLimb> anatomy, float scale, float cycleDistance,
            float supportFraction, float liftHeight, float bodyHeight, float weightTransfer, float initialPhase = 0)
        {
            if (anatomy == null || anatomy.Count != 6 || scale <= 0 || cycleDistance <= 0)
            {
                throw new ArgumentException("A Stalker needs six valid limb chains and a positive scale/stride.");
            }
            this.anatomy = new StalkerLimb[6];
            for (int i = 0; i < 6; i++)
            {
                this.anatomy[i] = anatomy[i];
            }
            this.scale = scale;
            this.cycleDistance = cycleDistance * scale;
            this.supportFraction = supportFraction;
            this.liftHeight = liftHeight * scale;
            this.bodyHeight = bodyHeight * scale;
            this.weightTransfer = weightTransfer * scale;
            this.initialPhase = Mathf.Repeat(initialPhase, 1);
        }

        public void Reset(Vector2 position, float heading)
        {
            previousPosition = position;
            previousHeading = heading;
            phase = initialPhase;
            weight = 0;
            BodyOffset = Vector2.zero;
            ReachCorrections = 0;
            for (int i = 0; i < 6; i++)
            {
                Feet[i] = World(anatomy[i].Tip, position, heading);
                Swinging[i] = false;
                previousSwingWindow[i] = false;
            }
            initialized = true;
        }

        public void Step(Vector2 position, float heading, float deltaTime, bool allowSteps, float compact)
        {
            if (deltaTime <= 0 && initialized)
            {
                return;
            }
            if (!initialized || Vector2.Distance(position, previousPosition) > cycleDistance * 3)
            {
                Reset(position, heading);
            }
            // Presentation may be sampled after several physics ticks or a slow
            // Editor frame. Reconstruct the measured pose interval in small steps
            // so a support can release before it becomes unreachable.
            Vector2 intervalStart = previousPosition;
            float headingStart = previousHeading;
            float headingChange = Mathf.DeltaAngle(headingStart, heading);
            int subdivisions = Mathf.Clamp(Mathf.CeilToInt(deltaTime * 120), 1, 60);
            for (int step = 1; step <= subdivisions; step++)
            {
                float fraction = (float)step / subdivisions;
                StepPose(Vector2.Lerp(intervalStart, position, fraction), headingStart + headingChange * fraction,
                    deltaTime / subdivisions, allowSteps, compact);
            }
        }

        private void StepPose(Vector2 position, float heading, float deltaTime, bool allowSteps, float compact)
        {
            float dt = Mathf.Max(.00001f, deltaTime);
            Vector2 velocity = (position - previousPosition) / dt;
            float turn = Mathf.DeltaAngle(previousHeading, heading);
            float travel = Vector2.Distance(position, previousPosition) + Mathf.Abs(turn) * Mathf.Deg2Rad * scale * .45f;
            bool moving = allowSteps && travel > .00001f;
            if (moving)
            {
                phase = Mathf.Repeat(phase + travel / cycleDistance, 1);
            }
            weight = Mathf.MoveTowards(weight, moving ? 1 : 0, dt * 8);
            // Transfer torso mass toward the supporting tripod; never move or scale the actor root.
            BodyOffset = Rotate(new Vector2(Mathf.Sin(phase * Mathf.PI * 2) * weightTransfer * weight, 0), heading);
            float height = bodyHeight * (1 - Mathf.Clamp01(compact) * .25f);
            for (int i = 0; i < 6; i++)
            {
                StalkerLimb limb = anatomy[i];
                Vector3 previousHip = Hips[i];
                Hips[i] = World(limb.Hip, position + BodyOffset, heading) + Vector3.forward * height;
                float localPhase = Mathf.Repeat(phase + limb.PhaseOffset, 1);
                bool swingWindow = localPhase >= supportFraction;
                float reach = Vector3.Distance(Hips[i], Feet[i]);
                bool extending = Vector3.Dot(Hips[i] - Feet[i], Hips[i] - previousHip) > 0;
                bool overreach = extending && reach > (limb.UpperLength + limb.LowerLength) * scale * ReachSafetyFraction;
                if (!Swinging[i] && allowSteps && ((moving && swingWindow && !previousSwingWindow[i]) || overreach))
                {
                    Swinging[i] = true;
                    takeoff[i] = Feet[i];
                    swingTime[i] = 0;
                    swingDuration[i] = Mathf.Clamp(cycleDistance * (1 - supportFraction) / Mathf.Max(.2f, velocity.magnitude), .065f, .24f);
                    float lead = cycleDistance * supportFraction * .42f;
                    Vector2 direction = velocity.sqrMagnitude > .001f ? velocity.normalized : Vector2.zero;
                    landing[i] = World(limb.Tip, position + direction * lead, heading);
                }
                previousSwingWindow[i] = swingWindow;
                if (Swinging[i])
                {
                    if (moving)
                    {
                        float duration = Mathf.Clamp(cycleDistance * (1 - supportFraction) / Mathf.Max(.2f, velocity.magnitude), .065f, .24f);
                        if (duration < swingDuration[i])
                        {
                            // A recoil can reverse and accelerate the root while
                            // this leg is already airborne. Keep its progress but
                            // finish at the new cadence before it falls behind.
                            swingTime[i] = swingTime[i] / swingDuration[i] * duration;
                            swingDuration[i] = duration;
                        }
                        // The airborne endpoint can adapt to a turn or a speed
                        // change. A planted endpoint never follows this target.
                        Vector2 direction = velocity.sqrMagnitude > .001f ? velocity.normalized : Vector2.zero;
                        landing[i] = World(limb.Tip, position + direction * (cycleDistance * supportFraction * .42f), heading);
                    }
                    swingTime[i] += dt;
                    float progress = Mathf.Clamp01(swingTime[i] / swingDuration[i]);
                    float eased = progress * progress * (3 - 2 * progress);
                    Feet[i] = Vector3.Lerp(takeoff[i], landing[i], eased);
                    Feet[i] += Vector3.forward * (Mathf.Sin(progress * Mathf.PI) * liftHeight);
                    if (progress >= 1)
                    {
                        Feet[i] = landing[i];
                        Swinging[i] = false;
                    }
                }
                Vector3 pole = World(limb.Knee, position + BodyOffset, heading) - Hips[i];
                pole.z = .08f * scale;
                Vector3 requestedFoot = Feet[i];
                Knees[i] = SolveKnee(Hips[i], ref requestedFoot, pole,
                    limb.UpperLength * scale, limb.LowerLength * scale);
                if (Vector3.Distance(requestedFoot, Feet[i]) > .0001f)
                {
                    ReachCorrections++;
                }
                Feet[i] = requestedFoot;
            }
            previousPosition = position;
            previousHeading = heading;
        }

        public static Vector3 SolveKnee(Vector3 hip, ref Vector3 foot, Vector3 pole, float upper, float lower)
        {
            Vector3 delta = foot - hip;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(upper - lower) + .0001f, upper + lower - .0001f);
            Vector3 axis = delta.sqrMagnitude > .00000001f ? delta.normalized : Vector3.right;
            foot = hip + axis * distance;
            float along = (upper * upper - lower * lower + distance * distance) / (2 * distance);
            float radius = Mathf.Sqrt(Mathf.Max(0, upper * upper - along * along));
            Vector3 bend = pole - axis * Vector3.Dot(pole, axis);
            if (bend.sqrMagnitude < .00000001f)
            {
                bend = Vector3.Cross(axis, Vector3.forward);
            }
            return hip + axis * along + bend.normalized * radius;
        }

        private Vector3 World(Vector2 point, Vector2 position, float heading) => position + Rotate(point * scale, heading);
        private static Vector2 Rotate(Vector2 value, float degrees) => Quaternion.Euler(0, 0, degrees) * value;
    }
}
