using System;
using System.Collections.Generic;
using EmergencyShift.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Tests
{
    public sealed class DialoguePrefabTests
    {
        private readonly List<Scene> temporaryScenes = new List<Scene>();
        private readonly List<string> temporaryPaths = new List<string>();
        private Scene previousScene;

        [SetUp]
        public void SetUp()
        {
            previousScene = SceneManager.GetActiveScene();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var scene in temporaryScenes)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            temporaryScenes.Clear();
            foreach (string path in temporaryPaths)
            {
                AssetDatabase.DeleteAsset(path);
            }

            temporaryPaths.Clear();
            if (previousScene.IsValid() && previousScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousScene);
            }
        }

        [Test]
        public void SharedPrefabContainsItsUiButNoSceneContextOrTestConversation()
        {
            var root = PrefabUtility.LoadPrefabContents(DialoguePrefabAuthoring.PrefabPath);
            try
            {
                Assert.That(root.transform.childCount, Is.EqualTo(1));
                var session = root.GetComponent<DialogueSession>();
                var view = root.GetComponentInChildren<DialogueView>(true);
                Assert.That(session, Is.Not.Null);
                Assert.That(view, Is.Not.Null);
                var data = new SerializedObject(session);
                Assert.That(data.FindProperty("view").objectReferenceValue, Is.EqualTo(view));
                Assert.That(data.FindProperty("gameplayHud").objectReferenceValue, Is.Null);
                Assert.That(data.FindProperty("reticle").objectReferenceValue, Is.Null);
                Assert.That(root.GetComponentsInChildren<DialogueInteraction2D>(true), Is.Empty);
                Assert.That(root.GetComponentsInChildren<EventSystem>(true), Is.Empty);

                var viewData = new SerializedObject(view);
                foreach (string field in new[] { "overlay", "panel", "backdrop", "portrait", "speakerName", "body", "advance", "advanceLabel", "backgroundAdvance", "choicesRoot", "choiceTemplate" })
                {
                    var component = viewData.FindProperty(field).objectReferenceValue as Component;
                    Assert.That(component, Is.Not.Null, field);
                    Assert.That(component.transform.IsChildOf(root.transform), Is.True, field);
                }

                Assert.That(EditorUtility.IsPersistent(viewData.FindProperty("style").objectReferenceValue), Is.True);
                var canvas = view.GetComponent<Canvas>();
                Assert.That(canvas.enabled, Is.False);
                Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>(), Is.Not.Null);
                Assert.That(canvas.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution, Is.EqualTo(new Vector2(1920, 1080)));
                var shadow = root.GetComponentInChildren<DialogueFadeGraphic>(true);
                Assert.That(shadow.GetComponent<CanvasRenderer>(), Is.Not.Null);
                Assert.That(shadow.raycastTarget, Is.False);
                var dependencies = AssetDatabase.GetDependencies(DialoguePrefabAuthoring.PrefabPath);
                Assert.That(dependencies, Has.No.Member("Assets/EmergencyShift/Data/Dialogue/SandboxConversation.asset"));
                foreach (string dependency in dependencies)
                {
                    Assert.That(dependency.EndsWith(".unity", StringComparison.Ordinal), Is.False, dependency);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void TwoScenesBindDifferentHudContextsWithoutChangingTheSharedPrefabOrDuplicatingEvents()
        {
            string before = System.IO.File.ReadAllText(DialoguePrefabAuthoring.PrefabPath);
            var first = NewScene();
            var firstHud = NewHud(first);
            var firstReticle = firstHud.GetComponentInChildren<AimReticle2D>(true);
            int eventsBefore = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include).Length;
            var firstRoot = DialoguePrefabAuthoring.Install(first, firstHud, firstReticle);
            var firstData = new SerializedObject(firstRoot.GetComponent<DialogueSession>());
            Assert.That(firstData.FindProperty("gameplayHud").objectReferenceValue, Is.EqualTo(firstHud));
            Assert.That(firstData.FindProperty("reticle").objectReferenceValue, Is.EqualTo(firstReticle));
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(firstRoot), Is.True);

            var second = NewScene();
            var secondHud = NewHud(second);
            var secondRoot = DialoguePrefabAuthoring.Install(second, secondHud, null);
            var secondData = new SerializedObject(secondRoot.GetComponent<DialogueSession>());
            Assert.That(secondData.FindProperty("gameplayHud").objectReferenceValue, Is.EqualTo(secondHud));
            Assert.That(secondData.FindProperty("reticle").objectReferenceValue, Is.Null);
            Assert.That(firstData.FindProperty("gameplayHud").objectReferenceValue, Is.EqualTo(firstHud));
            int expectedEvents = Mathf.Max(1, eventsBefore);
            Assert.That(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include).Length, Is.EqualTo(expectedEvents));
            Assert.That(System.IO.File.ReadAllText(DialoguePrefabAuthoring.PrefabPath), Is.EqualTo(before));
        }

        [Test]
        public void InstallerRejectsDuplicateIncludingInactiveSessionAndForeignHud()
        {
            var first = NewScene();
            var hud = NewHud(first);
            var root = DialoguePrefabAuthoring.Install(first, hud, null);
            root.SetActive(false);
            Assert.Throws<InvalidOperationException>(() => DialoguePrefabAuthoring.Install(first, hud, null));
            var second = NewScene();
            Assert.Throws<InvalidOperationException>(() => DialoguePrefabAuthoring.Install(second, hud, null));
            int count = 0;
            foreach (var session in UnityEngine.Object.FindObjectsByType<DialogueSession>(FindObjectsInactive.Include))
            {
                if (session.gameObject.scene == first)
                {
                    count++;
                }
            }

            Assert.That(count, Is.EqualTo(1));
        }

        private Scene NewScene()
        {
            // A saved copy also works when the test runner owns an unsaved untitled scene.
            string path = "Assets/DialoguePrefabTest_" + Guid.NewGuid().ToString("N") + ".unity";
            Assert.That(AssetDatabase.CopyAsset(SandboxSceneBuilder.ScenePath, path), Is.True);
            temporaryPaths.Add(path);
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            temporaryScenes.Add(scene);
            foreach (var root in scene.GetRootGameObjects())
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            SceneManager.SetActiveScene(scene);
            return scene;
        }

        private static Canvas NewHud(Scene scene)
        {
            var root = new GameObject("Test HUD", typeof(RectTransform), typeof(Canvas));
            SceneManager.MoveGameObjectToScene(root, scene);
            var reticle = new GameObject("Test reticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(AimReticle2D));
            reticle.transform.SetParent(root.transform, false);
            return root.GetComponent<Canvas>();
        }
    }
}
