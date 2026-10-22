using System;
using System.Collections.Generic;
using System.IO;

namespace EmergencyShift.Features.CheckpointSession.Logic
{
    /// <summary>Explicit world-session overrides survive level reload and close with their DI scope.</summary>
    public sealed class CheckpointPathRegistry : IDisposable
    {
        private const int MaximumOverrides = 128;
        private readonly Dictionary<string, string> overrides = new Dictionary<string, string>(StringComparer.Ordinal);
        private bool disposed;

        public string Resolve(string levelId, string directory)
        {
            RequireActive();
            ValidateId(levelId);
            return overrides.TryGetValue(levelId, out string path) ? path : Path.Combine(directory, levelId + ".json");
        }

        public void Override(string levelId, string path)
        {
            RequireActive();
            ValidateId(levelId);
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Checkpoint path is required.", nameof(path));
            }

            string fullPath = Path.GetFullPath(path);
            if (!overrides.ContainsKey(levelId) && overrides.Count >= MaximumOverrides)
            {
                throw new InvalidOperationException("Checkpoint session override limit reached.");
            }

            overrides[levelId] = fullPath;
        }

        public void Dispose()
        {
            overrides.Clear();
            disposed = true;
        }

        private void RequireActive()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(CheckpointPathRegistry));
            }
        }

        private static void ValidateId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || Path.GetFileName(id) != id || id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new ArgumentException("Level ID must be a stable filename token.", nameof(id));
            }
        }
    }
}
