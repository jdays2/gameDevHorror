using System;
using UnityEngine;
using EmergencyShift.Core.Commands;

namespace EmergencyShift
{
    public enum WeaponPhase
    {
        Ready,
        Pumping,
        Loading
    }

    public enum WeaponOperation
    {
        Pump,
        ShellInserted,
        MagazineReloadStarted,
        MagazineLoaded,
        ReloadCancelled
    }

    [Serializable]
    public sealed class WeaponCycleSnapshot
    {
        public int format;
        public bool initialized;
        public int ammo;
        public int loaded;
        public WeaponPhase phase;
        public float cooldown;
        public float operationRemaining;
        public bool cuePending;
        public float cueRemaining;
    }

    /// <summary>Instance-owned ammunition/cooldown, also the ordinary single-projectile weapon cycle.</summary>
    public class WeaponCycle
    {
        protected readonly WeaponSettings definition;
        public WeaponSettings Settings => definition;
        protected float readyAt;
        public int TotalAmmo { get; protected set; }
        public virtual int Loaded => 0;
        public virtual int MagazineCapacity => 0;
        public int Reserve => TotalAmmo - Loaded;
        public virtual WeaponPhase Phase => WeaponPhase.Ready;
        public virtual bool HasReadyRound => TotalAmmo > 0;
        public bool CanDryFire => !HasReadyRound && Phase == WeaponPhase.Ready;
        public virtual bool CanHolster => true;
        public virtual bool EjectsOnShot => true;
        public event Action<WeaponOperation> Operation;

        public WeaponCycle(WeaponSettings data, int cartridges)
        {
            definition = data ?? throw new ArgumentNullException(nameof(data));
            TotalAmmo = Mathf.Clamp(cartridges, 0, definition.MaximumAmmo);
        }

        public virtual bool CanFire(float now)
        {
            return ValidNow(now) && HasReadyRound && now >= readyAt;
        }

        internal virtual bool ApplyFire(float now)
        {
            if (!CanFire(now))
            {
                return false;
            }

            TotalAmmo--;
            readyAt = now + definition.SecondsBetweenShots;
            return true;
        }

        public bool TryFire(float now) => new FireWeaponCommand(this, now).Execute().Success;

        public bool TryReload(float now) => new ReloadWeaponCommand(this, now).Execute().Success;

        public void CancelReload() => new CancelWeaponReloadCommand(this).Execute();

        public int AddAmmo(int count)
        {
            var command = new AddWeaponAmmoCommand(this, count);
            command.Execute();
            return command.Received;
        }

        internal int ApplyAddAmmo(int count)
        {
            int received = Mathf.Clamp(count, 0, definition.MaximumAmmo - TotalAmmo);
            TotalAmmo += received;
            return received;
        }

        public virtual void Tick(float now) { }
        public virtual bool CanReload(float now) => false;
        internal virtual bool ApplyReload(float now) => false;
        internal virtual void ApplyCancelReload() { }

        internal static bool ValidNow(float now) => float.IsFinite(now) && now >= 0;

        protected void Emit(WeaponOperation operation)
        {
            Operation?.Invoke(operation);
        }

        public virtual WeaponCycleSnapshot Capture(float now)
        {
            if (!ValidNow(now))
            {
                throw new ArgumentOutOfRangeException(nameof(now));
            }

            return new WeaponCycleSnapshot
            {
                format = 1,
                initialized = true,
                ammo = TotalAmmo,
                cooldown = Mathf.Max(0, readyAt - now),
                phase = WeaponPhase.Ready
            };
        }

        protected void ValidateCommon(WeaponCycleSnapshot state, int format = 1)
        {
            if (state == null || state.format != format || !state.initialized || state.ammo < 0 ||
                state.ammo > definition.MaximumAmmo || !ValidTime(state.cooldown, definition.SecondsBetweenShots))
            {
                throw new InvalidOperationException("Invalid saved weapon cycle");
            }
        }

        protected static bool ValidTime(float value, float maximum)
        {
            return float.IsFinite(value) && value >= 0 && value <= maximum + .001f;
        }

        public virtual void Validate(WeaponCycleSnapshot state)
        {
            ValidateCommon(state);
            if (state.loaded != 0 || state.phase != WeaponPhase.Ready || state.cuePending ||
                state.operationRemaining != 0 || state.cueRemaining != 0)
            {
                throw new InvalidOperationException("Unexpected operation in ordinary weapon snapshot");
            }
        }

        public void Restore(WeaponCycleSnapshot state, float now)
        {
            CommandResult result = new RestoreWeaponCycleCommand(this, state, now).Execute();
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Reason);
            }
        }

        internal virtual void ApplyRestore(WeaponCycleSnapshot state, float now)
        {
            TotalAmmo = state.ammo;
            readyAt = now + state.cooldown;
        }
    }
}
