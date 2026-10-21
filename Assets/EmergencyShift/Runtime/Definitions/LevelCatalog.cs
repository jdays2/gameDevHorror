using System;
using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Level catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [SerializeField]
        private LevelDefinition[] levels;
        public int DefinitionCount => levels != null ? levels.Length : 0;
        public LevelDefinition DefinitionAt(int index) => levels[index];
        public LevelDefinition Find(string id)
        {
            Validate();
            for (int index = 0; index < DefinitionCount; index++)
            {
                if (string.Equals(levels[index].LevelId, id, StringComparison.Ordinal))
                {
                    return levels[index];
                }
            }

            return null;
        }

        public void Validate()
        {
            for (int index = 0; index < DefinitionCount; index++)
            {
                LevelDefinition definition = levels[index];
                if (definition == null)
                {
                    throw new InvalidOperationException("Missing level definition in catalog: " + name);
                }

                definition.Validate();
                for (int previous = 0; previous < index; previous++)
                {
                    if (string.Equals(levels[previous].LevelId, definition.LevelId, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Duplicate level ID: " + definition.LevelId);
                    }
                }
            }
        }
    }
}
