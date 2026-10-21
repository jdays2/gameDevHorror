using System;
using EmergencyShift.Core.Composition;
using EmergencyShift.Features.AudioMood;
using EmergencyShift.Features.AudioMood.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Scene-owned signal adapter. New enemy adapters expose state without changing the director.</summary>
    public abstract class AudioThreatEmitter2D : MonoBehaviour
    {
        private SceneRegistry<AudioThreatEmitter2D> registry;
        private AudioThreatSignalController signal;
        public int ObservationId { get; private set; }
        public abstract bool IsAlive
        {
            get;
        }
        public abstract bool IsEngaged
        {
            get;
        }
        public abstract bool IsAttacking
        {
            get;
        }
        public abstract Transform Target
        {
            get;
        }
        public abstract LayerMask Obstacles
        {
            get;
        }
        public float HeardUntil => signal != null ? signal.HeardUntil : float.NegativeInfinity;
        public float UrgentUntil => signal != null ? signal.UrgentUntil : float.NegativeInfinity;

        [Inject]
        public void Construct(SceneRegistry<AudioThreatEmitter2D> emitters, AudioThreatSignalControllerFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (emitters == null)
            {
                throw new ArgumentNullException(nameof(emitters));
            }

            registry?.Remove(this);
            registry = emitters;
            signal = factory.Create();
            if (isActiveAndEnabled)
            {
                Register();
            }
        }

        protected virtual void OnEnable()
        {
            Register();
        }

        protected virtual void OnDisable()
        {
            registry?.Remove(this);
            signal?.Reset();
        }

        protected void ReportHeard(float seconds, float gain, bool urgent)
        {
            if (!IsAlive)
            {
                return;
            }

            if (signal == null)
            {
                throw new InvalidOperationException("Threat signal source requires level composition before cues.");
            }

            signal.Report(seconds, gain, IsAlive, urgent);
        }

        private void Register()
        {
            if (registry == null)
            {
                return;
            }

            int ordinal = registry.Register(this);
            if (ordinal > 0)
            {
                ObservationId = ordinal;
            }
        }
    }
}
