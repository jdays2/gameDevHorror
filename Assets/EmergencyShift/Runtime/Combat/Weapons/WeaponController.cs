using System;
using EmergencyShift.Core.Time;

namespace EmergencyShift
{
    /// <summary>Delegates input intent to model commands and synchronous presentation; owns subscriptions.</summary>
    public sealed class WeaponController : IDisposable
    {
        private readonly WeaponOwnerModel model;
        private readonly IGameClock clock;
        private readonly IWeaponUseGate gate;
        private readonly IWeaponShotView view;
        private WeaponCycle boundCycle;
        private float nextEmptyTrigger;
        private bool disposed;
        private float Now => (float)clock.Time;
        public WeaponInventory Inventory => model.Inventory;
        public int ShotsFired => model.ShotsFired;
        public WeaponShotPlan LastShot => model.LastShot;
        public event Action SelectionChanged;
        public event Action<WeaponOperation> Operation;

        public WeaponController(WeaponOwnerModel model, IGameClock clock, IWeaponUseGate gate, IWeaponShotView view)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.gate = gate ?? throw new ArgumentNullException(nameof(gate));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public bool Update(WeaponInputIntent intent)
        {
            if (disposed || gate.InputBlocked)
            {
                return false;
            }

            Inventory.ActiveCycle?.Tick(Now);
            ReloadEmptyWeapon();
            if (intent.PrimarySlot)
            {
                Equip(0);
            }

            if (intent.SecondarySlot)
            {
                Equip(1);
            }

            if (intent.Reload)
            {
                Reload();
            }

            return Inventory.ActiveDefinition != null && Inventory.ActiveDefinition.Automatic
                ? intent.FireHeld : intent.FirePressed;
        }

        public bool Acquire(WeaponSettings definition, int cartridges)
        {
            if (!CanUse || !Inventory.Acquire(definition, cartridges))
            {
                return false;
            }

            BindCycle();
            return true;
        }

        public bool Equip(int slot)
        {
            if (!CanUse || !Inventory.Equip(slot))
            {
                return false;
            }

            BindCycle();
            SelectionChanged?.Invoke();
            return true;
        }

        public int AddAmmo(int cartridges, WeaponSettings target = null) => CanUse ? Inventory.AddAmmo(cartridges, target) : 0;

        public bool Reload() => CanUse && Inventory.ActiveCycle != null && Inventory.ActiveCycle.TryReload(Now);

        public bool CheckFireIntent()
        {
            WeaponCycle cycle = Inventory.ActiveCycle;
            if (!CanUse || cycle == null)
            {
                return false;
            }

            float now = Now;
            if (cycle.CanDryFire)
            {
                if (WeaponCycle.ValidNow(now) && now >= nextEmptyTrigger)
                {
                    nextEmptyTrigger = now + .25f;
                    view.PresentEmptyTrigger();
                }

                return false;
            }

            return cycle.CanFire(now);
        }

        public bool TryFire(WeaponShotRequest request)
        {
            if (!CheckFireIntent())
            {
                return false;
            }

            var command = new FireOwnedWeaponCommand(model, Now, request);
            if (!command.Execute().Success)
            {
                return false;
            }

            view.PresentShot(command.Plan);
            ReloadEmptyWeapon();
            return true;
        }

        public void ReplaceDefinition(WeaponSettings definition)
        {
            ThrowIfDisposed();
            Inventory.ReplaceActiveDefinition(definition);
            BindCycle();
        }

        public WeaponInventorySnapshot Capture() => Inventory.Capture(Now);

        public void Restore(WeaponInventorySnapshot state, WeaponSettingsCatalog catalog)
        {
            ThrowIfDisposed();
            WeaponInventory.Validate(state, catalog);
            if (!WeaponCycle.ValidNow(Now))
            {
                throw new InvalidOperationException("Invalid weapon simulation time.");
            }

            UnbindCycle();
            Inventory.Restore(state, catalog, Now);
            BindCycle();
        }

        public void RestoreLegacy(bool owned, int cartridges, WeaponSettings definition)
        {
            ThrowIfDisposed();
            UnbindCycle();
            Inventory.RestoreLegacy(owned, cartridges, definition);
            nextEmptyTrigger = 0;
            BindCycle();
        }

        public void CancelReload() => Inventory.ActiveCycle?.CancelReload();

        private bool CanUse => !disposed && !gate.InputBlocked && gate.CanUseWeapon;

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WeaponController));
            }
        }

        private void ReloadEmptyWeapon()
        {
            WeaponCycle cycle = Inventory.ActiveCycle;
            if (CanUse && cycle != null && cycle.MagazineCapacity > 0 && cycle.Loaded == 0 && cycle.Reserve > 0)
            {
                float now = Now;
                if (cycle.CanReload(now))
                {
                    cycle.TryReload(now);
                }
            }
        }

        private void BindCycle()
        {
            UnbindCycle();
            boundCycle = Inventory.ActiveCycle;
            if (boundCycle != null)
            {
                boundCycle.Operation += OnOperation;
            }
        }

        private void OnOperation(WeaponOperation operation) => Operation?.Invoke(operation);

        private void UnbindCycle()
        {
            if (boundCycle != null)
            {
                WeaponCycle previous = boundCycle;
                boundCycle = null;
                try
                {
                    previous.CancelReload();
                }
                finally
                {
                    previous.Operation -= OnOperation;
                }
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            UnbindCycle();
        }
    }
}
