using System;
using EmergencyShift.Core.Randomness;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;

namespace EmergencyShift.Features.Enemies
{
    public sealed class EnemyPatrolControllerFactory
    {
        private readonly RandomSourceFactory randoms;
        public EnemyPatrolControllerFactory(RandomSourceFactory randoms) => this.randoms = randoms ?? throw new ArgumentNullException(nameof(randoms));
        public EnemyPatrolController Create(EnemyPatrolSettings settings, IEnemyPatrolEnvironment environment) =>
            new EnemyPatrolController(new EnemyPatrolModel(settings, randoms.Create(), environment));
    }
}
