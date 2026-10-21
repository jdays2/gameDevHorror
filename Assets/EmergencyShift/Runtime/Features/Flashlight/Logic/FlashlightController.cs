using System;
using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Flashlight.Logic
{
    public sealed class FlashlightController
    {
        private readonly FlashlightStateModel model;
        private readonly IFlashlightView view;

        public FlashlightController(FlashlightStateModel model, IFlashlightView view)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public bool IsOwned => model.IsOwned;
        public bool IsOn => model.IsOn;
        public float Range => model.Range;
        public float BeamAngle => model.BeamAngle;

        public bool Acquire()
        {
            if (!new AcquireFlashlightStateCommand(model).Execute().Success)
            {
                return false;
            }

            view.OwnershipChanged();
            SetPower(true);
            return true;
        }

        public void RestoreOwnership(bool owned)
        {
            bool previousOwnership = IsOwned;
            bool previousPower = IsOn;
            new RestoreFlashlightOwnershipCommand(model, owned).Execute();
            if (!owned)
            {
                view.RenderPower(false);
            }

            if (previousPower != IsOn)
            {
                view.PowerChanged(IsOn);
            }

            if (previousOwnership != IsOwned)
            {
                view.OwnershipChanged();
            }
        }

        public void SetPower(bool on)
        {
            bool previous = IsOn;
            new SetFlashlightPowerCommand(model, on, view.HasBeam).Execute();
            view.RenderPower(IsOn);
            if (previous != IsOn)
            {
                view.PowerChanged(IsOn);
            }
        }

        public CommandResult SetOptics(Vector2 optics)
        {
            float previousRange = Range;
            float previousAngle = BeamAngle;
            CommandResult result = new SetFlashlightOpticsCommand(model, optics).Execute();
            if (result.Success)
            {
                view.RenderOptics(new Vector2(Range, BeamAngle));
                if (previousRange != Range || previousAngle != BeamAngle)
                {
                    view.OpticsChanged();
                }
            }

            return result;
        }

        public void RefreshPresentation()
        {
            view.RenderOptics(new Vector2(Range, BeamAngle));
            SetPower(IsOn);
        }

        public bool Covers(Vector2 origin, Vector2 direction, Vector2 point) => model.Covers(origin, direction, point);
    }
}
