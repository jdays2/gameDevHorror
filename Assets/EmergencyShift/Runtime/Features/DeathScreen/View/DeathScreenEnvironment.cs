using System;
using System.Collections;
using EmergencyShift.Core.Commands;
using EmergencyShift.Features.DeathScreen.Config;
using EmergencyShift.Features.DeathScreen.Logic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace EmergencyShift.Features.DeathScreen.View
{
    /// <summary>One scene's modal and reload adapter. Does not own listener or world-audio gain.</summary>
    public sealed class DeathScreenEnvironment : MonoBehaviour, IDeathScreenEnvironment
    {
        [SerializeField] private DeathScreenProfile profile;
        private PlayerLife2D player;
        private GameplayInputBlock input;
        private WorldMotionPause motion;
        private GameObject pauseRoot;
        private DialogueSession dialogue;
        private LevelCheckpointController2D checkpoint;
        private bool bound;
        private bool windowsCaptured;
        private bool previousPauseActive;
        private bool loading;

        public DeathScreenProfile Profile => profile;
        public bool IsDead => player != null && player.IsDead;
        public bool HasCheckpoint => checkpoint != null && checkpoint.isActiveAndEnabled && checkpoint.ResumesOnStart && checkpoint.HasCheckpoint;
        public string RestoreError => checkpoint != null && checkpoint.ResumesOnStart ? checkpoint.LastError : null;
        public string LastFailure { get; private set; }

        [Inject]
        private void Construct(PlayerLife2D player, GameplayInputBlock input, WorldMotionPause motion)
        {
            this.player = player;
            this.input = input;
            this.motion = motion;
        }

        public void BindScene(GameObject pauseMenuRoot, DialogueSession dialogueSession,
            LevelCheckpointController2D checkpointController)
        {
            if (bound)
            {
                throw new InvalidOperationException("Death-screen scene links can only be bound once.");
            }
            bound = true;
            pauseRoot = pauseMenuRoot;
            dialogue = dialogueSession;
            checkpoint = checkpointController;
        }

        public IDisposable AcquireInput() => input.Acquire();
        public IDisposable AcquireMotion() => motion.Acquire();

        public void CloseOtherWindows()
        {
            if (windowsCaptured)
            {
                return;
            }
            windowsCaptured = true;
            if (pauseRoot != null)
            {
                previousPauseActive = pauseRoot.activeSelf;
                // Existing pause OnDisable cancels its controller, pending quit and music.
                pauseRoot.SetActive(false);
            }
            dialogue?.Cancel();
        }

        public void RestoreOtherWindows()
        {
            if (!windowsCaptured)
            {
                return;
            }
            windowsCaptured = false;
            if (pauseRoot != null)
            {
                pauseRoot.SetActive(previousPauseActive);
            }
        }

        public void BeginRetry(bool fromCheckpoint, Action<CommandResult> completed)
        {
            if (completed == null)
            {
                throw new ArgumentNullException(nameof(completed));
            }
            if (loading || !isActiveAndEnabled)
            {
                completed(CommandResult.Fail("Переход уже выполняется. Попробуйте снова."));
                return;
            }
            if (fromCheckpoint || HasCheckpoint)
            {
                if (checkpoint == null || !checkpoint.isActiveAndEnabled || !checkpoint.ResumesOnStart ||
                    !checkpoint.TryValidateReload(out _))
                {
                    LastFailure = checkpoint != null ? checkpoint.LastError : "Checkpoint controller is unavailable.";
                    completed(CommandResult.Fail("Не удалось загрузить сохранение.\nФайл не изменён. Попробуйте снова."));
                    return;
                }
            }
            string path = gameObject.scene.path;
            if (string.IsNullOrEmpty(path) || !Application.CanStreamedLevelBeLoaded(path))
            {
                LastFailure = "The current scene cannot be loaded: " + path;
                completed(CommandResult.Fail("Не удалось загрузить сцену.\nПопробуйте снова."));
                return;
            }
            loading = true;
            StartCoroutine(Reload(path, completed));
        }

        public CommandResult Quit()
        {
            if (Application.isEditor)
            {
                return CommandResult.Fail("Выход доступен в сборке.\nМожно повторить попытку.");
            }
            Application.Quit();
            return CommandResult.Ok();
        }

        private IEnumerator Reload(string path, Action<CommandResult> completed)
        {
            AsyncOperation operation = null;
            try
            {
                operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
            }
            catch (Exception error)
            {
                LastFailure = error.Message;
            }
            if (operation == null)
            {
                loading = false;
                completed(CommandResult.Fail("Не удалось загрузить сцену.\nПопробуйте снова."));
                yield break;
            }
            yield return operation;
            // A normal Single load destroys this old scene owner; the new scope fences input.
            loading = false;
            LastFailure = null;
            completed(CommandResult.Ok());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            loading = false;
            RestoreOtherWindows();
        }
    }
}
