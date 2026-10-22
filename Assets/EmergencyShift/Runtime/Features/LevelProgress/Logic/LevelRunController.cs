using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.LevelProgress.Logic
{
    public sealed class LevelRunController
    {
        private readonly LevelRunModel model;
        private readonly ILevelRunEnvironment environment;
        public bool Completed => model.Completed;
        public float ElapsedSeconds => model.ElapsedSeconds;
        public event Action Finished;

        public LevelRunController(LevelRunModel model, ILevelRunEnvironment environment)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public CommandResult Start() => new StartLevelRunCommand(model).Execute();
        public CommandResult RestoreElapsed(float seconds) => new RestoreRunElapsedCommand(model, seconds).Execute();
        public CommandResult Restart() => new RestartLevelRunCommand(model, environment).Execute();

        public CommandResult TryFinish(bool ownsActor, bool alive, bool hasExitAccess)
        {
            CommandResult result = new FinishLevelRunCommand(model, ownsActor, alive, hasExitAccess).Execute();
            if (result.Success)
            {
                environment.StopParticipants();
                Finished?.Invoke();
            }

            return result;
        }
    }
}
