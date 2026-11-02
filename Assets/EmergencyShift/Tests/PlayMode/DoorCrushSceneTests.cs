#if UNITY_EDITOR
using System.Collections;
using EmergencyShift.Features.Doors.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class DoorCrushSceneTests : DoorCrushFixture
    {
        [UnityTest]
        public IEnumerator RestoreFromMotionCompletionCannotInheritClosingPermission()
        {
            var clock = new Clock();
            Door2D door = Door(new Vector2(900, 900), clock);
            EnemyHealth2D actor = Enemy("Sleeper", new Vector2(900.1f, 900));
            door.RestoreState(false);
            System.Action<bool> restore = opened =>
            {
                if (opened)
                {
                    door.RestoreState(false);
                }
            };
            door.MotionStopped += restore;
            try
            {
                door.Toggle();
                yield return Finish(door, clock);
                Assert.That(door.IsOpen, Is.False);
                Assert.That(actor.IsAlive, Is.True, "A synchronous restore inside MotionStopped is still restoration.");
            }
            finally
            {
                door.MotionStopped -= restore;
            }
        }

        [UnityTest]
        public IEnumerator PlayerUnderClosingLeafKeepsHealthAndLivingPhysics()
        {
            var clock = new Clock();
            Door2D door = Door(new Vector2(850, 850), clock);
            context.Player.transform.position = new Vector3(850.1f, 850, 0);
            int health = context.Player.Health;
            door.Toggle();
            yield return Finish(door, clock);
            Assert.That(context.Player.Health, Is.EqualTo(health));
            Assert.That(context.Player.IsDead, Is.False);
            Assert.That(context.Player.GetComponent<Rigidbody2D>().simulated, Is.True);
        }

        [UnityTest]
        public IEnumerator BothSpeciesAndMultipleCollidersDieOnceAndDoorFinishes()
        {
            var clock = new Clock();
            Door2D door = Door(new Vector2(600, 600), clock);
            var actors = new[]
            {
                Enemy("ShadowStalker", new Vector2(600.1f, 599)),
                Enemy("Sleeper", new Vector2(600.1f, 600)),
                Enemy("ShadowStalker", new Vector2(600.1f, 601))
            };
            int deaths = 0;
            foreach (EnemyHealth2D actor in actors)
            {
                actor.Died += () => deaths++;
                var duplicate = new GameObject("Second physical collider");
                duplicate.transform.SetParent(actor.transform, false);
                duplicate.AddComponent<CircleCollider2D>().radius = .25f;
            }
            door.Toggle();
            yield return Finish(door, clock);
            Assert.That(deaths, Is.EqualTo(3));
            foreach (EnemyHealth2D actor in actors)
            {
                Assert.That(actor.IsAlive, Is.False);
                Assert.That(actor.GetComponent<Rigidbody2D>().simulated, Is.False);
                Assert.That(actor.GetComponent<GoreBurst2D>().CaptureRemains(), Is.Not.Null);
            }
            int remains = All<RemainsRecord2D>().Length;
            door.Toggle();
            yield return Finish(door, clock);
            door.Toggle();
            yield return Finish(door, clock);
            Assert.That(deaths, Is.EqualTo(3));
            Assert.That(All<RemainsRecord2D>().Length, Is.EqualTo(remains));
        }

        [UnityTest]
        public IEnumerator FastRotatedSweepCatchesBetweenEndpointsButSparesOutsideAndTriggerOnlyContacts()
        {
            var clock = new Clock();
            Door2D door = Door(new Vector2(650, 650), clock);
            door.transform.rotation = Quaternion.Euler(0, 0, 37);
            door.Blocker.transform.localScale = new Vector3(.3f, .2f, 1);
            Vector2 middle = door.transform.TransformPoint(new Vector3(0, 1.55f, 0));
            EnemyHealth2D inside = Enemy("Sleeper", middle);
            EnemyHealth2D outside = Enemy("ShadowStalker", middle + (Vector2)door.transform.right * 1.6f);
            EnemyHealth2D sensor = Enemy("Sleeper", middle - (Vector2)door.transform.right * 1.6f);
            var trigger = sensor.gameObject.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 2;
            bool startInsideColliders = Physics2D.queriesStartInColliders;
            try
            {
                Physics2D.queriesStartInColliders = false;
                Physics2D.SyncTransforms();
                Assert.That(door.Blocker.Distance(inside.GetComponent<Collider2D>()).isOverlapped, Is.False);
                door.Toggle();
                yield return Finish(door, clock);
                Assert.That(inside.IsAlive, Is.False);
                Assert.That(outside.IsAlive, Is.True);
                Assert.That(sensor.IsAlive, Is.True);
            }
            finally
            {
                Physics2D.queriesStartInColliders = startInsideColliders;
            }
        }

        [UnityTest]
        public IEnumerator OpeningStationaryPauseAndLeavingBeforeContactAreSafeEnteringDuringCloseDies()
        {
            var clock = new Clock();
            Door2D door = Door(new Vector2(700, 700), clock);
            EnemyHealth2D actor = Enemy("ShadowStalker", new Vector2(700.1f, 698.8f));
            door.RestoreState(false);
            yield return null;
            Assert.That(actor.IsAlive, Is.True, "stationary / restore");
            door.Toggle();
            yield return Finish(door, clock);
            Assert.That(actor.IsAlive, Is.True, "opening");
            Time.timeScale = 0;
            door.Toggle();
            yield return null;
            yield return null;
            Assert.That(actor.IsAlive, Is.True, "paused closing has no displacement");
            Time.timeScale = 1;
            clock.Time += .05;
            yield return null;
            actor.transform.position += Vector3.right * 3;
            yield return Finish(door, clock);
            Assert.That(actor.IsAlive, Is.True, "left before leaf contact");
            door.Toggle();
            yield return Finish(door, clock);
            door.Toggle();
            clock.Time += .05;
            yield return null;
            actor.transform.position = new Vector3(700.1f, 698.8f, 0);
            yield return Finish(door, clock);
            Assert.That(actor.IsAlive, Is.False, "entered during close");
        }

        [UnityTest]
        public IEnumerator RestoreDisableAndOptOutCannotCrushAndUnloadingDoesNotEmitDeath()
        {
            var clock = new Clock();
            Door2D door = Door(new Vector2(750, 750), clock);
            EnemyHealth2D actor = Enemy("Sleeper", new Vector2(750.1f, 750));
            int deaths = 0;
            actor.Died += () => deaths++;
            door.RestoreState(false);
            Assert.That(deaths, Is.Zero);
            door.RestoreState(true);
            door.Toggle();
            door.enabled = false;
            clock.Time += 3;
            yield return null;
            Assert.That(actor.IsAlive, Is.True, "disable snap is not a closing timeline");
            door.enabled = true;
            door.RestoreState(true);
            door.Toggle();
            door.gameObject.SetActive(false);
            clock.Time += 3;
            yield return null;
            Assert.That(actor.IsAlive, Is.True, "whole object disable");
            door.gameObject.SetActive(true);
            door.RestoreState(true);
            door.GetComponent<DoorCrush2D>().enabled = false;
            door.Toggle();
            yield return Finish(door, clock);
            Assert.That(actor.IsAlive, Is.True, "feature disabled");
            Door2D ordinary = Door(new Vector2(760, 750), clock, false);
            actor.transform.position = new Vector3(760.1f, 750, 0);
            ordinary.Toggle();
            yield return Finish(ordinary, clock);
            Assert.That(actor.IsAlive, Is.True, "ordinary door");
            door.GetComponent<DoorCrush2D>().enabled = true;
            door.RestoreState(true);
            actor.transform.position = new Vector3(750.1f, 750, 0);
            door.Toggle();
            yield return SceneManager.UnloadSceneAsync(scene);
            Assert.That(deaths, Is.Zero, "unload must not sweep the disable snap");
        }

        [UnityTest]
        public IEnumerator ContactsInAnotherLoadedSceneAreNotVictims()
        {
            var clock = new Clock();
            Door2D door = Door(new Vector2(800, 800), clock);
            EnemyHealth2D actor = Enemy("Sleeper", new Vector2(800.1f, 800));
            Scene foreign = SceneManager.CreateScene("DoorCrush foreign ownership fixture");
            actor.transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(actor.gameObject, foreign);
            try
            {
                door.Toggle();
                yield return Finish(door, clock);
                Assert.That(actor.IsAlive, Is.True);
            }
            finally
            {
                SceneManager.UnloadSceneAsync(foreign);
            }
        }
    }
}
#endif
