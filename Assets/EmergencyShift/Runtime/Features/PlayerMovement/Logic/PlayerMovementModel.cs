using System;
using EmergencyShift.Features.PlayerMovement.Config;
using UnityEngine;

namespace EmergencyShift.Features.PlayerMovement.Logic
{
    public sealed class PlayerMovementModel
    {
        private readonly PlayerMovementSettings settings;
        public PlayerMovementModel(PlayerMovementSettings settings) => this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        public Vector2 Direction { get; private set; }
        public Vector2 Velocity => Direction * settings.Speed;
        internal void SetIntent(Vector2 intent) => Direction = intent.normalized;
        internal static bool ValidIntent(Vector2 intent) => float.IsFinite(intent.x) && float.IsFinite(intent.y) && float.IsFinite(intent.sqrMagnitude);
    }
}
