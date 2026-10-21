using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.AudioMood.Logic
{
    public readonly struct AudioMoodCueCommand : ICommand
    {
        private readonly AudioMoodModel model;
        private readonly bool shot;
        private readonly int remainingHealth;

        public AudioMoodCueCommand(AudioMoodModel model, bool shot, int remainingHealth)
        {
            this.model = model;
            this.shot = shot;
            this.remainingHealth = remainingHealth;
        }

        public CommandResult CanExecute() => model != null && (shot || remainingHealth > 0)
            ? CommandResult.Ok() : CommandResult.Fail("Audio cue requires a model and nonlethal damage.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (!result.Success)
            {
                return result;
            }

            if (shot)
            {
                model.ReportShot();
            }
            else
            {
                model.ReportDamage();
            }

            return CommandResult.Ok();
        }
    }
}
