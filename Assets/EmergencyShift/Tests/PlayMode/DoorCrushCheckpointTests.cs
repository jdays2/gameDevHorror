#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class DoorCrushCheckpointTests : DoorCrushFixture
    {
        [UnityTest]
        public IEnumerator RealSceneSlotRoundTripRestoresBothDeadActorsAndExactSettledRemainsSilently()
        {
            EnemyHealth2D[] actors = All<EnemyHealth2D>();
            var snapshots = new System.Collections.Generic.Dictionary<string, RemainsSnapshot>();
            int deaths = 0;
            foreach (EnemyHealth2D actor in actors)
            {
                actor.gameObject.SetActive(true);
                Freeze(actor.gameObject);
                float y = actor.transform.position.y;
                Door2D door = All<Door2D>().Single(item => item.GetComponent<Features.Doors.View.DoorCrush2D>() != null && Mathf.Abs(item.transform.position.y - y) < 1);
                actor.transform.position = door.transform.position + Vector3.right * .1f;
                actor.Died += () => deaths++;
                door.RestoreState(true);
                door.Toggle();
            }
            yield return new WaitForSeconds(.65f);
            Assert.That(deaths, Is.EqualTo(2));
            float deadline = Time.realtimeSinceStartup + 12;
            while (actors.Any(actor => !actor.GetComponent<GoreBurst2D>().IsSettled) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            foreach (EnemyHealth2D actor in actors)
            {
                Assert.That(actor.GetComponent<GoreBurst2D>().IsSettled, Is.True);
                snapshots.Add(actor.GetComponent<PersistentEntity2D>().Id, actor.GetComponent<GoreBurst2D>().CaptureRemains());
            }

            string slot = System.IO.Path.GetFullPath("work/door-crush01/isolated-" + Guid.NewGuid().ToString("N") + ".json");
            var store = new CheckpointStore(slot);
            try
            {
                store.Write(new CheckpointData
                {
                    levelId = "door-crush-test",
                    levelRevision = 1,
                    checkpointId = "after-close",
                    visitedCheckpoints = new[] { "after-close" },
                    elapsedSeconds = 4,
                    entities = CaptureScene()
                });
                SceneManager.SetActiveScene(previous);
                yield return SceneManager.UnloadSceneAsync(scene);
                yield return Load();
                int restoredDeaths = 0;
                foreach (EnemyHealth2D actor in All<EnemyHealth2D>())
                {
                    actor.Died += () => restoredDeaths++;
                }
                ApplyScene(store.Read().entities);
                yield return null;
                Assert.That(restoredDeaths, Is.Zero);
                Assert.That(All<RemainsRecord2D>().Length, Is.EqualTo(2));
                foreach (EnemyHealth2D actor in All<EnemyHealth2D>())
                {
                    Assert.That(actor.IsAlive, Is.False);
                    Assert.That(actor.GetComponent<Rigidbody2D>().simulated, Is.False);
                    string id = actor.GetComponent<PersistentEntity2D>().Id;
                    AssertRemains(snapshots[id], actor.GetComponent<GoreBurst2D>().CaptureRemains());
                }
                foreach (Door2D door in All<Door2D>())
                {
                    door.Toggle();
                }
                yield return new WaitForSeconds(.5f);
                foreach (Door2D door in All<Door2D>())
                {
                    door.Toggle();
                }
                yield return new WaitForSeconds(.5f);
                Assert.That(restoredDeaths, Is.Zero);
                Assert.That(All<RemainsRecord2D>().Length, Is.EqualTo(2));
            }
            finally
            {
                store.Delete();
            }
        }

        private static void AssertRemains(RemainsSnapshot expected, RemainsSnapshot actual)
        {
            Assert.That(actual.version, Is.EqualTo(expected.version));
            Assert.That(actual.variantId, Is.EqualTo(expected.variantId));
            Assert.That(actual.bloodVariantId, Is.EqualTo(expected.bloodVariantId));
            Assert.That(actual.hasPool, Is.EqualTo(expected.hasPool));
            Assert.That(Vector3.Distance(actual.position, expected.position), Is.LessThan(.0001f));
            Assert.That(actual.pool.Length, Is.EqualTo(expected.pool.Length));
            for (int i = 0; i < expected.pool.Length; i++)
            {
                AssertSprite(expected.pool[i], actual.pool[i]);
            }
            Assert.That(actual.parts.Length, Is.EqualTo(expected.parts.Length));
            for (int i = 0; i < expected.parts.Length; i++)
            {
                RemainsPartState first = expected.parts[i];
                RemainsPartState second = actual.parts[i];
                Assert.That(second.spriteIndex, Is.EqualTo(first.spriteIndex));
                Assert.That(second.hasShadow, Is.EqualTo(first.hasShadow));
                Assert.That(second.hasEyeSurface, Is.EqualTo(first.hasEyeSurface));
                Assert.That(second.eyeDamage, Is.EqualTo(first.eyeDamage));
                Assert.That(Vector3.Distance(second.position, first.position), Is.LessThan(.0001f));
                AssertSprite(first.body, second.body);
                if (first.hasShadow)
                {
                    AssertSprite(first.shadow, second.shadow);
                }
            }
        }

        private static void AssertSprite(RemainsSpriteState expected, RemainsSpriteState actual)
        {
            Assert.That(actual.visible, Is.EqualTo(expected.visible));
            Assert.That(actual.color, Is.EqualTo(expected.color));
            Assert.That(Vector3.Distance(actual.position, expected.position), Is.LessThan(.0001f));
            Assert.That(Vector3.Distance(actual.scale, expected.scale), Is.LessThan(.0001f));
            // Unity normalizes a restored Transform quaternion; q and -q describe the same orientation.
            Vector4 first = new Vector4(expected.rotation.x, expected.rotation.y, expected.rotation.z, expected.rotation.w);
            Vector4 second = new Vector4(actual.rotation.x, actual.rotation.y, actual.rotation.z, actual.rotation.w);
            Assert.That(Mathf.Min((first - second).magnitude, (first + second).magnitude), Is.LessThan(.0001f));
        }

        // Exercise the real internal persistence boundary without widening the production API for a fixture.
        private object Adapter() => Activator.CreateInstance(typeof(PersistentEntity2D).Assembly.GetType("EmergencyShift.CheckpointSceneAdapter2D"), scene);

        private CheckpointEntityState[] CaptureScene()
        {
            object adapter = Adapter();
            return (CheckpointEntityState[])adapter.GetType().GetMethod("Capture").Invoke(adapter, null);
        }

        private void ApplyScene(CheckpointEntityState[] entities)
        {
            object adapter = Adapter();
            adapter.GetType().GetMethod("Apply").Invoke(adapter, new object[] { entities });
        }
    }
}
#endif
