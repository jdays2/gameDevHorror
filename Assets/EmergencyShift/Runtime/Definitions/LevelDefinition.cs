using System;
using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Level definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField, Min(1)]
        private int saveRevision = 1;
        [SerializeField]
        private string levelId;
        [SerializeField, Tooltip("Project-relative scene path; add the same scene to Build Profiles.")]
        private string scenePath;
        [SerializeField]
        private string exitCardId = "evacuation";
        [SerializeField]
        private string nextLevelId;
        [SerializeField]
        private LevelObjectiveRule[] objectives = Array.Empty<LevelObjectiveRule>();

        public int SaveRevision => saveRevision;
        public string LevelId => levelId;
        public string ScenePath => scenePath;
        public string ExitCardId => exitCardId;
        public string NextLevelId => nextLevelId;
        public int ObjectiveCount => objectives != null ? objectives.Length : 0;
        public LevelObjectiveRule ObjectiveAt(int index) => objectives[index];

        public void Validate()
        {
            if (saveRevision < 1 || string.IsNullOrWhiteSpace(levelId) || string.IsNullOrWhiteSpace(exitCardId) ||
                (!string.IsNullOrEmpty(nextLevelId) && string.IsNullOrWhiteSpace(nextLevelId)) ||
                (!string.IsNullOrEmpty(scenePath) && (!scenePath.StartsWith("Assets/", StringComparison.Ordinal) ||
                    !scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))))
            {
                throw new InvalidOperationException("Invalid level definition: " + name);
            }

            for (int index = 0; index < ObjectiveCount; index++)
            {
                if (objectives[index] == null)
                {
                    throw new InvalidOperationException("Missing level objective rule: " + name);
                }

                objectives[index].Validate();
            }
        }
    }
}
