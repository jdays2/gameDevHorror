using EmergencyShift.Features.Health.Logic;
using Zenject;

namespace EmergencyShift.Features.Health
{
    public sealed class HealthInstaller : Installer<HealthInstaller>
    {
        public override void InstallBindings()
        {
            Container.Bind<HealthModelFactory>().AsSingle();
        }
    }
}
