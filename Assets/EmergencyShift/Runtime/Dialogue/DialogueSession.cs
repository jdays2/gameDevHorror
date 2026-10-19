using System;
using UnityEngine;
using EmergencyShift.Features.PlayerInput.Logic;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Owns one modal conversation, previous presentation state and input-release fence.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class DialogueSession : MonoBehaviour
    {
        [SerializeField] private DialogueView view;
        [SerializeField] private Canvas gameplayHud;
        [SerializeField] private AimReticle2D reticle;
        private DialogueSequence sequence;
        private DialogueRun run;
        private IDisposable inputLease;
        private IDisposable motionLease;
        private bool previousHud;
        private bool previousReticle;
        private bool previousCursor;
        private bool waitingForRelease;
        private int changedFrame;
        private IPlayerInput input;
        private GameplayInputBlock inputBlock;
        private WorldMotionPause motionPause;

        [Inject]
        private void Construct(IPlayerInput playerInput, GameplayInputBlock block, WorldMotionPause pause)
        {
            input = playerInput;
            inputBlock = block;
            motionPause = pause;
        }
        public bool IsOpen => run != null && !run.IsComplete;
        public int LineIndex => run == null ? -1 : run.LineIndex;
        public string LineId => IsOpen ? run.Current.id : null;
        public bool RequiresChoice => IsOpen && run.RequiresChoice;
        public event Action<DialogueTransition> Transitioned;

        private void OnEnable()
        {
            if (view != null)
            {
                view.AdvanceRequested += Advance;
                view.ChoiceRequested += Choose;
            }
        }

        private void OnDisable()
        {
            if (view != null)
            {
                view.AdvanceRequested -= Advance;
                view.ChoiceRequested -= Choose;
            }
            Cancel();
        }

        public bool TryBegin(DialogueSequence content)
        {
            if (content == null || view == null || gameplayHud == null || inputLease != null || inputBlock.IsBlocked)
            {
                return false;
            }

            var nextRun = new DialogueRun(content);
            previousHud = gameplayHud.enabled;
            previousReticle = reticle != null && reticle.enabled;
            if (reticle != null)
            {
                reticle.enabled = false;
            }

            previousCursor = Cursor.visible;
            inputLease = inputBlock.Acquire();
            gameplayHud.enabled = false;
            Cursor.visible = true;
            motionLease = motionPause.Acquire();
            sequence = content;
            run = nextRun;
            changedFrame = Time.frameCount;
            PresentLine();
            Transitioned?.Invoke(run.LastTransition);
            return true;
        }

        private void Update()
        {
            if (waitingForRelease)
            {
                if (Time.frameCount > changedFrame + 1 && !ConfirmationHeld())
                {
                    ReleaseContext();
                }

                return;
            }

            if (!IsOpen)
            {
                return;
            }

            if (input.DialogueAdvancePressed)
            {
                Advance();
            }
        }

        public void Advance()
        {
            if (!IsOpen || Time.frameCount <= changedFrame || !run.Advance())
            {
                return;
            }

            RefreshAfterTransition();
        }

        public void Choose(int index)
        {
            if (!IsOpen || Time.frameCount <= changedFrame || !run.Choose(index))
            {
                return;
            }

            RefreshAfterTransition();
        }

        private void RefreshAfterTransition()
        {
            changedFrame = Time.frameCount;
            var transition = run.LastTransition;
            if (run.IsComplete)
            {
                view.Hide();
                motionLease.Dispose();
                motionLease = null;
                waitingForRelease = true;
            }
            else
            {
                PresentLine();
            }

            Transitioned?.Invoke(transition);
        }

        private void PresentLine()
        {
            var line = run.Current;
            view.Present(sequence.speakers[line.speaker], line, string.IsNullOrEmpty(line.nextId));
        }

        public void Cancel()
        {
            if (inputLease == null)
            {
                return;
            }

            bool cancelled = IsOpen;
            run.Cancel();
            var transition = run.LastTransition;
            if (view != null)
            {
                view.Hide();
            }
            motionLease?.Dispose();
            motionLease = null;
            ReleaseContext();
            if (cancelled)
            {
                Transitioned?.Invoke(transition);
            }
        }

        private void ReleaseContext()
        {
            waitingForRelease = false;
            if (gameplayHud != null)
            {
                gameplayHud.enabled = previousHud;
            }
            Cursor.visible = previousCursor;
            if (reticle != null)
            {
                reticle.enabled = previousReticle;
            }

            inputLease.Dispose();
            inputLease = null;
            run = null;
            sequence = null;
        }

        private bool ConfirmationHeld() => input.ConfirmationHeld;
    }
}
