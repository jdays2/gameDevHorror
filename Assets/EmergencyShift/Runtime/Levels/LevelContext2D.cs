using System;
using System.Collections.Generic;
using EmergencyShift.Core.Composition;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.AccessControl;
using EmergencyShift.Features.Flashlight;
using EmergencyShift.Features.DamageFeedback;
using EmergencyShift.Features.Health;
using EmergencyShift.Features.Enemies;
using EmergencyShift.Features.PlayerInput;
using EmergencyShift.Features.PlayerInput.Logic;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Explicit scene-scoped player binding; no global player singleton or per-frame scene searches.</summary>
    [DefaultExecutionOrder(-10000), DisallowMultipleComponent]
    public sealed class LevelContext2D : MonoInstaller, IDisposable
    {
        [SerializeField]
        private PlayerLife2D player;
        [SerializeField]
        private Flashlight2D flashlight;
        [SerializeField]
        private InputActionAsset controls;
        private DisposableManager lifetime;
        private DisposableManager playerLifetime;
        private bool disposed;
        private ScenePrefabFactory prefabs;
        public PlayerLife2D Player => player;
        public Flashlight2D Flashlight => flashlight;
        internal DiContainer Scope => Container;

        private void Awake()
        {
            Initialize(ProjectContext.Instance.Container);
        }

        public void Initialize(DiContainer worldContainer)
        {
            if (Container != null || disposed)
            {
                throw new InvalidOperationException("A level composition can only initialize once.");
            }

            if (worldContainer == null)
            {
                throw new ArgumentNullException(nameof(worldContainer));
            }

            Container = new DiContainer(worldContainer);
            try
            {
                InstallBindings();
                // Capture the completed local bindings before user Inject callbacks can fail.
                lifetime = Container.Resolve<DisposableManager>();
                foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                {
                    Container.InjectGameObject(root);
                }

                Container.ResolveRoots();
                prefabs = Container.Resolve<ScenePrefabFactory>();
            }
            catch (Exception initializationError)
            {
                try
                {
                    Dispose();
                }
                catch (Exception cleanupError)
                {
                    throw new AggregateException("Level initialization and cleanup both failed.", initializationError, cleanupError);
                }

                throw;
            }
        }

        public override void InstallBindings()
        {
            Container.Bind<IGameClock>().To<UnityGameClock>().AsSingle();
            HealthInstaller.Install(Container);
            DamageFeedbackInstaller.Install(Container);
            LevelRegistriesInstaller.Install(Container);
            Container.Bind<ScenePrefabFactory>().AsSingle().WithArguments(transform);
            Container.Bind<PlayerLife2D>().FromInstance(player);
            Container.Bind<Flashlight2D>().FromInstance(flashlight);
            if (controls == null)
            {
                throw new System.InvalidOperationException("The level requires its authored Input Actions asset.");
            }

            DiContainer playerScope = Container.CreateSubContainer();
            PlayerInputInstaller.Install(playerScope, controls);
            playerLifetime = playerScope.Resolve<DisposableManager>();
            playerScope.ResolveRoots();
            IPlayerInput localInput = playerScope.Resolve<IPlayerInput>();
            GameplayInputBlock localInputBlock = playerScope.Resolve<GameplayInputBlock>();

            // This is the local input selection for UI/adapters, never a global player model.
            Container.Bind<IPlayerInput>().FromInstance(localInput);
            Container.Bind<GameplayInputBlock>().FromInstance(localInputBlock);
            DialogueInstaller.Install(Container);
            WeaponInstaller.Install(Container);
            AccessControlInstaller.Install(Container);
            FlashlightInstaller.Install(Container, flashlight);
            EnemyInstaller.Install(Container);
        }

        private void OnDestroy() => Dispose();

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            var errors = new List<Exception>();
            DisposeManager(playerLifetime, errors);
            DisposeManager(lifetime, errors);
            try
            {
                Container?.UnbindAll();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }

            playerLifetime = null;
            lifetime = null;
            prefabs = null;
            if (errors.Count > 0)
            {
                throw new AggregateException("Errors while closing level composition.", errors);
            }
        }

        public void Configure(PlayerLife2D target, Flashlight2D light, InputActionAsset inputActions)
        {
            if (Container != null || disposed)
            {
                throw new InvalidOperationException("Level references must be configured before initialization.");
            }

            player = target;
            flashlight = light;
            controls = inputActions;
        }

        /// <summary>Composes opt-in probes/controllers; gameplay consumers receive their own contracts.</summary>
        public void InjectScoped(object instance)
        {
            if (disposed || prefabs == null)
            {
                throw new InvalidOperationException("Scoped injection requires an active initialized level.");
            }

            if (instance is Component component && component.gameObject.scene != gameObject.scene)
            {
                throw new ArgumentException("A component must receive dependencies from its own scene.", nameof(instance));
            }

            Container.Inject(instance);
        }

        /// <summary>Explicit factory boundary for opt-in Development probes.</summary>
        public GameObject CreateInstance(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (disposed || prefabs == null)
            {
                throw new InvalidOperationException("An active initialized level owns instance creation.");
            }

            return prefabs.Instantiate(prefab, position, rotation, parent);
        }

        private static void DisposeManager(DisposableManager manager, List<Exception> errors)
        {
            if (manager == null)
            {
                return;
            }

            try
            {
                manager.Dispose();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }

            try
            {
                manager.LateDispose();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }
    }
}
