using System;
using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.DamageFeedback.Logic
{
    public sealed class DirectionalDamageController
    {
        private readonly DirectionalDamageModel model;
        private readonly IDirectionalDamageView view;

        public DirectionalDamageController(DirectionalDamageModel model, IDirectionalDamageView view)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public CommandResult Show(int acceptedDamage, Vector2 towardSource)
        {
            CommandResult result = new ShowDamagePulseCommand(model, acceptedDamage, towardSource).Execute();
            if (result.Success)
            {
                view.Render(model.Pulses);
            }

            return result;
        }

        public void Tick(float unscaledSeconds)
        {
            if (model.ActiveCount == 0)
            {
                return;
            }

            model.Advance(unscaledSeconds);
            view.Render(model.Pulses);
        }

        public void Reset()
        {
            model.Clear();
            view.Render(model.Pulses);
        }
    }
}
