using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    public interface IEnemyBrainMelee
    {
        bool CanStart(Vector2 at);
        void Begin();
        Vector2 Step(float progress, Vector2 at, Vector2 heading, bool fixedHeading);
        void Finish();
        void Interrupt(float delay);
    }
}
