using Zenject;

namespace EmergencyShift
{
    public static class DialogueInstaller
    {
        public static void Install(DiContainer container)
        {
            container.Bind<DialogueControllerFactory>().AsSingle();
        }
    }
}
