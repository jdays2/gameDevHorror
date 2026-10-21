using System;
using System.Reflection;
using EmergencyShift.Core.Composition;
using EmergencyShift.Features.PlayerInput.Logic;
using EmergencyShift.Features.PlayerInput.View;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;
using Object = UnityEngine.Object;

namespace EmergencyShift.Tests
{
    public sealed class LevelCompositionFailureTests
    {
        private const string RigPath = "Assets/EmergencyShift/Prefabs/Levels/LevelRig.prefab";
        private DiContainer world;
        private GameObject worldOwner;
        private GameObject rig;
        private GameObject siblingRig;
        private LevelContext2D context;
        private LevelContext2D sibling;
        private Component probe;

        [SetUp]
        public void Setup()
        {
            world = new DiContainer();
            ZenjectManagersInstaller.Install(world);
            worldOwner = new GameObject("Isolated composition world");
            var installer = worldOwner.AddComponent<WorldInstaller>();
            EmergencyShift.Editor.ThreatSignalAuthoring.ConfigureInstaller(installer);
            world.Inject(installer);
            installer.InstallBindings();
            rig = PrefabUtility.LoadPrefabContents(RigPath);
            context = rig.GetComponent<LevelContext2D>();
            var doorOwner = new GameObject("Partial registry member");
            doorOwner.SetActive(false);
            doorOwner.transform.SetParent(rig.transform, false);
            var leaf = new GameObject("Leaf");
            leaf.transform.SetParent(doorOwner.transform, false);
            var door = doorOwner.AddComponent<Door2D>();
            door.Configure(leaf.transform);
            Type probeType = Type.GetType("EmergencyShift.CompositionFailureProbe, EmergencyShift.Development", true);
            probe = rig.AddComponent(probeType);
            probeType.GetProperty("Door").SetValue(probe, door);
        }

        [TearDown]
        public void Cleanup()
        {
            context?.Dispose();
            sibling?.Dispose();
            // Also clean the deliberately broken baseline before the fix.
            Captured<SceneRegistry<Door2D>>("CapturedRegistry")?.Dispose();
            if (rig != null)
            {
                PrefabUtility.UnloadPrefabContents(rig);
            }

            if (siblingRig != null)
            {
                PrefabUtility.UnloadPrefabContents(siblingRig);
            }

            world.Resolve<DisposableManager>().Dispose();
            world.Resolve<DisposableManager>().LateDispose();
            Object.DestroyImmediate(worldOwner);
        }

        [Test]
        public void FailedInjectionImmediatelyReleasesItsOwnedInputActions()
        {
            FailComposition();
            IPlayerInput input = Captured<IPlayerInput>("CapturedInput");
            Assert.That(input, Is.Not.Null);
            Assert.That(Actions(input) == null, Is.True,
                "A failed level must not keep an enabled native InputActionAsset until its GameObject is destroyed.");
        }

        [Test]
        public void FailedInjectionClosesPartiallyPopulatedRegistriesAndRejectsMoreComposition()
        {
            FailComposition();
            SceneRegistry<Door2D> registry = Captured<SceneRegistry<Door2D>>("CapturedRegistry");
            Assert.That(registry, Is.Not.Null);
            Assert.That(registry.Members, Is.Empty);
            Assert.Throws<ObjectDisposedException>(() => registry.Register(Captured<Door2D>("Door")));
            Assert.Throws<InvalidOperationException>(() => context.InjectScoped(new object()));
            Assert.Throws<InvalidOperationException>(() => context.CreateInstance(rig, Vector3.zero, Quaternion.identity));
            Assert.Throws<InvalidOperationException>(() => context.Initialize(world));
            Assert.DoesNotThrow(() => context.Dispose());
            Assert.DoesNotThrow(() => context.Dispose());
        }

        [Test]
        public void FailedLevelDoesNotDisposeASiblingOrTheSharedWorld()
        {
            siblingRig = PrefabUtility.LoadPrefabContents(RigPath);
            sibling = siblingRig.GetComponent<LevelContext2D>();
            sibling.Initialize(world);
            IPlayerInput input = sibling.Scope.Resolve<IPlayerInput>();
            InputActionAsset actions = Actions(input);
            FailComposition();
            Assert.That(actions != null && actions.enabled, Is.True);
            Assert.That(sibling.Scope.Resolve<IPlayerInput>(), Is.SameAs(input));
            var pause = world.Resolve<WorldMotionPause>();
            float previousScale = Time.timeScale;
            using (pause.Acquire())
            {
                Assert.That(Time.timeScale, Is.Zero);
            }

            Assert.That(Time.timeScale, Is.EqualTo(previousScale));
        }

        [Test]
        public void InitializationAndCleanupErrorsArePreservedAndLateCleanupStillRuns()
        {
            var late = new LateCleanup();
            world.Bind<IDisposable>().FromInstance(new ThrowingCleanup()).MoveIntoDirectSubContainers();
            world.Bind<ILateDisposable>().FromInstance(late).MoveIntoDirectSubContainers();
            Exception failure = Assert.Catch(() => context.Initialize(world));
            Assert.That(failure, Is.TypeOf<AggregateException>());
            Assert.That(failure.ToString(), Does.Contain("Expected isolated level injection failure."));
            Assert.That(failure.ToString(), Does.Contain("Expected isolated disposal failure."));
            Assert.That(late.Calls, Is.EqualTo(1));
            Assert.That(Actions(Captured<IPlayerInput>("CapturedInput")) == null, Is.True);
            Assert.DoesNotThrow(() => context.Dispose());
        }

        [Test]
        public void LiveAndDisposedLevelsCannotReplaceAlreadyBoundReferences()
        {
            siblingRig = PrefabUtility.LoadPrefabContents(RigPath);
            sibling = siblingRig.GetComponent<LevelContext2D>();
            sibling.Initialize(world);
            InputActionAsset actions = Actions(sibling.Scope.Resolve<IPlayerInput>());
            Assert.Throws<InvalidOperationException>(() => sibling.Configure(context.Player, context.Flashlight, actions));
            Assert.That(sibling.Scope.Resolve<PlayerLife2D>(), Is.SameAs(sibling.Player));
            sibling.Dispose();
            Assert.Throws<InvalidOperationException>(() => sibling.Configure(context.Player, context.Flashlight, actions));
        }

        private void FailComposition()
        {
            Exception failure = Assert.Catch(() => context.Initialize(world));
            Assert.That(failure.ToString(), Does.Contain("Expected isolated level injection failure."));
        }

        private static InputActionAsset Actions(IPlayerInput input) =>
            (InputActionAsset)typeof(InputActionPlayerInput).GetField("actions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(input);

        private T Captured<T>(string property) where T : class => probe == null
            ? null : (T)probe.GetType().GetProperty(property).GetValue(probe);

        private sealed class ThrowingCleanup : IDisposable
        {
            public void Dispose() => throw new InvalidOperationException("Expected isolated disposal failure.");
        }

        private sealed class LateCleanup : ILateDisposable
        {
            public int Calls { get; private set; }
            public void LateDispose() => Calls++;
        }
    }
}
