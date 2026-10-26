using System;
using System.Collections.Generic;
using EmergencyShift.Features.Enemies.Config;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    /// <summary>Instance-owned presentation kinematics. Never moves the authoritative actor.</summary>
    public sealed class StalkerGaitModel
    {
        private const float ReachSafetyFraction = .97f;
        private const float PlannedReachFraction = .94f;
        private const float TurningReachSafetyFraction = .84f;
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
        public int RecoverySteps { get; private set; }
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
            // Reset creates six planted contacts. Start inside the overlap of
            // both support windows, rather than halfway through an absent swing.
            // Keep independent variation within either leading tripod.
            float supportOverlap = (supportFraction - .5f) * .9f;
            phase = Mathf.Floor(initialPhase * 2) * .5f + Mathf.Repeat(initialPhase * 2, 1) * supportOverlap;
            weight = 0;
            BodyOffset = Vector2.zero;
            ReachCorrections = 0;
            RecoverySteps = 0;
            for (int i = 0; i < 6; i++)
            {
                float localPhase = Mathf.Repeat(phase + anatomy[i].PhaseOffset, 1);
                float remainingSupport = supportFraction * .5f - Mathf.Min(localPhase, supportFraction);
                Feet[i] = ContactTarget(anatomy[i], position, heading, Rotate(Vector2.up, heading), remainingSupport * cycleDistance);
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
                float safety = Mathf.Abs(turn) > .001f ? TurningReachSafetyFraction : ReachSafetyFraction;
                bool overreach = extending && reach > (limb.UpperLength + limb.LowerLength) * scale * safety;
                if (!Swinging[i] && allowSteps && ((moving && swingWindow && !previousSwingWindow[i]) || overreach))
                {
                    if (overreach && !swingWindow)
                    {
                        RecoverySteps++;
                    }
                    Swinging[i] = true;
                    takeoff[i] = Feet[i];
                    swingTime[i] = 0;
                    swingDuration[i] = SwingDuration(velocity.magnitude);
                    Vector2 direction = velocity.sqrMagnitude > .001f ? velocity.normalized : Vector2.zero;
                    landing[i] = ContactTarget(limb, position, heading, direction, cycleDistance * supportFraction * .5f);
                }
                previousSwingWindow[i] = swingWindow;
                if (Swinging[i])
                {
                    if (moving)
                    {
                        float duration = SwingDuration(velocity.magnitude);
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
                        landing[i] = ContactTarget(limb, position, heading, direction, cycleDistance * supportFraction * .5f);
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
                Vector2 reachAxis = Feet[i] - Hips[i];
                if (reachAxis.sqrMagnitude < .000001f)
                {
                    reachAxis = Rotate(limb.Tip - limb.Hip, heading);
                }
                Vector2 sourceAxis = limb.Tip - limb.Hip;
                Vector2 sourceBend = limb.Knee - limb.Hip;
                float bendSide = Mathf.Sign(sourceAxis.x * sourceBend.y - sourceAxis.y * sourceBend.x);
                // Preserve the source chain's bend side in the top-down plane.
                // A nearly parallel fixed pole used to rotate the knee vertically,
                // making a complete painted spike shrink to a few pixels.
                Vector3 pole = new Vector3(-reachAxis.y, reachAxis.x, 0) * bendSide;
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

        private float SwingDuration(float speed)
        {
            return Mathf.Clamp(cycleDistance * (1 - supportFraction) / Mathf.Max(.2f, speed), .065f, .4f);
        }

        private Vector3 ContactTarget(StalkerLimb limb, Vector2 position, float heading, Vector2 direction, float lead)
        {
            if (direction.sqrMagnitude < .5f)
            {
                direction = Rotate(Vector2.up, heading);
            }
            Vector2 offset = Rotate((limb.Tip - limb.Hip) * scale, heading);
            float restAlong = Vector2.Dot(offset, direction);
            Vector2 sideways = offset - direction * restAlong;
            float halfSupportTravel = cycleDistance * supportFraction * .5f;
            float reach = (limb.UpperLength + limb.LowerLength) * scale * PlannedReachFraction;
            float planarReachSquared = Mathf.Max(0, reach * reach - bodyHeight * bodyHeight);
            float desiredAlong = Mathf.Min(Mathf.Abs(restAlong) + halfSupportTravel, Mathf.Sqrt(planarReachSquared));
            float maximumSideways = Mathf.Sqrt(Mathf.Max(0, planarReachSquared - desiredAlong * desiredAlong));
            sideways = Vector2.ClampMagnitude(sideways, maximumSideways);
            float availableAlong = Mathf.Sqrt(Mathf.Max(0, reach * reach - bodyHeight * bodyHeight - sideways.sqrMagnitude));
            float centerLimit = Mathf.Max(0, availableAlong - halfSupportTravel);
            // Reserve reach for the complete stance interval. A single lead
            // added to the source rear tips made them lift again too early.
            float center = Mathf.Clamp(restAlong, -centerLimit, centerLimit);
            return World(limb.Hip, position, heading) + (Vector3)(sideways + direction * (center + lead));
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
