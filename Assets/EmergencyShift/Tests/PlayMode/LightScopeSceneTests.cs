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
            lamp.Construct(lampsA, new Features.LampPower.LampPowerControllerFactory(new UnityGameClock()));
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

        [UnityTest]
        public IEnumerator SceneOverrideKeepsIntensityColorAndBulbAlphaDuringInjectedDarkCycle()
        {
            var clock = new LampClock { Now = 4.95 };
            LocalLamp2D lamp = BareLamp(clock, out var bulb);
            var profile = ScriptableObject.CreateInstance<LocalLightProfile>();
            try
            {
                lamp.SetProfile(profile);
                var settings = new LocalLampSceneSettings { intensity = 1.7f, lightRadius = 4, color = Color.cyan };
                lamp.ConfigureScene(settings);
                lamp.SetPower(true);
                var source = lamp.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
                Assert.That(source.intensity, Is.EqualTo(1.7f));
                Assert.That(source.pointLightOuterRadius, Is.EqualTo(4));
                Assert.That(source.color, Is.EqualTo(Color.cyan));
                yield return null;
                Assert.That(source.enabled, Is.True);
                Assert.That(source.intensity, Is.Zero);
                Assert.That(bulb.color.a, Is.Zero);
                Assert.That(bulb.enabled, Is.False);
                clock.Now = 0;
                yield return null;
                Assert.That(source.intensity, Is.EqualTo(settings.intensity * settings.Brightness(0)));
                Assert.That(bulb.color.a, Is.EqualTo(settings.Brightness(0) * Mathf.Clamp01(settings.intensity / .28f)));
            }
            finally
            {
                Object.Destroy(profile);
            }
        }

        [UnityTest]
        public IEnumerator ProfileCyclePreservesDropoutAndPowerAcrossDisableReenable()
        {
            var clock = new LampClock { Now = 2.5 };
            LocalLamp2D lamp = BareLamp(clock, out var bulb);
            var profile = ScriptableObject.CreateInstance<LocalLightProfile>();
            JsonUtility.FromJsonOverwrite("{\"cycleSeconds\":10,\"minimumFactor\":1,\"noiseFrequency\":0,\"darkIntervals\":[{\"x\":0.2,\"y\":0.3}]}", profile);
            try
            {
                lamp.SetProfile(profile);
                yield return null;
                Assert.That(lamp.CurrentIntensity, Is.Zero);
                Assert.That(bulb.enabled, Is.False);
                clock.Now = 1;
                yield return null;
                Assert.That(lamp.CurrentIntensity, Is.EqualTo(profile.Intensity));
                Assert.That(bulb.color.a, Is.EqualTo(1));
                lamp.SetPower(false);
                lamp.enabled = false;
                lamp.enabled = true;
                yield return null;
                Assert.That(lamp.CurrentIntensity, Is.Zero);
                Assert.That(bulb.enabled, Is.False);
                lamp.SetPower(true);
                Assert.That(lamp.CurrentIntensity, Is.EqualTo(profile.Intensity));
            }
            finally
            {
                Object.Destroy(profile);
            }
        }

        [UnityTest]
        public IEnumerator LegacyCycleAndClockRebindPreservePowerAndRegistry()
        {
            var oldClock = new LampClock { Now = 4.95 };
            LocalLamp2D lamp = BareLamp(oldClock, out var bulb);
            yield return null;
            Assert.That(lamp.CurrentIntensity, Is.Zero);
            Assert.That(bulb.enabled, Is.False);
            var nextClock = new LampClock();
            lamp.Construct(lampsA, new Features.LampPower.LampPowerControllerFactory(nextClock));
            int readsBeforeRebind = oldClock.Reads;
            yield return null;
            Assert.That(lamp.CurrentIntensity, Is.GreaterThan(0));
            Assert.That(oldClock.Reads, Is.EqualTo(readsBeforeRebind));
            Assert.That(nextClock.Reads, Is.GreaterThan(0));
            lamp.SetPower(false);
            lamp.Construct(lampsA, new Features.LampPower.LampPowerControllerFactory(nextClock));
            Assert.Throws<ArgumentNullException>(() => lamp.Construct(lampsA, null));
            Assert.That(lampsA.Members.Count, Is.EqualTo(1));
            yield return null;
            Assert.That(lamp.CurrentIntensity, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DisablingFlashStopsNativeOutputAndReenableDoesNotReplayIt()
        {
            ShotEffect2D effect = Effect(first, effectsA);
            Assert.That(effect.LightSource.enabled, Is.True);
            effect.enabled = false;
            Assert.That(effectsA.Members, Is.Empty);
            Assert.That(effect.LightSource.enabled, Is.False, "A disabled flash must not leave a frozen native light.");
            foreach (var renderer in effect.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Assert.That(renderer.enabled, Is.False);
            }

            effect.enabled = true;
            Assert.That(effect.LightSource.enabled, Is.False, "Reenable does not authorize replay.");
            yield return null;
            Assert.That(effect != null, Is.True);
            var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PistolDefinition.asset");
            effect.Play(definition.Visuals, false, Vector2.up);
            Assert.That(effect.LightSource.enabled, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemovingCeilingMaskDoesNotReactivateADisabledLamp()
        {
            LocalLamp2D lamp = BareLamp(new LampClock(), out var bulb);
            lamp.SetPower(true);
            var mask = lamp.gameObject.AddComponent<CeilingLightOcclusion2D>();
            lamp.enabled = false;
            mask.enabled = false;
            var source = lamp.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
            Assert.That(source.enabled, Is.False, "Mask cleanup must respect the lamp output owner.");
            Assert.That(bulb.enabled, Is.False);
            yield return null;
        }

        private LocalLamp2D BareLamp(IGameClock clock, out SpriteRenderer bulb)
        {
            var owner = new GameObject("Deterministic native lamp");
            owner.SetActive(false);
            SceneManager.MoveGameObjectToScene(owner, first);
            var source = owner.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            bulb = owner.AddComponent<SpriteRenderer>();
            var lamp = owner.AddComponent<LocalLamp2D>();
            lamp.Configure(source, bulb, Color.white, true);
            lamp.Construct(lampsA, new Features.LampPower.LampPowerControllerFactory(clock));
            owner.SetActive(true);
            return lamp;
        }

        private sealed class LampClock : IGameClock
        {
            public double Now;
            public int Reads;
            public double Time
            {
                get
                {
                    Reads++;
                    return Now;
                }
            }
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

            lamp.Construct(registry, new Features.LampPower.LampPowerControllerFactory(new UnityGameClock()));
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
