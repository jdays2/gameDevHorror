#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
            Assert.That(route.State, Is.EqualTo(ScriptedRouteState.WaitingForLight));
            Assert.That(actor.HasAggro, Is.False);
            context.Flashlight.AimAt(actor.transform.position);
            context.Flashlight.SetPower(true);
            yield return new WaitForSeconds(.12f);
            Assert.That(route.State, Is.EqualTo(ScriptedRouteState.WaitingForLight), "The closed starting door must block the trigger.");
            context.Flashlight.SetPower(false);
            MovePlayer(new Vector2(5.4f, 1.3f));
            yield return new WaitForSeconds(.1f);
            Assert.That(actor.HasAggro, Is.False, "Proximity alone cannot start combat during the intro.");
            actor.ApplyBulletHit(1, actor.transform.position, Vector2.right);
            Assert.That(actor.HasAggro, Is.False, "Bullet stagger cannot bypass scripted ownership.");
            yield return new WaitForSeconds(.15f);
            context.Flashlight.AimAt(actor.transform.position);
            context.Flashlight.SetPower(true);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(route.State, Is.Not.EqualTo(ScriptedRouteState.WaitingForLight));
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
            Assert.That(actor.Closure, Is.GreaterThan(0), "The ordinary delayed light reaction resumes.");
            Assert.That(route.State, Is.EqualTo(ScriptedRouteState.Completed), "Light cannot replay the one-shot route.");
            context.Flashlight.SetPower(false);
            MovePlayer(Vector2.zero);
            yield return new WaitForSeconds(.1f);
            Assert.That(actor.HasAggro, Is.True, "Ordinary permanent aggression remains permanent.");
            Directory.CreateDirectory("Docs/Validation/IntroEncounter01");
            File.WriteAllText("Docs/Validation/IntroEncounter01/live-route.json", JsonUtility.ToJson(new RouteReport
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
            MovePlayer(new Vector2(5.4f, 1.3f));
            context.Flashlight.AimAt(actor.transform.position);
            context.Flashlight.SetPower(true);
            yield return new WaitForSeconds(.2f);
            Assert.That(route.State, Is.Not.EqualTo(ScriptedRouteState.WaitingForLight));
            context.Flashlight.SetPower(false);
            MovePlayer(Vector2.zero);
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
