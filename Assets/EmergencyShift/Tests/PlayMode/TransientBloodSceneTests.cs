#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class TransientBloodSceneTests
    {
        private Scene ownerScene;
        private Scene foreignScene;
        private Scene previousScene;
        private float previousScale;
        private Sprite sprite;
        private Material material;
        private BloodPool2D poolPrefab;
        private GoreProfile profile;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousScene = SceneManager.GetActiveScene();
            previousScale = Time.timeScale;
            Time.timeScale = 0;
            ownerScene = SceneManager.CreateScene("Blood owner " + Guid.NewGuid().ToString("N"));
            foreignScene = SceneManager.CreateScene("Foreign active " + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(foreignScene);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/ShadowStalker.prefab");
            var gore = prefab.GetComponent<GoreBurst2D>();
            sprite = Get<Sprite>(gore, "bloodSpraySprite");
            material = Get<Material>(gore, "litMaterial");
            poolPrefab = Get<BloodPool2D>(gore, "bloodPoolPrefab");
            Assert.That(sprite, Is.Not.Null);
            Assert.That(material, Is.Not.Null);
            Assert.That(poolPrefab, Is.Not.Null);
            profile = ScriptableObject.CreateInstance<GoreProfile>();
            Set(profile, "releaseDelay", 0f);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (previousScene.IsValid() && previousScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousScene);
            }

            if (ownerScene.IsValid() && ownerScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(ownerScene);
            }

            if (foreignScene.IsValid() && foreignScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(foreignScene);
            }

            UnityEngine.Object.Destroy(profile);
            Time.timeScale = previousScale;
        }

        [UnityTest]
        public IEnumerator HitSprayUsesSourceScene() => Verify(false, false);

        [UnityTest]
        public IEnumerator RuptureUsesSourceScene() => Verify(true, false);

        [UnityTest]
        public IEnumerator HitSprayIsRemovedWithOwnerWhileItsTimerIsFrozen() => Verify(false, true);

        [UnityTest]
        public IEnumerator RuptureIsRemovedWithOwnerWhileItsTimerIsFrozen() => Verify(true, true);

        private IEnumerator Verify(bool rupture, bool unload)
        {
            Component effect = CreateEffect(rupture);
            Assert.That(effect, Is.Not.Null);
            if (unload)
            {
                yield return SceneManager.UnloadSceneAsync(ownerScene);
                Assert.That(effect == null, Is.True, "A frozen effect must be destroyed by its source scene, not its timer.");
                Assert.That(foreignScene.isLoaded, Is.True);
            }
            else
            {
                Assert.That(effect.gameObject.scene, Is.EqualTo(ownerScene));
                foreach (SpriteRenderer image in effect.GetComponentsInChildren<SpriteRenderer>())
                {
                    Assert.That(image.gameObject.scene, Is.EqualTo(ownerScene));
                }
            }
        }

        private Component CreateEffect(bool rupture)
        {
            var root = new GameObject("Blood effect owner fixture");
            SceneManager.MoveGameObjectToScene(root, ownerScene);
            if (rupture)
            {
                var gore = root.AddComponent<GoreBurst2D>();
                gore.Configure(poolPrefab, Array.Empty<Sprite>(), material);
                gore.ConfigureRupture(sprite, null, null);
                Set(gore, "profile", profile);
                Set(gore, "random", new Core.Randomness.SystemRandomSource(new System.Random(15)));
                gore.Play(0);
                var effects = UnityEngine.Object.FindObjectsByType<BloodRuptureBurst2D>(FindObjectsSortMode.None);
                Assert.That(effects.Length, Is.EqualTo(1));
                return effects[0];
            }

            var response = root.AddComponent<EnemyHitResponse2D>();
            response.Configure(root.transform, null, sprite, material);
            response.Play(Vector2.zero, Vector2.up, false);
            var sprays = UnityEngine.Object.FindObjectsByType<BloodHitSpray2D>(FindObjectsSortMode.None);
            Assert.That(sprays.Length, Is.EqualTo(1));
            return sprays[0];
        }

        private static T Get<T>(object target, string field)
        {
            return (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
#endif
