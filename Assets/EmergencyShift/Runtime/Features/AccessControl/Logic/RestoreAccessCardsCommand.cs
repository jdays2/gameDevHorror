using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.AccessControl.Logic
{
    public sealed class RestoreAccessCardsCommand : ICommand
    {
        private readonly AccessCardModel model;
        private readonly string[] values;

        public RestoreAccessCardsCommand(AccessCardModel model, string[] values)
        {
            this.model = model;
            this.values = values == null ? null : (string[])values.Clone();
        }

        public CommandResult CanExecute() => model == null
            ? CommandResult.Fail("Missing card inventory.") : AccessCardModel.ValidateSnapshot(values);

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Replace(values);
            }

            return result;
        }
    }
}
