namespace EmergencyShift.Features.LevelObjectives.Logic
{
    /// <summary>Scene-independent condition evaluation; unknown or unavailable facts never match.</summary>
    public static class ObjectiveRuleMatcher
    {
        public static bool Matches(ObjectiveCondition condition, string cardId, IObjectiveFacts facts)
        {
            switch (condition)
            {
                case ObjectiveCondition.Always:
                    return true;
                case ObjectiveCondition.HasCard:
                    return facts != null && !string.IsNullOrWhiteSpace(cardId) && facts.HasCard(cardId);
                case ObjectiveCondition.MissingCard:
                    return facts != null && !string.IsNullOrWhiteSpace(cardId) && !facts.HasCard(cardId);
                case ObjectiveCondition.WeaponMissing:
                    return facts != null && !facts.HasWeapon;
                default:
                    return false;
            }
        }
    }
}
