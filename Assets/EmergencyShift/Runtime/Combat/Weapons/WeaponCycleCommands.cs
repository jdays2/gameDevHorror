using System;
using EmergencyShift.Core.Commands;

namespace EmergencyShift
{
    public sealed class FireWeaponCommand : ICommand
    {
        private readonly WeaponCycle cycle;
        private readonly float now;

        public FireWeaponCommand(WeaponCycle cycle, float now)
        {
            this.cycle = cycle;
            this.now = now;
        }

        public CommandResult CanExecute() => cycle != null && cycle.CanFire(now)
            ? CommandResult.Ok() : CommandResult.Fail("The weapon has no ready round or is busy.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (!result.Success)
            {
                return result;
            }

            return cycle.ApplyFire(now) ? result : CommandResult.Fail("The weapon rejected the shot.");
        }
    }

    public sealed class ReloadWeaponCommand : ICommand
    {
        private readonly WeaponCycle cycle;
        private readonly float now;

        public ReloadWeaponCommand(WeaponCycle cycle, float now)
        {
            this.cycle = cycle;
            this.now = now;
        }

        public CommandResult CanExecute() => cycle != null && cycle.CanReload(now)
            ? CommandResult.Ok() : CommandResult.Fail("The weapon cannot reload now.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (!result.Success)
            {
                return result;
            }

            return cycle.ApplyReload(now) ? result : CommandResult.Fail("The weapon rejected the reload.");
        }
    }

    public sealed class CancelWeaponReloadCommand : ICommand
    {
        private readonly WeaponCycle cycle;

        public CancelWeaponReloadCommand(WeaponCycle cycle) => this.cycle = cycle;

        public CommandResult CanExecute() => cycle != null && cycle.Phase == WeaponPhase.Loading
            ? CommandResult.Ok() : CommandResult.Fail("The weapon is not reloading.");

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                cycle.ApplyCancelReload();
            }

            return result;
        }
    }

    public sealed class AddWeaponAmmoCommand : ICommand
    {
        private readonly WeaponCycle cycle;
        private readonly int count;
        public int Received { get; private set; }

        public AddWeaponAmmoCommand(WeaponCycle cycle, int count)
        {
            this.cycle = cycle;
            this.count = count;
        }

        public CommandResult CanExecute() => cycle != null && count > 0 && cycle.TotalAmmo < cycle.Settings.MaximumAmmo
            ? CommandResult.Ok() : CommandResult.Fail("No ammunition can be added.");

        public CommandResult Execute()
        {
            Received = 0;
            CommandResult result = CanExecute();
            if (result.Success)
            {
                Received = cycle.ApplyAddAmmo(count);
            }

            return result;
        }
    }

    public sealed class RestoreWeaponCycleCommand : ICommand
    {
        private readonly WeaponCycle cycle;
        private readonly WeaponCycleSnapshot state;
        private readonly float now;

        public RestoreWeaponCycleCommand(WeaponCycle cycle, WeaponCycleSnapshot state, float now)
        {
            this.cycle = cycle;
            this.state = state;
            this.now = now;
        }

        public CommandResult CanExecute()
        {
            if (cycle == null || !WeaponCycle.ValidNow(now))
            {
                return CommandResult.Fail("Missing weapon or invalid simulation time.");
            }

            try
            {
                cycle.Validate(state);
                return CommandResult.Ok();
            }
            catch (InvalidOperationException exception)
            {
                return CommandResult.Fail(exception.Message);
            }
        }

        public CommandResult Execute()
        {
            CommandResult result = CanExecute();
            if (result.Success)
            {
                cycle.ApplyRestore(state, now);
            }

            return result;
        }
    }
}
