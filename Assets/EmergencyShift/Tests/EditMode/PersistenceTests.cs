using System;
using System.IO;
using NUnit.Framework;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Health.Logic;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class PersistenceTests
    {
        private string folder;
        private CheckpointStore store;
        [SetUp]
        public void Setup()
        {
            folder = Path.Combine(Path.GetTempPath(), "EmergencyShift-tests-" + Guid.NewGuid().ToString("N"));
            store = new CheckpointStore(Path.Combine(folder, "slot.json"));
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }

        private static CheckpointData Data()
        {
            return new CheckpointData
            {
                levelId = "test",
                checkpointId = "door-a",
                entities = new[]
                {
                    new CheckpointEntityState
                    {
                        id = "enemy-a",
                        position = new Vector3(2, 3, 0),
                        rotation = Quaternion.identity,
                        parts = new[]
                        {
                            new CheckpointPart
                            {
                                key = "health.v1",
                                json = "{\"health\":2}"
                            }
                        }
                    }
                }
            };
        }

        [Test]
        public void DiskRoundTripPreservesStableIdAndPayload()
        {
            store.Write(Data());
            var read = store.Read();
            Assert.That(read.entities[0].id, Is.EqualTo("enemy-a"));
            Assert.That(read.entities[0].parts[0].json, Does.Contain("health"));
            Assert.That(read.entities[0].position, Is.EqualTo(new Vector3(2, 3, 0)));
        }

        [Test]
        public void ReplacementKeepsPreviousSlotBackup()
        {
            var data = Data();
            store.Write(data);
            data.checkpointId = "door-b";
            store.Write(data);
            Assert.That(store.Read().checkpointId, Is.EqualTo("door-b"));
            Assert.That(new CheckpointStore(Path.Combine(folder, "slot.json.bak")).Read().checkpointId, Is.EqualTo("door-a"));
        }

        [Test]
        public void DuplicateIdsAreRejectedBeforeWriting()
        {
            var data = Data();
            data.entities = new[]
            {
                data.entities[0],
                data.entities[0]
            };
            Assert.Throws<InvalidDataException>(() => store.Write(data));
            Assert.That(store.Exists, Is.False);
        }

        [Test]
        public void UnsupportedVersionDoesNotReplaceLastGoodSave()
        {
            store.Write(Data());
            var bad = Data();
            bad.version = 99;
            Assert.Throws<InvalidDataException>(() => store.Write(bad));
            Assert.That(store.Read().version, Is.EqualTo(1));
        }

        [Test]
        public void NonFinitePositionIsRejected()
        {
            var data = Data();
            data.entities[0].position.x = float.NaN;
            Assert.Throws<InvalidDataException>(() => store.Write(data));
        }

        [Test]
        public void BrokenJsonIsRejected()
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "slot.json"), "invalid json");
            Assert.Throws<ArgumentException>(() => store.Read());
        }

        [Test]
        public void ClearRemovesBackupAndSlot()
        {
            store.Write(Data());
            store.Write(Data());
            store.Delete();
            Assert.That(store.Exists, Is.False);
            Assert.That(File.Exists(Path.Combine(folder, "slot.json.bak")), Is.False);
        }

        [Test]
        public void HealthRestoreIsSilentAndStillPreventsSecondDeath()
        {
            var obj = new GameObject();
            try
            {
                var health = obj.AddComponent<EnemyHealth2D>();
                health.Construct(new HealthModelFactory(new UnityGameClock()));
                health.Initialize(3);
                int deaths = 0;
                health.Died += () => deaths++;
                health.Restore(0);
                health.Kill();
                Assert.That(deaths, Is.Zero);
                health.Restore(2);
                health.Kill();
                Assert.That(deaths, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        [Test]
        public void CardRestoreReplacesInventoryWithoutSharingIt()
        {
            var a = new GameObject();
            var b = new GameObject();
            try
            {
                var first = a.AddComponent<PlayerAccessCards2D>();
                var second = b.AddComponent<PlayerAccessCards2D>();
                first.Add("service");
                second.Add("other");
                second.Restore(first.Capture());
                Assert.That(second.Has("service"), Is.True);
                Assert.That(second.Has("other"), Is.False);
                second.Add("evacuation");
                Assert.That(first.Has("evacuation"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(a);
                UnityEngine.Object.DestroyImmediate(b);
            }
        }

        [Test]
        public void InactiveActorPayloadSurvivesJsonWithoutBecomingADeadActor()
        {
            var obj = new GameObject();
            try
            {
                var adapter = obj.AddComponent<StalkerCheckpoint2D>();
                string json = JsonUtility.ToJson(new StalkerCheckpoint2D.State { format = 1, initialized = false, hasRemains = false });
                Assert.DoesNotThrow(() => adapter.ValidateCheckpoint(json));
                // No ShadowStalker modules are present: restore must preserve the uninitialised spawn, not touch death/health.
                Assert.DoesNotThrow(() => adapter.RestoreCheckpoint(json));
                Assert.Throws<InvalidOperationException>(() => adapter.ValidateCheckpoint("{}"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        [Test]
        public void DuplicateSceneIdsFailRatherThanOverwritingAnEnemy()
        {
            var a = new GameObject();
            var b = new GameObject();
            try
            {
                a.AddComponent<PersistentEntity2D>().AssignId("duplicate-test");
                b.AddComponent<PersistentEntity2D>().AssignId("duplicate-test");
                Assert.Throws<InvalidOperationException>(() => PersistentEntity2D.Collect(a.scene));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(a);
                UnityEngine.Object.DestroyImmediate(b);
            }
        }
    }
}
