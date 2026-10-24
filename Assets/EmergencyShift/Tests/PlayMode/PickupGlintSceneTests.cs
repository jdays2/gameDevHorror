#if UNITY_EDITOR
using System.Collections;
using EmergencyShift.Features.Pickups.Config;
using EmergencyShift.Features.Pickups.View;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class PickupGlintSceneTests
    {
        private Scene scene;
        private Scene previous;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
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
        public IEnumerator BeamObstaclesInventoryChangesAndCleanupUseTheRealPickupPath()
        {
            previous = SceneManager.GetActiveScene();
            const string path = "Assets/EmergencyShift/Scenes/MorgueRouteTest.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath(path);
            yield return null;
            LevelContext2D context = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                context = context != null ? context : root.GetComponentInChildren<LevelContext2D>(true);
                foreach (EnemyHealth2D enemy in root.GetComponentsInChildren<EnemyHealth2D>(true))
                {
                    enemy.gameObject.SetActive(false);
                }
            }
            Assert.That(context, Is.Not.Null);
            var player = context.Player;
            var actor = player.GetComponent<PlayerInteraction>();
            var body = player.GetComponent<Rigidbody2D>();
            player.GetComponent<PlayerMotor2D>().enabled = false;
            player.GetComponent<PlayerAim2D>().enabled = false;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            Vector2 origin = new Vector2(10000, 10000);
            player.transform.position = origin;
            body.position = origin;
            Flashlight2D light = context.Flashlight;
            light.Acquire();
            light.SetPower(true);
            light.AimAt(origin + Vector2.up * 3);
            Physics2D.SyncTransforms();
            var profile = Resources.Load<PickupGlintProfile>("PickupGlintProfile");
            string settingsBefore = JsonUtility.ToJson(profile);
            GameObject first = Spawn(context, "World/AccessCards01/ServiceCard", origin + Vector2.up * 2);
            GameObject second = Spawn(context, "World/AccessCards01/ResearchCard", origin + new Vector2(.35f, 2.5f));
            var firstGlint = first.GetComponent<PickupGlint2D>();
            var secondGlint = second.GetComponent<PickupGlint2D>();
            var source = first.GetComponentInChildren<SpriteRenderer>();
            Material sourceMaterial = source.sharedMaterial;
            yield return new WaitForSeconds(.15f);
            Assert.That(firstGlint.IsShowing, Is.True);
            Assert.That(secondGlint.IsShowing, Is.True);
            Assert.That(first.GetComponentsInChildren<Light2D>(true), Is.Empty);
            Assert.That(source.sharedMaterial, Is.SameAs(sourceMaterial));
            Time.timeScale = 0;
            yield return null;
            float progress = firstGlint.Progress;
            yield return null;
            Assert.That(firstGlint.Progress, Is.EqualTo(progress));
            Assert.That(firstGlint.IsShowing, Is.True);
            Time.timeScale = 1;

            light.SetPower(false);
            yield return null;
            yield return null;
            Assert.That(firstGlint.IsShowing, Is.False);
            yield return new WaitForSeconds(.7f);
            light.SetPower(true);
            yield return new WaitForSeconds(.12f);
            Assert.That(firstGlint.IsShowing, Is.True);
            var wall = new GameObject("Temporary pickup visibility blocker", typeof(BoxCollider2D));
            SceneManager.MoveGameObjectToScene(wall, scene);
            wall.layer = 8;
            wall.transform.position = origin + Vector2.up;
            wall.GetComponent<BoxCollider2D>().size = new Vector2(3, .2f);
            Physics2D.SyncTransforms();
            yield return null;
            yield return null;
            Assert.That(firstGlint.IsShowing, Is.False, "A solid wall/closed-door collider hides the glint immediately.");
            Assert.That(secondGlint.IsShowing, Is.False);
            Object.Destroy(wall);
            light.SetPower(false);
            yield return new WaitForSeconds(.7f);
            first.GetComponent<AccessCardPickup2D>().RestoreState(true);
            light.SetPower(true);
            yield return new WaitForSeconds(.12f);
            Assert.That(firstGlint.IsShowing, Is.False, "Trusted restoration cannot replay a consumed item.");
            Assert.That(secondGlint.IsShowing, Is.True);
            Assert.That(player.GetComponent<PlayerAccessCards2D>().Add(second.GetComponent<AccessCardPickup2D>().CardId), Is.True);
            yield return null;
            yield return null;
            Assert.That(secondGlint.IsShowing, Is.False, "An owned card no longer promises another pickup.");

            var weapon = player.GetComponent<PlayerWeapon2D>();
            var pistol = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/PistolDefinition.asset");
            Assert.That(pistol, Is.Not.Null);
            weapon.RestoreState(true, pistol.MaximumAmmo, pistol);
            GameObject ammo = Spawn(context, "Loot/AmmoPickup", origin + Vector2.up * 2);
            var loot = ammo.GetComponent<WeaponLootPickup>();
            Assert.That(((IPickupGlintTarget)loot).CanReceive(actor), Is.False);
            weapon.RestoreState(true, pistol.MaximumAmmo - 1, pistol);
            Assert.That(((IPickupGlintTarget)loot).CanReceive(actor), Is.True);
            int remaining = loot.RemainingCartridges;
            Assert.That(loot.TryCollect(actor), Is.False, "Discovery does not extend automatic collection reach.");
            player.transform.position = ammo.transform.position;
            body.position = ammo.transform.position;
            Physics2D.SyncTransforms();
            yield return null;
            yield return null;
            Assert.That(loot.RemainingCartridges, Is.EqualTo(remaining - 1));
            Assert.That(weapon.Ammo, Is.EqualTo(pistol.MaximumAmmo));
            Assert.That(((IPickupGlintTarget)loot).CanReceive(actor), Is.False);

            GameObject module = Spawn(context, "Loot/FlashlightRangeUpgrade", origin + Vector2.up * 2.5f);
            var moduleTarget = module.GetComponent<IPickupGlintTarget>();
            Assert.That(moduleTarget.CanReceive(actor), Is.True);
            Assert.That(player.GetComponent<FlashlightUpgrades>().TryInstall("glint-test-cap", "Test", 100, 100, out _, true), Is.True);
            Assert.That(moduleTarget.CanReceive(actor), Is.False);
            second.SetActive(false);
            foreach (SpriteRenderer renderer in second.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.name == "Pickup glint stripe")
                {
                    Assert.That(renderer.enabled, Is.False);
                }
            }
            Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(settingsBefore));
        }

        private static GameObject Spawn(LevelContext2D context, string relative, Vector2 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/" + relative + ".prefab");
            Assert.That(prefab, Is.Not.Null, relative);
            return context.CreateInstance(prefab, position, Quaternion.identity);
        }
    }
}
#endif
