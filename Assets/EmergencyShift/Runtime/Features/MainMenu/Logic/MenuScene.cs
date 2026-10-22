using System;

namespace EmergencyShift.Features.MainMenu.Logic
{
    public sealed class MenuScene
    {
        public string Id { get; }
        public string Title { get; }
        public string Path { get; }

        public MenuScene(string id, string title, string path)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title) ||
                string.IsNullOrWhiteSpace(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("A menu scene needs a stable ID, title and project scene path.");
            }

            Id = id;
            Title = title;
            Path = path;
        }
    }
}
