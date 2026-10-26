using EmergencyShift.Core.Time;
using EmergencyShift.Features.ClawEnemy.Config;
using EmergencyShift.Features.ClawEnemy.Logic;
using EmergencyShift.Features.Enemies.Logic;

namespace EmergencyShift.Features.ClawEnemy
{
    public sealed class ClawEnemyControllerFactory
    {
        private readonly IGameClock clock;

        public ClawEnemyControllerFactory(IGameClock clock) => this.clock = clock;

        public ClawEnemyController Create(EnemyDefinition definition, IClawLocomotion movement, IEnemyBrainMelee melee)
        {
            definition.Validate();
            var settings = new ClawEnemySettings(definition.PursuitSpeed, definition.AttackDuration,
                definition.HitStaggerSeconds, definition.HitPushSpeed);
            return new ClawEnemyController(new ClawEnemyModel(settings, clock), movement, melee);
        }
    }
}
