#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class FlashlightProgressionSceneTests
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
        public IEnumerator AuthoredRouteCameraExpandsForAcquisitionAndUpgradeAndRestoresBase()
        {
            previous = SceneManager.GetActiveScene();
            const string path = "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath(path);
            // Async load completes before the camera's Start caches its authored target/optics.
            yield return null;
            LevelContext2D context = null;
            CameraFollow2D camera = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                context = context != null ? context : root.GetComponentInChildren<LevelContext2D>(true);
                camera = camera != null ? camera : root.GetComponentInChildren<CameraFollow2D>(true);
            }

            Assert.That(context, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);
            var light = context.Player.GetComponent<Flashlight2D>();
            var upgrades = context.Player.GetComponent<FlashlightUpgrades>();
            var projection = camera.GetComponent<Camera>();
            float baseRange = light.Range;
            Assert.That(light.IsOwned, Is.False);
            camera.SnapToTarget();
            float unarmed = projection.orthographicSize;
            Assert.That(light.Acquire(), Is.True);
            camera.SnapToTarget();
            float acquired = projection.orthographicSize;
            Assert.That(acquired, Is.GreaterThan(unarmed));
            Assert.That(upgrades.TryInstall("scene-test-lens", "Test", 1, 0, out _, true), Is.True);
            Assert.That(light.Range, Is.EqualTo(baseRange + 1).Within(.0001));
            Assert.That(light.IsOn, Is.True, "An optics upgrade preserves flashlight power.");
            camera.SnapToTarget();
            Assert.That(projection.orthographicSize, Is.GreaterThan(acquired));
            FlashlightUpgrades.ModuleState[] saved = upgrades.Capture();
            upgrades.Restore(null, 0);
            Assert.That(light.Range, Is.EqualTo(baseRange));
            upgrades.Restore(saved, 1);
            Assert.That(light.Range, Is.EqualTo(baseRange + 1).Within(.0001));
            upgrades.Restore(null, 0);
            light.RestoreOwnership(false);
            camera.SnapToTarget();
            Assert.That(projection.orthographicSize, Is.EqualTo(unarmed).Within(.0001));
        }
    }
}
#endif
