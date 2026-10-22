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
    public sealed class ProjectileSpeciesContactTests
    {
        [UnityTest]
        public IEnumerator BothSpeciesStopEachPistolAndShotgunProjectileAtLethalAndNonlethalContact()
        {
            Scene previous = SceneManager.GetActiveScene();
            const string path = "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            Scene scene = SceneManager.GetSceneByPath(path);
            LevelContext2D context = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (var enemy in root.GetComponentsInChildren<Damageable2D>(true))
                {
                    if (enemy.GetComponent<PlayerLife2D>() == null)
                    {
                        enemy.gameObject.SetActive(false);
                    }
                }
                context = context != null ? context : root.GetComponentInChildren<LevelContext2D>();
            }
            try
            {
                var player = context.Player;
                player.GetComponent<PlayerMotor2D>().enabled = false;
                player.GetComponent<Rigidbody2D>().position = new Vector2(100, 100);
                player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                context.Flashlight.SetPower(false);
                var bulletTemplate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Combat/PistolBullet.prefab");
                foreach (string species in new[] { "ShadowStalker", "Sleeper" })
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/" + species + ".prefab");
                    foreach (string weapon in new[] { "PistolDefinition", "PumpShotgun" })
                    {
                        var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/EmergencyShift/Data/Weapons/" + weapon + ".asset");
                        foreach (bool lethal in new[] { false, true })
                        {
                            GameObject front = context.CreateInstance(prefab, new Vector3(103, 100, 0), Quaternion.identity);
                            GameObject rear = context.CreateInstance(prefab, new Vector3(105, 100, 0), Quaternion.identity);
                            front.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                            rear.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
                            var health = front.GetComponent<EnemyHealth2D>();
                            health.Restore(lethal ? 1 : 2);
                            int rearBefore = rear.GetComponent<EnemyHealth2D>().Current;
                            int damageEvents = 0;
                            int deaths = 0;
                            System.Action damaged = () => damageEvents++;
                            System.Action died = () => deaths++;
                            health.Damaged += damaged;
                            health.Died += died;
                            var bullet = context.CreateInstance(bulletTemplate, new Vector3(101, 100, -.1f), Quaternion.identity).GetComponent<BulletProjectile2D>();
                            bullet.Launch(player.transform, Vector2.right, definition.Visuals, null, (1 << 8) | (1 << 9) | 1, 1, 8, 1);
                            Physics2D.SyncTransforms();
                            float deadline = Time.realtimeSinceStartup + 2;
                            while (bullet != null && Time.realtimeSinceStartup < deadline)
                            {
                                yield return new WaitForFixedUpdate();
                            }
                            Assert.That(bullet == null, Is.True, species + " " + weapon);
                            Assert.That(health.Current, Is.EqualTo(lethal ? 0 : 1));
                            Assert.That(damageEvents, Is.EqualTo(lethal ? 0 : 1));
                            Assert.That(deaths, Is.EqualTo(lethal ? 1 : 0));
                            Assert.That(rear.GetComponent<EnemyHealth2D>().Current, Is.EqualTo(rearBefore));
                            health.Damaged -= damaged;
                            health.Died -= died;
                            Object.Destroy(front);
                            Object.Destroy(rear);
                            yield return null;
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
