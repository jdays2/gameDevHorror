#if UNITY_EDITOR
using System;
using System.Collections;
using EmergencyShift.Core.Composition;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Interaction;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class LightScopeSceneTests
    {
        private Scene first;
        private Scene second;
        private PlayerInteraction actor;
        private UnityEngine.Random.State randomState;
        private float timeScale;
        private SceneRegistry<LocalLamp2D> lampsA;
        private SceneRegistry<LocalLamp2D> lampsB;
        private SceneRegistry<ShotEffect2D> effectsA;
        private SceneRegistry<ShotEffect2D> effectsB;
        private LocalIlluminationQuery2D queryA;
        private LocalIlluminationQuery2D queryB;
        private readonly Vector2 point = new Vector2(950, 950);

        [UnitySetUp]
        public IEnumerator Setup()
        {
            randomState = UnityEngine.Random.state;
            timeScale = Time.timeScale;
            Time.timeScale = 1;
            lampsA = new SceneRegistry<LocalLamp2D>();
            lampsB = new SceneRegistry<LocalLamp2D>();
            effectsA = new SceneRegistry<ShotEffect2D>();
            effectsB = new SceneRegistry<ShotEffect2D>();
            queryA = new LocalIlluminationQuery2D(lampsA, effectsA);
            queryB = new LocalIlluminationQuery2D(lampsB, effectsB);
            first = SceneManager.CreateScene("Light scope A " + Guid.NewGuid().ToString("N"));
            second = SceneManager.CreateScene("Light scope B " + Guid.NewGuid().ToString("N"));
            var owner = new GameObject("Inactive light-query actor");
            owner.SetActive(false);
            SceneManager.MoveGameObjectToScene(owner, first);
            actor = owner.AddComponent<PlayerInteraction>();
            actor.Construct(null, new GameplayInputBlock(), new SceneRegistry<Door2D>(),
                new PlayerInteractionControllerFactory(new UnityGameClock()), queryA);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            UnityEngine.Random.state = randomState;
            Time.timeScale = timeScale;
            if (second.IsValid() && second.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(second);
            }

            if (first.IsValid() && first.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(first);
            }
        }

        [UnityTest]
        public IEnumerator ForeignAdditiveLampCannotLightAnActorsPickupQuery()
        {
            LocalLamp2D lamp = Lamp(second, lampsB);
            Assert.That(lamp.Illuminates(point + Vector2.right * .1f, 1 << 8), Is.True, "The foreign lamp must actually illuminate the test point.");
            Assert.That(queryB.AnyLampIlluminates(second, point + Vector2.right * .1f, 1 << 8), Is.True);
            Assert.That(lampsB.Members.Count, Is.EqualTo(1));
            Assert.That(lampsA.Members.Count, Is.Zero);
            Assert.That(actor.IsCurrentlyLit(point + Vector2.right * .1f), Is.False, "Scene A must not see the lamp belonging to scene B.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ForeignAdditiveShotCannotLightAnActorsPickupQuery()
        {
            Effect(second, effectsB);
            Assert.That(queryB.AnyIlluminates(second, point + Vector2.right * .1f, 1 << 8), Is.True);
            Assert.That(actor.IsCurrentlyLit(point + Vector2.right * .1f), Is.False, "Scene A must not see the configured flash belonging to B.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator OwnLampRegistersOnceAndDisableRemovesIt()
        {
            LocalLamp2D lamp = Lamp(first, lampsA);
            lamp.Construct(lampsA);
            Assert.That(lampsA.Members.Count, Is.EqualTo(1));
            Assert.That(actor.IsCurrentlyLit(point + Vector2.right * .1f), Is.True);
            lamp.enabled = false;
            Assert.That(lampsA.Members.Count, Is.Zero);
            Assert.That(actor.IsCurrentlyLit(point + Vector2.right * .1f), Is.False);
            lamp.enabled = true;
            Assert.That(lampsA.Members.Count, Is.EqualTo(1));
            Assert.That(actor.IsCurrentlyLit(point + Vector2.right * .1f), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FrozenEffectUnloadsWithItsSceneAndSiblingLampRemains()
        {
            Lamp(first, lampsA);
            Effect(second, effectsB);
            Time.timeScale = 0;
            Assert.That(effectsB.Members.Count, Is.EqualTo(1));
            yield return SceneManager.UnloadSceneAsync(second);
            Assert.That(effectsB.Members.Count, Is.Zero);
            Assert.That(queryB.AnyIlluminates(second, point + Vector2.right * .1f, 1 << 8), Is.False);
            Assert.That(actor.IsCurrentlyLit(point + Vector2.right * .1f), Is.True);
            Assert.That(lampsA.Members.Count, Is.EqualTo(1));
        }

        private LocalLamp2D Lamp(Scene scene, SceneRegistry<LocalLamp2D> registry)
        {
            var root = new GameObject("Scoped light fixture");
            root.SetActive(false);
            SceneManager.MoveGameObjectToScene(root, scene);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/World/EmergencyLamp.prefab");
            var instance = Object.Instantiate(prefab, point, Quaternion.identity, root.transform);
            var lamp = instance.GetComponentInChildren<LocalLamp2D>(true);
            foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != lamp)
                {
                    behaviour.enabled = false;
                }
            }

            lamp.Construct(registry);
            root.SetActive(true);
            lamp.SetPower(true);
            return lamp;
        }

        private ShotEffect2D Effect(Scene scene, SceneRegistry<ShotEffect2D> registry)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/PistolShotEffect.prefab");
            var instance = Object.Instantiate(prefab, point, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(instance, scene);
            var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PistolDefinition.asset");
            var effect = instance.GetComponent<ShotEffect2D>();
            effect.Construct(registry);
            effect.Play(definition.Visuals, false, Vector2.up);
            return effect;
        }
    }
}
#endif
