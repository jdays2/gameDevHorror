using EmergencyShift.Core.Time;

namespace EmergencyShift
{
    /// <summary>Creates an owned controller per session from explicit level/player dependencies.</summary>
    public sealed class DialogueControllerFactory
    {
        private readonly GameplayInputBlock inputBlock;
        private readonly WorldMotionPause motionPause;
        private readonly IFrameClock clock;

        public DialogueControllerFactory(GameplayInputBlock inputBlock, WorldMotionPause motionPause, IFrameClock clock)
        {
            this.inputBlock = inputBlock;
            this.motionPause = motionPause;
            this.clock = clock;
        }

        public DialogueController Create() => new DialogueController(inputBlock, motionPause, clock);
    }
}
