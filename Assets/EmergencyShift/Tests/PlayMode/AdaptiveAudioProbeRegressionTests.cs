#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class AdaptiveAudioProbeRegressionTests
    {
        private Scene scene;
        private Scene previousScene;
        private float previousScale;

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

            Time.timeScale = previousScale;
        }

        [UnityTest]
        public IEnumerator ExistingOptInProbeCompletesDryFireAndAllThreeGroundMaterials()
        {
            previousScene = SceneManager.GetActiveScene();
            previousScale = Time.timeScale;
            Time.timeScale = 1;
            const string path = "Assets/EmergencyShift/Scenes/MechanicsSandbox.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath(path);
            Assert.That(scene.isLoaded, Is.True);
            SceneManager.SetActiveScene(scene);
            yield return null;
            Type type = Type.GetType("EmergencyShift.AdaptiveAudioProbe, EmergencyShift.Development", true);
            var owner = new GameObject("Temporary opt-in audio probe");
            var probe = (Component)owner.AddComponent(type);
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/AdaptiveAudioProbeRegression"));
            type.GetMethod("BeginTests").Invoke(probe, new object[] { folder });
            PropertyInfo finished = type.GetProperty("Finished");
            float deadline = Time.realtimeSinceStartup + 70;
            while (!(bool)finished.GetValue(probe) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That((bool)finished.GetValue(probe), Is.True, "The real probe must finish within its bounded Play session.");
            string failure = (string)type.GetProperty("Failure").GetValue(probe);
            Assert.That(failure, Is.Null.Or.Empty, failure);
            Assert.That(File.Exists(Path.Combine(folder, "report.json")), Is.True);
        }
    }
}
#endif
