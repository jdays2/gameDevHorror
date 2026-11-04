#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class FixedCreatureSpriteSceneTests
    {
        private Scene loaded;
        private Scene previous;
        private LevelContext2D context;
        private float previousCaptureDelta;
        private static readonly FieldInfo Phase = typeof(CreatureSpriteAnimator2D).GetField("walkPhase", BindingFlags.Instance | BindingFlags.NonPublic);

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = previousCaptureDelta;
            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }
            if (loaded.IsValid() && loaded.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(loaded);
            }
        }

        [UnityTest]
        public IEnumerator OriginalWholeAttackUsesCombatTimeWithoutIndependentLegGait()
        {
            yield return LoadSandbox();
            foreach (string species in new[] { "ShadowStalker", "Sleeper" })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/" + species + ".prefab");
                GameObject actor = context.CreateInstance(prefab, new Vector3(1000, 1000), Quaternion.identity);
                GameObject other = context.CreateInstance(prefab, new Vector3(1005, 1000), Quaternion.identity);
                HoldStrike(actor, .43f);
                HoldStrike(other, .43f);
                var animator = actor.GetComponent<CreatureSpriteAnimator2D>();
                var otherAnimator = other.GetComponent<CreatureSpriteAnimator2D>();
                var serialized = new SerializedObject(animator);
                Assert.That(serialized.FindProperty("spriteSet").objectReferenceValue, Is.Null);
                Assert.That(serialized.FindProperty("attackSpriteSet").objectReferenceValue, Is.Null);
                SerializedProperty attack = serialized.FindProperty("attackFrames");
                Sprite expected = (Sprite)attack.GetArrayElementAtIndex(Mathf.FloorToInt(.43f * attack.arraySize)).objectReferenceValue;
                var renderer = actor.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
                var otherRenderer = other.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
                yield return null;
                yield return new WaitForEndOfFrame();
                Assert.That(renderer.sprite, Is.SameAs(expected));
                float phase = (float)Phase.GetValue(animator);
                float otherPhase = (float)Phase.GetValue(otherAnimator);
                Vector3 destination = actor.transform.position + Vector3.up * .08f;
                actor.transform.position = destination;
                actor.GetComponent<Rigidbody2D>().position = destination;
                yield return null;
                yield return new WaitForEndOfFrame();
                Assert.That((float)Phase.GetValue(animator), Is.EqualTo(phase).Within(.000001f), "The old whole-body attack does not move separate support legs.");
                Assert.That((float)Phase.GetValue(otherAnimator), Is.EqualTo(otherPhase).Within(.000001f));
                Assert.That(renderer.sprite, Is.SameAs(expected));
                Assert.That(Vector3.Distance(actor.transform.position, destination), Is.LessThan(.00001f));
                HoldStrike(actor, .8f);
                yield return null;
                yield return new WaitForEndOfFrame();
                Sprite later = (Sprite)attack.GetArrayElementAtIndex(Mathf.FloorToInt(.8f * attack.arraySize)).objectReferenceValue;
                Assert.That(renderer.sprite, Is.SameAs(later), "Combat progress selects a later original whole frame.");
                Assert.That(otherRenderer.sprite, Is.SameAs(expected), "Another actor keeps its own combat pose.");
                Time.timeScale = 0;
                yield return null;
                yield return new WaitForEndOfFrame();
                Assert.That(renderer.sprite, Is.SameAs(later));
                Time.timeScale = 1;
                int deaths = 0;
                var health = actor.GetComponent<EnemyHealth2D>();
                System.Action died = () => deaths++;
                health.Died += died;
                health.Kill();
                health.Kill();
                yield return null;
                yield return new WaitForEndOfFrame();
                Assert.That(deaths, Is.EqualTo(1));
                Assert.That(animator.EyeOpen, Is.False);
                Assert.That(other.GetComponent<EnemyHealth2D>().IsAlive, Is.True);
                Sprite death = (Sprite)serialized.FindProperty("deathFrames").GetArrayElementAtIndex(0).objectReferenceValue;
                Assert.That(renderer.sprite, Is.SameAs(death), "Legacy playback uses its original death drawings.");
                health.Died -= died;
                Object.Destroy(actor);
                Object.Destroy(other);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator OriginalWalkAdvancesOnlyForTravelAndFoldUsesOriginalWholePose()
        {
            yield return LoadSandbox();
            foreach (string species in new[] { "ShadowStalker", "Sleeper" })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/" + species + ".prefab");
                GameObject actor = context.CreateInstance(prefab, new Vector3(1000, 1000), Quaternion.identity);
                HoldStrike(actor, .43f);
                var stalker = actor.GetComponent<ShadowStalker2D>();
                var sleeper = actor.GetComponent<Sleeper2D>();
                if (stalker != null)
                {
                    ShadowStalker2D.Snapshot snapshot = stalker.CaptureState();
                    snapshot.state = ShadowStalker2D.BehaviourState.Sneaking;
                    stalker.RestoreState(snapshot);
                }
                else
                {
                    Sleeper2D.Snapshot snapshot = sleeper.CaptureState();
                    snapshot.state = Sleeper2D.BehaviourState.Hunting;
                    sleeper.RestoreState(snapshot);
                }
                var animator = actor.GetComponent<CreatureSpriteAnimator2D>();
                var serialized = new SerializedObject(animator);
                var renderer = actor.transform.Find("Visual/Body").GetComponent<SpriteRenderer>();
                SerializedProperty walk = serialized.FindProperty("walkFrames");
                Assert.That(walk.arraySize, Is.EqualTo(8));
                yield return null;
                yield return new WaitForEndOfFrame();
                float phase = (float)Phase.GetValue(animator);
                Vector3 destination = actor.transform.position + Vector3.up * .08f;
                actor.transform.position = destination;
                actor.GetComponent<Rigidbody2D>().position = destination;
                yield return null;
                yield return new WaitForEndOfFrame();
                Assert.That((float)Phase.GetValue(animator), Is.Not.EqualTo(phase));
                Assert.That(Enumerable.Range(0, walk.arraySize).Select(index => walk.GetArrayElementAtIndex(index).objectReferenceValue), Does.Contain(renderer.sprite));
                float stopped = (float)Phase.GetValue(animator);
                yield return null;
                yield return new WaitForEndOfFrame();
                Assert.That((float)Phase.GetValue(animator), Is.EqualTo(stopped).Within(.000001f));
                if (stalker != null)
                {
                    ShadowStalker2D.Snapshot snapshot = stalker.CaptureState();
                    snapshot.state = ShadowStalker2D.BehaviourState.Waiting;
                    stalker.RestoreState(snapshot);
                }
                else
                {
                    Sleeper2D.Snapshot snapshot = sleeper.CaptureState();
                    snapshot.state = Sleeper2D.BehaviourState.Sleeping;
                    snapshot.aggro = false;
                    snapshot.age = 0;
                    sleeper.RestoreState(snapshot);
                }
                yield return null;
                yield return new WaitForEndOfFrame();
                SerializedProperty recoil = serialized.FindProperty("recoilFrames");
                Assert.That(renderer.sprite, Is.SameAs(recoil.GetArrayElementAtIndex(recoil.arraySize - 1).objectReferenceValue));
                Assert.That(actor.transform.Find("Visual").localScale, Is.EqualTo(Vector3.one));
                Object.Destroy(actor);
                yield return null;
            }
        }

        private IEnumerator LoadSandbox()
        {
            previous = SceneManager.GetActiveScene();
            previousCaptureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60;
            const string path = "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            loaded = SceneManager.GetSceneByPath(path);
            SceneManager.SetActiveScene(loaded);
            GameObject[] roots = loaded.GetRootGameObjects();
            context = roots.SelectMany(root => root.GetComponentsInChildren<LevelContext2D>(true)).First();
            foreach (EnemyHealth2D actor in roots.SelectMany(root => root.GetComponentsInChildren<EnemyHealth2D>(true)))
            {
                actor.gameObject.SetActive(false);
            }
            context.Player.SetHealthCapacity(100, 100);
            context.Player.GetComponent<PlayerMotor2D>().enabled = false;
            context.Flashlight.SetPower(false);
        }

        private static void HoldStrike(GameObject actor, float progress)
        {
            var stalker = actor.GetComponent<ShadowStalker2D>();
            if (stalker != null)
            {
                ShadowStalker2D.Snapshot snapshot = stalker.CaptureState();
                snapshot.state = ShadowStalker2D.BehaviourState.Attacking;
                snapshot.aggro = true;
                snapshot.age = stalker.Definition.AttackDuration * progress;
                snapshot.approach = Vector2.up;
                snapshot.staggerLeft = 0;
                stalker.RestoreState(snapshot);
                stalker.enabled = false;
            }
            else
            {
                var sleeper = actor.GetComponent<Sleeper2D>();
                Sleeper2D.Snapshot snapshot = sleeper.CaptureState();
                snapshot.state = Sleeper2D.BehaviourState.Attacking;
                snapshot.aggro = true;
                snapshot.age = sleeper.Definition.AttackDuration * progress;
                snapshot.heading = Vector2.up;
                snapshot.staggerLeft = 0;
                sleeper.RestoreState(snapshot);
                sleeper.enabled = false;
            }
            actor.GetComponent<CrowdAgent2D>().enabled = false;
            var body = actor.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }
    }
}
#endif
