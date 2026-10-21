using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class CameraOpticsLifetimeTests
    {
        private Scene scene;
        private Scene previous;
        private CameraFramingProfile profile;

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

            UnityEngine.Object.Destroy(profile);
        }

        [UnityTest]
        public IEnumerator EventsResizeAndRebindingOwnTheirSubscriptionsWithoutPerFrameOpticsReads()
        {
            previous = SceneManager.GetActiveScene();
            scene = SceneManager.CreateScene("Camera optics " + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(scene);
            var first = new GameObject("First projection target").AddComponent<CameraOpticsTestSource>();
            var owner = new GameObject("Independent presentation camera");
            owner.SetActive(false);
            var camera = owner.AddComponent<Camera>();
            camera.orthographic = true;
            camera.aspect = 1.5f;
            var follow = owner.AddComponent<CameraFollow2D>();
            profile = ScriptableObject.CreateInstance<CameraFramingProfile>();
            profile.unarmedViewRadius = 2;
            profile.lightRangeMargin = .25f;
            follow.SetFraming(profile);
            follow.Configure(first.transform);
            owner.SetActive(true);
            yield return null;
            Assert.That(first.Subscribers, Is.EqualTo(2));
            int reads = first.RangeReads;
            float initial = camera.orthographicSize;
            follow.SnapToTarget();
            follow.SnapToTarget();
            Assert.That(first.RangeReads, Is.EqualTo(reads));
            first.SetOwned(true);
            follow.SnapToTarget();
            Assert.That(camera.orthographicSize, Is.GreaterThan(initial));
            first.SetRange(5);
            follow.SnapToTarget();
            Assert.That(camera.orthographicSize, Is.EqualTo(5.25f).Within(.0001));
            camera.aspect = .75f;
            follow.SnapToTarget();
            Assert.That(camera.orthographicSize, Is.EqualTo(7).Within(.0001));
            follow.enabled = false;
            yield return null;
            Assert.That(first.Subscribers, Is.Zero);
            first.SetRange(6);
            follow.enabled = true;
            yield return null;
            follow.SnapToTarget();
            Assert.That(first.Subscribers, Is.EqualTo(2));
            Assert.That(camera.orthographicSize, Is.EqualTo(6.25f / .75f).Within(.0001));
            var second = new GameObject("Second projection target").AddComponent<CameraOpticsTestSource>();
            second.SetOwned(true);
            second.SetRange(7);
            follow.Configure(second.transform);
            follow.SnapToTarget();
            Assert.That(first.Subscribers, Is.Zero);
            Assert.That(second.Subscribers, Is.EqualTo(2));
            int secondReads = second.RangeReads;
            first.SetRange(100);
            follow.SnapToTarget();
            Assert.That(second.RangeReads, Is.EqualTo(secondReads));
            UnityEngine.Object.Destroy(owner);
            yield return null;
            Assert.That(second.Subscribers, Is.Zero);
        }
    }
}
