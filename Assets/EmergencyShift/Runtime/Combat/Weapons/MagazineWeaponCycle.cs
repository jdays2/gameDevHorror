using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Shared loaded/reserve ownership; ordinary reload swaps a magazine atomically.</summary>
    public class MagazineWeaponCycle : WeaponCycle
    {
        private readonly int capacity;
        private readonly float reloadSeconds;
        protected int loaded;
        protected WeaponPhase phase;
        protected float operationEnds;
        protected virtual int SnapshotFormat => 2;
        public override int Loaded => loaded;
        public override int MagazineCapacity => capacity;
        public override WeaponPhase Phase => phase;
        public override bool HasReadyRound => loaded > 0;

        public MagazineWeaponCycle(WeaponDefinition data, int cartridges, int magazineCapacity, float reloadDuration)
            : base(data, cartridges)
        {
            capacity = magazineCapacity;
            reloadSeconds = reloadDuration;
            loaded = Mathf.Min(TotalAmmo, capacity);
        }

        public override bool CanFire(float now)
        {
            return phase == WeaponPhase.Ready && base.CanFire(now);
        }

        public override bool TryFire(float now)
        {
            if (!base.TryFire(now))
            {
                return false;
            }

            loaded--;
            return true;
        }

        public override bool TryReload(float now)
        {
            if (phase != WeaponPhase.Ready || loaded >= capacity || Reserve <= 0)
            {
                return false;
            }

            phase = WeaponPhase.Loading;
            operationEnds = now + reloadSeconds;
            Emit(WeaponOperation.MagazineReloadStarted);
            return true;
        }

        public override void CancelReload()
        {
            if (phase == WeaponPhase.Loading)
            {
                phase = WeaponPhase.Ready;
                operationEnds = 0;
                Emit(WeaponOperation.ReloadCancelled);
            }
        }

        public override void Tick(float now)
        {
            if (phase != WeaponPhase.Loading || now < operationEnds)
            {
                return;
            }

            loaded += Mathf.Min(capacity - loaded, Reserve);
            phase = WeaponPhase.Ready;
            operationEnds = 0;
            Emit(WeaponOperation.MagazineLoaded);
        }

        public override WeaponCycleSnapshot Capture(float now)
        {
            var state = base.Capture(now);
            state.format = SnapshotFormat;
            state.loaded = loaded;
            state.phase = phase;
            state.operationRemaining = phase == WeaponPhase.Ready ? 0 : Mathf.Max(0, operationEnds - now);
            return state;
        }

        public override void Validate(WeaponCycleSnapshot state)
        {
            if (state != null && state.format == 1 && SnapshotFormat == 2)
            {
                // Old pistol snapshots held total ammo only. Reconstruct one loaded magazine.
                base.Validate(state);
                return;
            }

            ValidateCommon(state, SnapshotFormat);
            if (state.loaded < 0 || state.loaded > capacity || state.loaded > state.ammo ||
                (state.phase != WeaponPhase.Ready && state.phase != WeaponPhase.Loading) ||
                !ValidTime(state.operationRemaining, reloadSeconds) || state.cuePending || state.cueRemaining != 0 ||
                (state.phase == WeaponPhase.Ready && state.operationRemaining != 0) ||
                (state.phase == WeaponPhase.Loading && (state.loaded >= capacity || state.loaded >= state.ammo)))
            {
                throw new InvalidOperationException("Invalid saved magazine weapon state");
            }
        }

        public override void Restore(WeaponCycleSnapshot state, float now)
        {
            base.Restore(state, now);
            bool legacy = state.format != SnapshotFormat;
            loaded = legacy ? Mathf.Min(TotalAmmo, capacity) : state.loaded;
            phase = legacy ? WeaponPhase.Ready : state.phase;
            operationEnds = now + state.operationRemaining;
        }
    }
}
