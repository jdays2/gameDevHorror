#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using EmergencyShift.Core.Composition;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zenject;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class MuzzleSmokeSceneTests
    {
        [UnityTest]
        public IEnumerator LateralGasEndsBeforeForwardCloudForBothWeapons()
        {
            var scope = new DiContainer();
            scope.Bind<SceneRegistry<ShotEffect2D>>().AsSingle();
            var owner = new GameObject("Lateral smoke lifetime");
            var prefabs = new ScenePrefabFactory(scope, owner.transform);
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/PistolShotEffect.prefab");
            float oldScale = Time.timeScale;
            Time.timeScale = 1;
            try
            {
                foreach (string name in new[] { "PistolDefinition", "PumpShotgun" })
                {
                    var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/" + name + ".asset");
                    var effect = prefabs.Instantiate(template, Vector3.zero, Quaternion.identity).GetComponent<ShotEffect2D>();
                    effect.Play(weapon.Visuals, false, Vector2.right);
                    var smoke = effect.GetComponentInChildren<MuzzleSmoke2D>();
                    Assert.That(smoke.HasSideBurst, Is.True);
                    Time.timeScale = 0;
                    yield return null;
                    Assert.That(smoke.HasSideBurst, Is.True);
                    Time.timeScale = 1;
                    yield return new WaitForSeconds(weapon.Visuals.SideSmokeSeconds + .04f);
                    Assert.That(smoke.HasSideBurst, Is.False, name);
                    Assert.That(effect.HasSmoke, Is.True, "Forward cloud outlives the sharp lateral burst.");
                    Assert.That(effect.LightSource.enabled, Is.False);
                    effect.enabled = false;
                    Assert.That(smoke.IsVisible, Is.False);
                    Object.Destroy(effect.gameObject);
                }
            }
            finally
            {
                Time.timeScale = oldScale;
                Object.Destroy(owner);
            }
        }

        [UnityTest]
        public IEnumerator CloudBudgetDoesNotExtendLightAndSourceUnloadCleansPausedSmoke()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene source = SceneManager.CreateScene("Smoke source");
            Scene foreign = SceneManager.CreateScene("Smoke foreign");
            float oldScale = Time.timeScale;
            var random = Random.state;
            try
            {
                Time.timeScale = 1;
                SceneManager.SetActiveScene(source);
                var owner = new GameObject("Source lifetime");
                var scope = new DiContainer();
                var registry = new SceneRegistry<ShotEffect2D>();
                scope.Bind<SceneRegistry<ShotEffect2D>>().FromInstance(registry);
                var prefabs = new ScenePrefabFactory(scope, owner.transform);
                var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/PistolShotEffect.prefab");
                var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PistolDefinition.asset");
                string config = EditorJsonUtility.ToJson(weapon.Visuals);
                SceneManager.SetActiveScene(foreign);
                for (int index = 0; index < 20; index++)
                {
                    var effect = prefabs.Instantiate(template, Vector3.zero, Quaternion.identity).GetComponent<ShotEffect2D>();
                    effect.Play(weapon.Visuals, false, Vector2.right);
                    Assert.That(registry.Members.Count(item => item.HasSmoke), Is.LessThanOrEqualTo(weapon.Visuals.MaximumSmokeClouds));
                }
                yield return new WaitForSeconds(.14f);
                Assert.That(registry.Members.Any(item => item.HasSmoke), Is.True);
                Assert.That(registry.Members.All(item => !item.LightSource.enabled), Is.True, "Smoke cannot extend flash illumination.");
                var survivor = registry.Members.First(item => item.HasSmoke);
                Assert.That(survivor.gameObject.scene, Is.EqualTo(source));
                Time.timeScale = 0;
                yield return null;
                yield return null;
                Assert.That(survivor.HasSmoke, Is.True, "Paused smoke must freeze rather than leak unscaled work.");
                Assert.That(EditorJsonUtility.ToJson(weapon.Visuals), Is.EqualTo(config));
                yield return SceneManager.UnloadSceneAsync(source);
                Assert.That(survivor == null, Is.True);
                Assert.That(registry.Members, Is.Empty);
            }
            finally
            {
                Time.timeScale = oldScale;
                Random.state = random;
                SceneManager.SetActiveScene(previous);
                if (source.IsValid() && source.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(source);
                }
                if (foreign.IsValid() && foreign.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(foreign);
                }
            }
        }

        [UnityTest]
        public IEnumerator ResumeFinishesCloudAndDisablingImmediatelyHidesIt()
        {
            var scope = new DiContainer();
            scope.Bind<SceneRegistry<ShotEffect2D>>().AsSingle();
            var owner = new GameObject("Smoke resume lifetime");
            var prefabs = new ScenePrefabFactory(scope, owner.transform);
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/PistolShotEffect.prefab");
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PumpShotgun.asset");
            var random = Random.state;
            float oldScale = Time.timeScale;
            var effect = prefabs.Instantiate(template, Vector3.zero, Quaternion.identity).GetComponent<ShotEffect2D>();
            try
            {
                effect.Play(weapon.Visuals, false, Vector2.up);
                Assert.That(effect.HasSmoke, Is.True);
                effect.enabled = false;
                Assert.That(effect.HasSmoke, Is.False);
                Assert.That(effect.LightSource.enabled, Is.False);
                effect.enabled = true;
                effect.Play(weapon.Visuals, false, Vector2.up);
                Time.timeScale = 0;
                yield return null;
                Assert.That(effect.HasSmoke, Is.True);
                Time.timeScale = 1;
                yield return new WaitForSeconds(weapon.Visuals.SmokeSeconds + .1f);
                Assert.That(effect == null, Is.True);
            }
            finally
            {
                Time.timeScale = oldScale;
                Random.state = random;
                if (effect != null)
                {
                    Object.Destroy(effect.gameObject);
                }
                Object.Destroy(owner);
            }
        }
    }
}
#endif
