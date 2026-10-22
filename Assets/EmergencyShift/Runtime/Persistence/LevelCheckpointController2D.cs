using System;
using System.Collections;
using System.IO;
using EmergencyShift.Features.CheckpointSession;
using EmergencyShift.Features.CheckpointSession.Logic;
using UnityEngine;
using Zenject;

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
        private CheckpointPathRegistry paths;
        private CheckpointSessionController session;
        private CheckpointSceneAdapter2D world;
        private LevelRunController2D subscribedRun;
        private CheckpointStore store;
        private Coroutine pending;
        public string LastCheckpointId => session?.LastCheckpointId;
        public string LastError { get; private set; }
        public bool IsSaving => session != null && session.IsSaving;
        public bool HasCheckpoint => Store.Exists;
        private string StorePath => RequirePaths().Resolve(definition.LevelId, StorageDirectory());
        private CheckpointStore Store => store ??= new CheckpointStore(StorePath);
        private bool CanSave => isActiveAndEnabled && player != null && !player.IsDead && run != null && !run.Completed;

        [Inject]
        public void Construct(CheckpointSessionFactory factory, CheckpointPathRegistry registry)
        {
            if (session != null)
            {
                throw new InvalidOperationException("Checkpoint session is already composed.");
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            paths = registry ?? throw new ArgumentNullException(nameof(registry));
            session = factory.Create();
            world = new CheckpointSceneAdapter2D(gameObject.scene);
        }

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
            CancelPendingSave();
            DetachRun();
            if (definition != data)
            {
                store = null;
                session?.Clear();
            }

            catalog = levels;
            definition = data;
            run = owner;
            player = hero;
            BindRun();
        }

        public void UseStore(string path, bool autoResume = false)
        {
            string canonical = Path.GetFullPath(path);
            var selected = new CheckpointStore(canonical);
            RequirePaths().Override(definition.LevelId, canonical);
            store = selected;
            resumeOnStart = autoResume;
        }

        private void OnEnable() => BindRun();

        private void Start()
        {
            RequireSession();
            BindRun();
            if (resumeOnStart && Store.Exists)
            {
                TryRestore();
            }
        }

        private void OnDisable()
        {
            CancelPendingSave();
            DetachRun();
        }

        private void OnDestroy()
        {
            CancelPendingSave();
            DetachRun();
        }

        private void BindRun()
        {
            if (subscribedRun != null || !isActiveAndEnabled || run == null)
            {
                return;
            }

            subscribedRun = run;
            subscribedRun.Finished += OnFinished;
        }

        private void DetachRun()
        {
            if (subscribedRun != null)
            {
                subscribedRun.Finished -= OnFinished;
            }

            subscribedRun = null;
        }

        private void OnFinished()
        {
            Clear();
        }

        [ContextMenu("Clear local checkpoint slot")]
        public void Clear()
        {
            CancelPendingSave();
            Store.Delete();
            RequireSession().Clear();
        }

        public bool RequestSave(string checkpointId)
        {
            if (!RequireSession().CanRequest(checkpointId, CanSave).Success)
            {
                return false;
            }

            CancelPendingSave();
            if (!session.Request(checkpointId, CanSave).Success)
            {
                return false;
            }

            pending = StartCoroutine(SaveWhenSettled(checkpointId));
            return true;
        }

        private void CancelPendingSave()
        {
            Coroutine current = pending;
            pending = null;
            try
            {
                if (current != null)
                {
                    StopCoroutine(current);
                }
            }
            finally
            {
                session?.Cancel();
            }
        }

        private IEnumerator SaveWhenSettled(string id)
        {
            // No snapshot of flying fragments/half-open leaves; cancel with this adapter's lifetime.
            try
            {
                yield return null;
                while (!WorldSettled())
                {
                    if (!CanSave)
                    {
                        yield break;
                    }

                    yield return null;
                }

                if (CanSave)
                {
                    TrySaveNow(id);
                }
            }
            finally
            {
                pending = null;
                session?.Cancel();
            }
        }

        private bool WorldSettled() => world.IsSettled();

        public CheckpointData Capture(string id)
        {
            if (player.IsDead || run.Completed || !WorldSettled())
            {
                throw new InvalidOperationException("Checkpoint requires a living player and settled world");
            }

            var saved = new CheckpointData
            {
                levelId = definition.LevelId,
                levelRevision = definition.SaveRevision,
                checkpointId = id,
                elapsedSeconds = run.ElapsedSeconds,
                entities = world.Capture()
            };
            saved.visitedCheckpoints = RequireSession().CaptureVisits(id);
            return saved;
        }

        public bool TrySaveNow(string id)
        {
            try
            {
                Store.Write(Capture(id));
                RequireSession().RecordSaved(id);
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
            var visits = new CheckpointVisitSnapshot(saved.checkpointId, saved.visitedCheckpoints);
            if (catalog.Find(saved.levelId) != definition || saved.levelId != definition.LevelId || saved.levelRevision != definition.SaveRevision)
            {
                throw new InvalidOperationException("Checkpoint belongs to another level");
            }

            world.Apply(saved.entities);
            run.RestoreElapsed(saved.elapsedSeconds);
            RequireSession().Restore(visits);
            player.GetComponent<PlayerInteraction>().ShowNotification("Контрольная точка загружена");
        }

        private CheckpointSessionController RequireSession() => session ?? throw new InvalidOperationException("Checkpoint requires level composition.");
        private CheckpointPathRegistry RequirePaths() => paths ?? throw new InvalidOperationException("Checkpoint paths require world composition.");

        public void Reload()
        {
            world.Reload();
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
