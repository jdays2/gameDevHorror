using System;
using EmergencyShift.Features.Pickups.Config;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Features.Pickups
{
    public sealed class PickupGlintInstaller : MonoInstaller
    {
        [SerializeField] private PickupGlintProfile profile;

        public override void InstallBindings()
        {
            if (profile == null)
            {
                throw new InvalidOperationException("Pickup presentation needs its authored world profile.");
            }

            profile.Validate();
            Container.Bind<PickupGlintProfile>().FromInstance(profile);
        }
    }
}
