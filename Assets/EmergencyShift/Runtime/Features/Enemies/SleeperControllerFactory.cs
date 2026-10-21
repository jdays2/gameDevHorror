using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;

namespace EmergencyShift.Features.Enemies
{
    public sealed class SleeperControllerFactory
    {
        private readonly IGameClock clock;
        public SleeperControllerFactory(IGameClock clock) => this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        public SleeperController Create(SleeperSettings settings, IEnemyBrainMotor motor, IEnemyBrainMelee melee) =>
            new SleeperController(new SleeperModel(settings, clock), motor, melee);
    }
}
