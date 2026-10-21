#if UNITY_EDITOR
using System.Collections;
using EmergencyShift.Features.PlayerInput.Logic;
using EmergencyShift.Features.PlayerMovement;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class PlayerMovementSceneTests
    {
        private Scene scene;
        private Scene previous;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
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
        public IEnumerator AuthoredPlayerMovesStopsForModalAndClearsVelocityOnDisable()
        {
            previous = SceneManager.GetActiveScene();
            const string path = "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath(path);
            SceneManager.SetActiveScene(scene);
            LevelContext2D context = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (ShadowStalker2D actor in root.GetComponentsInChildren<ShadowStalker2D>(true))
                {
                    actor.gameObject.SetActive(false);
                }

                foreach (Sleeper2D actor in root.GetComponentsInChildren<Sleeper2D>(true))
                {
                    actor.gameObject.SetActive(false);
                }

                context = context != null ? context : root.GetComponentInChildren<LevelContext2D>(true);
            }

            Assert.That(context, Is.Not.Null);
            var motor = context.Player.GetComponent<PlayerMotor2D>();
            var body = context.Player.GetComponent<Rigidbody2D>();
            Assert.That(motor, Is.Not.Null);
            Assert.That(body, Is.Not.Null);
            var input = new Input { Intent = Vector2.one };
            var block = new GameplayInputBlock();
            motor.Construct(new PlayerMovementControllerFactory(input, block));
            body.position = new Vector2(230, 230);
            Vector2 start = body.position;
            yield return null;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(body.linearVelocity.magnitude, Is.EqualTo(3.5f).Within(.001f));
            Assert.That(body.linearVelocity.x, Is.EqualTo(body.linearVelocity.y).Within(.001f));
            Assert.That(Vector2.Distance(body.position, start), Is.GreaterThan(.01f));
            using (block.Acquire())
            {
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
            }

            yield return null;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(body.linearVelocity.magnitude, Is.EqualTo(3.5f).Within(.001f));
            motor.enabled = false;
            Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
            input.Intent = new Vector2(float.NaN, 0);
            motor.enabled = true;
            yield return null;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
        }

        private sealed class Input : IPlayerInput
        {
            public Vector2 Intent;
            public Vector2 Movement => Intent;
            public Vector2 PointerPosition => Vector2.zero;
            public bool HasPointer => false;
            public bool FirePressed => false;
            public bool FireHeld => false;
            public bool InteractPressed => false;
            public bool ReloadPressed => false;
            public bool SelectPistolPressed => false;
            public bool SelectShotgunPressed => false;
            public bool ToggleLightPressed => false;
            public bool RestartPressed => false;
            public bool RefillAmmoPressed => false;
            public bool DialogueAdvancePressed => false;
            public bool PausePressed => false;
            public bool ConfirmationHeld => false;
        }
    }
}
#endif
