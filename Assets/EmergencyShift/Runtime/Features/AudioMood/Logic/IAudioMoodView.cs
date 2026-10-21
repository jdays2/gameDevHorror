using MoodState = EmergencyShift.AudioMood;

namespace EmergencyShift.Features.AudioMood.Logic
{
    public interface IAudioMoodView
    {
        void ApplyMood(MoodState mood, int perceived, int engaged);
    }
}
