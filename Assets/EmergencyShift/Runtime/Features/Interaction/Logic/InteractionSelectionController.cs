using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.Interaction.Logic
{
    public sealed class InteractionSelectionController
    {
        private readonly IInteractionEnvironment environment;

        public InteractionSelectionController(InteractionSelectionModel model, IInteractionEnvironment environment)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public InteractionSelectionModel Model { get; }

        public void Refresh()
        {
            Model.Reset();
            if (environment.CanAct)
            {
                int count = environment.ScanCandidates();
                for (int i = 0; i < count; i++)
                {
                    Model.Consider(environment.Observe(i));
                }
            }

            environment.PresentTarget(Model.CurrentIndex);
        }

        public CommandResult TryInteract()
        {
            Refresh();
            CommandResult result = new InteractSelectionCommand(Model, environment).Execute();
            Refresh();
            return result;
        }

        public void Clear()
        {
            Model.Reset();
            environment.PresentTarget(-1);
        }
    }
}
