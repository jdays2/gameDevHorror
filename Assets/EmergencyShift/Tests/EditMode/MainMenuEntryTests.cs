using System.Linq;
using EmergencyShift.Editor;
using EmergencyShift.Features.MainMenu.Config;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Tests
{
    public sealed class MainMenuEntryTests
    {
        [Test]
        public void RestoreEntryRepairsResetWithoutChangingTheOpenScene()
        {
            SceneAsset previous = EditorSceneManager.playModeStartScene;
            string active = SceneManager.GetActiveScene().path;
            try
            {
                EditorSceneManager.playModeStartScene = null;
                MainMenuEntryPolicy.RestoreEntry();
                Assert.That(AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene), Is.EqualTo(MainMenuAuthoring.ScenePath));
                Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(active));
            }
            finally
            {
                EditorSceneManager.playModeStartScene = previous;
            }
        }

        [Test]
        public void RegistryDiscoversNewSceneAndRemovesDeletedSceneWithoutLosingExistingIdentity()
        {
            const string path = "Assets/EmergencyShift/Scenes/__MainMenuRegistryTest.unity";
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), Is.Null);
            var source = AssetDatabase.LoadAssetAtPath<MainMenuCatalog>(MainMenuAuthoring.CatalogPath);
            var before = source.ToCatalog();
            try
            {
                Assert.That(AssetDatabase.CopyAsset(MainMenuAuthoring.ScenePath, path), Is.True);
                MainMenuSceneRegistry.Synchronize();
                var withScene = source.ToCatalog();
                Assert.That(withScene.Count, Is.EqualTo(before.Count + 1));
                Assert.That(Enumerable.Range(0, withScene.Count).Select(withScene.At).Any(scene => scene.Path == path), Is.True);
                Assert.That(withScene.Find("sandbox").Title, Is.EqualTo(before.Find("sandbox").Title));
                Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo(MainMenuAuthoring.ScenePath));
                MainMenuSceneRegistry.Synchronize();
                Assert.That(source.ToCatalog().Count, Is.EqualTo(withScene.Count));
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
                MainMenuSceneRegistry.Synchronize();
            }
            Assert.That(source.ToCatalog().Count, Is.EqualTo(before.Count));
        }
    }
}
