using EmergencyShift.Core.Time;
using EmergencyShift.Features.Enemies.Logic;
using EmergencyShift.Features.Scorpion.Config;
using EmergencyShift.Features.Scorpion.Logic;
using UnityEngine;

namespace EmergencyShift.Features.Scorpion
{
    public sealed class ScorpionControllerFactory
    {
        private readonly IGameClock clock;
        public ScorpionControllerFactory(IGameClock clock) => this.clock = clock;
        public ScorpionController Create(EnemyDefinition definition, ScorpionPresentationSet presentation, IEnemyBrainMotor motor, IEnemyBrainMelee melee, Vector2 facing)
        {
            definition.Validate();
            presentation.Validate();
            var settings = new ScorpionSettings(definition.PursuitSpeed, presentation.Attack.ReferenceSpeed,
                definition.AttackDuration, presentation.DistancePerCycle, presentation.TurnDegrees,
                presentation.Acceleration, presentation.Braking);
            return new ScorpionController(new ScorpionModel(clock, settings, facing), motor, melee);
        }
    }
}
