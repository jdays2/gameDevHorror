using System;

namespace EmergencyShift.Features.Interaction.Config
{
    public sealed class PlayerInteractionSettings
    {
        public PlayerInteractionSettings(float reach, float tieDistance, float noticeSeconds, int maximumNotices, float pickupRadius = .65f)
        {
            if (!float.IsFinite(reach) || reach < .1f || !float.IsFinite(tieDistance) || tieDistance < 0 ||
                !float.IsFinite(noticeSeconds) || noticeSeconds < .5f || maximumNotices < 1 || maximumNotices > 64 ||
                !float.IsFinite(pickupRadius) || pickupRadius < .1f)
            {
                throw new ArgumentException("Invalid interaction reach, selection tolerance or notice bounds.");
            }

            Reach = reach;
            TieDistance = tieDistance;
            NoticeSeconds = noticeSeconds;
            MaximumNotices = maximumNotices;
            PickupRadius = pickupRadius;
        }

        public float Reach { get; }
        public float TieDistance { get; }
        public float NoticeSeconds { get; }
        public int MaximumNotices { get; }
        public float PickupRadius { get; }
    }
}
