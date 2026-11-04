#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using EmergencyShift.Features.Scorpion.View;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class ScorpionAlarmSceneTests
    {
        private Scene previous;
        private Scene loaded;
        private LevelContext2D context;
        private ScorpionPack2D pack;
        private bool listenerPaused;
        private float listenerVolume;

        [UnitySetUp]
        public IEnumerator LoadSandbox()
        {
            previous = SceneManager.GetActiveScene();
            listenerPaused = AudioListener.pause;
            listenerVolume = AudioListener.volume;
            AudioListener.pause = false;
            AudioListener.volume = 1;
            const string path = "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            loaded = SceneManager.GetSceneByPath(path);
            SceneManager.SetActiveScene(loaded);
            GameObject[] roots = loaded.GetRootGameObjects();
            context = roots.SelectMany(root => root.GetComponentsInChildren<LevelContext2D>(true)).Single();
            pack = roots.SelectMany(root => root.GetComponentsInChildren<ScorpionPack2D>(true)).Single();
            foreach (EnemyHealth2D enemy in roots.SelectMany(root => root.GetComponentsInChildren<EnemyHealth2D>(true)))
            {
                if (enemy.GetComponent<Scorpion2D>() == null)
                {
                    enemy.gameObject.SetActive(false);
                }
            }
            foreach (Scorpion2D member in pack.Members)
            {
                member.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            }
            context.Player.GetComponent<PlayerMotor2D>().enabled = false;
            context.Player.GetComponent<PlayerAim2D>().enabled = false;
            context.Player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            context.Player.SetHealthCapacity(999, 999);
            context.Flashlight.SetPower(false);
            Assert.That(pack.IsAlerted, Is.False);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            AudioListener.pause = listenerPaused;
            AudioListener.volume = listenerVolume;
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
        public IEnumerator SelectedVoiceIsOncePerPackAfterReenableAndOtherPackStaysIndependent()
        {
            Scorpion2D caller = pack.Members[1];
            ScorpionAudio2D audio = caller.GetComponent<ScorpionAudio2D>();
            ScorpionPack2D other = CreateOtherPack();
            for (int index = 0; index < 3; index++)
            {
                audio.enabled = false;
                audio.enabled = true;
            }
            MovePlayer((Vector2)caller.transform.position + Vector2.up);
            int cues = 0;
            int countDuringCue = -1;
            bool membersAlertedDuringCue = false;
            System.Action observe = () =>
            {
                cues++;
                countDuringCue = pack.AlarmCount;
                membersAlertedDuringCue = pack.Members.All(member => member.HasAggro);
            };
            caller.AlarmCueRequested += observe;
            try
            {
                pack.Alert(caller);
                pack.Alert(pack.Members[0]);
            }
            finally
            {
                caller.AlarmCueRequested -= observe;
            }
            Assert.That(cues, Is.EqualTo(1));
            Assert.That(countDuringCue, Is.Zero, "Keep the existing cue-before-counter order.");
            Assert.That(membersAlertedDuringCue, Is.True);
            Assert.That(pack.AlarmCount, Is.EqualTo(1));
            Assert.That(audio.DetectionAccentsPlayed, Is.EqualTo(1));
            Assert.That(pack.Members.Sum(member => member.GetComponent<ScorpionAudio2D>().DetectionAccentsPlayed), Is.EqualTo(1));
            Assert.That(pack.Members.All(member => member.HasAggro), Is.True);
            Assert.That(other.IsAlerted, Is.False);
            Assert.That(other.Members[0].HasAggro, Is.False);
            AudioSource detection = Detection(audio);
            Assert.That(detection.isPlaying, Is.True);
            Assert.That(detection.clip, Is.SameAs(caller.Presentation.Alarm));
            yield return null;
            Assert.That(detection.timeSamples, Is.GreaterThan(0));
            other.Alert(other.Members[0]);
            Assert.That(other.AlarmCount, Is.EqualTo(1));
            Assert.That(other.Members[0].GetComponent<ScorpionAudio2D>().DetectionAccentsPlayed, Is.EqualTo(1));
            Assert.That(pack.AlarmCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DisabledAudioDoesNotReplayAnOldAlarmWhenEnabled()
        {
            Scorpion2D caller = pack.Members[0];
            ScorpionAudio2D audio = caller.GetComponent<ScorpionAudio2D>();
            audio.enabled = false;
            pack.Alert(caller);
            Assert.That(pack.AlarmCount, Is.EqualTo(1));
            Assert.That(audio.DetectionAccentsPlayed, Is.Zero);
            audio.enabled = true;
            yield return null;
            pack.Alert(caller);
            Assert.That(audio.DetectionAccentsPlayed, Is.Zero);
            Assert.That(pack.AlarmCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator MutedDetectionDoesNotEmitAPerceptibleCueAndUnmutedControlDoes()
        {
            Scorpion2D caller = pack.Members[0];
            ScorpionAudio2D audio = caller.GetComponent<ScorpionAudio2D>();
            AudioSource detection = Detection(audio);
            MovePlayer((Vector2)caller.transform.position + Vector2.up);
            detection.mute = true;
            int cues = 0;
            System.Action<float, float, bool> observe = (duration, gain, urgent) => cues++;
            audio.PerceptibleCue += observe;
            try
            {
                pack.Alert(caller);
                Assert.That(audio.DetectionAccentsPlayed, Is.EqualTo(1));
                Assert.That(detection.volume, Is.GreaterThanOrEqualTo(.02f), "Mute must be the reason for silence, rather than distance.");
                Assert.That(cues, Is.Zero);
                detection.mute = false;
                // Explicit public View API is a positive audibility control, not a second pack alert.
                audio.PlayAlarm();
                Assert.That(cues, Is.GreaterThan(0));
                Assert.That(pack.AlarmCount, Is.EqualTo(1));
            }
            finally
            {
                audio.PerceptibleCue -= observe;
                detection.mute = false;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator StandaloneHitAlertsOnlyItselfAndFatalHitDoesNotReplaceDeathVoice()
        {
            Scorpion2D actor = Spawn(new Vector3(1000, 1000));
            ScorpionAudio2D audio = actor.GetComponent<ScorpionAudio2D>();
            actor.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            MovePlayer((Vector2)actor.transform.position + Vector2.up);
            actor.ReceiveBulletHit(1, actor.transform.position, Vector2.right);
            actor.ReceiveBulletHit(1, actor.transform.position, Vector2.right);
            Assert.That(actor.Pack, Is.Null);
            Assert.That(actor.HasAggro, Is.True);
            Assert.That(audio.DetectionAccentsPlayed, Is.EqualTo(1));
            Assert.That(pack.IsAlerted, Is.False);
            int hurt = audio.HitsPlayed;
            actor.ReceiveBulletHit(999, actor.transform.position, Vector2.right);
            actor.ReceiveBulletHit(999, actor.transform.position, Vector2.right);
            Assert.That(actor.IsAlive, Is.False);
            Assert.That(audio.DeathsPlayed, Is.EqualTo(1));
            Assert.That(audio.HitsPlayed, Is.EqualTo(hurt));
            Assert.That(audio.DetectionAccentsPlayed, Is.EqualTo(1));
            yield return null;
        }

        private ScorpionPack2D CreateOtherPack()
        {
            var root = new GameObject("Temporary independent Scorpion pack");
            root.SetActive(false);
            SceneManager.MoveGameObjectToScene(root, loaded);
            var result = root.AddComponent<ScorpionPack2D>();
            Scorpion2D member = Spawn(new Vector3(1010, 1000), root.transform);
            var serialized = new SerializedObject(result);
            SerializedProperty members = serialized.FindProperty("members");
            members.arraySize = 1;
            members.GetArrayElementAtIndex(0).objectReferenceValue = member;
            serialized.FindProperty("entranceDoors").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);
            member.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            return result;
        }

        private Scorpion2D Spawn(Vector3 position, Transform parent = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergencyShift/Prefabs/Enemies/Scorpion.prefab");
            return context.CreateInstance(prefab, position, Quaternion.identity, parent).GetComponent<Scorpion2D>();
        }

        private void MovePlayer(Vector2 position)
        {
            context.Player.GetComponent<Rigidbody2D>().position = position;
            context.Player.transform.position = position;
            Physics2D.SyncTransforms();
        }

        private static AudioSource Detection(ScorpionAudio2D audio) =>
            (AudioSource)new SerializedObject(audio).FindProperty("detection").objectReferenceValue;
    }
}
#endif
