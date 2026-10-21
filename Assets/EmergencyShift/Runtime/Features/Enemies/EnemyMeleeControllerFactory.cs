using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;

namespace EmergencyShift.Features.Enemies
{
    public sealed class EnemyMeleeControllerFactory
    {
        private readonly IGameClock clock;

        public EnemyMeleeControllerFactory(IGameClock clock) => this.clock = clock ?? throw new ArgumentNullException(nameof(clock));

        public EnemyMeleeController Create(EnemyMeleeSettings settings, IEnemyMeleeEnvironment environment) =>
            new EnemyMeleeController(new EnemyMeleeModel(settings, clock), environment);
    }
}
