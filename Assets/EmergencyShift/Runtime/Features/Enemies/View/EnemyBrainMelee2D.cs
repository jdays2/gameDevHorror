using System;
using EmergencyShift.Features.Enemies.Logic;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.View
{
    public sealed class EnemyBrainMelee2D : IEnemyBrainMelee
    {
        private readonly EnemyMeleeAttack2D melee;
        public EnemyBrainMelee2D(EnemyMeleeAttack2D melee) => this.melee = melee != null ? melee : throw new ArgumentNullException(nameof(melee));
        public bool CanStart(Vector2 at) => melee.CanStart(at);
        public void Begin() => melee.Begin();
        public Vector2 Step(float progress, Vector2 at, Vector2 heading, bool fixedHeading) => melee.Step(progress, at, heading, fixedHeading);
        public void Finish() => melee.Finish();
        public void Interrupt(float delay) => melee.Interrupt(delay);
    }
}
