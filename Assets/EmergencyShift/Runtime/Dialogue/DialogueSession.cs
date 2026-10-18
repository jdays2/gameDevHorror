using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
        private IDisposable inputLease;
        private float previousTimeScale;
        private bool previousHud;
        private bool previousReticle;
        private bool previousCursor;
        private bool waitingForRelease;
        private int changedFrame;
        public bool IsOpen => sequence != null;
        public int LineIndex { get; private set; }

        private void OnEnable()
        {
            if (view != null)
            {
                view.AdvanceRequested += Advance;
            }
        }

        private void OnDisable()
        {
            if (view != null)
            {
                view.AdvanceRequested -= Advance;
            }
            Cancel();
        }

        public bool TryBegin(DialogueSequence content)
        {
            if (content == null || view == null || gameplayHud == null || inputLease != null || GameplayInputBlock.IsBlocked)
            {
                return false;
            }

            content.Validate();
            previousTimeScale = Time.timeScale;
            previousHud = gameplayHud.enabled;
            previousReticle = reticle != null && reticle.enabled;
            if (reticle != null)
            {
                reticle.enabled = false;
            }

            previousCursor = Cursor.visible;
            inputLease = GameplayInputBlock.Acquire();
            gameplayHud.enabled = false;
            Cursor.visible = true;
            Time.timeScale = 0;
            sequence = content;
            LineIndex = 0;
            changedFrame = Time.frameCount;
            PresentLine();
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

            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
            {
                Advance();
            }
        }

        public void Advance()
        {
            if (!IsOpen || Time.frameCount <= changedFrame)
            {
                return;
            }

            changedFrame = Time.frameCount;
            if (LineIndex + 1 >= sequence.lines.Length)
            {
                sequence = null;
                view.Hide();
                Time.timeScale = previousTimeScale;
                waitingForRelease = true;
                return;
            }

            LineIndex++;
            PresentLine();
        }

        private void PresentLine()
        {
            var line = sequence.lines[LineIndex];
            view.Present(sequence.speakers[line.speaker], line.text, LineIndex == sequence.lines.Length - 1);
        }

        public void Cancel()
        {
            if (inputLease == null)
            {
                return;
            }

            sequence = null;
            if (view != null)
            {
                view.Hide();
            }
            Time.timeScale = previousTimeScale;
            ReleaseContext();
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
        }

        private static bool ConfirmationHeld()
        {
            var keyboard = Keyboard.current;
            bool keyHeld = keyboard != null && (keyboard.spaceKey.isPressed || keyboard.enterKey.isPressed || keyboard.eKey.isPressed);
            return keyHeld || (Mouse.current != null && Mouse.current.leftButton.isPressed);
        }
    }
}
