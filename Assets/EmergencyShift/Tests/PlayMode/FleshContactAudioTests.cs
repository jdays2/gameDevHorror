#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class FleshContactAudioTests
    {
        [UnityTest]
        public IEnumerator BothWeaponsPlayOneContactForEitherSpeciesIncludingLethalHits()
        {
            Scene previous = SceneManager.GetActiveScene();
            const string path = "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            Scene scene = SceneManager.GetSceneByPath(path);
            LevelContext2D context = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                context = context != null ? context : root.GetComponentInChildren<LevelContext2D>();
                foreach (var enemy in root.GetComponentsInChildren<Damageable2D>(true))
                {
                    if (enemy.GetComponent<PlayerLife2D>() == null)
                    {
                        enemy.gameObject.SetActive(false);
                    }
                }
            }
            try
            {
                var player = context.Player;
                player.GetComponent<PlayerMotor2D>().enabled = false;
                player.GetComponent<Rigidbody2D>().position = new Vector2(100, 100);
                player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                player.GetComponent<PlayerAim2D>().SetScriptedTarget(new Vector2(102.6f, 100));
                context.Flashlight.SetPower(false);
                var weapon = player.GetComponent<PlayerWeapon2D>();
                var playback = player.GetComponent<PlayerAudio2D>();
                foreach (string weaponName in new[] { "PistolDefinition", "PumpShotgun" })
                {
                    var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/" + weaponName + ".asset");
                    weapon.Acquire(40, definition);
                    weapon.TryEquip(definition.SelectionSlot);
                    Assert.That(weapon.Definition, Is.SameAs(definition));
                    foreach (string species in new[] { "ShadowStalker", "Sleeper" })
                    {
                        foreach (bool lethal in new[] { false, true })
                        {
                            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/" + species + ".prefab");
                            GameObject target = context.CreateInstance(prefab, new Vector3(102.6f, 100, 0), Quaternion.identity);
                            target.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                            var health = target.GetComponent<EnemyHealth2D>();
                            health.Initialize(lethal ? 1 : 100);
                            var stalker = target.GetComponent<ShadowStalker2D>();
                            if (stalker != null)
                            {
                                stalker.AttacksEnabled = false;
                            }
                            yield return null;
                            Physics2D.SyncTransforms();
                            int before = playback.ImpactAudioCount;
                            Assert.That(weapon.TryFire(), Is.True, weaponName);
                            yield return new WaitForSeconds(.25f);
                            Assert.That(health.Current, Is.LessThan(lethal ? 1 : 100), species);
                            Assert.That(playback.ImpactAudioCount - before, Is.EqualTo(1), weaponName + " " + species + " lethal=" + lethal);
                            Assert.That(playback.LastImpactMaterial, Is.EqualTo(ImpactMaterial.Flesh));
                            Object.Destroy(target);
                            yield return new WaitForSeconds(1.5f);
                        }
                    }
                }
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                SceneManager.UnloadSceneAsync(scene);
            }
        }
    }
}
#endif
