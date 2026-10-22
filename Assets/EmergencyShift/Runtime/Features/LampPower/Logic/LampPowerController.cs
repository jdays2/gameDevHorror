using System;
using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.LampPower.Logic
{
    public sealed class LampPowerController
    {
        private readonly LampPowerModel model;
        private readonly ILampPowerView view;
        public bool Powered => model.Powered;

        public LampPowerController(LampPowerModel model, ILampPowerView view)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public CommandResult SetPower(bool on)
        {
            CommandResult result = new SetLampPowerCommand(model, on).Execute();
            if (result.Success)
            {
                Refresh();
            }

            return result;
        }

        public void Refresh() => view.ApplyPower(model.Powered);

        public void Tick(float phase, Vector2 position)
        {
            if (model.Powered)
            {
                view.ApplyFlicker(model.SampleClock(phase, position));
            }
        }
    }
}
