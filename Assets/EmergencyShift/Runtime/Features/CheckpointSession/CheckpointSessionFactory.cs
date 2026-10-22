using EmergencyShift.Features.CheckpointSession.Logic;

namespace EmergencyShift.Features.CheckpointSession
{
    public sealed class CheckpointSessionFactory
    {
        public CheckpointSessionController Create() => new CheckpointSessionController(new CheckpointSessionModel());
    }
}
