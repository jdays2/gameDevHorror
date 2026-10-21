using System;
using System.Collections.Generic;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class EnemyMeleeModelTests
    {
        [Test]
        public void TimelineDeliversOneLungeAndOneStrikeAndKeepsTheirOrder()
        {
            var environment = new Environment();
            EnemyMeleeController attack = Create(environment, new Clock());
            var events = new List<string>();
            attack.Lunged += () => events.Add("lunge");
            attack.Resolved += landed => events.Add(landed ? "hit" : "miss");
            attack.Begin();
            Assert.That(attack.Step(.2f, Vector2.zero, Vector2.right, false), Is.EqualTo(Vector2.zero));
            Assert.That(attack.Step(.25f, Vector2.zero, Vector2.right, false), Is.EqualTo(Vector2.right * 2.7f));
            attack.Step(.42f, Vector2.zero, Vector2.right, false);
            attack.Step(.43f, Vector2.zero, Vector2.right, false);
            Assert.That(events, Is.EqualTo(new[] { "lunge", "hit" }));
            Assert.That(environment.DamageCalls, Is.EqualTo(1));
            Assert.That(attack.Capture().Struck && attack.Capture().Lunged, Is.True);
        }

        [Test]
        public void SkippedFrameStillResolvesOnceAndLungeCallbacksSeePreStrikeState()
        {
            var environment = new Environment();
            EnemyMeleeController attack = Create(environment, new Clock());
            bool struckDuringLunge = true;
            attack.Lunged += () => struckDuringLunge = attack.Capture().Struck;
            attack.Begin();
            Assert.That(attack.Step(1.5f, Vector2.zero, Vector2.right, false), Is.EqualTo(Vector2.zero));
            Assert.That(struckDuringLunge, Is.False);
            Assert.That(environment.DamageCalls, Is.EqualTo(1));
            attack.Step(1.5f, Vector2.zero, Vector2.right, false);
            Assert.That(environment.DamageCalls, Is.EqualTo(1));
        }

        [Test]
        public void ReentrantLungeListenerCannotDeliverTwoStrikes()
        {
            var environment = new Environment();
            EnemyMeleeController attack = Create(environment, new Clock());
            attack.Lunged += () => attack.Step(.5f, Vector2.zero, Vector2.right, false);
            attack.Begin();
            attack.Step(.5f, Vector2.zero, Vector2.right, false);
            Assert.That(environment.DamageCalls, Is.EqualTo(1));
        }

        [TestCase(true, 65, false)]
        [TestCase(true, 60, true)]
        [TestCase(false, 180, true)]
        public void CommittedHeadingUsesArcWhileSteeringAttackKeepsLegacyContactRule(bool fixedHeading, float angle, bool expected)
        {
            var environment = new Environment { Observation = new EnemyMeleeObservation(true, true, 1, angle) };
            EnemyMeleeController attack = Create(environment, new Clock());
            attack.Begin();
            attack.Step(.42f, Vector2.zero, Vector2.right, fixedHeading);
            Assert.That(environment.DamageCalls, Is.EqualTo(expected ? 1 : 0));
        }

        [TestCase(false, true, true)]
        [TestCase(true, false, true)]
        [TestCase(true, true, false)]
        public void MissingHealthDeathAndObstructionCannotAuthorizeDamage(bool damageable, bool alive, bool visible)
        {
            var environment = new Environment
            {
                Observation = new EnemyMeleeObservation(damageable, alive, 1, 0),
                Visible = visible
            };
            EnemyMeleeController attack = Create(environment, new Clock());
            Assert.That(attack.CanStart(Vector2.zero), Is.False);
            bool? resolved = null;
            attack.Resolved += landed => resolved = landed;
            attack.Begin();
            attack.Step(.42f, Vector2.zero, Vector2.right, true);
            Assert.That(environment.DamageCalls, Is.Zero);
            Assert.That(resolved, Is.False);
        }

        [Test]
        public void StartRangeUsesSquaredDistanceAndBiteContactRemainsStrict()
        {
            EnemyMeleeSettings settings = Settings();
            var environment = new Environment
            {
                Observation = new EnemyMeleeObservation(true, true, 1.279f * 1.279f, 0)
            };
            EnemyMeleeController attack = Create(environment, new Clock());
            Assert.That(attack.CanStart(Vector2.zero), Is.True);
            environment.Observation = new EnemyMeleeObservation(true, true, 1.281f * 1.281f, 0);
            Assert.That(attack.CanStart(Vector2.zero), Is.False);
            environment.Observation = new EnemyMeleeObservation(true, true, settings.BiteDistance * settings.BiteDistance, 0);
            attack.Begin();
            attack.Step(.42f, Vector2.zero, Vector2.right, false);
            Assert.That(environment.DamageCalls, Is.Zero);
        }

        [Test]
        public void RejectedProtectedTargetConsumesOnlyOneStrikeAndEmitsMiss()
        {
            var environment = new Environment { AcceptsDamage = false };
            EnemyMeleeController attack = Create(environment, new Clock());
            var results = new List<bool>();
            attack.Resolved += results.Add;
            attack.Begin();
            attack.Step(.42f, Vector2.zero, Vector2.right, false);
            attack.Step(.5f, Vector2.zero, Vector2.right, false);
            Assert.That(environment.DamageCalls, Is.EqualTo(1));
            Assert.That(results, Is.EqualTo(new[] { false }));
        }

        [Test]
        public void CooldownRestoresRelativeToNewClockWithoutReplayingStrikeOrEvents()
        {
            var clock = new Clock { Time = 10 };
            var environment = new Environment();
            EnemyMeleeController attack = Create(environment, clock);
            attack.Begin();
            attack.Step(.5f, Vector2.zero, Vector2.right, false);
            attack.Finish();
            Assert.That(attack.CanStart(Vector2.zero), Is.False);
            EnemyMeleeState state = attack.Capture();
            clock.Time = 100;
            var restoredEnvironment = new Environment();
            EnemyMeleeController restored = Create(restoredEnvironment, clock);
            int events = 0;
            restored.Lunged += () => events++;
            restored.Resolved += landed => events++;
            Assert.That(restored.Restore(state).Success, Is.True);
            restored.Step(.5f, Vector2.zero, Vector2.right, false);
            Assert.That(restoredEnvironment.DamageCalls, Is.Zero);
            Assert.That(events, Is.Zero);
            clock.Time = 100d + state.Cooldown;
            Assert.That(restored.CanStart(Vector2.zero), Is.True);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(-1)]
        public void InvalidRestoreAndInterruptAreRejectedWithoutChangingFlagsOrCooldown(float cooldown)
        {
            EnemyMeleeController attack = Create(new Environment(), new Clock());
            attack.Begin();
            attack.Step(.5f, Vector2.zero, Vector2.right, false);
            EnemyMeleeState before = attack.Capture();
            Assert.That(attack.Restore(new EnemyMeleeState(false, false, cooldown)).Success, Is.False);
            Assert.That(attack.Interrupt(cooldown).Success, Is.False);
            Assert.That(attack.Capture().Struck, Is.EqualTo(before.Struck));
            Assert.That(attack.Capture().Cooldown, Is.EqualTo(before.Cooldown));
        }

        [Test]
        public void SharedSettingsKeepActorsIndependentAndInterruptionUsesInjectedTime()
        {
            var clock = new Clock();
            EnemyMeleeSettings settings = Settings();
            var first = new EnemyMeleeController(new EnemyMeleeModel(settings, clock), new Environment());
            var second = new EnemyMeleeController(new EnemyMeleeModel(settings, clock), new Environment());
            first.Interrupt(.35f);
            Assert.That(first.CanStart(Vector2.zero), Is.False);
            Assert.That(second.CanStart(Vector2.zero), Is.True);
            clock.Time = (double).35f;
            Assert.That(first.CanStart(Vector2.zero), Is.True);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-1)]
        public void InvalidProgressCannotEmitEventsOrTouchPhysics(float progress)
        {
            var environment = new Environment();
            EnemyMeleeController attack = Create(environment, new Clock());
            attack.Begin();
            attack.Step(progress, Vector2.zero, Vector2.right, false);
            Assert.That(environment.DamageCalls, Is.Zero);
            Assert.That(environment.MotionQueries, Is.Zero);
            Assert.That(attack.Capture().Lunged || attack.Capture().Struck, Is.False);
        }

        [Test]
        public void AuthoredDefinitionsProduceValidatedSnapshotsWithoutMutatingAssets()
        {
            string[] definitions = AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/EmergencyShift/Data/Enemies" });
            Assert.That(definitions, Is.Not.Empty);
            foreach (string guid in definitions)
            {
                var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                string before = EditorJsonUtility.ToJson(definition);
                Assert.DoesNotThrow(() => definition.ToMeleeSettings(), definition.name);
                Assert.That(EditorJsonUtility.ToJson(definition), Is.EqualTo(before));
            }
        }

        [TestCase("damage")]
        [TestCase("duration")]
        [TestCase("speed")]
        [TestCase("cooldown")]
        [TestCase("timeline")]
        [TestCase("arc")]
        public void MalformedAttackSettingsCannotEnterTheModel(string fault)
        {
            Assert.Throws<ArgumentException>(() => new EnemyMeleeSettings(
                fault == "damage" ? 0 : 1, 1.28f, fault == "duration" ? float.NaN : .8f,
                fault == "cooldown" ? -1 : 1.2f, fault == "speed" ? float.PositiveInfinity : 2.7f,
                1.4f, fault == "timeline" ? .8f : .25f, .48f, .42f, fault == "arc" ? 361 : 120));
        }

        private static EnemyMeleeSettings Settings() => new EnemyMeleeSettings(1, 1.28f, .8f, 1.2f, 2.7f, 1.4f, .25f, .48f, .42f, 120);
        private static EnemyMeleeController Create(Environment environment, Clock clock) => new EnemyMeleeController(new EnemyMeleeModel(Settings(), clock), environment);

        private sealed class Clock : IGameClock
        {
            public double Time { get; set; }
        }

        private sealed class Environment : IEnemyMeleeEnvironment
        {
            public EnemyMeleeObservation Observation = new EnemyMeleeObservation(true, true, 1, 0);
            public bool Visible = true;
            public bool AcceptsDamage = true;
            public int DamageCalls { get; private set; }
            public int MotionQueries { get; private set; }
            public EnemyMeleeObservation Observe(Vector2 at, Vector2 heading, bool fixedHeading) => Observation;
            public bool HasLineOfSight(Vector2 at) => Visible;
            public Vector2 LungeVelocity(Vector2 heading, float speed, bool fixedHeading)
            {
                MotionQueries++;
                return heading * speed;
            }

            public bool TryDamage(int amount, Vector2 at, Vector2 heading)
            {
                DamageCalls++;
                return AcceptsDamage;
            }
        }
    }
}
