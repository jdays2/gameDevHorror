using System;
using EmergencyShift.Features.MainMenu.View;
using EmergencyShift.Features.PauseMenu.Config;
using EmergencyShift.Features.PauseMenu.View;
using UnityEditor;
using UnityEngine;

namespace EmergencyShift.Editor
{
    public static class MainMenuAudioAuthoring
    {
        [MenuItem("Emergency Shift/UI/Install existing music in main menu")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Author menu audio in Edit Mode.");
            }
            var config = AssetDatabase.LoadAssetAtPath<PauseMenuAudioConfig>("Assets/EmergencyShift/Data/Audio/PauseMenuAudio.asset");
            config.Validate();
            GameObject root = PrefabUtility.LoadPrefabContents(MainMenuAuthoring.PrefabPath);
            try
            {
                if (root.GetComponent<CanvasGroup>() == null)
                {
                    root.AddComponent<CanvasGroup>();
                }
                PauseMenuMusic music = root.GetComponentInChildren<PauseMenuMusic>(true);
                if (music == null)
                {
                    var audio = new GameObject("Menu music");
                    audio.transform.SetParent(root.transform, false);
                    music = audio.AddComponent<PauseMenuMusic>();
                    AudioSource intro = audio.AddComponent<AudioSource>();
                    AudioSource loop = audio.AddComponent<AudioSource>();
                    intro.playOnAwake = false;
                    loop.playOnAwake = false;
                    intro.clip = config.Intro;
                    loop.clip = config.Loop;
                    loop.loop = true;
                    intro.volume = 0;
                    loop.volume = 0;
                    intro.ignoreListenerPause = true;
                    loop.ignoreListenerPause = true;
                    Assign(music, "config", config);
                    Assign(music, "introSource", intro);
                    Assign(music, "loopSource", loop);
                }
                Assign(root.GetComponent<MainMenuPresenter>(), "music", music);
                PrefabUtility.SaveAsPrefabAsset(root, MainMenuAuthoring.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            const string worldPath = "Assets/EmergencyShift/Resources/ProjectContext.prefab";
            GameObject world = PrefabUtility.LoadPrefabContents(worldPath);
            try
            {
                Assign(world.GetComponent<MenuNavigationAdapter>(), "transitionAudio", config);
                PrefabUtility.SaveAsPrefabAsset(world, worldPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(world);
            }
        }

        private static void Assign(UnityEngine.Object owner, string name, UnityEngine.Object value)
        {
            var data = new SerializedObject(owner);
            data.FindProperty(name).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
