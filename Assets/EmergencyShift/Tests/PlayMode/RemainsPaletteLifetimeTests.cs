#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class RemainsPaletteLifetimeTests
    {
        private Scene previous;
        private Scene scene;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
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
        public IEnumerator BothSpeciesEmitConfiguredOriginalFragmentsAndRestoreTheirActualTint()
        {
            previous = SceneManager.GetActiveScene();
            const string path = "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath(path);
            SceneManager.SetActiveScene(scene);
            LevelContext2D context = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (EnemyHealth2D enemy in root.GetComponentsInChildren<EnemyHealth2D>(true))
                {
                    enemy.gameObject.SetActive(false);
                }
                context = context != null ? context : root.GetComponentInChildren<LevelContext2D>(true);
            }
            Assert.That(context, Is.Not.Null);
            int index = 0;
            foreach (string name in new[] { "ShadowStalker", "Sleeper" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/" + name + ".prefab");
                GameObject actor = context.CreateInstance(prefab, new Vector3(600 + index * 15, 600, 0), Quaternion.identity);
                var burst = actor.GetComponent<GoreBurst2D>();
                GoreProfile profile;
                Sprite[] sprites;
                using (var serialized = new SerializedObject(burst))
                {
                    profile = (GoreProfile)serialized.FindProperty("profile").objectReferenceValue;
                    var fragments = serialized.FindProperty("fragments");
                    sprites = new Sprite[fragments.arraySize];
                    for (int part = 0; part < sprites.Length; part++)
                    {
                        sprites[part] = (Sprite)fragments.GetArrayElementAtIndex(part).objectReferenceValue;
                    }
                }
                actor.GetComponent<EnemyHealth2D>().Kill();
                yield return null;
                float deadline = Time.realtimeSinceStartup + 8;
                while (!burst.IsSettled && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }
                Assert.That(burst.IsSettled, Is.True, name);
                RemainsSnapshot snapshot = burst.CaptureRemains();
                Assert.That(snapshot, Is.Not.Null, name);
                Assert.That(snapshot.parts.Length, Is.InRange(13, 16));
                foreach (RemainsPartState part in snapshot.parts)
                {
                    Assert.That(sprites[part.spriteIndex], Is.Not.Null);
                    Assert.That(part.body.color, Is.EqualTo(profile.SettledTint));
                }
                Assert.That(profile.SettledTint.r, Is.GreaterThan(RemainsVisualStyle.Tint.r));
                Assert.That(Mathf.Max(profile.SettledTint.r, profile.SettledTint.g, profile.SettledTint.b), Is.LessThan(1));
                // A restored record owns the captured tint; later profile edits must not recolour history.
                GameObject restoredActor = context.CreateInstance(prefab, actor.transform.position, Quaternion.identity);
                var restored = restoredActor.GetComponent<GoreBurst2D>();
                restored.RestoreRemains(snapshot);
                RemainsSnapshot roundTrip = restored.CaptureRemains();
                Assert.That(roundTrip.parts.Length, Is.EqualTo(snapshot.parts.Length));
                for (int part = 0; part < snapshot.parts.Length; part++)
                {
                    Assert.That(roundTrip.parts[part].body.color, Is.EqualTo(snapshot.parts[part].body.color));
                    Assert.That(roundTrip.parts[part].spriteIndex, Is.EqualTo(snapshot.parts[part].spriteIndex));
                }
                index++;
            }
        }
    }
}
#endif
