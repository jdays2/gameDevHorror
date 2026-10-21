#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class ProjectileSceneOwnershipTests
    {
        private Scene ownerScene;
        private Scene foreignScene;
        private Scene previous;
        private float previousScale;
        private UnityEngine.Random.State previousRandom;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            previous = SceneManager.GetActiveScene();
            previousScale = Time.timeScale;
            previousRandom = UnityEngine.Random.state;
            Time.timeScale = 1;
            ownerScene = SceneManager.CreateScene("Projectile owner " + Guid.NewGuid().ToString("N"));
            foreignScene = SceneManager.CreateScene("Foreign projectile scene " + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(foreignScene);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = previousScale;
            UnityEngine.Random.state = previousRandom;
            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }

            if (ownerScene.IsValid() && ownerScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(ownerScene);
            }

            if (foreignScene.IsValid() && foreignScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(foreignScene);
            }
        }

        [UnityTest]
        public IEnumerator FlyingBulletImpactBelongsToSource() => Verify(false);

        [UnityTest]
        public IEnumerator FlyingBulletImpactUnloadsWithSourceWhileFrozen() => Verify(true);

        private IEnumerator Verify(bool unload)
        {
            var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PistolDefinition.asset");
            var bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/PistolBullet.prefab");
            var effectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/PistolShotEffect.prefab").GetComponent<ShotEffect2D>();
            var shooter = new GameObject("Projectile source");
            SceneManager.MoveGameObjectToScene(shooter, ownerScene);
            var wall = new GameObject("Projectile collision wall");
            SceneManager.MoveGameObjectToScene(wall, ownerScene);
            wall.layer = 8;
            wall.transform.position = Vector3.right * .5f;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.1f, 2);
            var bullet = Object.Instantiate(bulletPrefab).GetComponent<BulletProjectile2D>();
            SceneManager.MoveGameObjectToScene(bullet.gameObject, ownerScene);
            bullet.Launch(shooter.transform, Vector2.right, definition.visuals, effectPrefab, 1 << 8, 1, 2);
            Physics2D.SyncTransforms();
            ShotEffect2D impact = null;
            float deadline = Time.realtimeSinceStartup + 2;
            while (impact == null && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForFixedUpdate();
                foreach (ShotEffect2D effect in Object.FindObjectsByType<ShotEffect2D>(FindObjectsSortMode.None))
                {
                    impact = effect;
                }
            }

            Assert.That(impact, Is.Not.Null, "The native cast must actually produce an impact.");
            Time.timeScale = 0;
            if (unload)
            {
                yield return SceneManager.UnloadSceneAsync(ownerScene);
                Assert.That(impact == null, Is.True, "Frozen VFX must disappear with the source scene.");
            }
            else
            {
                Assert.That(impact.gameObject.scene.handle, Is.EqualTo(ownerScene.handle),
                    $"Impact scene: {impact.gameObject.scene.name}; expected source: {ownerScene.name}");
            }
            Assert.That(foreignScene.isLoaded, Is.True);
        }
    }
}
#endif
