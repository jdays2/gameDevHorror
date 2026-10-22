using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift
{
    /// <summary>Cold native traversal/restore boundary for exactly one authored scene.</summary>
    internal sealed class CheckpointSceneAdapter2D
    {
        private readonly Scene scene;

        public CheckpointSceneAdapter2D(Scene scene) => this.scene = scene;

        public bool IsSettled()
        {
            RequireLoaded();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (GoreBurst2D gore in root.GetComponentsInChildren<GoreBurst2D>(true))
                {
                    if (!gore.IsSettled)
                    {
                        return false;
                    }
                }

                foreach (Door2D door in root.GetComponentsInChildren<Door2D>(true))
                {
                    if (door.IsMoving)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public CheckpointEntityState[] Capture()
        {
            RequireLoaded();
            var entities = PersistentEntity2D.Collect(scene);
            var saved = new CheckpointEntityState[entities.Count];
            int index = 0;
            foreach (PersistentEntity2D entity in entities.Values)
            {
                saved[index++] = entity.Capture();
            }

            return saved;
        }

        public void Apply(CheckpointEntityState[] saved)
        {
            RequireLoaded();
            var entities = PersistentEntity2D.Collect(scene);
            if (saved == null || entities.Count != saved.Length)
            {
                throw new InvalidOperationException("Authored level differs from checkpoint; start a fresh run.");
            }

            // Validate every participant before any activation, pose write or native restoration.
            foreach (CheckpointEntityState state in saved)
            {
                if (state == null || string.IsNullOrWhiteSpace(state.id) || !entities.TryGetValue(state.id, out PersistentEntity2D entity))
                {
                    throw new InvalidOperationException("Saved entity missing.");
                }

                entity.Validate(state);
            }

            foreach (CheckpointEntityState state in saved)
            {
                entities[state.id].PrepareRestore(state);
            }

            foreach (CheckpointEntityState state in saved)
            {
                entities[state.id].Restore(state);
            }

            Physics2D.SyncTransforms();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (CameraFollow2D camera in root.GetComponentsInChildren<CameraFollow2D>())
                {
                    camera.SnapToTarget();
                }
            }
        }

        public void Reload()
        {
            RequireLoaded();
            SceneManager.LoadScene(scene.buildIndex);
        }

        private void RequireLoaded()
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException("Checkpoint scene is no longer loaded.");
            }
        }
    }
}
