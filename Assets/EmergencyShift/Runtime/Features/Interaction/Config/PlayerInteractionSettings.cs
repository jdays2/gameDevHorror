using System;

namespace EmergencyShift.Features.Interaction.Config
{
    public sealed class PlayerInteractionSettings
    {
        public PlayerInteractionSettings(float reach, float tieDistance, float noticeSeconds, int maximumNotices)
        {
            if (!float.IsFinite(reach) || reach < .1f || !float.IsFinite(tieDistance) || tieDistance < 0 ||
                !float.IsFinite(noticeSeconds) || noticeSeconds < .5f || maximumNotices < 1 || maximumNotices > 64)
            {
                throw new ArgumentException("Invalid interaction reach, selection tolerance or notice bounds.");
            }

            Reach = reach;
            TieDistance = tieDistance;
            NoticeSeconds = noticeSeconds;
            MaximumNotices = maximumNotices;
        }

        public float Reach { get; }
        public float TieDistance { get; }
        public float NoticeSeconds { get; }
        public int MaximumNotices { get; }
    }
}
