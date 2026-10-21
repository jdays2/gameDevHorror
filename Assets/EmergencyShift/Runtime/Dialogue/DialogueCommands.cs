using EmergencyShift.Core.Commands;

namespace EmergencyShift
{
    public sealed class AdvanceDialogueCommand : ICommand
    {
        private readonly DialogueRun run;

        public AdvanceDialogueCommand(DialogueRun run) => this.run = run;

        public CommandResult CanExecute()
        {
            if (run == null || run.IsComplete || run.RequiresChoice)
            {
                return CommandResult.Fail("The dialogue has ended or requires a choice.");
            }

            return CommandResult.Ok();
        }

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                run.ApplyAdvance();
            }

            return result;
        }
    }

    public sealed class ChooseDialogueCommand : ICommand
    {
        private readonly DialogueRun run;
        private readonly int index;

        public ChooseDialogueCommand(DialogueRun run, int index)
        {
            this.run = run;
            this.index = index;
        }

        public CommandResult CanExecute()
        {
            if (run == null || !run.RequiresChoice || index < 0 || index >= run.Current.Choices.Count)
            {
                return CommandResult.Fail("The requested dialogue choice is unavailable.");
            }

            return CommandResult.Ok();
        }

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                run.ApplyChoice(index);
            }

            return result;
        }
    }

    public sealed class CancelDialogueCommand : ICommand
    {
        private readonly DialogueRun run;

        public CancelDialogueCommand(DialogueRun run) => this.run = run;

        public CommandResult CanExecute()
        {
            return run != null && !run.IsComplete
                ? CommandResult.Ok()
                : CommandResult.Fail("The dialogue has already ended.");
        }

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                run.ApplyCancel();
            }

            return result;
        }
    }

    public sealed class BeginDialogueCommand : ICommand
    {
        private readonly DialogueController controller;
        private readonly DialogueGraph graph;

        public BeginDialogueCommand(DialogueController controller, DialogueGraph graph)
        {
            this.controller = controller;
            this.graph = graph;
        }

        public CommandResult CanExecute()
        {
            return controller != null && controller.CanBegin && graph != null
                ? CommandResult.Ok()
                : CommandResult.Fail("The dialogue context is unavailable.");
        }

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                controller.ApplyBegin(graph);
            }

            return result;
        }
    }
}
