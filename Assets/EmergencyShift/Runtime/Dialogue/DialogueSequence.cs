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
            [SerializeField]
            private string displayName;
            [SerializeField]
            private Sprite portrait;
            public string DisplayName => displayName;
            public Sprite Portrait => portrait;

            public Speaker()
            {
            }

            public Speaker(string displayName = null, Sprite portrait = null)
            {
                this.displayName = displayName;
                this.portrait = portrait;
            }
        }

        [Serializable]
        public sealed class Choice
        {
            [SerializeField]
            private string id;
            [SerializeField, TextArea(1, 3)]
            private string text;
            [SerializeField]
            private string targetId;
            public string Id => id;
            public string Text => text;
            public string TargetId => targetId;

            public Choice()
            {
            }

            public Choice(string id = null, string text = null, string targetId = null)
            {
                this.id = id;
                this.text = text;
                this.targetId = targetId;
            }
        }

        [Serializable]
        public sealed class Line
        {
            [SerializeField]
            private string id;
            [SerializeField, Min(0)]
            private int speaker;
            [SerializeField, TextArea(2, 6)]
            private string text;
            [SerializeField, Tooltip("Empty ends the conversation. Leave empty when choices are present.")]
            private string nextId;
            [SerializeField]
            private Choice[] choices = Array.Empty<Choice>();
            public string Id => id;
            public int SpeakerIndex => speaker;
            public string Text => text;
            public string NextId => nextId;
            public int ChoiceCount => choices != null ? choices.Length : 0;
            public Choice ChoiceAt(int index) => choices[index];

            public Line()
            {
            }

            public Line(string id = null, int speaker = 0, string text = null, string nextId = null, Choice[] choices = null)
            {
                this.id = id;
                this.speaker = speaker;
                this.text = text;
                this.nextId = nextId;
                this.choices = choices != null ? (Choice[])choices.Clone() : Array.Empty<Choice>();
            }
        }

        [SerializeField]
        private string entryId;
        [SerializeField]
        private Speaker[] speakers = Array.Empty<Speaker>();
        [SerializeField]
        private Line[] lines = Array.Empty<Line>();
        public string EntryId => entryId;
        public int SpeakerCount => speakers != null ? speakers.Length : 0;
        public Speaker SpeakerAt(int index) => speakers[index];
        public int LineCount => lines != null ? lines.Length : 0;
        public Line LineAt(int index) => lines[index];

        public void Validate() => ToGraph();

        public DialogueGraph ToGraph()
        {
            if (speakers == null || speakers.Length == 0 || lines == null || lines.Length == 0)
            {
                throw new InvalidOperationException("Dialogue requires speakers and lines: " + name);
            }

            foreach (Speaker speaker in speakers)
            {
                if (speaker == null || string.IsNullOrWhiteSpace(speaker.DisplayName))
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
                if (line.ChoiceCount > 0)
                {
                    for (int index = 0; index < line.ChoiceCount; index++)
                    {
                        Choice choice = line.ChoiceAt(index);
                        if (choice == null)
                        {
                            throw new InvalidOperationException("Dialogue choice is missing: " + line.Id);
                        }

                        choices.Add(new DialogueOption(choice.Id, choice.Text, choice.TargetId));
                    }
                }

                nodes.Add(new DialogueNode(line.Id, line.SpeakerIndex, line.Text, line.NextId, choices));
            }

            return new DialogueGraph(entryId, speakers.Length, nodes);
        }
    }
}
