using System;

namespace EmergencyShift.Features.Health.Config
{
    /// <summary>Immutable projection of authored health settings; never stores actor state.</summary>
    public sealed class HealthSettings
    {
        public HealthSettings(int maximum, double protectionSeconds)
        {
            if (maximum < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximum));
            }

            if (double.IsNaN(protectionSeconds) || double.IsInfinity(protectionSeconds) || protectionSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(protectionSeconds));
            }

            Maximum = maximum;
            ProtectionSeconds = protectionSeconds;
        }

        public int Maximum { get; }
        public double ProtectionSeconds { get; }
    }
}
