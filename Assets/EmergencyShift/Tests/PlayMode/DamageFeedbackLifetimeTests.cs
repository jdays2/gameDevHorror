#if UNITY_EDITOR
using System;
using System.Collections;
using EmergencyShift.Features.DamageFeedback.View;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class DamageFeedbackLifetimeTests
    {
        private Scene previous;
        private Scene scene;
        private float timeScale;

        [SetUp]
        public void Setup()
        {
            previous = SceneManager.GetActiveScene();
            timeScale = Time.timeScale;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = timeScale;
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
        public IEnumerator CameraOffsetDoesNotAccumulateAndClearsOnPauseDisableAndSnap()
        {
            scene = SceneManager.CreateScene("Camera impact " + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(scene);
            var target = new GameObject("Stationary impact target");
            target.transform.position = new Vector3(400, 400, 0);
            var owner = new GameObject("Camera impact presentation");
            owner.SetActive(false);
            var camera = owner.AddComponent<Camera>();
            camera.orthographic = true;
            var follow = owner.AddComponent<CameraFollow2D>();
            follow.Configure(target.transform);
            owner.SetActive(true);
            yield return null;
            follow.SnapToTarget();
            Vector3 baseline = owner.transform.position;
            float size = camera.orthographicSize;
            Quaternion rotation = owner.transform.rotation;
            follow.SetImpactOffset(new Vector2(.08f, 0));
            for (int frame = 0; frame < 12; frame++)
            {
                yield return null;
            }
            Assert.That(owner.transform.position.x - baseline.x, Is.EqualTo(.08f).Within(.0001f));
            Assert.That(camera.orthographicSize, Is.EqualTo(size));
            Assert.That(owner.transform.rotation, Is.EqualTo(rotation));
            Time.timeScale = 0;
            yield return null;
            yield return null;
            Assert.That(Vector3.Distance(owner.transform.position, baseline), Is.LessThan(.0001f));
            follow.SetImpactOffset(Vector2.zero);
            Time.timeScale = 1;
            yield return null;
            follow.SetImpactOffset(new Vector2(0, -.08f));
            yield return null;
            yield return null;
            follow.enabled = false;
            Assert.That(Vector3.Distance(owner.transform.position, baseline), Is.LessThan(.0001f));
            follow.enabled = true;
            follow.SetImpactOffset(new Vector2(.08f, 0));
            yield return null;
            follow.SnapToTarget();
            Assert.That(Vector3.Distance(owner.transform.position, baseline), Is.LessThan(.0001f));
        }

        [UnityTest]
        public IEnumerator AcceptedSceneDamageShowsLongerBloodAndOneKickButRestoreClearsBoth()
        {
            const string path = "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath(path);
            SceneManager.SetActiveScene(scene);
            LevelContext2D context = null;
            DirectionalDamageFeedbackView feedback = null;
            CameraFollow2D follow = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (EnemyHealth2D enemy in root.GetComponentsInChildren<EnemyHealth2D>(true))
                {
                    enemy.gameObject.SetActive(false);
                }
                context = context != null ? context : root.GetComponentInChildren<LevelContext2D>(true);
                feedback = feedback != null ? feedback : root.GetComponentInChildren<DirectionalDamageFeedbackView>(true);
                follow = follow != null ? follow : root.GetComponentInChildren<CameraFollow2D>(true);
            }
            Assert.That(context, Is.Not.Null);
            Assert.That(feedback, Is.Not.Null);
            Assert.That(follow, Is.Not.Null);
            PlayerLife2D player = context.Player;
            player.GetComponent<PlayerMotor2D>().enabled = false;
            var body = player.GetComponent<Rigidbody2D>();
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            body.position = new Vector2(500, 500);
            player.transform.position = new Vector3(500, 500, 0);
            player.SetHealthCapacity(100, 100);
            yield return null;
            follow.SnapToTarget();
            Vector3 baseline = follow.transform.position;
            var image = feedback.GetComponent<UnityEngine.UI.RawImage>();
            Assert.That(player.TryTakeDamage(1, player.transform.position, Vector2.right), Is.True);
            Assert.That(player.TryTakeDamage(1, player.transform.position, Vector2.right), Is.False, "Protection must reject the second hit.");
            Assert.That(image.enabled, Is.True);
            yield return new WaitForSecondsRealtime(.04f);
            Assert.That(Vector3.Distance(follow.transform.position, baseline), Is.GreaterThan(.005f));
            Assert.That(Vector3.Distance(follow.transform.position, baseline), Is.LessThan(.081f));
            yield return new WaitForSecondsRealtime(1);
            Assert.That(image.enabled, Is.True, "The blood must remain visible beyond the previous whole lifetime.");
            Assert.That(Vector3.Distance(follow.transform.position, baseline), Is.LessThan(.0001f));
            player.RestoreHealth(100);
            Assert.That(image.enabled, Is.False);
            Assert.That(player.TryTakeDamage(1, player.transform.position, Vector2.left), Is.True);
            yield return new WaitForSecondsRealtime(.04f);
            feedback.enabled = false;
            yield return null;
            yield return null;
            Assert.That(image.enabled, Is.False);
            Assert.That(Vector3.Distance(follow.transform.position, baseline), Is.LessThan(.0001f));
        }
    }
}
#endif
