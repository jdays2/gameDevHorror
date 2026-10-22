using EmergencyShift.Features.Health.Config;
using EmergencyShift.Features.Interaction.Config;
using EmergencyShift.Features.PlayerMovement.Config;
using UnityEngine;

namespace EmergencyShift.Features.PlayerRules.Config
{
    /// <summary>Immutable player tuning. Legacy constants identify old default fields, not current balance.</summary>
    public sealed class PlayerRulesSettings
    {
        public const int LegacyMaximumHealth = 2;
        public const float LegacyProtectionSeconds = .7f;
        public const float LegacyMoveSpeed = 3.5f;
        public const float LegacyReach = 1.6f;
        public const float LegacyPickupRadius = .65f;
        public const float LegacyTieDistance = .05f;
        public const float LegacyNoticeSeconds = 1.8f;
        public const int LegacyMaximumNotices = 8;
        public const float LegacyHitFlashSeconds = .16f;
        public static readonly Color LegacyHitTint = new Color(1, .25f, .25f);
        public static readonly Color LegacyDefeatedTint = new Color(.35f, .08f, .10f);

        public static readonly PlayerRulesSettings LegacyDefaults = new PlayerRulesSettings(
            LegacyMaximumHealth, LegacyProtectionSeconds, LegacyMoveSpeed, LegacyReach,
            LegacyPickupRadius, LegacyTieDistance, LegacyNoticeSeconds, LegacyMaximumNotices);

        public int MaximumHealth { get; }
        public float ProtectionSeconds { get; }
        public float MoveSpeed { get; }
        public float Reach { get; }
        public float PickupRadius { get; }
        public float TieDistance { get; }
        public float NoticeSeconds { get; }
        public int MaximumNotices { get; }
        public float HitFlashSeconds { get; }
        public Color HitTint { get; }
        public Color DefeatedTint { get; }

        public PlayerRulesSettings(int maximumHealth, float protectionSeconds, float moveSpeed, float reach,
            float pickupRadius, float tieDistance, float noticeSeconds, int maximumNotices)
            : this(maximumHealth, protectionSeconds, moveSpeed, reach, pickupRadius, tieDistance,
                noticeSeconds, maximumNotices, LegacyHitFlashSeconds, LegacyHitTint, LegacyDefeatedTint)
        {
        }

        public PlayerRulesSettings(int maximumHealth, float protectionSeconds, float moveSpeed, float reach,
            float pickupRadius, float tieDistance, float noticeSeconds, int maximumNotices,
            float hitFlashSeconds, Color hitTint, Color defeatedTint)
        {
            _ = new HealthSettings(maximumHealth, protectionSeconds);
            _ = new PlayerMovementSettings(moveSpeed);
            _ = new PlayerInteractionSettings(reach, tieDistance, noticeSeconds, maximumNotices, pickupRadius);
            if (!float.IsFinite(hitFlashSeconds) || hitFlashSeconds < 0 || !ValidTint(hitTint) || !ValidTint(defeatedTint))
            {
                throw new System.ArgumentException("Player hit presentation requires a finite duration and colors.");
            }

            MaximumHealth = maximumHealth;
            ProtectionSeconds = protectionSeconds;
            MoveSpeed = moveSpeed;
            Reach = reach;
            PickupRadius = pickupRadius;
            TieDistance = tieDistance;
            NoticeSeconds = noticeSeconds;
            MaximumNotices = maximumNotices;
            HitFlashSeconds = hitFlashSeconds;
            HitTint = hitTint;
            DefeatedTint = defeatedTint;
        }

        public HealthSettings ProjectHealth(int authoredMaximum, float authoredProtection, bool forceAuthored = false)
        {
            return new HealthSettings(
                forceAuthored || authoredMaximum != LegacyMaximumHealth ? authoredMaximum : MaximumHealth,
                Select(authoredProtection, LegacyProtectionSeconds, ProtectionSeconds, forceAuthored));
        }

        public float ProjectMoveSpeed(float authoredSpeed, bool forceAuthored = false)
        {
            float resolved = Select(authoredSpeed, LegacyMoveSpeed, MoveSpeed, forceAuthored);
            _ = new PlayerMovementSettings(resolved);
            return resolved;
        }

        public PlayerInteractionSettings ProjectInteraction(PlayerInteractionSettings authored, bool forceAuthored = false)
        {
            if (authored == null)
            {
                throw new System.ArgumentNullException(nameof(authored));
            }

            return new PlayerInteractionSettings(
                Select(authored.Reach, LegacyReach, Reach, forceAuthored),
                Select(authored.TieDistance, LegacyTieDistance, TieDistance, forceAuthored),
                Select(authored.NoticeSeconds, LegacyNoticeSeconds, NoticeSeconds, forceAuthored),
                forceAuthored || authored.MaximumNotices != LegacyMaximumNotices ? authored.MaximumNotices : MaximumNotices,
                Select(authored.PickupRadius, LegacyPickupRadius, PickupRadius, forceAuthored));
        }

        private static float Select(float authored, float legacyDefault, float shared, bool forceAuthored) =>
            forceAuthored || authored != legacyDefault ? authored : shared;

        private static bool ValidTint(Color color) => float.IsFinite(color.r) && float.IsFinite(color.g) &&
            float.IsFinite(color.b) && float.IsFinite(color.a) && color.a >= 0 && color.a <= 1;
    }
}
