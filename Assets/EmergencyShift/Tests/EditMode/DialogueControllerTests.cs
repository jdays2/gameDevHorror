using System;
using System.Collections.Generic;
using EmergencyShift.Core.Time;
using NUnit.Framework;
using UnityEngine;

namespace EmergencyShift.Tests
{
    public sealed class DialogueControllerTests
    {
        [Test]
        public void SnapshotOwnsItsDataAndNeitherCollectionCanBeMutated()
        {
            var asset = ScriptableObject.CreateInstance<DialogueSequence>();
            try
            {
                SetDefinitionField(asset, "entryId", "fork");
                SetDefinitionField(asset, "speakers", new[] { new DialogueSequence.Speaker(displayName: "Doctor") });
                SetDefinitionField(asset, "lines", new[]
                {
                    new DialogueSequence.Line(id: "fork", text: "Where?", choices: new[]
                        {
                            new DialogueSequence.Choice(id: "left", text: "Left", targetId: "end")
                        }),
                    new DialogueSequence.Line(id: "end", text: "Done")
                });
                DialogueGraph graph = asset.ToGraph();
                SetDefinitionField(asset, "entryId", "missing");
                SetDefinitionField(asset.LineAt(0), "id", "changed");
                SetDefinitionField(asset.LineAt(0).ChoiceAt(0), "targetId", "missing");
                SetDefinitionField(asset.LineAt(1), "text", "Changed");
                var run = new DialogueRun(graph);
                Assert.That(run.Current.Id, Is.EqualTo("fork"));
                Assert.That(run.Choose(0), Is.True);
                Assert.That(run.Current.Text, Is.EqualTo("Done"));
                Assert.Throws<NotSupportedException>(() => ((IList<DialogueNode>)graph.Nodes).Clear());
                Assert.Throws<NotSupportedException>(() => ((IList<DialogueOption>)graph.Nodes[0].Choices).Clear());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void ExpectedCommandRejectionsDoNotChangeTheTraversal()
        {
            var run = new DialogueRun(Branch());
            Assert.That(new AdvanceDialogueCommand(run).CanExecute().Success, Is.False);
            Assert.That(new ChooseDialogueCommand(run, -1).Execute().Success, Is.False);
            Assert.That(new ChooseDialogueCommand(run, 2).Execute().Success, Is.False);
            Assert.That(run.Current.Id, Is.EqualTo("fork"));
            Assert.That(new ChooseDialogueCommand(run, 0).Execute().Success, Is.True);
            Assert.That(new AdvanceDialogueCommand(run).Execute().Success, Is.True);
            Assert.That(new CancelDialogueCommand(run).Execute().Success, Is.False);
            Assert.That(new AdvanceDialogueCommand(run).Execute().Success, Is.False);
            Assert.That(run.LastTransition.Kind, Is.EqualTo(DialogueTransitionKind.Completed));
        }

        [TestCase(0, "left")]
        [TestCase(1, "right")]
        public void CompletionRestoresMotionButFencesInputUntilAReleasedLaterFrame(int choice, string expected)
        {
            var clock = new TestClock();
            var scale = new TestScale { Scale = .35f };
            var block = new GameplayInputBlock();
            using (var pause = new WorldMotionPause(scale))
            using (var controller = new DialogueController(block, pause, clock))
            {
                Assert.That(controller.TryBegin(Branch()), Is.True);
                Assert.That(controller.Choose(choice), Is.False, "Opening frame is fenced");
                clock.Frame++;
                Assert.That(controller.Choose(choice), Is.True);
                Assert.That(controller.Current.Id, Is.EqualTo(expected));
                Assert.That(controller.Advance(), Is.False, "One transition per frame");
                clock.Frame++;
                Assert.That(controller.Advance(), Is.True);
                Assert.That(controller.IsOpen, Is.False);
                Assert.That(scale.Scale, Is.EqualTo(.35f));
                Assert.That(block.IsBlocked, Is.True);
                Assert.That(controller.TryBegin(Branch()), Is.False);
                clock.Frame++;
                controller.Tick(false);
                Assert.That(block.IsBlocked, Is.True);
                clock.Frame++;
                controller.Tick(true);
                Assert.That(block.IsBlocked, Is.True);
                controller.Tick(false);
                Assert.That(block.IsBlocked, Is.False);
                Assert.That(controller.TryBegin(Branch()), Is.True);
            }
        }

        [Test]
        public void CancelAndDisposeReleaseOnlyOwnedLeasesAndRejectDisposedStart()
        {
            var scale = new TestScale { Scale = .5f };
            var block = new GameplayInputBlock();
            using (var pause = new WorldMotionPause(scale))
            {
                var controller = new DialogueController(block, pause, new TestClock());
                Assert.That(controller.TryBegin(Branch()), Is.True);
                using (pause.Acquire())
                using (block.Acquire())
                {
                    controller.Cancel();
                    controller.Cancel();
                    controller.Dispose();
                    Assert.That(block.IsBlocked, Is.True);
                    Assert.That(scale.Scale, Is.Zero);
                    Assert.That(controller.TryBegin(Branch()), Is.False);
                }

                Assert.That(block.IsBlocked, Is.False);
                Assert.That(scale.Scale, Is.EqualTo(.5f));
            }
        }

        [Test]
        public void AnotherInputOwnerPreventsStartingWithoutAcquiringMotion()
        {
            var block = new GameplayInputBlock();
            var scale = new TestScale { Scale = 1 };
            using (var pause = new WorldMotionPause(scale))
            using (var controller = new DialogueController(block, pause, new TestClock()))
            using (block.Acquire())
            {
                Assert.That(controller.TryBegin(Branch()), Is.False);
                Assert.That(pause.IsActive, Is.False);
                controller.Cancel();
                Assert.That(block.IsBlocked, Is.True);
            }
        }

        [TestCase("acquire")]
        [TestCase("started")]
        [TestCase("advance")]
        [TestCase("cancel")]
        [TestCase("release")]
        public void PresentationFailureCannotLeakInputOrMotion(string stage)
        {
            var block = new GameplayInputBlock();
            var clock = new TestClock();
            var scale = new TestScale { Scale = .6f };
            using (var pause = new WorldMotionPause(scale))
            using (var controller = new DialogueController(block, pause, clock))
            {
                controller.ContextAcquired += () => { if (stage == "acquire") { throw new InvalidOperationException("test"); } };
                controller.Transitioned += step =>
                {
                    if ((stage == "started" && step.Kind == DialogueTransitionKind.Started)
                        || (stage == "advance" && step.Kind == DialogueTransitionKind.ChoiceSelected)
                        || (stage == "cancel" && step.Kind == DialogueTransitionKind.Cancelled))
                    {
                        throw new InvalidOperationException("test");
                    }
                };
                controller.ContextReleased += () => { if (stage == "release") { throw new InvalidOperationException("test"); } };
                Assert.Throws<InvalidOperationException>(() =>
                {
                    controller.TryBegin(Branch());
                    clock.Frame++;
                    if (stage == "advance")
                    {
                        controller.Choose(0);
                    }
                    else
                    {
                        controller.Cancel();
                    }
                });
                Assert.That(block.IsBlocked, Is.False);
                Assert.That(pause.IsActive, Is.False);
                Assert.That(scale.Scale, Is.EqualTo(.6f));
            }
        }

        [Test]
        public void FailedMotionAcquisitionRollsBackTheInputLease()
        {
            var block = new GameplayInputBlock();
            var pause = new WorldMotionPause(new TestScale());
            pause.Dispose();
            using (var controller = new DialogueController(block, pause, new TestClock()))
            {
                Assert.Throws<ObjectDisposedException>(() => controller.TryBegin(Branch()));
                Assert.That(block.IsBlocked, Is.False);
                Assert.That(controller.IsOpen, Is.False);
            }
        }

        private static DialogueGraph Branch()
        {
            return new DialogueGraph("fork", 1, new[]
            {
                new DialogueNode("fork", 0, "Where?", null, new[]
                {
                    new DialogueOption("choose-left", "Left", "left"),
                    new DialogueOption("choose-right", "Right", "right")
                }),
                new DialogueNode("left", 0, "Left", null),
                new DialogueNode("right", 0, "Right", null)
            });
        }

        private sealed class TestClock : IFrameClock
        {
            public int Frame { get; set; }
        }

        private sealed class TestScale : IWorldTimeScale
        {
            public float Scale { get; set; }
        }

        private static void SetDefinitionField(object target, string field, object value) =>
            target.GetType().GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(target, value);
    }
}
