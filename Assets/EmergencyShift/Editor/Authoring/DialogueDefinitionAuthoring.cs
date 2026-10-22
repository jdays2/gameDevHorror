using System;
using System.Collections.Generic;
using UnityEditor;

namespace EmergencyShift.Editor
{
    /// <summary>Explicit Editor write boundary for readonly definitions. Does not save assets/scenes.</summary>
    public static class DialogueDefinitionAuthoring
    {
        public static void SetSpeakers(DialogueSequence sequence, IReadOnlyList<DialogueSequence.Speaker> speakers)
        {
            if (sequence == null || speakers == null)
            {
                throw new ArgumentNullException("Dialogue definition and speakers are required.");
            }

            foreach (DialogueSequence.Speaker speaker in speakers)
            {
                if (speaker == null || string.IsNullOrWhiteSpace(speaker.DisplayName))
                {
                    throw new ArgumentException("Dialogue speaker needs a name.", nameof(speakers));
                }
            }

            var serialized = new SerializedObject(sequence);
            SerializedProperty target = serialized.FindProperty("speakers");
            target.arraySize = speakers.Count;
            for (int index = 0; index < speakers.Count; index++)
            {
                SerializedProperty item = target.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("displayName").stringValue = speakers[index].DisplayName;
                item.FindPropertyRelative("portrait").objectReferenceValue = speakers[index].Portrait;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetGraph(DialogueSequence sequence, string entryId, IReadOnlyList<DialogueSequence.Line> lines)
        {
            if (sequence == null || lines == null)
            {
                throw new ArgumentNullException("Dialogue definition and lines are required.");
            }

            ValidateGraph(sequence, entryId, lines);
            var serialized = new SerializedObject(sequence);
            serialized.FindProperty("entryId").stringValue = entryId;
            SerializedProperty target = serialized.FindProperty("lines");
            target.arraySize = lines.Count;
            for (int index = 0; index < lines.Count; index++)
            {
                DialogueSequence.Line line = lines[index];
                SerializedProperty item = target.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("id").stringValue = line.Id;
                item.FindPropertyRelative("speaker").intValue = line.SpeakerIndex;
                item.FindPropertyRelative("text").stringValue = line.Text;
                item.FindPropertyRelative("nextId").stringValue = line.NextId;
                SerializedProperty choices = item.FindPropertyRelative("choices");
                choices.arraySize = line.ChoiceCount;
                for (int choiceIndex = 0; choiceIndex < line.ChoiceCount; choiceIndex++)
                {
                    DialogueSequence.Choice choice = line.ChoiceAt(choiceIndex);
                    SerializedProperty option = choices.GetArrayElementAtIndex(choiceIndex);
                    option.FindPropertyRelative("id").stringValue = choice.Id;
                    option.FindPropertyRelative("text").stringValue = choice.Text;
                    option.FindPropertyRelative("targetId").stringValue = choice.TargetId;
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ValidateGraph(DialogueSequence sequence, string entryId, IReadOnlyList<DialogueSequence.Line> lines)
        {
            var nodes = new List<DialogueNode>(lines.Count);
            foreach (DialogueSequence.Line line in lines)
            {
                if (line == null)
                {
                    throw new ArgumentException("Dialogue line is missing.", nameof(lines));
                }

                var options = new List<DialogueOption>(line.ChoiceCount);
                for (int index = 0; index < line.ChoiceCount; index++)
                {
                    DialogueSequence.Choice choice = line.ChoiceAt(index);
                    if (choice == null)
                    {
                        throw new ArgumentException("Dialogue choice is missing.", nameof(lines));
                    }

                    options.Add(new DialogueOption(choice.Id, choice.Text, choice.TargetId));
                }

                nodes.Add(new DialogueNode(line.Id, line.SpeakerIndex, line.Text, line.NextId, options));
            }

            new DialogueGraph(entryId, sequence.SpeakerCount, nodes);
        }
    }
}
