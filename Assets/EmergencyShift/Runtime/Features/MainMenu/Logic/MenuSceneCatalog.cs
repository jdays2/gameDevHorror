using System;
using System.Collections.Generic;

namespace EmergencyShift.Features.MainMenu.Logic
{
    public sealed class MenuSceneCatalog
    {
        private readonly MenuScene[] scenes;
        public string HomePath { get; }
        public string NewGameId { get; }
        public int Count => scenes.Length;
        public MenuScene At(int index) => scenes[index];

        public MenuSceneCatalog(string homePath, string newGameId, IEnumerable<MenuScene> entries)
        {
            HomePath = new MenuScene("home", "Главное меню", homePath).Path;
            var values = new List<MenuScene>(entries ?? throw new ArgumentNullException(nameof(entries)));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (MenuScene scene in values)
            {
                if (scene == null || !ids.Add(scene.Id) || !paths.Add(scene.Path) || scene.Path == homePath)
                {
                    throw new ArgumentException("Menu scene IDs and paths must be unique and exclude the home scene.");
                }
            }

            scenes = values.ToArray();
            if (Find(newGameId) == null)
            {
                throw new ArgumentException("New game must reference an entry in the scene catalog.");
            }

            NewGameId = newGameId;
        }

        public MenuScene Find(string id)
        {
            foreach (MenuScene scene in scenes)
            {
                if (string.Equals(scene.Id, id, StringComparison.Ordinal))
                {
                    return scene;
                }
            }

            return null;
        }
    }
}
