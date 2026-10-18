using System;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class DialogueDataTests
    {
        [Test]
        public void ClosingOneModalCannotReleaseAnotherOwnersInput()
        {
            using (var first = GameplayInputBlock.Acquire())
            using (var second = GameplayInputBlock.Acquire())
            {
                first.Dispose();
                first.Dispose();
                Assert.That(GameplayInputBlock.IsBlocked, Is.True);
            }

            Assert.That(GameplayInputBlock.IsBlocked, Is.False);
        }

        [Test]
        public void UnknownSpeakerIsRejectedWithoutChangingContent()
        {
            var sequence = ScriptableObject.CreateInstance<DialogueSequence>();
            try
            {
                sequence.speakers = new[] { new DialogueSequence.Speaker { displayName = "Врач" } };
                sequence.lines = new[] { new DialogueSequence.Line { speaker = 5, text = "Реплика" } };
                Assert.Throws<InvalidOperationException>(sequence.Validate);
                Assert.That(sequence.lines[0].speaker, Is.EqualTo(5));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sequence);
            }
        }

        [Test]
        public void DifferentSpeakersCanUseTheSameWhitePlaceholder()
        {
            var sequence = ScriptableObject.CreateInstance<DialogueSequence>();
            try
            {
                sequence.speakers = new[]
                {
                    new DialogueSequence.Speaker { displayName = "Врач" },
                    new DialogueSequence.Speaker { displayName = "Вы" }
                };
                sequence.lines = new[]
                {
                    new DialogueSequence.Line { speaker = 0, text = "Стойте." },
                    new DialogueSequence.Line { speaker = 1, text = "Почему?" }
                };
                Assert.DoesNotThrow(sequence.Validate);
                Assert.That(sequence.speakers[0].portrait, Is.Null);
                Assert.That(sequence.speakers[1].portrait, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sequence);
            }
        }
    }
}
