#if UNITY_EDITOR
using System.Collections;
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmergencyShift.Tests
{
    public sealed class FurnitureStudyLifetimeTests
    {
        private Scene scene;
        private Scene previous;
        private Material material;

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

            if (material != null)
            {
                UnityEngine.Object.Destroy(material);
            }
        }

        [UnityTest]
        public IEnumerator DisableRestoresActualMaterialPropertyBlockAndLightFlagsAcrossReenable()
        {
            previous = SceneManager.GetActiveScene();
            const string path = "Assets/EmergencyShift/Scenes/ArtReview/FurnitureLightStudy.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            scene = SceneManager.GetSceneByPath(path);
            Type type = Type.GetType("EmergencyShift.FurnitureLightStudy2D, EmergencyShift.Development", true);
            MonoBehaviour study = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                study = study != null ? study : (MonoBehaviour)root.GetComponentInChildren(type, true);
            }

            Assert.That(study, Is.Not.Null);
            yield return null;
            study.enabled = false;
            var samples = (Array)type.GetProperty("Samples").GetValue(study);
            object first = samples.GetValue(0);
            var renderer = (SpriteRenderer)first.GetType().GetField("renderer").GetValue(first);
            material = new Material(renderer.sharedMaterial);
            renderer.sharedMaterial = material;
            int sentinel = Shader.PropertyToID("_StudyLifetimeSentinel");
            var block = new MaterialPropertyBlock();
            block.SetFloat(sentinel, .37f);
            renderer.SetPropertyBlock(block);
            Light2D[] ceiling = Get<Light2D[]>(study, "ceilingLights");
            Light2D[] handheld = Get<Light2D[]>(study, "flashlightLights");
            Assert.That(ceiling.Length, Is.GreaterThan(0));
            Assert.That(handheld.Length, Is.GreaterThan(0));
            ceiling[0].enabled = true;
            handheld[0].shadowsEnabled = false;
            study.enabled = true;
            yield return null;
            Assert.That(renderer.sharedMaterial, Is.Not.SameAs(material));
            study.enabled = false;
            Assert.That(renderer.sharedMaterial, Is.SameAs(material));
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetFloat(sentinel), Is.EqualTo(.37f));
            Assert.That(ceiling[0].enabled, Is.True);
            Assert.That(handheld[0].shadowsEnabled, Is.False);

            block.SetFloat(sentinel, .8f);
            renderer.SetPropertyBlock(block);
            ceiling[0].enabled = false;
            handheld[0].shadowsEnabled = true;
            study.enabled = true;
            study.enabled = false;
            renderer.GetPropertyBlock(block);
            Assert.That(block.GetFloat(sentinel), Is.EqualTo(.8f), "Each enabled lifetime captures its actual incoming overrides.");
            Assert.That(ceiling[0].enabled, Is.False);
            Assert.That(handheld[0].shadowsEnabled, Is.True);

            var extraObject = UnityEngine.Object.Instantiate(renderer.gameObject, renderer.transform.parent);
            var extraRenderer = extraObject.GetComponent<SpriteRenderer>();
            extraRenderer.sharedMaterial = material;
            block.SetFloat(sentinel, .6f);
            extraRenderer.SetPropertyBlock(block);
            object extra = Activator.CreateInstance(first.GetType());
            first.GetType().GetField("renderer").SetValue(extra, extraRenderer);
            first.GetType().GetField("surfaceRect").SetValue(extra, first.GetType().GetField("surfaceRect").GetValue(first));
            first.GetType().GetField("responseIndex").SetValue(extra, first.GetType().GetField("responseIndex").GetValue(first));
            Array expanded = Array.CreateInstance(first.GetType(), 2);
            expanded.SetValue(first, 0);
            expanded.SetValue(extra, 1);
            type.GetMethod("ReplaceRuntimeSamples").Invoke(study, new object[] { expanded });
            study.enabled = true;
            study.enabled = false;
            Assert.That(extraRenderer.sharedMaterial, Is.SameAs(material));
            extraRenderer.GetPropertyBlock(block);
            Assert.That(block.GetFloat(sentinel), Is.EqualTo(.6f), "Dense fixture replacement owns matching native snapshots.");
            type.GetMethod("ReplaceRuntimeSamples").Invoke(study, new object[] { samples });
            UnityEngine.Object.Destroy(extraObject);
        }

        private static T Get<T>(object target, string field) =>
            (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
#endif
