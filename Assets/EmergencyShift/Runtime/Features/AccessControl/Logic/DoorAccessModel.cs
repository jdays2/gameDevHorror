using System;
using EmergencyShift.Features.AccessControl.Config;

namespace EmergencyShift.Features.AccessControl.Logic
{
    public sealed class DoorAccessModel
    {
        private DoorAccessSettings settings;
        private double nextFeedback;
        private double nextNotice;

        public DoorAccessModel(DoorAccessSettings settings)
        {
            Configure(settings);
        }

        public string RequiredCardId => settings.RequiredCardId;
        public bool IsUnlocked { get; private set; }

        internal void Configure(DoorAccessSettings value)
        {
            settings = value ?? throw new ArgumentNullException(nameof(value));
        }

        internal DoorAccessAttempt ApplyAccess(bool hasCard, bool hasNoticeRecipient, double now)
        {
            if (IsUnlocked)
            {
                return new DoorAccessAttempt(true, false, false);
            }

            if (hasCard)
            {
                IsUnlocked = true;
                return new DoorAccessAttempt(true, true, false);
            }

            bool feedback = now >= nextFeedback;
            if (feedback)
            {
                nextFeedback = now + settings.DenialCooldown;
            }

            bool notice = hasNoticeRecipient && now >= nextNotice;
            if (notice)
            {
                nextNotice = now + settings.NoticeCooldown;
            }

            return new DoorAccessAttempt(false, feedback, notice);
        }

        internal void Restore(bool unlocked)
        {
            IsUnlocked = unlocked;
            nextFeedback = 0;
            nextNotice = 0;
        }
    }
}
