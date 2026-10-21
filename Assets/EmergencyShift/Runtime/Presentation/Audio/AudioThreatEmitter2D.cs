using System;
using EmergencyShift.Core.Composition;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Scene-owned signal adapter. New enemy adapters expose state without changing the director.</summary>
    public abstract class AudioThreatEmitter2D : MonoBehaviour
    {
        private SceneRegistry<AudioThreatEmitter2D> registry;
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
        public float HeardUntil { get; private set; } = float.NegativeInfinity;
        public float UrgentUntil { get; private set; } = float.NegativeInfinity;

        [Inject]
        public void Construct(SceneRegistry<AudioThreatEmitter2D> emitters)
        {
            registry?.Remove(this);
            registry = emitters ?? throw new ArgumentNullException(nameof(emitters));
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
            HeardUntil = float.NegativeInfinity;
            UrgentUntil = float.NegativeInfinity;
        }

        protected void ReportHeard(float seconds, float gain, bool urgent)
        {
            if (gain < .02f || !IsAlive)
            {
                return;
            }

            HeardUntil = Mathf.Max(HeardUntil, Time.time + Mathf.Clamp(seconds, .25f, 2f));
            if (urgent)
            {
                UrgentUntil = Time.time + .4f;
            }
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
