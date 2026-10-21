using System;
using EmergencyShift.Features.Enemies.Logic;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Compatibility API for the motor; the personal solver owns cached route state.</summary>
    internal sealed class EnemyChasePath2D
    {
        private readonly EnemyChasePathModel model;
        public EnemyChasePath2D(EnemyChasePathModel model) => this.model = model ?? throw new ArgumentNullException(nameof(model));
        public int WaypointCount => model.WaypointCount;
        public Vector2 Direction(Vector2 position, Vector2 goal, float radius, LayerMask solids) => model.Direction(position, goal, radius, solids.value);
    }
}
