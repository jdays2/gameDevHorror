using System;
using EmergencyShift.Features.Flashlight;
using EmergencyShift.Features.Flashlight.Config;
using EmergencyShift.Features.Flashlight.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Unity optics and compatibility snapshot adapter for personal upgrade progression.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Flashlight2D))]
    public sealed class FlashlightUpgrades : MonoBehaviour, IFlashlightUpgradeEnvironment
    {
        [Header("Progression limits — editable balance")]
        [SerializeField, Min(.5f)]
        private float maximumRange = 7f;
        [SerializeField, Range(20f, 140f)]
        private float maximumAngle = 110f;
        private Flashlight2D flashlight;
        private PlayerLife2D life;
        private FlashlightControllerFactory factory;
        private FlashlightUpgradeController controller;

        public int InstalledCount => RequireController().InstalledCount;
        bool IFlashlightUpgradeEnvironment.CanUpgrade => flashlight != null && isActiveAndEnabled && (life == null || !life.IsDead);
        Vector2 IFlashlightUpgradeEnvironment.Optics => new Vector2(flashlight.Range, flashlight.BeamAngle);
        FlashlightUpgradeLimits IFlashlightUpgradeEnvironment.Limits => new FlashlightUpgradeLimits(maximumRange, maximumAngle);

        // Retains the existing checkpoint JSON and public authoring API.
        [Serializable]
        public sealed class ModuleState
        {
            public string id;
            public Vector2 bonus;
        }

        [Inject]
        public void Construct(FlashlightControllerFactory controllerFactory)
        {
            factory = controllerFactory ?? throw new ArgumentNullException(nameof(controllerFactory));
        }

        private void Awake() => RequireController();

        public ModuleState[] Capture()
        {
            FlashlightModule[] modules = RequireController().Capture();
            var result = new ModuleState[modules.Length];
            for (int index = 0; index < modules.Length; index++)
            {
                result[index] = new ModuleState { id = modules[index].Id, bonus = modules[index].Bonus };
            }

            return result;
        }

        public void Restore(ModuleState[] modules, int count)
        {
            var result = RequireController().Restore(FlashlightModuleValidation.ToModules(modules), count);
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Reason);
            }
        }

        public bool HasModule(string moduleId) => RequireController().HasModule(moduleId);

        public bool CanImprove(float rangeBonus, float angleBonus) => RequireController().CanImprove(new Vector2(rangeBonus, angleBonus));

        public bool TryInstall(string moduleId, string displayName, float rangeBonus, float angleBonus,
            out string message, bool repeatable = false)
        {
            message = null;
            FlashlightUpgradeController upgrades = RequireController();
            float previousRange = flashlight.Range;
            float previousAngle = flashlight.BeamAngle;
            if (!upgrades.TryInstall(moduleId, new Vector2(rangeBonus, angleBonus), repeatable).Success)
            {
                return false;
            }

            string change = !Mathf.Approximately(previousRange, flashlight.Range)
                ? $"дальность {previousRange:0.#} → {flashlight.Range:0.#}" : "";
            if (!Mathf.Approximately(previousAngle, flashlight.BeamAngle))
            {
                change += (change.Length > 0 ? "; " : "") + $"угол {previousAngle:0.#}° → {flashlight.BeamAngle:0.#}°";
            }

            message = displayName + " · " + change;
            return true;
        }

        void IFlashlightUpgradeEnvironment.ApplyOptics(Vector2 optics) => flashlight.SetOptics(optics.x, optics.y);

        private FlashlightUpgradeController RequireController()
        {
            if (factory == null)
            {
                throw new InvalidOperationException("Flashlight upgrades require level composition.");
            }

            if (controller == null)
            {
                flashlight = GetComponent<Flashlight2D>();
                life = GetComponent<PlayerLife2D>();
                controller = factory.CreateUpgrades(new Vector2(flashlight.Range, flashlight.BeamAngle), this);
            }

            return controller;
        }
    }
}