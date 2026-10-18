using System;
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
        public sealed class Line
        {
            [Min(0)] public int speaker;
            [TextArea(2, 6)] public string text;
        }

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

            foreach (var line in lines)
            {
                if (line == null || line.speaker < 0 || line.speaker >= speakers.Length || string.IsNullOrWhiteSpace(line.text))
                {
                    throw new InvalidOperationException("Dialogue line has invalid speaker or text: " + name);
                }
            }
        }
    }
}
