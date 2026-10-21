using System;
using UnityEngine;
using EmergencyShift.Features.PlayerInput.Logic;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Unity input/presentation adapter. Modal lifecycle belongs to its injected controller.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class DialogueSession : MonoBehaviour
    {
        [SerializeField] private DialogueView view;
        [SerializeField] private Canvas gameplayHud;
        [SerializeField] private AimReticle2D reticle;
        private DialogueSequence sequence;
        private DialogueController controller;
        private IPlayerInput input;
        private bool previousHud;
        private bool previousReticle;
        private bool previousCursor;
        private bool presentationCaptured;

        [Inject]
        private void Construct(DialogueControllerFactory factory, IPlayerInput playerInput)
        {
            if (controller != null)
            {
                throw new InvalidOperationException("Dialogue session is already composed.");
            }

            input = playerInput;
            controller = factory.Create();
            controller.ContextAcquired += CapturePresentation;
            controller.ContextReleased += RestorePresentation;
            controller.Transitioned += PresentTransition;
        }

        public bool IsOpen => controller != null && controller.IsOpen;
        public bool CanBegin => isActiveAndEnabled && view != null && gameplayHud != null
            && controller != null && controller.CanBegin;
        public int LineIndex => controller == null ? -1 : controller.LineIndex;
        public string LineId => IsOpen ? controller.Current.Id : null;
        public bool RequiresChoice => IsOpen && controller.RequiresChoice;
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

        private void OnDestroy()
        {
            if (controller == null)
            {
                return;
            }

            controller.Dispose();
            controller.ContextAcquired -= CapturePresentation;
            controller.ContextReleased -= RestorePresentation;
            controller.Transitioned -= PresentTransition;
        }

        public bool TryBegin(DialogueSequence content)
        {
            if (content == null || !CanBegin)
            {
                return false;
            }

            DialogueGraph graph = content.ToGraph();
            sequence = content;
            return controller.TryBegin(graph);
        }

        private void Update()
        {
            if (controller == null)
            {
                return;
            }

            if (input.DialogueAdvancePressed)
            {
                Advance();
            }

            controller.Tick(input.ConfirmationHeld);
        }

        public void Advance() => controller?.Advance();

        public void Choose(int index) => controller?.Choose(index);

        public void Cancel() => controller?.Cancel();

        private void CapturePresentation()
        {
            previousHud = gameplayHud.enabled;
            previousReticle = reticle != null && reticle.enabled;
            previousCursor = Cursor.visible;
            presentationCaptured = true;
            gameplayHud.enabled = false;
            Cursor.visible = true;
            if (reticle != null)
            {
                reticle.enabled = false;
            }
        }

        private void PresentTransition(DialogueStep step)
        {
            if (IsOpen)
            {
                DialogueNode line = controller.Current;
                view.Present(sequence.speakers[line.Speaker], line, string.IsNullOrEmpty(line.NextId));
            }
            else if (view != null)
            {
                view.Hide();
            }

            Transitioned?.Invoke(new DialogueTransition(sequence, step));
        }

        private void RestorePresentation()
        {
            if (!presentationCaptured)
            {
                return;
            }

            presentationCaptured = false;
            if (view != null)
            {
                view.Hide();
            }

            if (gameplayHud != null)
            {
                gameplayHud.enabled = previousHud;
            }

            Cursor.visible = previousCursor;
            if (reticle != null)
            {
                reticle.enabled = previousReticle;
            }
        }
    }
}
