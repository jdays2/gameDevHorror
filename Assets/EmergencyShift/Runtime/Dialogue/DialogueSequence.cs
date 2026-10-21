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

        public void Validate() => ToGraph();

        public DialogueGraph ToGraph()
        {
            if (speakers == null || speakers.Length == 0 || lines == null || lines.Length == 0)
            {
                throw new InvalidOperationException("Dialogue requires speakers and lines: " + name);
            }

            foreach (Speaker speaker in speakers)
            {
                if (speaker == null || string.IsNullOrWhiteSpace(speaker.displayName))
                {
                    throw new InvalidOperationException("Dialogue speaker has no name: " + name);
                }
            }

            var nodes = new List<DialogueNode>(lines.Length);
            foreach (Line line in lines)
            {
                if (line == null)
                {
                    throw new InvalidOperationException("Dialogue line is missing: " + name);
                }

                var choices = new List<DialogueOption>();
                if (line.choices != null)
                {
                    foreach (Choice choice in line.choices)
                    {
                        if (choice == null)
                        {
                            throw new InvalidOperationException("Dialogue choice is missing: " + line.id);
                        }

                        choices.Add(new DialogueOption(choice.id, choice.text, choice.targetId));
                    }
                }

                nodes.Add(new DialogueNode(line.id, line.speaker, line.text, line.nextId, choices));
            }

            return new DialogueGraph(entryId, speakers.Length, nodes);
        }
    }
}
