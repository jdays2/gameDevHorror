using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.AccessControl.Logic
{
    public sealed class AddAccessCardCommand : ICommand
    {
        private readonly AccessCardModel model;
        private readonly string id;

        public AddAccessCardCommand(AccessCardModel model, string id)
        {
            this.model = model;
            this.id = id;
        }

        public CommandResult CanExecute() => model?.CanAdd(id) ?? CommandResult.Fail("Missing card inventory.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                model.Add(id);
            }

            return result;
        }
    }
}
