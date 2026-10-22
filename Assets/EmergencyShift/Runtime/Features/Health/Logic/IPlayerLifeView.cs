using UnityEngine;

namespace EmergencyShift.Features.Health.Logic
{
    public interface IPlayerLifeView
    {
        void PresentDamage(int amount, Vector2 direction, bool defeated);
        void PresentHealth(int current, bool restored);
    }
}
