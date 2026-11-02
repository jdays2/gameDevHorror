using System;
using System.Collections.Generic;
using EmergencyShift.Features.WorldAudio.Config;
using EmergencyShift.Features.WorldAudio.Logic;
using UnityEngine;
using UnityEngine.Audio;

namespace EmergencyShift.Features.WorldAudio.View
{
    /// <summary>World-scoped mixer route. Independent scene leases cannot unmute one another.</summary>
    public sealed class WorldAudioBus : IDisposable
    {
        public const float SilenceDecibels = -80;
        private readonly AudioMixerGroup output;
        private readonly string parameter;
        private readonly List<GainLease> leases = new List<GainLease>();
        private float baseline;
        private float appliedGain = 1;
        private bool disposed;

        public AudioMixerGroup Output => output;
        public float Gain => appliedGain;

        public WorldAudioBus(WorldAudioProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            profile.Validate();
            output = profile.WorldOutput;
            parameter = profile.AttenuationParameter;
        }

        /// <summary>Route owned native voices during injection, before their first playback. Never changes source levels.</summary>
        public void Route(params AudioSource[] sources)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WorldAudioBus));
            }

            if (sources == null)
            {
                return;
            }

            foreach (AudioSource source in sources)
            {
                if (source != null)
                {
                    source.outputAudioMixerGroup = output;
                }
            }
        }

        /// <summary>Acquire from Start or later; Unity does not support SetFloat from Awake/OnEnable.</summary>
        public IWorldAudioGain AcquireGain()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WorldAudioBus));
            }

            if (leases.Count == 0 && !output.audioMixer.GetFloat(parameter, out baseline))
            {
                throw new InvalidOperationException("World audio attenuation is unavailable.");
            }

            var lease = new GainLease(this);
            leases.Add(lease);
            return lease;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            if (leases.Count > 0)
            {
                leases.Clear();
                ApplyMix();
            }
        }

        private void ApplyMix()
        {
            float gain = 1;
            foreach (GainLease lease in leases)
            {
                gain = Mathf.Min(gain, lease.Gain);
            }

            if (gain == appliedGain)
            {
                return;
            }

            float decibels = gain <= .0001f ? SilenceDecibels : Mathf.Max(SilenceDecibels, baseline + 20 * Mathf.Log10(gain));
            if (gain >= 1)
            {
                decibels = baseline;
            }

            if (!output.audioMixer.SetFloat(parameter, decibels))
            {
                throw new InvalidOperationException("World audio mixer rejected its attenuation parameter.");
            }

            appliedGain = gain;
        }

        private sealed class GainLease : IWorldAudioGain
        {
            private WorldAudioBus owner;
            public float Gain { get; private set; } = 1;

            public GainLease(WorldAudioBus owner) => this.owner = owner;

            public void Apply(float gain)
            {
                if (!float.IsFinite(gain) || gain < 0 || gain > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(gain));
                }

                if (owner == null || owner.disposed)
                {
                    return;
                }

                Gain = gain;
                owner.ApplyMix();
            }

            public void Dispose()
            {
                WorldAudioBus previous = owner;
                owner = null;
                if (previous != null && !previous.disposed)
                {
                    previous.leases.Remove(this);
                    previous.ApplyMix();
                }
            }
        }
    }
}
