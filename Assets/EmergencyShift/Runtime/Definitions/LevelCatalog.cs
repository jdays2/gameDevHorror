using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Level catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        public LevelDefinition[] levels;
        public LevelDefinition Find(string id)
        {
            if (levels == null)
            {
                return null;
            }

            foreach (var level in levels)
            {
                if (level != null && level.levelId == id)
                {
                    return level;
                }
            }

            return null;
        }
    }
}
