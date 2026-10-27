using EmergencyShift.Core.Time;
using EmergencyShift.Features.Enemies.Logic;
using EmergencyShift.Features.Scorpion.Logic;
using UnityEngine;

namespace EmergencyShift.Features.Scorpion
{
    public sealed class ScorpionControllerFactory
    {
        private readonly IGameClock clock;
        public ScorpionControllerFactory(IGameClock clock) => this.clock = clock;
        public ScorpionController Create(EnemyDefinition definition, IEnemyBrainMotor motor, IEnemyBrainMelee melee, Vector2 facing)
        {
            definition.Validate();
            return new ScorpionController(new ScorpionModel(clock, definition.AttackDuration, facing), motor, melee);
        }
    }
}
