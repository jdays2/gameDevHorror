using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift
{
    [DefaultExecutionOrder(500), DisallowMultipleComponent]
    public sealed class LevelCheckpointController2D : MonoBehaviour
    {
        [SerializeField]
        private LevelCatalog catalog;
        [SerializeField]
        private LevelDefinition definition;
        [SerializeField]
        private LevelRunController2D run;
        [SerializeField]
        private PlayerLife2D player;
        [SerializeField, Tooltip("Normal play resumes the local slot. Development probes use isolated paths.")]
        private bool resumeOnStart = true;
        private static readonly Dictionary<string, string> sessionPaths = new Dictionary<string, string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSessionPaths()
        {
            sessionPaths.Clear();
        }

        private string StorePath => sessionPaths.TryGetValue(definition.levelId, out var path) ? path : Path.Combine(StorageDirectory(), definition.levelId + ".json");

        private CheckpointStore store;
        private Coroutine pending;
        private readonly HashSet<string> visited = new HashSet<string>();
        public string LastCheckpointId
        {
            get; private set;
        }
        public string LastError
        {
            get; private set;
        }
        public bool IsSaving => pending != null;
        public bool HasCheckpoint => Store.Exists;
        private CheckpointStore Store => store ?? (store = new CheckpointStore(StorePath));

        private static string StorageDirectory()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--checkpoint-directory");
            if (index >= 0 && index + 1 < args.Length)
            {
                return Path.GetFullPath(args[index + 1]);
            }

            return Path.Combine(Application.persistentDataPath, "Checkpoints");
        }

        public void Configure(LevelCatalog levels, LevelDefinition data, LevelRunController2D owner, PlayerLife2D hero)
        {
            catalog = levels;
            definition = data;
            run = owner;
            player = hero;
        }

        public void UseStore(string path, bool autoResume = false)
        {
            sessionPaths[definition.levelId] = Path.GetFullPath(path);
            store = new CheckpointStore(path);
            resumeOnStart = autoResume;
        }

        private void Start()
        {
            run.Finished += OnFinished;
            if (resumeOnStart && Store.Exists)
            {
                TryRestore();
            }
        }

        private void OnDestroy()
        {
            if (run != null)
            {
                run.Finished -= OnFinished;
            }
        }

        private void OnFinished()
        {
            if (pending != null)
            {
                StopCoroutine(pending);
            }

            pending = null;
            Clear();
        }

        [ContextMenu("Clear local checkpoint slot")]
        public void Clear()
        {
            Store.Delete();
            visited.Clear();
            LastCheckpointId = null;
        }

        public bool RequestSave(string checkpointId)
        {
            if (!isActiveAndEnabled || player.IsDead || run.Completed || string.IsNullOrWhiteSpace(checkpointId) || visited.Contains(checkpointId))
            {
                return false;
            }

            if (pending != null)
            {
                StopCoroutine(pending);
            }

            pending = StartCoroutine(SaveWhenSettled(checkpointId));
            return true;
        }

        private IEnumerator SaveWhenSettled(string id)
        {
            // Never capture flying fragments or a half-open leaf. Keep the existing slot until the new state is complete.
            yield return null;
            while (!WorldSettled())
            {
                if (player.IsDead || run.Completed)
                {
                    pending = null;
                    yield break;
                }

                yield return null;
            }

            if (!player.IsDead && !run.Completed)
            {
                TrySaveNow(id);
            }

            pending = null;
        }

        private bool WorldSettled()
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var gore in root.GetComponentsInChildren<GoreBurst2D>(true))
                {
                    if (!gore.IsSettled)
                    {
                        return false;
                    }
                }

                foreach (var door in root.GetComponentsInChildren<Door2D>(true))
                {
                    if (door.IsMoving)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public CheckpointData Capture(string id)
        {
            if (player.IsDead || run.Completed || !WorldSettled())
            {
                throw new InvalidOperationException("Checkpoint requires a living player and settled world");
            }

            var entities = PersistentEntity2D.Collect(gameObject.scene);
            var saved = new CheckpointData
            {
                levelId = definition.levelId,
                levelRevision = definition.saveRevision,
                checkpointId = id,
                elapsedSeconds = run.ElapsedSeconds,
                entities = new CheckpointEntityState[entities.Count]
            };
            var visits = new HashSet<string>(visited);
            visits.Add(id);
            saved.visitedCheckpoints = new string[visits.Count];
            visits.CopyTo(saved.visitedCheckpoints);
            int index = 0;
            foreach (var entity in entities.Values)
            {
                saved.entities[index++] = entity.Capture();
            }

            return saved;
        }

        public bool TrySaveNow(string id)
        {
            try
            {
                Store.Write(Capture(id));
                visited.Add(id);
                LastCheckpointId = id;
                LastError = null;
                player.GetComponent<PlayerInteraction>().ShowNotification("Контрольная точка сохранена");
                return true;
            }
            catch (Exception error)
            {
                Report(error);
                return false;
            }
        }

        public bool TryRestore()
        {
            try
            {
                Apply(Store.Read());
                LastError = null;
                return true;
            }
            catch (Exception error)
            {
                Report(error);
                return false;
            }
        }

        public void Apply(CheckpointData saved)
        {
            CheckpointStore.Validate(saved);
            if (catalog.Find(saved.levelId) != definition || saved.levelId != definition.levelId || saved.levelRevision != definition.saveRevision)
            {
                throw new InvalidOperationException("Checkpoint belongs to another level");
            }

            var entities = PersistentEntity2D.Collect(gameObject.scene);
            if (entities.Count != saved.entities.Length)
            {
                throw new InvalidOperationException("Authored level differs from checkpoint; start a fresh run");
            }

            // Validate the entire snapshot before touching any live entity.
            foreach (var state in saved.entities)
            {
                if (!entities.TryGetValue(state.id, out var entity))
                {
                    throw new InvalidOperationException("Saved entity missing: " + state.id);
                }

                entity.Validate(state);
            }

            foreach (var state in saved.entities)
            {
                entities[state.id].PrepareRestore(state);
            }

            foreach (var state in saved.entities)
            {
                entities[state.id].Restore(state);
            }

            Physics2D.SyncTransforms();
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var camera in root.GetComponentsInChildren<CameraFollow2D>())
                {
                    camera.SnapToTarget();
                }
            }

            run.RestoreElapsed(saved.elapsedSeconds);
            LastCheckpointId = saved.checkpointId;
            visited.Clear();
            if (saved.visitedCheckpoints != null)
            {
                foreach (var id in saved.visitedCheckpoints)
                {
                    visited.Add(id);
                }
            }

            visited.Add(saved.checkpointId);
            player.GetComponent<PlayerInteraction>().ShowNotification("Контрольная точка загружена");
        }

        public void Reload()
        {
            SceneManager.LoadScene(gameObject.scene.buildIndex);
        }

        private void Report(Exception error)
        {
            LastError = error.Message;
            Debug.LogWarning("Checkpoint: " + error.Message, this);
            player.GetComponent<PlayerInteraction>().ShowNotification("Ошибка контрольной точки: " + error.Message);
        }

        public static LevelCheckpointController2D For(Component owner)
        {
            foreach (var root in owner.gameObject.scene.GetRootGameObjects())
            {
                var result = root.GetComponentInChildren<LevelCheckpointController2D>(true);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
