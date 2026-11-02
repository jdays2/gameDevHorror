using System;
using EmergencyShift.Features.DeathScreen.Config;
using UnityEngine;

namespace EmergencyShift.Features.DeathScreen.Logic
{
    public sealed class DeathScreenModel
    {
        private DeathScreenPhase returnFromQuit = DeathScreenPhase.Ready;
        public DeathScreenSettings Settings { get; }
        public DeathScreenPhase Phase { get; private set; }
        public float Elapsed { get; private set; }
        public bool HasCheckpoint { get; private set; }
        public string Error { get; private set; }
        public bool IsRecoveryError { get; private set; }
        public bool IsVisible => Phase != DeathScreenPhase.Hidden;
        public bool IsInteractive => Phase == DeathScreenPhase.Ready ||
            Phase == DeathScreenPhase.Error || Phase == DeathScreenPhase.ConfirmQuit;

        public float WorldOpacity
        {
            get
            {
                if (Phase == DeathScreenPhase.Hidden)
                {
                    return 0;
                }
                if (Phase == DeathScreenPhase.Entering)
                {
                    return Ease(Ratio(Elapsed, Settings.WorldFadeSeconds));
                }
                if (Phase == DeathScreenPhase.Returning)
                {
                    return 1 - Ease(Ratio(Elapsed, Settings.ExitFadeSeconds));
                }
                return 1;
            }
        }

        public float ContentOpacity
        {
            get
            {
                if (Phase == DeathScreenPhase.Hidden || Phase == DeathScreenPhase.Returning)
                {
                    return 0;
                }
                if (Phase == DeathScreenPhase.Entering)
                {
                    return Elapsed < Settings.ContentDelaySeconds ? 0 :
                        Ease(Ratio(Elapsed - Settings.ContentDelaySeconds, Settings.ContentFadeSeconds));
                }
                if (Phase == DeathScreenPhase.LeavingRetry || Phase == DeathScreenPhase.LeavingQuit)
                {
                    return 1 - BlackoutOpacity;
                }
                return Phase == DeathScreenPhase.Loading ? 0 : 1;
            }
        }

        public float BlackoutOpacity
        {
            get
            {
                if (Phase == DeathScreenPhase.Loading)
                {
                    return 1;
                }
                if (Phase == DeathScreenPhase.LeavingRetry || Phase == DeathScreenPhase.LeavingQuit)
                {
                    return Ease(Ratio(Elapsed, Settings.ExitFadeSeconds));
                }
                if (Phase == DeathScreenPhase.Returning)
                {
                    return 1 - Ease(Ratio(Elapsed, Settings.ExitFadeSeconds));
                }
                return 0;
            }
        }

        public event Action Changed;

        public DeathScreenModel(DeathScreenSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        internal void Advance(float seconds)
        {
            Elapsed = Mathf.Min(float.MaxValue, Elapsed + seconds);
        }

        internal void Enter(bool checkpoint)
        {
            HasCheckpoint = checkpoint;
            IsRecoveryError = false;
            Error = null;
            SetPhase(DeathScreenPhase.Entering);
        }

        internal void AskQuit()
        {
            returnFromQuit = Phase;
            SetPhase(DeathScreenPhase.ConfirmQuit);
        }

        internal void CancelQuit() => SetPhase(returnFromQuit);

        internal void ShowError(string error, bool checkpoint, bool recovery)
        {
            Error = error;
            HasCheckpoint = checkpoint;
            IsRecoveryError = recovery;
            SetPhase(DeathScreenPhase.Error);
        }

        internal void SetPhase(DeathScreenPhase phase)
        {
            Phase = phase;
            Elapsed = 0;
            Changed?.Invoke();
        }

        internal void Reset()
        {
            Error = null;
            IsRecoveryError = false;
            HasCheckpoint = false;
            SetPhase(DeathScreenPhase.Hidden);
        }

        private static float Ratio(float value, float duration) => duration <= 0 ? 1 : Mathf.Clamp01(value / duration);
        private static float Ease(float value) => value * value * (3 - 2 * value);
    }
}
