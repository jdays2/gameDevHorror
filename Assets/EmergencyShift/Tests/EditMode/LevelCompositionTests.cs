using EmergencyShift.Core.Composition;
using EmergencyShift.Features.PlayerInput.Logic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zenject;

namespace EmergencyShift.Tests
{
    public sealed class LevelCompositionTests
    {
        private const string RigPath = "Assets/EmergencyShift/Prefabs/Levels/LevelRig.prefab";

        [Test]
        public void TwoAuthoredLevelsShareWorldPauseAndOwnIndependentInputLifetimes()
        {
            var world = new DiContainer();
            ZenjectManagersInstaller.Install(world);
            var installerObject = new GameObject("Disposable world installer fixture");
            WorldInstaller installer = installerObject.AddComponent<WorldInstaller>();
            EmergencyShift.Editor.ThreatSignalAuthoring.ConfigureInstaller(installer);
            EmergencyShift.Editor.PlayerRulesAuthoring.ConfigureInstaller(installer);
            EmergencyShift.Editor.WorldVisualRulesAuthoring.ConfigureInstaller(installer);
            world.Inject(installer);
            installer.InstallBindings();
            GameObject firstRig = PrefabUtility.LoadPrefabContents(RigPath);
            GameObject secondRig = PrefabUtility.LoadPrefabContents(RigPath);
            LevelContext2D first = firstRig.GetComponent<LevelContext2D>();
            LevelContext2D second = secondRig.GetComponent<LevelContext2D>();
            try
            {
                first.Initialize(world);
                second.Initialize(world);
                IPlayerInput firstInput = first.Scope.Resolve<IPlayerInput>();
                IPlayerInput secondInput = second.Scope.Resolve<IPlayerInput>();
                Assert.That(firstInput, Is.Not.SameAs(secondInput));
                SceneRegistry<Door2D> firstDoors = first.Scope.Resolve<SceneRegistry<Door2D>>();
                SceneRegistry<Door2D> secondDoors = second.Scope.Resolve<SceneRegistry<Door2D>>();
                Assert.That(firstDoors, Is.Not.SameAs(secondDoors));

                GameplayInputBlock firstBlock = first.Scope.Resolve<GameplayInputBlock>();
                GameplayInputBlock secondBlock = second.Scope.Resolve<GameplayInputBlock>();
                using (firstBlock.Acquire())
                {
                    Assert.That(firstBlock.IsBlocked, Is.True);
                    Assert.That(secondBlock.IsBlocked, Is.False);
                }

                WorldMotionPause worldPause = world.Resolve<WorldMotionPause>();
                Assert.That(first.Scope.Resolve<WorldMotionPause>(), Is.SameAs(worldPause));
                Assert.That(second.Scope.Resolve<WorldMotionPause>(), Is.SameAs(worldPause));
                Assert.Throws<System.ArgumentException>(() => second.InjectScoped(first.Player));
                Assert.Throws<System.ArgumentException>(() => first.CreateInstance(firstRig, Vector3.zero, Quaternion.identity, secondRig.transform));
                first.Dispose();
                first.Dispose();
                Assert.That(second.Scope.Resolve<IPlayerInput>(), Is.SameAs(secondInput));
                Assert.That(second.Scope.Resolve<WorldMotionPause>(), Is.SameAs(worldPause));
                Assert.Throws<System.InvalidOperationException>(() => first.Initialize(world));
            }
            finally
            {
                first.Dispose();
                second.Dispose();
                world.Resolve<DisposableManager>().Dispose();
                world.Resolve<DisposableManager>().LateDispose();
                PrefabUtility.UnloadPrefabContents(firstRig);
                PrefabUtility.UnloadPrefabContents(secondRig);
                Object.DestroyImmediate(installerObject);
            }
        }
    }
}
