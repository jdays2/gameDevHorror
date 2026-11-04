#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmergencyShift.Features.ScriptedEncounters.Logic;
using EmergencyShift.Features.ScriptedEncounters.View;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class ScriptedIntroSceneTests
    {
        private Scene scene;
        private Scene previous;
        private LevelContext2D context;
        private ScriptedStalkerRoute2D route;
        private ShadowStalker2D actor;
        private Rigidbody2D playerBody;
        private ScriptedStalkerRoute2D[] group;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            previous = SceneManager.GetActiveScene();
            const string path = "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath(path);
            SceneManager.SetActiveScene(scene);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                context = context != null ? context : root.GetComponentInChildren<LevelContext2D>(true);
                route = route != null ? route : root.GetComponentInChildren<ScriptedStalkerRoute2D>(true);
            }

            group = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ScriptedStalkerRoute2D>(true)).ToArray();
            route = group.Single(value => value.EncounterId == "morgue-intro-light-retreat");
            Assert.That(route, Is.Not.Null);
            Assert.That(context, Is.Not.Null);
            actor = route.GetComponent<ShadowStalker2D>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (ShadowStalker2D other in root.GetComponentsInChildren<ShadowStalker2D>(true))
                {
                    if (other != actor)
                    {
                        other.gameObject.SetActive(false);
                    }
                }
            }

            context.Player.SetHealthCapacity(100, 100);
            context.Player.GetComponent<PlayerMotor2D>().enabled = false;
            context.Player.GetComponent<PlayerAim2D>().enabled = false;
            context.Player.GetComponent<PlayerWeapon2D>().enabled = false;
            playerBody = context.Player.GetComponent<Rigidbody2D>();
            playerBody.constraints = RigidbodyConstraints2D.FreezeAll;
            context.Flashlight.Acquire();
            context.Flashlight.SetPower(false);
            MovePlayer(new Vector2(3.1f, .35f));
            yield return new WaitForFixedUpdate();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }

            if (scene.IsValid() && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        [UnityTest]
        public IEnumerator RealBeamStartsAuthoredRouteAndArrivalReturnsOrdinaryStalker()
        {
            // Exercise the reusable flashlight condition on a runtime instance only.
            route.Dispose();
            var fields = new UnityEditor.SerializedObject(route);
            fields.FindProperty("startCondition").enumValueIndex = (int)ScriptedRouteStartCondition.Flashlight;
            fields.ApplyModifiedPropertiesWithoutUndo();
            var controller = (EmergencyShift.Features.Enemies.Logic.StalkerController)typeof(ShadowStalker2D)
                .GetField("controller", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(actor);
            route.Initialize(controller.Model, actor.GetComponent<EnemyMotor2D>(), actor.BlockingGeometry.value | (1 << 9));
            Assert.That(route.State, Is.EqualTo(ScriptedRouteState.WaitingForTrigger));
            Assert.That(actor.HasAggro, Is.False);
            context.Flashlight.AimAt(actor.transform.position);
            context.Flashlight.SetPower(true);
            yield return new WaitForSeconds(.12f);
            Assert.That(route.State, Is.EqualTo(ScriptedRouteState.WaitingForTrigger), "The closed starting door must block the trigger.");
            context.Flashlight.SetPower(false);
            MovePlayer((Vector2)actor.transform.position + Vector2.left * 1.5f);
            yield return new WaitForSeconds(.1f);
            Assert.That(actor.HasAggro, Is.False, "Proximity alone cannot start combat during the intro.");
            actor.ApplyBulletHit(1, actor.transform.position, Vector2.right);
            Assert.That(actor.HasAggro, Is.False, "Bullet stagger cannot bypass scripted ownership.");
            yield return new WaitForSeconds(.15f);
            context.Flashlight.AimAt(actor.transform.position);
            context.Flashlight.SetPower(true);
            yield return new WaitForSeconds(actor.ReactionDelay + .08f);
            Assert.That(route.State, Is.Not.EqualTo(ScriptedRouteState.WaitingForTrigger));
            Assert.That(route.State, Is.EqualTo(ScriptedRouteState.Reacting));
            Assert.That(actor.GetComponent<StalkerAudio2D>().PlaybackCount(StalkerAudioSet.Cue.EyeBurn), Is.GreaterThan(0));
            Assert.That(actor.HasAggro, Is.False);
            context.Flashlight.SetPower(false);
            MovePlayer(Vector2.zero);

            var samples = new List<RouteSample>();
            bool passedBelowBlock = false;
            float deadline = Time.time + 15;
            while (route.ControlsActor && Time.time < deadline)
            {
                Vector2 position = actor.transform.position;
                samples.Add(new RouteSample { x = position.x, y = position.y, point = route.PointIndex, state = route.State.ToString(), aggro = actor.HasAggro });
                passedBelowBlock |= position.x > 22 && position.x < 25 && position.y < -1.6f;
                Assert.That(actor.HasAggro, Is.False);
                Assert.That(actor.State, Is.Not.EqualTo(ShadowStalker2D.BehaviourState.Attacking));
                yield return new WaitForFixedUpdate();
            }

            Assert.That(route.State, Is.EqualTo(ScriptedRouteState.Completed));
            Assert.That(passedBelowBlock, Is.True, "The authored route must use the bottom side of the left block.");
            Vector2 end = route.RoutePoints[route.RoutePoints.Length - 1].position;
            Assert.That(Vector2.Distance(actor.transform.position, end), Is.LessThan(.2f));
            yield return new WaitForSeconds(.2f);
            Assert.That(actor.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Dormant));
            Assert.That(actor.HasAggro, Is.False);
            Assert.That(actor.GetComponent<EnemyPatrol2D>().PatrolEnabled, Is.False);
            Vector2 settled = actor.transform.position;
            yield return new WaitForSeconds(.25f);
            Assert.That(Vector2.Distance(actor.transform.position, settled), Is.LessThan(.01f));

            MovePlayer(end + Vector2.up * 1.5f);
            yield return new WaitForSeconds(.08f);
            Assert.That(actor.HasAggro, Is.True, "Normal proximity and sight resume after arrival.");
            context.Flashlight.AimAt(actor.transform.position);
            context.Flashlight.SetPower(true);
            yield return new WaitForSeconds(actor.ReactionDelay + .12f);
            Assert.That(actor.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Recoiling), "The ordinary delayed light reaction resumes.");
            Assert.That(route.State, Is.EqualTo(ScriptedRouteState.Completed), "Light cannot replay the one-shot route.");
            context.Flashlight.SetPower(false);
            MovePlayer(Vector2.zero);
            yield return new WaitForSeconds(.1f);
            Assert.That(actor.HasAggro, Is.True, "Ordinary permanent aggression remains permanent.");
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "Tests", "ScriptedIntro", "flashlight-route.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(new RouteReport
            {
                route = route.EncounterId,
                completed = true,
                passedBelowBlock = passedBelowBlock,
                restingPosition = new[] { settled.x, settled.y },
                normalAggroAfterArrival = actor.HasAggro,
                samples = samples.ToArray()
            }, true));
        }

        [UnityTest]
        public IEnumerator LightCannotLaunchDoorGroupBeforeDoorOpens()
        {
            foreach (ScriptedStalkerRoute2D member in group)
            {
                member.gameObject.SetActive(true);
            }

            MovePlayer(new Vector2(8.7f, 1.05f));
            context.Flashlight.AimAt(actor.transform.position);
            context.Flashlight.SetPower(true);
            yield return new WaitForSeconds(.3f);
            Assert.That(group.Any(value => value.Exposed), Is.True, "The beam must really reach the group for this check.");
            foreach (ScriptedStalkerRoute2D member in group)
            {
                Assert.That(member.State, Is.EqualTo(ScriptedRouteState.WaitingForTrigger));
                Assert.That(member.GetComponent<ShadowStalker2D>().HasAggro, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator FullDoorOpeningScattersAllFourOnceToTheirOwnPositionsWithoutLightOrAggro()
        {
            Assert.That(group.Length, Is.EqualTo(4));
            Vector2[] starts = group.Select(value => (Vector2)value.transform.position).ToArray();
            foreach (ScriptedStalkerRoute2D member in group)
            {
                member.gameObject.SetActive(true);
                Assert.That(member.StartCondition, Is.EqualTo(ScriptedRouteStartCondition.DoorOpened));
                Assert.That(member.TriggerDoor, Is.SameAs(route.TriggerDoor));
            }

            yield return new WaitForSeconds(.15f);
            for (int index = 0; index < group.Length; index++)
            {
                Assert.That(group[index].State, Is.EqualTo(ScriptedRouteState.WaitingForTrigger));
                Assert.That(Vector2.Distance(group[index].transform.position, starts[index]), Is.LessThan(.02f));
            }

            context.Flashlight.SetPower(false);
            MovePlayer(Vector2.zero);
            route.TriggerDoor.Toggle();
            Assert.That(route.TriggerDoor.IsMoving, Is.True);
            yield return new WaitForFixedUpdate();
            Assert.That(group.All(value => value.State == ScriptedRouteState.WaitingForTrigger), Is.True,
                "Opening intent/animation must not launch the group before the door is open.");

            var samples = new List<GroupSample>();
            float openedAt = float.NaN;
            float[] firstMovement = Enumerable.Repeat(float.NaN, group.Length).ToArray();
            float deadline = Time.time + 22;
            while (group.Any(value => value.ControlsActor) && Time.time < deadline)
            {
                if (float.IsNaN(openedAt) && route.TriggerDoor.IsOpen && !route.TriggerDoor.IsMoving)
                {
                    openedAt = Time.fixedTime;
                }

                for (int index = 0; index < group.Length; index++)
                {
                    ScriptedStalkerRoute2D member = group[index];
                    var stalker = member.GetComponent<ShadowStalker2D>();
                    float distanceFromStart = Vector2.Distance(member.transform.position, starts[index]);
                    if (float.IsNaN(firstMovement[index]) && distanceFromStart > .025f)
                    {
                        firstMovement[index] = Time.fixedTime;
                    }

                    if (member.State == ScriptedRouteState.WaitingToStart)
                    {
                        Assert.That(distanceFromStart, Is.LessThan(.02f), "An actor must remain still during its start delay.");
                        Assert.That(member.ControlsActor, Is.True, "Start delay retains scripted ownership.");
                        Assert.That(stalker.Closure, Is.Zero, "The delay is not a light recoil animation.");
                    }

                    Assert.That(stalker.HasAggro, Is.False, member.EncounterId);
                    Assert.That(stalker.State, Is.Not.EqualTo(ShadowStalker2D.BehaviourState.Attacking));
                    Assert.That(stalker.EyeDamageProgress, Is.Zero, "Door opening must not create eye damage: " + member.EncounterId);
                    Assert.That(member.GetComponent<StalkerAudio2D>().PlaybackCount(StalkerAudioSet.Cue.EyeBurn), Is.Zero,
                        "Door opening must not play an eye burn: " + member.EncounterId);
                    samples.Add(new GroupSample { id = member.EncounterId, x = member.transform.position.x,
                        y = member.transform.position.y, point = member.PointIndex, state = member.State.ToString(), time = Time.fixedTime });
                }

                yield return new WaitForFixedUpdate();
            }

            int[] order = Enumerable.Range(0, group.Length).OrderBy(index => group[index].StartDelaySeconds).ToArray();
            Assert.That(order.Select(index => group[index].StartDelaySeconds), Is.EqualTo(new[] { 0, .2f, .45f, .7f }),
                "The authored four starts must have individual offsets.");
            foreach (int index in order)
            {
                Assert.That(float.IsNaN(firstMovement[index]), Is.False, group[index].EncounterId);
                Assert.That(firstMovement[index] - openedAt, Is.EqualTo(group[index].StartDelaySeconds).Within(.08f), group[index].EncounterId);
            }

            for (int index = 1; index < order.Length; index++)
            {
                Assert.That(firstMovement[order[index]] - firstMovement[order[index - 1]], Is.GreaterThan(.1f),
                    "Each actor must visibly start after the previous one.");
            }

            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "Tests", "ScriptedIntro", "group-route.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(new GroupReport
            {
                completed = group.All(value => value.State == ScriptedRouteState.Completed),
                starts = order.Select(index => new StartSample { id = group[index].EncounterId, configuredDelay = group[index].StartDelaySeconds, actualDelay = firstMovement[index] - openedAt }).ToArray(),
                samples = samples.ToArray()
            }, true));
            foreach (ScriptedStalkerRoute2D member in group)
            {
                Assert.That(member.State, Is.EqualTo(ScriptedRouteState.Completed), member.EncounterId);
                Vector2 goal = member.RoutePoints.Last().position;
                Assert.That(Vector2.Distance(member.transform.position, goal), Is.LessThan(.2f), member.EncounterId);
                Assert.That(member.GetComponent<EnemyPatrol2D>().PatrolEnabled, Is.False);
            }

            Vector2[] settled = group.Select(value => (Vector2)value.transform.position).ToArray();
            route.TriggerDoor.Toggle();
            yield return new WaitForSeconds(.4f);
            route.TriggerDoor.Toggle();
            yield return new WaitForSeconds(.5f);
            for (int index = 0; index < group.Length; index++)
            {
                Assert.That(group[index].State, Is.EqualTo(ScriptedRouteState.Completed));
                Assert.That(Vector2.Distance(group[index].transform.position, settled[index]), Is.LessThan(.01f));
            }

        }

        [UnityTest]
        public IEnumerator DeathDuringTheRouteImmediatelyCancelsItAndCannotReviveTheActor()
        {
            yield return StartRoute();
            int deaths = 0;
            Action died = () => deaths++;
            var health = actor.GetComponent<EnemyHealth2D>();
            health.Died += died;
            try
            {
                actor.ApplyBulletHit(9999, actor.transform.position, Vector2.right);
                Assert.That(route.State, Is.EqualTo(ScriptedRouteState.Aborted));
                Assert.That(actor.IsAlive, Is.False);
                Assert.That(actor.GetComponent<Rigidbody2D>().simulated, Is.False);
                actor.ApplyBulletHit(9999, actor.transform.position, Vector2.right);
                yield return new WaitForSeconds(.2f);
                Assert.That(deaths, Is.EqualTo(1));
                Assert.That(actor.State, Is.EqualTo(ShadowStalker2D.BehaviourState.Dead));
            }
            finally
            {
                health.Died -= died;
            }
        }

        [UnityTest]
        public IEnumerator TemporaryObstructionKeepsOwnershipThenRetriesTheRoute()
        {
            var blocker = new GameObject("Intro route temporary full corridor obstruction");
            SceneManager.MoveGameObjectToScene(blocker, scene);
            blocker.layer = 8;
            blocker.transform.position = new Vector3(12, 0, 0);
            blocker.AddComponent<BoxCollider2D>().size = new Vector2(.4f, 3.5f);
            Physics2D.SyncTransforms();
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Scripted route blocked; retrying without aggression:"));
            yield return StartRoute();
            float deadline = Time.time + 8;
            while (route.State != ScriptedRouteState.Blocked && Time.time < deadline)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(route.State, Is.EqualTo(ScriptedRouteState.Blocked));
            Assert.That(route.ControlsActor, Is.True);
            Assert.That(actor.HasAggro, Is.False);
            blocker.SetActive(false);
            Physics2D.SyncTransforms();
            deadline = Time.time + 15;
            while (route.ControlsActor && Time.time < deadline)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(route.State, Is.EqualTo(ScriptedRouteState.Completed));
            Assert.That(actor.HasAggro, Is.False);
        }

        private IEnumerator StartRoute()
        {
            MovePlayer(Vector2.zero);
            route.TriggerDoor.Toggle();
            yield return new WaitForSeconds(.4f);
            Assert.That(route.State, Is.Not.EqualTo(ScriptedRouteState.WaitingForTrigger));
            context.Flashlight.SetPower(false);
            MovePlayer(Vector2.zero);
        }

        [Serializable]
        private sealed class GroupSample
        {
            public string id;
            public float x;
            public float y;
            public int point;
            public string state;
            public float time;
        }

        [Serializable]
        private sealed class StartSample
        {
            public string id;
            public float configuredDelay;
            public float actualDelay;
        }

        [Serializable]
        private sealed class GroupReport
        {
            public bool completed;
            public StartSample[] starts;
            public GroupSample[] samples;
        }

        private void MovePlayer(Vector2 position)
        {
            playerBody.position = position;
            playerBody.linearVelocity = Vector2.zero;
            context.Player.transform.position = position;
            Physics2D.SyncTransforms();
        }

        [Serializable]
        private sealed class RouteSample
        {
            public float x;
            public float y;
            public int point;
            public string state;
            public bool aggro;
        }

        [Serializable]
        private sealed class RouteReport
        {
            public string route;
            public bool completed;
            public bool passedBelowBlock;
            public float[] restingPosition;
            public bool normalAggroAfterArrival;
            public RouteSample[] samples;
        }
    }
}
#endif
