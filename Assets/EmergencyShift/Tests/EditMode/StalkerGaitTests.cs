using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class StalkerGaitTests
    {
        private static StalkerRigProfile Profile => AssetDatabase.LoadAssetAtPath<StalkerRigProfile>("Assets/EmergencyShift/Data/Enemies/StalkerLimbRig.asset");
        private static StalkerGaitModel Create(float initialPhase = 0)
        {
            StalkerRigProfile profile = Profile;
            return new StalkerGaitModel(profile.Limbs, 1, profile.CycleDistance, profile.SupportFraction,
                profile.LiftHeight, profile.BodyHeight, profile.WeightTransfer, initialPhase);
        }

        [TestCase(1.2f, 30)]
        [TestCase(1.5f, 30)]
        [TestCase(3.8f, 30)]
        [TestCase(1.2f, 60)]
        [TestCase(1.5f, 60)]
        [TestCase(3.8f, 60)]
        [TestCase(1.2f, 120)]
        [TestCase(1.5f, 120)]
        [TestCase(3.8f, 120)]
        public void StraightTravelKeepsSupportFeetAndTrueBoneLengths(float speed, int rate)
        {
            var gait = Create();
            gait.Step(Vector2.zero, 0, .01f, true, 0);
            int contacts = 0;
            for (int frame = 1; frame <= 600; frame++)
            {
                var feet = (Vector3[])gait.Feet.Clone();
                var swinging = (bool[])gait.Swinging.Clone();
                gait.Step(Vector2.up * (frame * speed / rate), 0, 1f / rate, true, 0);
                for (int i = 0; i < 6; i++)
                {
                    Assert.That(Vector3.Distance(gait.Hips[i], gait.Knees[i]), Is.EqualTo(Profile.Limbs[i].UpperLength).Within(.00001f));
                    Assert.That(Vector3.Distance(gait.Knees[i], gait.Feet[i]), Is.EqualTo(Profile.Limbs[i].LowerLength).Within(.00001f));
                    if (!swinging[i] && !gait.Swinging[i])
                    {
                        contacts++;
                        Assert.That(Vector3.Distance(feet[i], gait.Feet[i]), Is.LessThan(.0001f), "Sliding planted foot " + i + " frame " + frame);
                    }
                }
            }
            Assert.That(contacts, Is.GreaterThan(100));
            Assert.That(gait.ReachCorrections, Is.Zero);
        }

        [Test]
        public void StopFinishesTransferThenSettlesWithoutCycling()
        {
            var gait = Create();
            for (int i = 0; i < 30; i++)
            {
                gait.Step(Vector2.up * (i * .015f), 0, .01f, true, 0);
            }
            float phase = gait.Phase;
            for (int i = 0; i < 100; i++)
            {
                gait.Step(Vector2.up * .435f, 0, .01f, true, 0);
            }
            Assert.That(gait.Phase, Is.EqualTo(phase).Within(.00001f));
            Assert.That(gait.BodyOffset.magnitude, Is.LessThan(.00001f));
            for (int i = 0; i < 6; i++)
            {
                Assert.That(gait.Swinging[i], Is.False);
            }
        }

        [TestCase(1.2f)]
        [TestCase(1.5f)]
        [TestCase(3.8f)]
        public void SteadyTravelKeepsTripodsAndDoesNotInsertExtraSteps(float speed)
        {
            const int rate = 120;
            const int seconds = 6;
            for (int sample = 0; sample < 8; sample++)
            {
                var gait = Create(sample / 8f);
                int[] lifts = new int[6];
                gait.Step(Vector2.zero, 0, 1f / rate, true, 0);
                for (int frame = 1; frame <= seconds * rate; frame++)
                {
                    var previous = (bool[])gait.Swinging.Clone();
                    gait.Step(Vector2.up * (frame * speed / rate), 0, 1f / rate, true, 0);
                    int supports = 0;
                    for (int limb = 0; limb < 6; limb++)
                    {
                        if (!previous[limb] && gait.Swinging[limb])
                        {
                            lifts[limb]++;
                        }
                        if (!gait.Swinging[limb])
                        {
                            supports++;
                        }
                        float projectedSpike = Vector2.Distance(gait.Knees[limb], gait.Feet[limb]);
                        Assert.That(projectedSpike, Is.GreaterThan(Profile.Limbs[limb].LowerLength * .85f), "Spike collapsed in the overhead projection");
                    }
                    Assert.That(supports, Is.GreaterThanOrEqualTo(3), "Lost supporting tripod at phase " + sample / 8f);
                }
                Assert.That(gait.RecoverySteps, Is.Zero, "Unplanned extra step during steady travel");
                foreach (int count in lifts)
                {
                    Assert.That(count, Is.EqualTo(seconds * speed / Profile.CycleDistance).Within(1.1f));
                }
            }
        }

        [TestCase(0f)]
        [TestCase(.1f)]
        [TestCase(.25f)]
        [TestCase(.5f)]
        [TestCase(.75f)]
        [TestCase(.9f)]
        public void StartRunSlowTurnStopAndResumeNeverForceAContactOutsideReach(float initialPhase)
        {
            var gait = Create(initialPhase);
            Vector2 position = Vector2.zero;
            float heading = 0;
            float turnVelocity = 0;
            for (int frame = 0; frame < 480; frame++)
            {
                float speed = 1.2f;
                if (frame >= 180 && frame < 240)
                {
                    speed = 1.5f;
                }
                else if (frame >= 240 && frame < 360)
                {
                    speed = 3.8f;
                }
                if (frame >= 405 && frame < 450)
                {
                    speed = 0;
                }
                float intended = Mathf.Clamp((frame - 360) * 2.25f, 0, 90);
                position += (Vector2)(Quaternion.Euler(0, 0, intended) * Vector2.up) * (speed / 60);
                if (speed > 0)
                {
                    heading = Mathf.SmoothDampAngle(heading, intended, ref turnVelocity, .1f, 160, 1f / 60);
                }
                gait.Step(position, heading, 1f / 60, true, 0);
            }
            Assert.That(gait.ReachCorrections, Is.Zero);
        }

        [Test]
        public void RecoilIntoFasterReverseRetreatKeepsReachAcrossInitialPhases()
        {
            for (int sample = 0; sample < 20; sample++)
            {
                var gait = Create(sample / 20f);
                Vector2 position = Vector2.zero;
                float heading = 0;
                float turnVelocity = 0;
                for (int frame = 0; frame < 180; frame++)
                {
                    float dt = frame % 23 == 0 ? 1f / 12 : 1f / 60;
                    Vector2 velocity = Vector2.down * 1.5f;
                    float targetHeading = 180;
                    float compact = 0;
                    if (frame >= 40 && frame < 65)
                    {
                        velocity = Vector2.zero;
                        compact = (frame - 40) / 25f;
                    }
                    else if (frame >= 65)
                    {
                        velocity = Vector2.up * 3.8f;
                        targetHeading = 0;
                        compact = 1;
                    }
                    position += velocity * dt;
                    if (velocity.sqrMagnitude > 0)
                    {
                        heading = Mathf.SmoothDampAngle(heading, targetHeading, ref turnVelocity, .1f, 160, dt);
                    }
                    else
                    {
                        turnVelocity = 0;
                    }
                    gait.Step(position, heading, dt, true, compact);
                }
                Assert.That(gait.ReachCorrections, Is.Zero, "Initial phase " + sample / 20f);
            }
        }

        [Test]
        public void ReversingIntoAFullTurnToleratesSlowPresentationFrames()
        {
            var gait = Create(.7f);
            Vector2 position = Vector2.zero;
            float heading = 0;
            float turnVelocity = 0;
            for (int frame = 0; frame < 180; frame++)
            {
                float dt = frame % 19 == 0 ? 1f / 8 : 1f / 60;
                position += Vector2.down * (1.5f * dt);
                heading = Mathf.SmoothDampAngle(heading, 180, ref turnVelocity, .1f, 160, dt);
                gait.Step(position, heading, dt, true, 0);
            }
            Assert.That(gait.ReachCorrections, Is.Zero);
        }

        [Test]
        public void PausedFrameCannotAdvanceASwingOrChangeContacts()
        {
            var gait = Create();
            for (int i = 0; i < 40; i++)
            {
                gait.Step(Vector2.up * (i * .015f), 0, .01f, true, 0);
            }
            var before = (Vector3[])gait.Feet.Clone();
            float phase = gait.Phase;
            for (int i = 0; i < 100; i++)
            {
                gait.Step(Vector2.up * .585f, 0, 0, true, 0);
            }
            Assert.That(gait.Phase, Is.EqualTo(phase));
            CollectionAssert.AreEqual(before, gait.Feet);
        }

        [Test]
        public void TeleportResetsAndInstancesDoNotShareContacts()
        {
            var a = Create();
            var b = Create();
            a.Step(Vector2.zero, 0, .01f, true, 0);
            b.Step(Vector2.zero, 0, .01f, true, 0);
            Vector3 untouched = b.Feet[0];
            a.Step(new Vector2(100, 100), 90, .01f, true, 0);
            Assert.That(a.Phase, Is.Zero);
            Assert.That(b.Feet[0], Is.EqualTo(untouched));
            Assert.That(Vector2.Distance(a.Feet[0], new Vector2(100, 100)), Is.LessThan(2));
        }

        [Test]
        public void TurningAndCompactRetreatRemainFiniteAndKeepBoneLengths()
        {
            var gait = Create();
            for (int frame = 0; frame < 1000; frame++)
            {
                float angle = frame * .7f;
                Vector2 position = Quaternion.Euler(0, 0, angle) * Vector2.up * 2;
                gait.Step(position, angle + 90, 1f / 120, true, 1);
                for (int i = 0; i < 6; i++)
                {
                    Assert.That(float.IsFinite(gait.Knees[i].x), Is.True);
                    Assert.That(Vector3.Distance(gait.Hips[i], gait.Knees[i]), Is.EqualTo(Profile.Limbs[i].UpperLength).Within(.00001f));
                    Assert.That(Vector3.Distance(gait.Knees[i], gait.Feet[i]), Is.EqualTo(Profile.Limbs[i].LowerLength).Within(.00001f));
                }
            }
        }
    }
}
