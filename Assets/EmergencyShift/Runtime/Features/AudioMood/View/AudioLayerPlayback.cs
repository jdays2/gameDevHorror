using System;
using UnityEngine;

namespace EmergencyShift.Features.AudioMood.View
{
    /// <summary>Owns playback lifetime, borrowing the three authored AudioSources.</summary>
    public sealed class AudioLayerPlayback : IDisposable
    {
        private readonly AudioSource exploration;
        private readonly AudioSource threat;
        private readonly AudioSource combat;
        private bool paused;
        private double scheduledStart;
        public bool Started { get; private set; }
        public bool IsConfigured => exploration != null && threat != null && combat != null;

        public AudioLayerPlayback(AudioSource exploration, AudioSource threat, AudioSource combat)
        {
            this.exploration = exploration;
            this.threat = threat;
            this.combat = combat;
        }

        public void Start(AudioMoodProfile profile)
        {
            scheduledStart = AudioSettings.dspTime + profile.ScheduleLeadSeconds;
            Schedule(exploration, profile.Exploration, scheduledStart, profile.LayerPriority);
            Schedule(threat, profile.Threat, scheduledStart, profile.LayerPriority);
            Schedule(combat, profile.Combat, scheduledStart, profile.LayerPriority);
            Started = true;
        }

        public bool UpdatePause(bool wantPause)
        {
            if (wantPause)
            {
                if (!paused && Started)
                {
                    if (AudioSettings.dspTime < scheduledStart)
                    {
                        Stop();
                    }
                    else
                    {
                        exploration.Pause();
                        threat.Pause();
                        combat.Pause();
                    }
                }

                paused = true;
                return true;
            }

            if (paused && Started)
            {
                exploration.UnPause();
                threat.UnPause();
                combat.UnPause();
            }

            paused = false;
            return false;
        }

        public void ApplyGains(Vector3 gains)
        {
            if (exploration != null)
            {
                exploration.volume = gains.x;
            }

            if (threat != null)
            {
                threat.volume = gains.y;
            }

            if (combat != null)
            {
                combat.volume = gains.z;
            }
        }

        public void Stop()
        {
            if (exploration != null)
            {
                exploration.Stop();
            }

            if (threat != null)
            {
                threat.Stop();
            }

            if (combat != null)
            {
                combat.Stop();
            }

            Started = false;
        }

        public void Dispose()
        {
            Stop();
            paused = false;
        }

        private static void Schedule(AudioSource source, AudioClip clip, double at, int priority)
        {
            source.Stop();
            source.clip = clip;
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.dopplerLevel = 0;
            source.pitch = 1;
            source.loop = true;
            source.priority = priority;
            source.volume = 0;
            if (clip != null)
            {
                source.PlayScheduled(at);
            }
        }
    }
}
