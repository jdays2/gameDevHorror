using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Editor
{
    /// <summary>Reusable UI asset; scene context and conversation content remain scene-owned.</summary>
    public static class DialoguePrefabAuthoring
    {
        public const string PrefabPath = "Assets/EmergencyShift/Prefabs/UI/DialogueUI.prefab";
        private const string SandboxFixture = "Dialogue test - start corridor";

        [MenuItem("Emergency Shift/UI/Create dialogue prefab from sandbox")]
        public static void ExportAndConnectSandbox()
        {
            Scene scene = SceneManager.GetActiveScene();
            RequireEditMode(scene);
            if (scene.path != SandboxSceneBuilder.ScenePath || scene.isDirty)
            {
                throw new InvalidOperationException("Open the saved MechanicsSandbox scene first.");
            }

            GameObject fixture = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == SandboxFixture)
                {
                    fixture = root;
                    break;
                }
            }

            if (fixture == null || fixture.GetComponent<DialogueSession>() == null)
            {
                throw new InvalidOperationException("The authored dialogue test is missing.");
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                prefab = CreatePrefab(fixture);
            }

            if (PrefabUtility.IsPartOfPrefabInstance(fixture))
            {
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(fixture) != PrefabPath)
                {
                    throw new InvalidOperationException("The fixture belongs to another prefab.");
                }

                return;
            }

            var settings = new ConvertToPrefabInstanceSettings
            {
                objectMatchMode = ObjectMatchMode.ByHierarchy,
                componentsNotMatchedBecomesOverride = true,
                gameObjectsNotMatchedBecomesOverride = true,
                recordPropertyOverridesOfMatches = true,
                changeRootNameToAssetName = false
            };
            int siblingIndex = fixture.transform.GetSiblingIndex();
            PrefabUtility.ConvertToPrefabInstance(fixture, prefab, settings, InteractionMode.AutomatedAction);
            fixture.transform.SetSiblingIndex(siblingIndex);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = fixture;
        }

        private static GameObject CreatePrefab(GameObject source)
        {
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var template = UnityEngine.Object.Instantiate(source);
                SceneManager.MoveGameObjectToScene(template, preview);
                template.name = "Dialogue UI";
                var view = template.GetComponentInChildren<DialogueView>(true);
                if (view == null || view.transform.parent != template.transform)
                {
                    throw new InvalidOperationException("Expected one authored dialogue Canvas under the fixture.");
                }

                for (int i = template.transform.childCount - 1; i >= 0; i--)
                {
                    var child = template.transform.GetChild(i);
                    if (child != view.transform)
                    {
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                    }
                }

                var session = template.GetComponent<DialogueSession>();
                Assign(session, "view", view);
                Assign(session, "gameplayHud", null);
                Assign(session, "reticle", null);
                view.GetComponent<Canvas>().enabled = false;
                var prefab = PrefabUtility.SaveAsPrefabAsset(template, PrefabPath, out bool saved);
                if (!saved || prefab == null)
                {
                    throw new InvalidOperationException("Could not save the dialogue prefab.");
                }

                return prefab;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        [MenuItem("Emergency Shift/UI/Add dialogue to current scene")]
        public static void InstallInCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            RequireEditMode(scene);
            var hud = SelectedOrUniqueHud(scene);
            var reticle = hud.GetComponentInChildren<AimReticle2D>(true);
            var instance = Install(scene, hud, reticle);
            Selection.activeGameObject = instance;
        }

        /// <summary>Editor setup only. Does not save the scene or add a story/interaction marker.</summary>
        public static GameObject Install(Scene scene, Canvas gameplayHud, AimReticle2D reticle)
        {
            RequireEditMode(scene);
            if (gameplayHud == null || gameplayHud.gameObject.scene != scene
                || gameplayHud.GetComponent<DialogueView>() != null)
            {
                throw new InvalidOperationException("Assign the gameplay HUD Canvas from this scene.");
            }

            if (reticle != null && reticle.gameObject.scene != scene)
            {
                throw new InvalidOperationException("The reticle must belong to this scene.");
            }

            foreach (var existing in UnityEngine.Object.FindObjectsByType<DialogueSession>(FindObjectsInactive.Include))
            {
                if (existing.gameObject.scene == scene)
                {
                    throw new InvalidOperationException("This scene already has a dialogue session.");
                }
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Create the dialogue prefab from the sandbox first.");
            }

            var events = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            if (events.Length > 1 || (events.Length == 1 && (!events[0].isActiveAndEnabled
                || events[0].GetComponent<InputSystemUIInputModule>() == null)))
            {
                throw new InvalidOperationException("Use one active EventSystem with InputSystemUIInputModule.");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(instance, "Add dialogue UI");
            var session = instance.GetComponent<DialogueSession>();
            Assign(session, "gameplayHud", gameplayHud);
            Assign(session, "reticle", reticle);
            PrefabUtility.RecordPrefabInstancePropertyModifications(session);
            if (events.Length == 0)
            {
                var eventObject = new GameObject("UI EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(eventObject, scene);
                eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                Undo.RegisterCreatedObjectUndo(eventObject, "Add UI EventSystem");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            return instance;
        }

        private static Canvas SelectedOrUniqueHud(Scene scene)
        {
            if (Selection.activeGameObject != null)
            {
                var selected = Selection.activeGameObject.GetComponentInParent<Canvas>();
                if (selected != null && selected.gameObject.scene == scene && selected.GetComponent<DialogueView>() == null)
                {
                    return selected;
                }
            }

            Canvas result = null;
            foreach (var hud in UnityEngine.Object.FindObjectsByType<PrototypeHud>(FindObjectsInactive.Include))
            {
                if (hud.gameObject.scene != scene)
                {
                    continue;
                }

                if (result != null)
                {
                    throw new InvalidOperationException("Select the desired gameplay HUD Canvas before installing.");
                }

                result = hud.GetComponentInParent<Canvas>();
            }

            if (result == null)
            {
                throw new InvalidOperationException("Select the gameplay HUD Canvas before installing.");
            }

            return result;
        }

        private static void RequireEditMode(Scene scene)
        {
            if (Application.isPlaying || !scene.IsValid() || !scene.isLoaded || EditorSceneManager.IsPreviewScene(scene))
            {
                throw new InvalidOperationException("Use a loaded scene in Edit Mode.");
            }
        }

        private static void Assign(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(property).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
