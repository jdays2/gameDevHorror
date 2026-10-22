using System;
using EmergencyShift.Features.MainMenu.Logic;
using UnityEngine;

namespace EmergencyShift.Features.MainMenu.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/UI/Main menu catalog")]
    public sealed class MainMenuCatalog : ScriptableObject
    {
        [Serializable]
        private sealed class Entry
        {
            [SerializeField] private string id;
            [SerializeField] private string title;
            [SerializeField] private string scenePath;
            public MenuScene ToScene() => new MenuScene(id, title, scenePath);
        }

        [SerializeField] private string homePath = "Assets/EmergencyShift/Scenes/MainMenu.unity";
        [SerializeField] private string newGameId = "sandbox";
        [SerializeField] private Entry[] scenes = Array.Empty<Entry>();

        public MenuSceneCatalog ToCatalog()
        {
            var entries = new MenuScene[scenes.Length];
            for (int index = 0; index < entries.Length; index++)
            {
                entries[index] = scenes[index]?.ToScene() ?? throw new InvalidOperationException("Missing menu scene entry.");
            }

            return new MenuSceneCatalog(homePath, newGameId, entries);
        }
    }
}
