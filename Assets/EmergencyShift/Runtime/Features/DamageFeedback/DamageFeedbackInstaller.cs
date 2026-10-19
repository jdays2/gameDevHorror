using EmergencyShift.Features.DamageFeedback.Logic;
using Zenject;

namespace EmergencyShift.Features.DamageFeedback
{
    public sealed class DamageFeedbackInstaller : Installer<DamageFeedbackInstaller>
    {
        public override void InstallBindings()
        {
            Container.Bind<DirectionalDamageFactory>().AsSingle();
        }
    }
}
