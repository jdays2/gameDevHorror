#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using EmergencyShift.Features.PlayerHitAudio.Config;
using EmergencyShift.Features.PlayerHitAudio.View;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class PlayerHitAudioLifetimeTests
    {
        private Scene previous;
        private Scene scene;
        private float originalTimeScale;
        private bool originalPause;
        private PlayerLife2D player;
        private PlayerHitAudioPresenter audio;

        [SetUp]
        public void Setup()
        {
            previous = SceneManager.GetActiveScene();
            originalTimeScale = Time.timeScale;
            originalPause = AudioListener.pause;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = originalTimeScale;
            AudioListener.pause = originalPause;
            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }
            if (scene.IsValid() && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        private IEnumerator Load()
        {
            const string path = "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity";
            SceneHandle[] loadedHandles = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(index => SceneManager.GetSceneAt(index).handle).ToArray();
            Time.timeScale = 1;
            AudioListener.pause = false;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            scene = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                .Single(candidate => candidate.path == path && !loadedHandles.Contains(candidate.handle));
            SceneManager.SetActiveScene(scene);
            foreach (EnemyHealth2D enemy in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnemyHealth2D>(true)))
            {
                enemy.gameObject.SetActive(false);
            }
            player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LevelContext2D>(true)).Single().Player;
            player.GetComponent<PlayerMotor2D>().enabled = false;
            player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            player.SetHealthCapacity(100, 100);
            yield return null;
            audio = player.GetComponent<PlayerHitAudioPresenter>();
            Assert.That(audio, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator AcceptedDamageIsOneCueWhileRestorationPauseDisableAndScopeDisposalAreSilent()
        {
            yield return Load();
            var profile = AssetDatabase.LoadAssetAtPath<PlayerHitAudioProfile>("Assets/EmergencyShift/Data/Audio/PlayerHitAudio.asset");
            string unchanged = JsonUtility.ToJson(profile);
            Transform voiceRoot = player.transform.Find("Player hit voices (runtime)");
            Assert.That(voiceRoot, Is.Not.Null);
            AudioSource[] voices = voiceRoot.GetComponents<AudioSource>();
            Assert.That(voices.Length, Is.EqualTo(2));
            Assert.That(voices.All(voice => voice.outputAudioMixerGroup != null && !voice.ignoreListenerPause), Is.True);
            Assert.That(audio.PlayedCount, Is.Zero);
            player.RestoreHealth(90);
            player.SetHealthCapacity(100, 90);
            Assert.That(audio.PlayedCount, Is.Zero);
            Assert.That(player.TakeDamage(0), Is.False);
            Assert.That(player.TryTakeDamage(1, player.transform.position, Vector2.right), Is.True);
            Assert.That(audio.PlayedCount, Is.EqualTo(1));
            AudioClip first = audio.LastClip;
            Assert.That(player.TryTakeDamage(1, player.transform.position, Vector2.left), Is.False);
            Assert.That(audio.PlayedCount, Is.EqualTo(1));
            Assert.That(audio.ActiveVoiceCount, Is.EqualTo(1));

            yield return new WaitForSecondsRealtime(.05f);
            AudioSource active = voices.First(voice => voice.isPlaying);
            AudioListener.pause = true;
            Time.timeScale = 0;
            int pausedSample = active.timeSamples;
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(active.timeSamples, Is.EqualTo(pausedSample));
            Assert.That(audio.PlayedCount, Is.EqualTo(1));
            AudioListener.pause = false;
            Time.timeScale = 1;
            yield return new WaitForSecondsRealtime(.75f);

            audio.enabled = false;
            Assert.That(player.TakeDamage(1), Is.True);
            Assert.That(audio.PlayedCount, Is.EqualTo(1));
            Assert.That(voices.All(voice => !voice.isPlaying), Is.True);
            audio.enabled = true;
            player.RestoreHealth(90);
            Assert.That(player.TakeDamage(1), Is.True);
            Assert.That(audio.PlayedCount, Is.EqualTo(2));
            Assert.That(audio.LastClip, Is.Not.SameAs(first));
            Assert.That(audio.ActiveVoiceCount, Is.EqualTo(1));
            player.SetHealthCapacity(100, player.Health);
            Assert.That(audio.PlayedCount, Is.EqualTo(2));
            Assert.That(audio.ActiveVoiceCount, Is.EqualTo(1), "A capacity update must not truncate a real hit.");
            player.RestoreHealth(90);
            Assert.That(audio.PlayedCount, Is.EqualTo(2));
            Assert.That(voices.All(voice => !voice.isPlaying), Is.True);
            audio.Dispose();
            Assert.That(player.TakeDamage(1), Is.True);
            Assert.That(audio.PlayedCount, Is.EqualTo(2));
            Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(unchanged));
        }

        [UnityTest]
        public IEnumerator LethalDamagePlaysOnceAndFinishesBeforeTheWorldBecomesSilent()
        {
            yield return Load();
            player.SetHealthCapacity(1, 1);
            Assert.That(audio.PlayedCount, Is.Zero);
            Assert.That(player.TakeDamage(1), Is.True);
            Assert.That(player.IsDead, Is.True);
            Assert.That(audio.PlayedCount, Is.EqualTo(1));
            Assert.That(audio.ActiveVoiceCount, Is.EqualTo(1), "HealthChanged must not cancel the lethal damage cue.");
            Assert.That(player.TakeDamage(1), Is.False);
            Assert.That(audio.PlayedCount, Is.EqualTo(1));
            yield return new WaitForSecondsRealtime(1.7f);
            Assert.That(audio.ActiveVoiceCount, Is.Zero);
            Assert.That(audio.PlayedCount, Is.EqualTo(1));
        }
    }
}
#endif
