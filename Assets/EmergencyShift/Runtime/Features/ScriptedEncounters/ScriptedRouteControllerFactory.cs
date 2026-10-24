using EmergencyShift.Features.Enemies.Logic;
using EmergencyShift.Features.ScriptedEncounters.Config;
using EmergencyShift.Features.ScriptedEncounters.Logic;
using UnityEngine;

namespace EmergencyShift.Features.ScriptedEncounters
{
    public sealed class ScriptedRouteControllerFactory
    {
        public ScriptedRouteController Create(ScriptedRouteSettings settings, Vector2[] points, IEnemyBrainMotor motor, float startDelaySeconds = 0) =>
            new ScriptedRouteController(new ScriptedRouteModel(settings, points, startDelaySeconds), motor);
    }
}
