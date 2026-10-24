using System;
using EmergencyShift.Features.Enemies.Config;
using UnityEngine;

namespace EmergencyShift.Features.Enemies.Logic
{
    /// <summary>One physical light contact: approach warning, close burn and gradual recovery.</summary>
    public sealed class StalkerEyeModel
    {
        private readonly StalkerSettings settings;
        private bool burning;

        public StalkerEyeModel(StalkerSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public event Action BurnStarted;
        public float Damage { get; private set; }
        public float Warning { get; private set; }
        public float Exposure { get; private set; }
        public float BurnContact { get; private set; }
        public bool Critical { get; private set; }
        public bool Ready => Exposure >= settings.ReactionDelay;

        public void Step(bool illuminated, float distance, bool canReact, float deltaTime)
        {
            if (!float.IsFinite(deltaTime) || deltaTime <= 0 || !float.IsFinite(distance) || distance < 0)
            {
                return;
            }

            bool warningLight = illuminated && distance <= settings.FearDistance;
            Critical = illuminated && distance <= settings.BurnDistance;
            float warningTarget = warningLight
                ? Mathf.Lerp(.2f, 1, Mathf.InverseLerp(settings.FearDistance, settings.BurnDistance, distance)) : 0;
            Warning = Mathf.MoveTowards(Warning, warningTarget, deltaTime / settings.EyeWarningSeconds);
            float previousExposure = Exposure;
            Exposure = canReact && Critical ? Mathf.Min(settings.ReactionDelay, Exposure + deltaTime) : 0;
            float burnTime = burning ? deltaTime : 0;

            if (!Critical)
            {
                burning = false;
                BurnContact = 0;
            }
            else if (Ready && !burning)
            {
                burning = true;
                burnTime = Mathf.Max(0, previousExposure + deltaTime - settings.ReactionDelay);
                BurnStarted?.Invoke();
            }

            if (Critical && burning)
            {
                BurnContact += burnTime;
                Damage = Mathf.MoveTowards(Damage, 1, burnTime / settings.EyeBurnSeconds);
            }
            else if (!illuminated)
            {
                Damage = Mathf.MoveTowards(Damage, 0, deltaTime / settings.EyeRecoverySeconds);
            }
        }

        public void ResetReaction() => Exposure = 0;

        internal void Restore(float damage, float contact, float exposure, bool reacting)
        {
            Damage = damage;
            BurnContact = contact;
            Exposure = exposure;
            Warning = 0;
            Critical = false;
            burning = contact > 0 || reacting;
        }
    }
}
