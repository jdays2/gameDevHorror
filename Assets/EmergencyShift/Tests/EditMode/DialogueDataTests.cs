using System;
using EmergencyShift.Core.Time;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class DialogueDataTests
    {
        [Test]
        public void ClosingOneModalCannotReleaseAnotherOwnersInput()
        {
            var inputBlock = new GameplayInputBlock();
            using (var first = inputBlock.Acquire())
            using (var second = inputBlock.Acquire())
            {
                first.Dispose();
                first.Dispose();
                Assert.That(inputBlock.IsBlocked, Is.True);
            }

            Assert.That(inputBlock.IsBlocked, Is.False);
        }

        [Test]
        public void UnknownSpeakerIsRejectedWithoutChangingContent()
        {
            var sequence = ScriptableObject.CreateInstance<DialogueSequence>();
            try
            {
                SetDefinitionField(sequence, "speakers", new[] { new DialogueSequence.Speaker(displayName: "Врач") });
                SetDefinitionField(sequence, "lines", new[] { new DialogueSequence.Line(speaker: 5, text: "Реплика") });
                Assert.Throws<InvalidOperationException>(sequence.Validate);
                Assert.That(sequence.LineAt(0).SpeakerIndex, Is.EqualTo(5));
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
                SetDefinitionField(sequence, "speakers", new[]
                {
                    new DialogueSequence.Speaker(displayName: "Врач"),
                    new DialogueSequence.Speaker(displayName: "Вы")
                });
                SetDefinitionField(sequence, "lines", new[]
                {
                    new DialogueSequence.Line(id: "stop", speaker: 0, text: "Стойте.", nextId: "why"),
                    new DialogueSequence.Line(id: "why", speaker: 1, text: "Почему?")
                });
                SetDefinitionField(sequence, "entryId", "stop");
                Assert.DoesNotThrow(sequence.Validate);
                Assert.That(sequence.SpeakerAt(0).Portrait, Is.Null);
                Assert.That(sequence.SpeakerAt(1).Portrait, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sequence);
            }
        }

        [TestCase(0, "left", "take-left")]
        [TestCase(1, "right", "take-right")]
        public void ChoiceRequiresSelectionAndFollowsStableTargetAfterReordering(int choice, string target, string choiceId)
        {
            var sequence = BranchingSequence();
            try
            {
                var reordered = new DialogueSequence.Line[sequence.LineCount];
                for (int index = 0; index < reordered.Length; index++)
                {
                    reordered[index] = sequence.LineAt(index);
                }

                Array.Reverse(reordered);
                SetDefinitionField(sequence, "lines", reordered);
                var run = new DialogueRun(sequence.ToGraph());
                Assert.That(run.Current.Id, Is.EqualTo("fork"));
                Assert.That(run.Advance(), Is.False);
                Assert.That(run.Choose(-1), Is.False);
                Assert.That(run.Choose(2), Is.False);
                Assert.That(run.Current.Id, Is.EqualTo("fork"));
                Assert.That(run.Choose(choice), Is.True);
                Assert.That(run.Current.Id, Is.EqualTo(target));
                Assert.That(run.LastTransition.ChoiceId, Is.EqualTo(choiceId));
                Assert.That(run.LastTransition.FromId, Is.EqualTo("fork"));
                Assert.That(run.LastTransition.Kind, Is.EqualTo(DialogueTransitionKind.ChoiceSelected));
                Assert.That(run.Advance(), Is.True);
                Assert.That(run.Current.Id, Is.EqualTo("join"));
                Assert.That(run.Advance(), Is.True);
                Assert.That(run.IsComplete, Is.True);
                Assert.That(run.LastTransition.Kind, Is.EqualTo(DialogueTransitionKind.Completed));
                Assert.That(run.Advance(), Is.False);
                Assert.That(run.Choose(0), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sequence);
            }
        }

        [Test]
        public void RunsAndReplayDoNotShareProgressOrMutateTheAsset()
        {
            var sequence = BranchingSequence();
            try
            {
                string before = JsonUtility.ToJson(sequence);
                var first = new DialogueRun(sequence.ToGraph());
                var second = new DialogueRun(sequence.ToGraph());
                first.Choose(0);
                second.Choose(1);
                first.Cancel();
                Assert.That(first.LastTransition.Kind, Is.EqualTo(DialogueTransitionKind.Cancelled));
                Assert.That(second.Current.Id, Is.EqualTo("right"));
                Assert.That(new DialogueRun(sequence.ToGraph()).Current.Id, Is.EqualTo("fork"));
                Assert.That(JsonUtility.ToJson(sequence), Is.EqualTo(before));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sequence);
            }
        }

        [TestCase("target")]
        [TestCase("duplicate-line")]
        [TestCase("duplicate-choice")]
        [TestCase("entry")]
        [TestCase("ambiguous-next")]
        public void BrokenGraphIsRejectedBeforeStarting(string defect)
        {
            var sequence = BranchingSequence();
            try
            {
                switch (defect)
                {
                    case "target":
                        SetDefinitionField(sequence.LineAt(0).ChoiceAt(1), "targetId", "missing");
                        break;
                    case "duplicate-line":
                        SetDefinitionField(sequence.LineAt(1), "id", "fork");
                        break;
                    case "duplicate-choice":
                        SetDefinitionField(sequence.LineAt(0).ChoiceAt(1), "id", "take-left");
                        break;
                    case "entry":
                        SetDefinitionField(sequence, "entryId", "missing");
                        break;
                    case "ambiguous-next":
                        SetDefinitionField(sequence.LineAt(0), "nextId", "left");
                        break;
                }

                Assert.Throws<InvalidOperationException>(() => new DialogueRun(sequence.ToGraph()));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sequence);
            }
        }

        [Test]
        public void NestedMotionPauseRestoresScaleOnlyAfterLastOwnerAndPreservesExistingPause()
        {
            var scale = new TestTimeScale { Scale = .35f };
            using (var pause = new WorldMotionPause(scale))
            {
                using (var first = pause.Acquire())
                using (var second = pause.Acquire())
                {
                    first.Dispose();
                    first.Dispose();
                    Assert.That(scale.Scale, Is.Zero);
                    Assert.That(pause.PreservesAudioPlayback, Is.True);
                }

                Assert.That(scale.Scale, Is.EqualTo(.35f));
                scale.Scale = 0;
                using (pause.Acquire())
                {
                    Assert.That(pause.PreservesAudioPlayback, Is.False);
                }

                Assert.That(scale.Scale, Is.Zero);
                Assert.That(pause.IsActive, Is.False);
            }
        }

        [Test]
        public void InputBlocksBelongToTheirPlayer()
        {
            var firstPlayer = new GameplayInputBlock();
            var secondPlayer = new GameplayInputBlock();
            using (firstPlayer.Acquire())
            {
                Assert.That(firstPlayer.IsBlocked, Is.True);
                Assert.That(secondPlayer.IsBlocked, Is.False);
            }
        }

        [Test]
        public void DisposingWorldRestoresScaleAndLateLeasesCannotChangeItAgain()
        {
            var scale = new TestTimeScale { Scale = .5f };
            var pause = new WorldMotionPause(scale);
            IDisposable lease = pause.Acquire();
            pause.Dispose();
            Assert.That(scale.Scale, Is.EqualTo(.5f));
            scale.Scale = .8f;
            lease.Dispose();
            pause.Dispose();
            Assert.That(scale.Scale, Is.EqualTo(.8f));
            Assert.Throws<ObjectDisposedException>(() => pause.Acquire());
        }

        private sealed class TestTimeScale : IWorldTimeScale
        {
            public float Scale { get; set; }
        }

        private static DialogueSequence BranchingSequence()
        {
            var sequence = ScriptableObject.CreateInstance<DialogueSequence>();
            SetDefinitionField(sequence, "speakers", new[] { new DialogueSequence.Speaker(displayName: "Врач") });
            SetDefinitionField(sequence, "entryId", "fork");
            SetDefinitionField(sequence, "lines", new[]
            {
                new DialogueSequence.Line(id: "fork", text: "Куда?", choices: new[]
                    {
                        new DialogueSequence.Choice(id: "take-left", text: "Налево", targetId: "left"),
                        new DialogueSequence.Choice(id: "take-right", text: "Направо", targetId: "right")
                    }),
                new DialogueSequence.Line(id: "left", text: "Левый путь", nextId: "join"),
                new DialogueSequence.Line(id: "right", text: "Правый путь", nextId: "join"),
                new DialogueSequence.Line(id: "join", text: "Пришли")
            });
            return sequence;
        }

        private static void SetDefinitionField(object target, string field, object value) =>
            target.GetType().GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(target, value);
    }
}
