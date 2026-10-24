using Zenject;

namespace EmergencyShift.Features.ScriptedEncounters
{
    public static class ScriptedEncounterInstaller
    {
        public static void Install(DiContainer container) => container.Bind<ScriptedRouteControllerFactory>().AsSingle();
    }
}
