#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EmergencyShift.Core.Randomness;
using EmergencyShift.Features.PlayerInput.Logic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class WeaponSpreadLifetimeTests
    {
        private Scene scene;
        private Scene previousScene;
        private PumpShotgunDefinition source;
        private readonly List<UnityEngine.Object> ownedAssets = new List<UnityEngine.Object>();
        private UnityEngine.Random.State previousRandom;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousRandom = UnityEngine.Random.state;
            previousScene = SceneManager.GetActiveScene();
            scene = SceneManager.CreateScene("Weapon spread " + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(scene);
            string[] matches = AssetDatabase.FindAssets("t:PumpShotgunDefinition", new[] { "Assets/EmergencyShift/Data" });
            Assert.That(matches.Length, Is.GreaterThan(0));
            Array.Sort(matches, StringComparer.Ordinal);
            source = AssetDatabase.LoadAssetAtPath<PumpShotgunDefinition>(AssetDatabase.GUIDToAssetPath(matches[0]));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (previousScene.IsValid() && previousScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousScene);
            }

            if (scene.IsValid() && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }

            foreach (UnityEngine.Object asset in ownedAssets)
            {
                UnityEngine.Object.Destroy(asset);
            }

            ownedAssets.Clear();
            UnityEngine.Random.state = previousRandom;
        }

        [UnityTest]
        public IEnumerator ActualVolleyKeepsAllDirectionsWithDifferentFlashAndCosmeticDraws()
        {
            PlayerWeapon2D first = CreateWeapon(31415, false);
            PlayerWeapon2D noisy = CreateWeapon(31415, true);
            Action consumeCosmetics = () =>
            {
                for (int i = 0; i < 137; i++)
                {
                    _ = UnityEngine.Random.value;
                }
            };
            noisy.ShotFired += consumeCosmetics;
            try
            {
                Assert.That(first.TryFire(), Is.True);
                Vector2[] expected = Directions(first);
                Assert.That(noisy.TryFire(), Is.True);
                Vector2[] actual = Directions(noisy);
                Assert.That(expected.Length, Is.EqualTo(source.ProjectileCount));
                Assert.That(actual, Is.EqualTo(expected));
                Assert.That(first.LoadedAmmo, Is.EqualTo(source.magazineCapacity - 1));
                Assert.That(noisy.LoadedAmmo, Is.EqualTo(source.magazineCapacity - 1));
                Assert.That(first.Phase, Is.EqualTo(WeaponPhase.Pumping));
                Assert.That(noisy.Phase, Is.EqualTo(WeaponPhase.Pumping));
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/WeaponSpreadEvidence.json"));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(new SpreadEvidence
                {
                    baseline = expected,
                    changedCosmetics = actual,
                    extraCosmeticDraws = 137,
                    seed = 31415
                }, true));
            }
            finally
            {
                noisy.ShotFired -= consumeCosmetics;
            }

            yield return null;
        }

        private PlayerWeapon2D CreateWeapon(int seed, bool withoutPlume)
        {
            var definition = UnityEngine.Object.Instantiate(source);
            ownedAssets.Add(definition);
            var root = new GameObject("Player weapon fixture");
            root.SetActive(false);
            var weapon = root.AddComponent<PlayerWeapon2D>();
            root.GetComponent<Flashlight2D>().enabled = false;
            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(root.transform, false);
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(pivot, false);
            muzzle.localPosition = Vector3.up * .4f;
            var bullet = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/PistolBullet.prefab")
                .GetComponent<BulletProjectile2D>();
            var effect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/PistolShotEffect.prefab")
                .GetComponent<ShotEffect2D>();
            if (withoutPlume)
            {
                effect = UnityEngine.Object.Instantiate(effect);
                effect.gameObject.SetActive(false);
                Set(effect, "plume", null);
            }

            Set(weapon, "definition", definition);
            weapon.Configure(pivot, muzzle, bullet, effect, definition.visuals);
            typeof(PlayerWeapon2D).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(weapon, new object[] { new TestInput(), new GameplayInputBlock(), new RandomSourceFactory(new System.Random(seed)) });
            root.SetActive(true);
            Assert.That(weapon.Acquire(12, definition), Is.True);
            return weapon;
        }

        private static Vector2[] Directions(PlayerWeapon2D weapon)
        {
            var directions = new List<Vector2>();
            foreach (BulletProjectile2D bullet in UnityEngine.Object.FindObjectsByType<BulletProjectile2D>(FindObjectsSortMode.None))
            {
                var owner = (Transform)Get(bullet, "owner");
                if (owner == weapon.transform)
                {
                    directions.Add((Vector2)Get(bullet, "direction"));
                }
            }

            directions.Sort((first, second) => Vector2.SignedAngle(Vector2.up, first).CompareTo(Vector2.SignedAngle(Vector2.up, second)));
            return directions.ToArray();
        }

        private static object Get(object target, string field)
        {
            return target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private sealed class TestInput : IPlayerInput
        {
            public Vector2 Movement => Vector2.zero;
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

        [Serializable]
        private sealed class SpreadEvidence
        {
            public Vector2[] baseline;
            public Vector2[] changedCosmetics;
            public int extraCosmeticDraws;
            public int seed;
        }
    }
}
#endif
