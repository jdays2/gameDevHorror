using System;
using EmergencyShift.Features.Pickups.Config;
using EmergencyShift.Features.Pickups.View;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Features.Pickups
{
    public static class PickupInstaller
    {
        public static void Install(DiContainer container)
        {
            var profile = Resources.Load<PickupGlintProfile>("PickupGlintProfile");
            if (profile == null)
            {
                throw new InvalidOperationException("Pickup presentation requires Resources/PickupGlintProfile.");
            }

            profile.Validate();
            container.Bind<PickupGlintProfile>().FromInstance(profile);
            container.Bind<PickupGlintFactory>().AsSingle();
            container.Bind<PickupControllerFactory>().AsSingle();
        }
    }
}
