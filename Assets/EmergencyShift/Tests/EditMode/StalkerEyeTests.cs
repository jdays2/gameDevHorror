using System;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class StalkerEyeTests
    {
        [TestCase(-.01f, 0)]
        [TestCase(1.01f, 0)]
        [TestCase(float.NaN, 0)]
        [TestCase(float.PositiveInfinity, 0)]
        [TestCase(.5f, -1)]
        [TestCase(.5f, float.PositiveInfinity)]
        public void InvalidEyeDamagePayloadIsRejected(float damage, float contact)
        {
            var state = new StalkerCheckpoint2D.State
            {
                format = 1,
                initialized = true,
                actor = new ShadowStalker2D.Snapshot
                {
                    health = 1,
                    state = ShadowStalker2D.BehaviourState.Dormant,
                    melee = new EnemyMeleeAttack2D.Snapshot(),
                    hasEyeDamageProgress = true,
                    eyeDamage = damage,
                    burnContact = contact
                }
            };
            var fixture = new GameObject("Stalker checkpoint validation");
            try
            {
                var checkpoint = fixture.AddComponent<StalkerCheckpoint2D>();
                Assert.Throws<InvalidOperationException>(() => checkpoint.ValidateCheckpoint(JsonUtility.ToJson(state)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fixture);
            }
        }
    }
}
