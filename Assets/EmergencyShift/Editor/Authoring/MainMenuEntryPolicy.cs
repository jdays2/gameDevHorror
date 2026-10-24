using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;

namespace EmergencyShift.Editor
{
    /// <summary>Editor-owned entry policy; authored gameplay scenes remain open and unsaved.</summary>
    [InitializeOnLoad]
    public static class MainMenuEntryPolicy
    {
        internal const string TestRunKey = "EmergencyShift.MainMenu.AutomatedTestRun";
        // This pinned Test Framework exposes pending-run state only internally.
        // RunStarted callbacks are too late: they fire after the Play scene was chosen.
        private static readonly MethodInfo IsRunActive = typeof(TestRunnerApi).GetMethod("IsRunActive", BindingFlags.Static | BindingFlags.NonPublic);

        private static bool AutomatedTestRun => SessionState.GetBool(TestRunKey, false)
            || (bool)(IsRunActive?.Invoke(null, null) ?? false);

        static MainMenuEntryPolicy()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.projectChanged += ScheduleSynchronization;
            EditorBuildSettings.sceneListChanged += ScheduleSynchronization;
            TestRunnerApi.RegisterTestCallback(new MenuTestRunCallbacks());
            ScheduleSynchronization();
        }

        public static void RestoreEntry()
        {
            var menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuAuthoring.ScenePath);
            if (menu != null && EditorSceneManager.playModeStartScene != menu)
            {
                EditorSceneManager.playModeStartScene = menu;
            }
        }

        [MenuItem("Emergency Shift/UI/Synchronize universal menu entry")]
        public static void Synchronize()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || AutomatedTestRun)
            {
                return;
            }

            RestoreEntry();
            MainMenuSceneRegistry.Synchronize();
        }

        internal static void ScheduleSynchronization()
        {
            EditorApplication.delayCall -= Synchronize;
            EditorApplication.delayCall += Synchronize;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (AutomatedTestRun)
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                {
                    // Test Framework uses the currently prepared fixture scene.
                    EditorSceneManager.playModeStartScene = null;
                }
                return;
            }

            if (state == PlayModeStateChange.ExitingEditMode)
            {
                RestoreEntry();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                ScheduleSynchronization();
            }
        }
    }

    internal sealed class MenuTestRunCallbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) => SessionState.SetBool(MainMenuEntryPolicy.TestRunKey, true);

        public void RunFinished(ITestResultAdaptor result)
        {
            SessionState.SetBool(MainMenuEntryPolicy.TestRunKey, false);
            MainMenuEntryPolicy.ScheduleSynchronization();
        }

        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
    }
}
