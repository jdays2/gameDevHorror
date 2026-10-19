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
                    new DialogueSequence.Line { id = "stop", speaker = 0, text = "Стойте.", nextId = "why" },
                    new DialogueSequence.Line { id = "why", speaker = 1, text = "Почему?" }
                };
                sequence.entryId = "stop";
                Assert.DoesNotThrow(sequence.Validate);
                Assert.That(sequence.speakers[0].portrait, Is.Null);
                Assert.That(sequence.speakers[1].portrait, Is.Null);
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
                Array.Reverse(sequence.lines);
                var run = new DialogueRun(sequence);
                Assert.That(run.Current.id, Is.EqualTo("fork"));
                Assert.That(run.Advance(), Is.False);
                Assert.That(run.Choose(-1), Is.False);
                Assert.That(run.Choose(2), Is.False);
                Assert.That(run.Current.id, Is.EqualTo("fork"));
                Assert.That(run.Choose(choice), Is.True);
                Assert.That(run.Current.id, Is.EqualTo(target));
                Assert.That(run.LastTransition.ChoiceId, Is.EqualTo(choiceId));
                Assert.That(run.LastTransition.FromId, Is.EqualTo("fork"));
                Assert.That(run.LastTransition.Kind, Is.EqualTo(DialogueTransitionKind.ChoiceSelected));
                Assert.That(run.Advance(), Is.True);
                Assert.That(run.Current.id, Is.EqualTo("join"));
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
                var first = new DialogueRun(sequence);
                var second = new DialogueRun(sequence);
                first.Choose(0);
                second.Choose(1);
                first.Cancel();
                Assert.That(first.LastTransition.Kind, Is.EqualTo(DialogueTransitionKind.Cancelled));
                Assert.That(second.Current.id, Is.EqualTo("right"));
                Assert.That(new DialogueRun(sequence).Current.id, Is.EqualTo("fork"));
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
                        sequence.lines[0].choices[1].targetId = "missing";
                        break;
                    case "duplicate-line":
                        sequence.lines[1].id = "fork";
                        break;
                    case "duplicate-choice":
                        sequence.lines[0].choices[1].id = "take-left";
                        break;
                    case "entry":
                        sequence.entryId = "missing";
                        break;
                    case "ambiguous-next":
                        sequence.lines[0].nextId = "left";
                        break;
                }

                Assert.Throws<InvalidOperationException>(() => new DialogueRun(sequence));
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
            sequence.speakers = new[] { new DialogueSequence.Speaker { displayName = "Врач" } };
            sequence.entryId = "fork";
            sequence.lines = new[]
            {
                new DialogueSequence.Line
                {
                    id = "fork", text = "Куда?", choices = new[]
                    {
                        new DialogueSequence.Choice { id = "take-left", text = "Налево", targetId = "left" },
                        new DialogueSequence.Choice { id = "take-right", text = "Направо", targetId = "right" }
                    }
                },
                new DialogueSequence.Line { id = "left", text = "Левый путь", nextId = "join" },
                new DialogueSequence.Line { id = "right", text = "Правый путь", nextId = "join" },
                new DialogueSequence.Line { id = "join", text = "Пришли" }
            };
            return sequence;
        }
    }
}
