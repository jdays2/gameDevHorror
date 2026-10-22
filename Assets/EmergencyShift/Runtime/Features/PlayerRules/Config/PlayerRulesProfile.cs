using UnityEngine;

namespace EmergencyShift.Features.PlayerRules.Config
{
    [CreateAssetMenu(menuName = "Emergency Shift/Player rules")]
    public sealed class PlayerRulesProfile : ScriptableObject
    {
        [SerializeField, Min(1)]
        private int maximumHealth = PlayerRulesSettings.LegacyMaximumHealth;
        [SerializeField, Min(0)]
        private float protectionSeconds = PlayerRulesSettings.LegacyProtectionSeconds;
        [SerializeField, Min(0)]
        private float moveSpeed = PlayerRulesSettings.LegacyMoveSpeed;
        [SerializeField, Min(.1f)]
        private float reach = PlayerRulesSettings.LegacyReach;
        [SerializeField, Min(.1f)]
        private float pickupRadius = PlayerRulesSettings.LegacyPickupRadius;
        [SerializeField, Min(0)]
        private float tieDistance = PlayerRulesSettings.LegacyTieDistance;
        [SerializeField, Min(.5f)]
        private float noticeSeconds = PlayerRulesSettings.LegacyNoticeSeconds;
        [SerializeField, Range(1, 64)]
        private int maximumNotices = PlayerRulesSettings.LegacyMaximumNotices;
        [Header("Life presentation")]
        [SerializeField, Min(0)]
        private float hitFlashSeconds = PlayerRulesSettings.LegacyHitFlashSeconds;
        [SerializeField]
        private Color hitTint = PlayerRulesSettings.LegacyHitTint;
        [SerializeField]
        private Color defeatedTint = PlayerRulesSettings.LegacyDefeatedTint;

        public PlayerRulesSettings ToSettings() => new PlayerRulesSettings(maximumHealth, protectionSeconds,
            moveSpeed, reach, pickupRadius, tieDistance, noticeSeconds, maximumNotices,
            hitFlashSeconds, hitTint, defeatedTint);

        public void Validate() => ToSettings();
    }
}
