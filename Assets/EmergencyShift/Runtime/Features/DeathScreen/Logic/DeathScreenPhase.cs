namespace EmergencyShift.Features.DeathScreen.Logic
{
    public enum DeathScreenPhase
    {
        Hidden,
        Entering,
        Ready,
        ConfirmQuit,
        WaitingRetry,
        WaitingQuit,
        LeavingRetry,
        LeavingQuit,
        Loading,
        Returning,
        Error
    }
}
