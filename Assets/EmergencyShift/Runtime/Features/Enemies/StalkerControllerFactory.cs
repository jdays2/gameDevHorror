using System;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Enemies.Config;
using EmergencyShift.Features.Enemies.Logic;
using UnityEngine;

namespace EmergencyShift.Features.Enemies
{
    public sealed class StalkerControllerFactory
    {
        private readonly IGameClock clock;
        public StalkerControllerFactory(IGameClock clock) => this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        public StalkerController Create(StalkerSettings settings, Vector2 home, IStalkerLocomotion movement, IEnemyBrainMelee melee) =>
            new StalkerController(new StalkerModel(settings, clock, home), movement, melee);
    }
}
