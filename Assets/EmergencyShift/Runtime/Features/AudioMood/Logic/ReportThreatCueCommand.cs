using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.AudioMood.Logic
{
    public readonly struct ReportThreatCueCommand : ICommand
    {
        private readonly AudioThreatSignalModel model;
        private readonly float seconds;
        private readonly float gain;
        private readonly bool alive;
        private readonly bool urgent;

        public ReportThreatCueCommand(AudioThreatSignalModel model, float seconds, float gain, bool alive, bool urgent)
        {
            this.model = model;
            this.seconds = seconds;
            this.gain = gain;
            this.alive = alive;
            this.urgent = urgent;
        }

        public CommandResult CanExecute() => model != null
            ? model.CanReport(seconds, gain, alive) : CommandResult.Fail("Threat signal model is missing.");
        public CommandResult Execute() => model != null
            ? model.Report(seconds, gain, alive, urgent) : CommandResult.Fail("Threat signal model is missing.");
    }
}
