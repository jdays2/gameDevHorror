using System;

namespace EmergencyShift.Features.AccessControl.Config
{
    public sealed class DoorAccessSettings
    {
        public DoorAccessSettings(string requiredCardId, double denialCooldown, double noticeCooldown)
        {
            if (string.IsNullOrWhiteSpace(requiredCardId) || !double.IsFinite(denialCooldown) ||
                !double.IsFinite(noticeCooldown) || denialCooldown < 0 || noticeCooldown < 0)
            {
                throw new ArgumentException("Access requires a card ID and finite nonnegative cooldowns.");
            }

            RequiredCardId = requiredCardId;
            DenialCooldown = denialCooldown;
            NoticeCooldown = noticeCooldown;
        }

        public string RequiredCardId { get; }
        public double DenialCooldown { get; }
        public double NoticeCooldown { get; }
    }
}
