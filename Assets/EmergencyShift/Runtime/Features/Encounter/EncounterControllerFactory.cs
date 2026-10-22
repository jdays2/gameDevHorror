using EmergencyShift.Features.Encounter.Logic;

namespace EmergencyShift.Features.Encounter
{
    public sealed class EncounterControllerFactory
    {
        public EncounterController Create(IEncounterView view, bool activated = false) =>
            new EncounterController(new EncounterModel(activated), view);
    }
}
