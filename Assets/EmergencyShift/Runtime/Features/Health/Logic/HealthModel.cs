using System;
using EmergencyShift.Core.Commands;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Health.Config;

namespace EmergencyShift.Features.Health.Logic
{
    /// <summary>Instance health and protection, independent of Unity and presentation.</summary>
    public sealed class HealthModel
    {
        private readonly HealthSettings settings;
        private readonly IGameClock clock;
        private double protectedUntil;

        public HealthModel(HealthSettings settings, IGameClock clock)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Maximum = settings.Maximum;
            Current = Maximum;
        }

        public event Action Damaged;
        public event Action Died;

        public int Current { get; private set; }
        public int Maximum { get; private set; }
        public bool IsAlive => Current > 0;

        public CommandResult CanTakeDamage(int amount)
        {
            if (!IsAlive)
            {
                return CommandResult.Fail("Target is dead.");
            }

            if (amount <= 0)
            {
                return CommandResult.Fail("Damage must be positive.");
            }

            if (clock.Time < protectedUntil)
            {
                return CommandResult.Fail("Target is protected.");
            }

            return CommandResult.Ok();
        }

        internal void ApplyDamage(int amount)
        {
            Current = Math.Max(0, Current - amount);
            protectedUntil = clock.Time + settings.ProtectionSeconds;
            if (Current == 0)
            {
                Died?.Invoke();
            }
            else
            {
                Damaged?.Invoke();
            }
        }

        /// <summary>Trusted persistence restoration is silent and never replays a death.</summary>
        public void Restore(int current)
        {
            Current = Math.Max(0, Math.Min(current, Maximum));
            protectedUntil = 0;
        }

        /// <summary>Temporary scene rules can change instance capacity without changing a config.</summary>
        internal void SetCapacity(int maximum, int current)
        {
            Maximum = Math.Max(1, maximum);
            Current = Math.Max(1, Math.Min(current, Maximum));
        }
    }
}
