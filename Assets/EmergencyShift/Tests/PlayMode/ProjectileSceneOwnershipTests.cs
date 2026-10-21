#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using EmergencyShift.Core.Composition;
using EmergencyShift.Features.Projectiles;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zenject;
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
        private readonly List<Object> assets = new List<Object>();

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
            foreach (Object asset in assets)
            {
                Object.Destroy(asset);
            }

            assets.Clear();
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

        [UnityTest]
        public IEnumerator InvisibleNativeBulletSkipsOwnerAndTriggerAndHitsNearestTargetOnce()
        {
            ProjectileProbeTarget owner = Target("Owner", 0);
            var child = new GameObject("Owner child", typeof(CircleCollider2D));
            child.layer = 9;
            child.transform.SetParent(owner.transform);
            child.transform.position = Vector2.right * .15f;
            child.GetComponent<CircleCollider2D>().radius = .04f;
            ProjectileProbeTarget trigger = Target("Trigger", .25f);
            trigger.GetComponent<CircleCollider2D>().isTrigger = true;
            ProjectileProbeTarget nearest = Target("Nearest", .45f);
            ProjectileProbeTarget farther = Target("Farther", .7f);
            var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PistolDefinition.asset");
            var style = Object.Instantiate(definition.Visuals);
            var visual = Object.Instantiate(style.projectile);
            assets.Add(style);
            assets.Add(visual);
            visual.projectileVisibility = 0;
            style.projectile = visual;
            BulletProjectile2D bullet = Spawn(owner.transform);
            bullet.Launch(owner.transform, Vector2.right, style, null, 1 << 9, 3, 3);
            Assert.That(bullet.GetComponentInChildren<SpriteRenderer>().enabled, Is.False);
            Physics2D.SyncTransforms();
            float deadline = Time.realtimeSinceStartup + 2;
            while (bullet != null && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(bullet == null, Is.True);
            Assert.That(owner.Hits, Is.Zero);
            Assert.That(trigger.Hits, Is.Zero);
            Assert.That(nearest.Hits, Is.EqualTo(1));
            Assert.That(nearest.Damage, Is.EqualTo(3));
            Assert.That(farther.Hits, Is.Zero);
        }

        [UnityTest]
        public IEnumerator NativeBulletCannotDamageBeyondItsRange()
        {
            ProjectileProbeTarget owner = Target("Range owner", 0);
            ProjectileProbeTarget beyond = Target("Beyond range", .4f);
            var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PistolDefinition.asset");
            BulletProjectile2D bullet = Spawn(owner.transform);
            bullet.Launch(owner.transform, Vector2.right, definition.Visuals, null, 1 << 9, 3, .1f);
            Physics2D.SyncTransforms();
            float deadline = Time.realtimeSinceStartup + 2;
            while (bullet != null && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(bullet == null, Is.True);
            Assert.That(beyond.Hits, Is.Zero);
        }

        private ProjectileProbeTarget Target(string name, float x)
        {
            var target = new GameObject(name, typeof(ProjectileProbeTarget), typeof(CircleCollider2D));
            SceneManager.MoveGameObjectToScene(target, ownerScene);
            target.layer = 9;
            target.transform.position = Vector2.right * x;
            target.GetComponent<CircleCollider2D>().radius = .05f;
            return target.GetComponent<ProjectileProbeTarget>();
        }

        private static BulletProjectile2D Spawn(Transform owner)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/PistolBullet.prefab");
            var scope = new DiContainer();
            ProjectileInstaller.Install(scope);
            scope.Bind<SceneRegistry<ShotEffect2D>>().AsSingle();
            var prefabs = new ScenePrefabFactory(scope, owner);
            scope.Bind<ScenePrefabFactory>().FromInstance(prefabs);
            return prefabs.Instantiate(prefab, Vector3.zero, Quaternion.identity).GetComponent<BulletProjectile2D>();
        }

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
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.01f, 2);
            ProjectileProbeTarget behindWall = Target("Behind thin wall", .7f);
            var scope = new DiContainer();
            ProjectileInstaller.Install(scope);
            scope.Bind<SceneRegistry<ShotEffect2D>>().AsSingle();
            var prefabs = new ScenePrefabFactory(scope, shooter.transform);
            scope.Bind<ScenePrefabFactory>().FromInstance(prefabs);
            var bullet = prefabs.Instantiate(bulletPrefab, Vector3.zero, Quaternion.identity).GetComponent<BulletProjectile2D>();
            bullet.Launch(shooter.transform, Vector2.right, definition.Visuals, effectPrefab, (1 << 8) | (1 << 9), 1, 2);
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
            Assert.That(behindWall.Hits, Is.Zero, "A full-segment cast must stop at the thin wall before the receiver.");
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
