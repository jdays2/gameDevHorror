using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class SleeperTests
    {
        private const string PrefabPath = "Assets/EmergencyShift/Prefabs/Enemies/Sleeper.prefab";

        [Test]
        public void InactiveSleeperCheckpointHasExplicitUninitializedPayload()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var checkpoint = prefab.GetComponent<SleeperCheckpoint2D>();
            string json = checkpoint.CaptureCheckpoint();
            checkpoint.ValidateCheckpoint(json);
            Assert.That(JsonUtility.FromJson<SleeperCheckpoint2D.State>(json).initialized, Is.False);
        }

        [TestCase("{}")]
        [TestCase("{\"format\":1,\"initialized\":true}")]
        [TestCase("{\"format\":1,\"initialized\":true,\"hasActor\":true}")]
        public void MissingCheckpointPayloadIsRejected(string json)
        {
            var checkpoint = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<SleeperCheckpoint2D>();
            Assert.Throws<InvalidOperationException>(() => checkpoint.ValidateCheckpoint(json));
        }

        [TestCase(-1, Sleeper2D.BehaviourState.Sleeping, false)]
        [TestCase(0, Sleeper2D.BehaviourState.Hunting, true)]
        [TestCase(8, Sleeper2D.BehaviourState.Sleeping, true)]
        [TestCase(8, Sleeper2D.BehaviourState.Attacking, false)]
        [TestCase(8, (Sleeper2D.BehaviourState)999, true)]
        public void ContradictoryCheckpointStateIsRejected(int health, Sleeper2D.BehaviourState state, bool aggro)
        {
            var checkpoint = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<SleeperCheckpoint2D>();
            var saved = new SleeperCheckpoint2D.State
            {
                format = 1,
                initialized = true,
                hasActor = true,
                hasMelee = true,
                actor = new Sleeper2D.Snapshot
                {
                    health = health,
                    state = state,
                    aggro = aggro,
                    melee = new EnemyMeleeAttack2D.Snapshot()
                }
            };
            Assert.Throws<InvalidOperationException>(() => checkpoint.ValidateCheckpoint(JsonUtility.ToJson(saved)));
        }
    }
}
