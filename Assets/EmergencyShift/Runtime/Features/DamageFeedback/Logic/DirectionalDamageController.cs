using System;
using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.DamageFeedback.Logic
{
    public sealed class DirectionalDamageController
    {
        private readonly DirectionalDamageModel model;
        private readonly IDirectionalDamageView view;
        private readonly ICameraImpactView camera;

        public DirectionalDamageController(DirectionalDamageModel model, IDirectionalDamageView view, ICameraImpactView camera)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.camera = camera ?? throw new ArgumentNullException(nameof(camera));
        }

        public CommandResult Show(int acceptedDamage, Vector2 towardSource)
        {
            CommandResult result = new ShowDamagePulseCommand(model, acceptedDamage, towardSource).Execute();
            if (result.Success)
            {
                view.Render(model.Pulses);
                camera.SetImpactOffset(model.ImpactOffset);
            }

            return result;
        }

        public void Tick(float unscaledSeconds)
        {
            if (model.ActiveCount == 0 && !model.ImpactActive)
            {
                return;
            }

            model.Advance(unscaledSeconds);
            view.Render(model.Pulses);
            camera.SetImpactOffset(model.ImpactOffset);
        }

        public void Reset()
        {
            model.Clear();
            view.Render(model.Pulses);
            camera.SetImpactOffset(model.ImpactOffset);
        }
    }
}
