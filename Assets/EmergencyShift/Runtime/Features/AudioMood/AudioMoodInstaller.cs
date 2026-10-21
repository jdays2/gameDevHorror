using EmergencyShift.Features.AudioMood.View;
using Zenject;

namespace EmergencyShift.Features.AudioMood
{
    public static class AudioMoodInstaller
    {
        public static void Install(DiContainer container)
        {
            container.Bind<AudioMoodControllerFactory>().AsSingle();
            container.Bind<AudioThreatQuery2D>().AsSingle();
        }
    }
}
