using System;
using System.IO;
using EmergencyShift.Core.Composition;
using EmergencyShift.Features.CheckpointSession;
using EmergencyShift.Features.CheckpointSession.Logic;
using NUnit.Framework;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Tests
{
    public sealed class CheckpointSessionTests
    {
        [Test]
        public void RequestCancelSaveAndClearHaveIndependentExplicitTransitions()
        {
            var controller = new CheckpointSessionFactory().Create();
            Assert.That(controller.Request("first", false).Success, Is.False);
            Assert.That(controller.Request("first", true).Success, Is.True);
            Assert.That(controller.LastCheckpointId, Is.Null);
            controller.Cancel();
            Assert.That(controller.IsSaving, Is.False);
            Assert.That(controller.Request("first", true).Success, Is.True);
            Assert.That(controller.RecordSaved("first").Success, Is.True);
            Assert.That(controller.IsSaving, Is.False);
            Assert.That(controller.LastCheckpointId, Is.EqualTo("first"));
            Assert.That(controller.Request("first", true).Success, Is.False);
            Assert.That(controller.Request("second", true).Success, Is.True);
            controller.Cancel();
            Assert.That(controller.LastCheckpointId, Is.EqualTo("first"));
            Assert.That(controller.CaptureVisits("second"), Is.EquivalentTo(new[] { "first", "second" }));
            controller.Clear();
            Assert.That(controller.LastCheckpointId, Is.Null);
            Assert.That(controller.Request("first", true).Success, Is.True);
        }

        [Test]
        public void InvalidCommandsKeepTheExistingRequestAndHistory()
        {
            var controller = new CheckpointSessionFactory().Create();
            controller.RecordSaved("old");
            controller.Request("pending", true);
            Assert.That(controller.Request(" ", true).Success, Is.False);
            Assert.That(controller.RecordSaved(null).Success, Is.False);
            Assert.That(controller.Restore(null).Success, Is.False);
            Assert.That(controller.IsSaving, Is.True);
            Assert.That(controller.LastCheckpointId, Is.EqualTo("old"));
        }

        [Test]
        public void SnapshotAndCaptureArraysDoNotExposePersonalHistory()
        {
            var controller = new CheckpointSessionFactory().Create();
            string[] original = { "old" };
            var snapshot = new CheckpointVisitSnapshot("current", original);
            original[0] = "mutated";
            controller.Restore(snapshot);
            string[] capture = controller.CaptureVisits("current");
            capture[0] = "mutated again";
            Assert.That(controller.CaptureVisits("next"), Is.EquivalentTo(new[] { "old", "current", "next" }));
            Assert.That(controller.IsSaving, Is.False);
        }

        [Test]
        public void LegacyNullVisitsRestoreCurrentCheckpointAndSeparateSessionsStayIndependent()
        {
            var factory = new CheckpointSessionFactory();
            var first = factory.Create();
            var second = factory.Create();
            first.Restore(new CheckpointVisitSnapshot("legacy", null));
            Assert.That(first.Request("legacy", true).Success, Is.False);
            Assert.That(second.Request("legacy", true).Success, Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void InvalidVisitIdsAreRejectedBeforeSessionMutation(string invalid)
        {
            Assert.Throws<ArgumentException>(() => new CheckpointVisitSnapshot("valid", new[] { invalid }));
        }

        [Test]
        public void DuplicateVisitIdsAreRejectedAndOrdinalCaseIsPreserved()
        {
            Assert.Throws<ArgumentException>(() => new CheckpointVisitSnapshot("valid", new[] { "a", "a" }));
            Assert.DoesNotThrow(() => new CheckpointVisitSnapshot("valid", new[] { "a", "A" }));
        }

        [Test]
        public void PathOverridesAreBoundedAndNormalizeWithoutTouchingDisk()
        {
            using var registry = new CheckpointPathRegistry();
            string directory = Path.GetTempPath();
            string selected = Path.Combine(directory, "session-override.json");
            registry.Override("first", selected);
            Assert.That(registry.Resolve("first", directory), Is.EqualTo(Path.GetFullPath(selected)));
            for (int index = 1; index < 128; index++)
            {
                registry.Override("level-" + index, selected);
            }

            Assert.Throws<InvalidOperationException>(() => registry.Override("overflow", selected));
            Assert.DoesNotThrow(() => registry.Override("first", selected));
        }

        [TestCase("")]
        [TestCase("../other")]
        [TestCase("folder/level")]
        public void PathRegistryRejectsInvalidLevelTokens(string id)
        {
            using var registry = new CheckpointPathRegistry();
            Assert.Throws<ArgumentException>(() => registry.Resolve(id, Path.GetTempPath()));
        }

        [Test]
        public void ActualWorldInstallerOwnsOverridesAcrossChildrenAndClosesThemWithItsLifetime()
        {
            var firstOwner = new GameObject("First world checkpoint paths");
            var secondOwner = new GameObject("Second world checkpoint paths");
            DiContainer first = World(firstOwner);
            DiContainer second = World(secondOwner);
            bool firstClosed = false;
            try
            {
                var paths = first.Resolve<CheckpointPathRegistry>();
                var child = new DiContainer(first);
                var reloaded = new DiContainer(first);
                Assert.That(child.Resolve<CheckpointPathRegistry>(), Is.SameAs(paths));
                Assert.That(reloaded.Resolve<CheckpointPathRegistry>(), Is.SameAs(paths));
                var independent = second.Resolve<CheckpointPathRegistry>();
                Assert.That(independent, Is.Not.SameAs(paths));
                string directory = Path.GetTempPath();
                string selected = Path.Combine(directory, "isolated-override.json");
                paths.Override("same-level", selected);
                Assert.That(reloaded.Resolve<CheckpointPathRegistry>().Resolve("same-level", directory), Is.EqualTo(selected));
                Assert.That(independent.Resolve("same-level", directory), Is.EqualTo(Path.Combine(directory, "same-level.json")));
                first.Resolve<DisposableManager>().Dispose();
                first.Resolve<DisposableManager>().LateDispose();
                firstClosed = true;
                Assert.Throws<ObjectDisposedException>(() => paths.Resolve("same-level", directory));
                Assert.DoesNotThrow(() => independent.Resolve("same-level", directory));
            }
            finally
            {
                if (!firstClosed)
                {
                    first.Resolve<DisposableManager>().Dispose();
                    first.Resolve<DisposableManager>().LateDispose();
                }

                second.Resolve<DisposableManager>().Dispose();
                second.Resolve<DisposableManager>().LateDispose();
                UnityEngine.Object.DestroyImmediate(firstOwner);
                UnityEngine.Object.DestroyImmediate(secondOwner);
            }
        }

        private static DiContainer World(GameObject owner)
        {
            var scope = new DiContainer();
            ZenjectManagersInstaller.Install(scope);
            var installer = owner.AddComponent<WorldInstaller>();
            EmergencyShift.Editor.ThreatSignalAuthoring.ConfigureInstaller(installer);
            EmergencyShift.Editor.PlayerRulesAuthoring.ConfigureInstaller(installer);
            EmergencyShift.Editor.WorldVisualRulesAuthoring.ConfigureInstaller(installer);
            scope.Inject(installer);
            installer.InstallBindings();
            return scope;
        }
    }
}
