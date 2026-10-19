namespace EmergencyShift.Core.Commands
{
    public interface ICommand
    {
        CommandResult CanExecute();
        CommandResult Execute();
    }
}
