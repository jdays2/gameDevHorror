using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift.Features.AudioMood.Logic
{
    /// <summary>Sensor command boundary; exposes values to perception, not graphical presentation.</summary>
    public sealed class AudioThreatSignalController
    {
        private readonly AudioThreatSignalModel model;
        public float HeardUntil => model.HeardUntil;
        public float UrgentUntil => model.UrgentUntil;
        public AudioThreatSignalController(AudioThreatSignalModel model) =>
            this.model = model ?? throw new ArgumentNullException(nameof(model));
        public CommandResult Report(float seconds, float gain, bool alive, bool urgent) =>
            new ReportThreatCueCommand(model, seconds, gain, alive, urgent).Execute();
        public CommandResult Reset() => new ResetThreatSignalCommand(model).Execute();
    }
}
