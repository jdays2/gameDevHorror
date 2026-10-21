using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Automatic pump and interruptible single-shell loading; no input, visuals or audio ownership.</summary>
    public sealed class PumpShotgunCycle : MagazineWeaponCycle
    {
        private readonly PumpShotgunSettings profile;
        private float pumpCueAt;
        private bool cuePending;
        protected override int SnapshotFormat => 1;
        public override bool CanHolster => phase != WeaponPhase.Pumping;
        public override bool EjectsOnShot => false;

        public PumpShotgunCycle(PumpShotgunSettings data, int cartridges)
            : base(data, cartridges)
        {
            profile = data;
        }

        public override bool CanFire(float now)
        {
            return ValidNow(now) && phase != WeaponPhase.Pumping && HasReadyRound && now >= readyAt;
        }

        internal override bool ApplyFire(float now)
        {
            if (!CanFire(now))
            {
                return false;
            }

            CancelReload();
            if (!base.ApplyFire(now))
            {
                return false;
            }

            phase = WeaponPhase.Pumping;
            operationEnds = now + profile.PumpSeconds;
            pumpCueAt = now + profile.PumpCueDelay;
            cuePending = true;
            return true;
        }

        public override bool CanReload(float now)
        {
            return ValidNow(now) && phase == WeaponPhase.Ready && loaded < MagazineCapacity && Reserve > 0;
        }

        internal override bool ApplyReload(float now)
        {
            if (!CanReload(now))
            {
                return false;
            }

            phase = WeaponPhase.Loading;
            operationEnds = now + profile.ReloadSeconds;
            return true;
        }

        public override void Tick(float now)
        {
            if (!ValidNow(now))
            {
                return;
            }

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
                    operationEnds += profile.ReloadSeconds;
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
            float operationLimit = state.phase == WeaponPhase.Pumping ? profile.PumpSeconds : profile.ReloadSeconds;
            if (state.loaded < 0 || state.loaded > MagazineCapacity || state.loaded > state.ammo ||
                !Enum.IsDefined(typeof(WeaponPhase), state.phase) ||
                !ValidTime(state.operationRemaining, operationLimit) ||
                !ValidTime(state.cueRemaining, profile.PumpCueDelay) ||
                (state.phase == WeaponPhase.Ready && state.operationRemaining != 0) ||
                (state.phase != WeaponPhase.Pumping && (state.cuePending || state.cueRemaining != 0)) ||
                (!state.cuePending && state.cueRemaining != 0) ||
                (state.cuePending && state.cueRemaining > state.operationRemaining + .001f) ||
                (state.phase == WeaponPhase.Pumping && state.loaded >= MagazineCapacity) ||
                (state.phase == WeaponPhase.Loading && (state.loaded >= MagazineCapacity || state.loaded >= state.ammo)))
            {
                throw new InvalidOperationException("Invalid saved pump shotgun state");
            }
        }

        internal override void ApplyRestore(WeaponCycleSnapshot state, float now)
        {
            base.ApplyRestore(state, now);
            cuePending = state.cuePending;
            pumpCueAt = now + state.cueRemaining;
        }
    }
}
