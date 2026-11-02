using System;
using UnityEngine;

namespace EmergencyShift.Features.DeathScreen.View
{
    /// <summary>Owns one reversible HUD/reticle/cursor capture, after competing windows close.</summary>
    public sealed class DeathScreenGameplayOverlay : MonoBehaviour
    {
        [SerializeField] private Canvas[] gameplayCanvases = Array.Empty<Canvas>();
        [SerializeField] private AimReticle2D reticle;
        private bool[] previousCanvases;
        private bool previousReticle;
        private bool previousCursor;
        private CursorLockMode previousLock;
        private bool captured;

        public void Capture()
        {
            if (captured)
            {
                return;
            }
            captured = true;
            previousCanvases = new bool[gameplayCanvases.Length];
            for (int index = 0; index < gameplayCanvases.Length; index++)
            {
                Canvas canvas = gameplayCanvases[index];
                previousCanvases[index] = canvas != null && canvas.enabled;
                if (canvas != null)
                {
                    canvas.enabled = false;
                }
            }
            previousReticle = reticle != null && reticle.enabled;
            previousLock = Cursor.lockState;
            if (reticle != null)
            {
                reticle.enabled = false;
            }
            previousCursor = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Restore()
        {
            if (!captured)
            {
                return;
            }
            captured = false;
            for (int index = 0; index < gameplayCanvases.Length; index++)
            {
                if (gameplayCanvases[index] != null)
                {
                    gameplayCanvases[index].enabled = previousCanvases[index];
                }
            }
            if (reticle != null)
            {
                reticle.enabled = previousReticle;
            }
            Cursor.lockState = previousLock;
            Cursor.visible = previousCursor;
        }

        private void OnDisable() => Restore();
    }
}
