using System;

namespace EmergencyShift.Features.WorldAudio.Logic
{
    public interface IWorldAudioGain : IDisposable
    {
        void Apply(float gain);
    }
}
