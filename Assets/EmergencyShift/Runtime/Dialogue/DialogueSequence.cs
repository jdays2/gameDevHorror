using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    [CreateAssetMenu(menuName = "Emergency Shift/Dialogue/Sequence")]
    public sealed class DialogueSequence : ScriptableObject
    {
        [Serializable]
        public sealed class Speaker
        {
            public string displayName;
            public Sprite portrait;
        }

        [Serializable]
        public sealed class Choice
        {
            public string id;
            [TextArea(1, 3)] public string text;
            public string targetId;
        }

        [Serializable]
        public sealed class Line
        {
            public string id;
            [Min(0)] public int speaker;
            [TextArea(2, 6)] public string text;
            [Tooltip("Empty ends the conversation. Leave empty when choices are present.")]
            public string nextId;
            public Choice[] choices = Array.Empty<Choice>();
        }

        public string entryId;
        public Speaker[] speakers = Array.Empty<Speaker>();
        public Line[] lines = Array.Empty<Line>();

        public void Validate()
        {
            if (speakers == null || speakers.Length == 0 || lines == null || lines.Length == 0)
            {
                throw new InvalidOperationException("Dialogue requires speakers and lines: " + name);
            }

            foreach (var speaker in speakers)
            {
                if (speaker == null || string.IsNullOrWhiteSpace(speaker.displayName))
                {
                    throw new InvalidOperationException("Dialogue speaker has no name: " + name);
                }
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var line in lines)
            {
                if (line == null || line.speaker < 0 || line.speaker >= speakers.Length || string.IsNullOrWhiteSpace(line.text))
                {
                    throw new InvalidOperationException("Dialogue line has invalid speaker or text: " + name);
                }

                if (string.IsNullOrWhiteSpace(line.id) || !ids.Add(line.id))
                {
                    throw new InvalidOperationException("Dialogue line IDs must be nonempty and unique: " + name);
                }
            }

            if (string.IsNullOrWhiteSpace(entryId) || !ids.Contains(entryId))
            {
                throw new InvalidOperationException("Dialogue entry does not exist: " + name);
            }

            foreach (var line in lines)
            {
                bool hasChoices = line.choices != null && line.choices.Length > 0;
                if (hasChoices && !string.IsNullOrEmpty(line.nextId))
                {
                    throw new InvalidOperationException("A line cannot have both nextId and choices: " + line.id);
                }

                if (!string.IsNullOrEmpty(line.nextId) && !ids.Contains(line.nextId))
                {
                    throw new InvalidOperationException("Dialogue next target does not exist: " + line.id);
                }

                if (!hasChoices)
                {
                    continue;
                }

                var choiceIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var choice in line.choices)
                {
                    if (choice == null || string.IsNullOrWhiteSpace(choice.id) || !choiceIds.Add(choice.id)
                        || string.IsNullOrWhiteSpace(choice.text) || string.IsNullOrWhiteSpace(choice.targetId)
                        || !ids.Contains(choice.targetId))
                    {
                        throw new InvalidOperationException("Dialogue choice has invalid ID, text or target: " + line.id);
                    }
                }
            }
        }
    }
}
