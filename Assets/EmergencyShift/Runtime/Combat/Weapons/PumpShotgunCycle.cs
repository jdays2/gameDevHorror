using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Automatic pump and interruptible single-shell loading; no input, visuals or audio ownership.</summary>
    public sealed class PumpShotgunCycle : MagazineWeaponCycle
    {
        private readonly PumpShotgunDefinition profile;
        private float pumpCueAt;
        private bool cuePending;
        protected override int SnapshotFormat => 1;
        public override bool CanHolster => phase != WeaponPhase.Pumping;
        public override bool EjectsOnShot => false;

        public PumpShotgunCycle(PumpShotgunDefinition data, int cartridges)
            : base(data, cartridges, data.magazineCapacity, data.shellInsertSeconds)
        {
            profile = data;
        }

        public override bool CanFire(float now)
        {
            return phase != WeaponPhase.Pumping && HasReadyRound && now >= readyAt;
        }

        public override bool TryFire(float now)
        {
            if (!CanFire(now))
            {
                return false;
            }

            CancelReload();
            if (!base.TryFire(now))
            {
                return false;
            }

            phase = WeaponPhase.Pumping;
            operationEnds = now + profile.pumpSeconds;
            pumpCueAt = now + profile.pumpCueDelay;
            cuePending = true;
            return true;
        }

        public override bool TryReload(float now)
        {
            if (phase != WeaponPhase.Ready || loaded >= MagazineCapacity || Reserve <= 0)
            {
                return false;
            }

            phase = WeaponPhase.Loading;
            operationEnds = now + profile.shellInsertSeconds;
            return true;
        }

        public override void Tick(float now)
        {
            if (phase == WeaponPhase.Pumping)
            {
                if (cuePending && now >= pumpCueAt)
                {
                    cuePending = false;
                    Emit(WeaponOperation.Pump);
                }

                if (now >= operationEnds)
                {
                    phase = WeaponPhase.Ready;
                }

                return;
            }

            while (phase == WeaponPhase.Loading && now >= operationEnds)
            {
                loaded++;
                Emit(WeaponOperation.ShellInserted);
                if (loaded >= MagazineCapacity || Reserve <= 0)
                {
                    phase = WeaponPhase.Ready;
                }
                else
                {
                    operationEnds += profile.shellInsertSeconds;
                }
            }
        }

        public override WeaponCycleSnapshot Capture(float now)
        {
            var state = base.Capture(now);
            state.cuePending = cuePending && phase == WeaponPhase.Pumping;
            state.cueRemaining = state.cuePending ? Mathf.Max(0, pumpCueAt - now) : 0;
            return state;
        }

        public override void Validate(WeaponCycleSnapshot state)
        {
            ValidateCommon(state);
            if (state.loaded < 0 || state.loaded > MagazineCapacity || state.loaded > state.ammo ||
                !Enum.IsDefined(typeof(WeaponPhase), state.phase) ||
                !ValidTime(state.operationRemaining, Mathf.Max(profile.pumpSeconds, profile.shellInsertSeconds)) ||
                !ValidTime(state.cueRemaining, profile.pumpCueDelay) ||
                (state.phase == WeaponPhase.Ready && state.operationRemaining != 0) ||
                (state.phase != WeaponPhase.Pumping && (state.cuePending || state.cueRemaining != 0)) ||
                (state.phase == WeaponPhase.Loading && (state.loaded >= MagazineCapacity || state.loaded >= state.ammo)))
            {
                throw new InvalidOperationException("Invalid saved pump shotgun state");
            }
        }

        public override void Restore(WeaponCycleSnapshot state, float now)
        {
            base.Restore(state, now);
            cuePending = state.cuePending;
            pumpCueAt = now + state.cueRemaining;
        }
    }
}
