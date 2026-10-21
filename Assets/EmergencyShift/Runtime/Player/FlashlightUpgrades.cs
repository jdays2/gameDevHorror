using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Flashlight2D))]
    /// <summary>Хранит модули игрока в текущем запуске комнаты; пересчитывает оптику от базы.</summary>
    public sealed class FlashlightUpgrades : MonoBehaviour
    {
        [Header("Progression limits — editable balance")]
        [SerializeField, Min(0.5f)]
        private float maximumRange = 7f;
        [SerializeField, Range(20f, 140f)]
        private float maximumAngle = 110f;
        private Flashlight2D flashlight;
        private PlayerLife2D life;
        private readonly Dictionary<string, Vector2> installed = new Dictionary<string, Vector2>();
        private float baseRange;
        private float baseAngle;
        public int InstalledCount
        {
            get; private set;
        }

        private void Awake()
        {
            flashlight = GetComponent<Flashlight2D>();
            life = GetComponent<PlayerLife2D>();
            baseRange = flashlight.Range;
            baseAngle = flashlight.BeamAngle;
        }

        [System.Serializable]
        public sealed class ModuleState
        {
            public string id;
            public Vector2 bonus;
        }

        public ModuleState[] Capture()
        {
            var result = new ModuleState[installed.Count];
            int i = 0;
            foreach (var item in installed)
            {
                result[i++] = new ModuleState
                {
                    id = item.Key,
                    bonus = item.Value
                };
            }

            return result;
        }

        public void Restore(ModuleState[] modules, int count)
        {
            FlashlightModuleValidation.Validate(modules, count);
            installed.Clear();
            if (modules != null)
            {
                foreach (var module in modules)
                {
                    installed.Add(module.id, module.bonus);
                }
            }

            InstalledCount = count;
            float range = baseRange;
            float angle = baseAngle;
            foreach (var bonus in installed.Values)
            {
                range += bonus.x;
                angle += bonus.y;
            }

            flashlight.SetOptics(Mathf.Min(range, Mathf.Max(baseRange, maximumRange)), Mathf.Min(angle, Mathf.Max(baseAngle, maximumAngle)));
        }

        public bool HasModule(string moduleId) => !string.IsNullOrWhiteSpace(moduleId) && installed.ContainsKey(moduleId);
        public bool CanImprove(float rangeBonus, float angleBonus)
        {
            if (flashlight == null || !isActiveAndEnabled || (life != null && life.IsDead) || float.IsNaN(rangeBonus) || float.IsInfinity(rangeBonus) || float.IsNaN(angleBonus) || float.IsInfinity(angleBonus) || rangeBonus < 0 || angleBonus < 0 || (rangeBonus == 0 && angleBonus == 0))
            {
                return false;
            }

            Vector2 optics = CalculateOptics(rangeBonus, angleBonus);
            return !Mathf.Approximately(flashlight.Range, optics.x) || !Mathf.Approximately(flashlight.BeamAngle, optics.y);
        }

        private Vector2 CalculateOptics(float rangeBonus, float angleBonus)
        {
            // Сумма от базы, включая все успешные находки повторяемых модулей.
            float totalRange = baseRange + rangeBonus;
            float totalAngle = baseAngle + angleBonus;
            foreach (Vector2 bonus in installed.Values)
            {
                totalRange += bonus.x;
                totalAngle += bonus.y;
            }

            // Снижение лимита в Inspector не должно превратить следующий апгрейд в ухудшение.
            float range = Mathf.Max(flashlight.Range, Mathf.Min(totalRange, Mathf.Max(baseRange, maximumRange)));
            float angle = Mathf.Max(flashlight.BeamAngle, Mathf.Clamp(totalAngle, 20f, Mathf.Max(baseAngle, maximumAngle)));
            return new Vector2(range, angle);
        }

        public bool TryInstall(string moduleId, string displayName, float rangeBonus, float angleBonus, out string message, bool repeatable = false)
        {
            message = null;
            if (string.IsNullOrWhiteSpace(moduleId) || (!repeatable && HasModule(moduleId)) || !CanImprove(rangeBonus, angleBonus))
            {
                return false;
            }

            float previousRange = flashlight.Range;
            float previousAngle = flashlight.BeamAngle;
            Vector2 optics = CalculateOptics(rangeBonus, angleBonus);
            Vector2 accumulated;
            installed.TryGetValue(moduleId, out accumulated);
            installed[moduleId] = accumulated + new Vector2(rangeBonus, angleBonus);
            InstalledCount++;
            flashlight.SetOptics(optics.x, optics.y);
            string change = !Mathf.Approximately(previousRange, flashlight.Range) ? $"дальность {previousRange:0.#} → {flashlight.Range:0.#}" : "";
            if (!Mathf.Approximately(previousAngle, flashlight.BeamAngle))
            {
                change += (change.Length > 0 ? "; " : "") + $"угол {previousAngle:0.#}° → {flashlight.BeamAngle:0.#}°";
            }

            message = displayName + " · " + change;
            return true;
        }
    }
}
