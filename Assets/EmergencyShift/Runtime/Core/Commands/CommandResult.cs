namespace EmergencyShift.Core.Commands
{
    public readonly struct CommandResult
    {
        private CommandResult(bool success, string reason)
        {
            Success = success;
            Reason = reason;
        }

        public bool Success { get; }
        public string Reason { get; }

        public static CommandResult Ok() => new CommandResult(true, null);
        public static CommandResult Fail(string reason) => new CommandResult(false, reason);
    }
}
