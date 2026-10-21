using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.AccessControl.Logic
{
    public sealed class AccessCardController
    {
        private readonly AccessCardModel model;
        private readonly IAccessCardView view;

        public AccessCardController(AccessCardModel model, IAccessCardView view)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public int Count => model.Count;
        public bool Has(string id) => model.Has(id);
        public string[] Capture() => model.Capture();

        public CommandResult Add(string id) => Execute(new AddAccessCardCommand(model, id));

        public CommandResult Restore(string[] values) => Execute(new RestoreAccessCardsCommand(model, values));

        private CommandResult Execute(ICommand command)
        {
            CommandResult result = command.Execute();
            if (result.Success)
            {
                view.CardsChanged();
            }

            return result;
        }
    }
}
