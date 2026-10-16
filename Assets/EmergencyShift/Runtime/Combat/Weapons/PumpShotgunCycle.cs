using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Automatic pump and interruptible single-shell loading; no input, visuals or audio ownership.</summary>
    public sealed class PumpShotgunCycle : WeaponCycle
    {
        private readonly PumpShotgunDefinition profile;
        private int loaded;
        private WeaponPhase phase;
        private float operationEnds;
        private float pumpCueAt;
        private bool cuePending;
        public override int Loaded => loaded;
        public override int MagazineCapacity => profile.magazineCapacity;
        public override WeaponPhase Phase => phase;
        public override bool HasReadyRound => loaded > 0;
        public override bool CanHolster => phase != WeaponPhase.Pumping;
        public override bool EjectsOnShot => false;

        public PumpShotgunCycle(PumpShotgunDefinition data, int cartridges) : base(data, cartridges)
        {
            profile = data;
            loaded = Mathf.Min(TotalAmmo, MagazineCapacity);
        }

        public override bool CanFire(float now)
        {
            return phase != WeaponPhase.Pumping && base.CanFire(now);
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

            loaded--;
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

        public override void CancelReload()
        {
            if (phase == WeaponPhase.Loading)
            {
                phase = WeaponPhase.Ready;
                operationEnds = 0;
            }
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
            state.loaded = loaded;
            state.phase = phase;
            state.operationRemaining = phase == WeaponPhase.Ready ? 0 : Mathf.Max(0, operationEnds - now);
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
            loaded = state.loaded;
            phase = state.phase;
            operationEnds = now + state.operationRemaining;
            cuePending = state.cuePending;
            pumpCueAt = now + state.cueRemaining;
        }
    }
}
