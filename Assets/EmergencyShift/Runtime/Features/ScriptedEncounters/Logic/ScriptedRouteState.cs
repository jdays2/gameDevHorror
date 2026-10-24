namespace EmergencyShift.Features.ScriptedEncounters.Logic
{
    public enum ScriptedRouteState
    {
        WaitingForTrigger,
        Reacting,
        Moving,
        Blocked,
        Completed,
        Aborted,
        WaitingToStart
    }
}
