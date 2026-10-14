using System.Collections.Generic;
using UnityEngine;

namespace EmergencyShift
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Flashlight2D))]
    /// <summary>Хранит модули игрока в текущем запуске комнаты; пересчитывает оптику от базы.</summary>
    public sealed class FlashlightUpgrades : MonoBehaviour
    {
        private Flashlight2D flashlight;
        private PlayerLife2D life;
        private readonly Dictionary<string, Vector2> installed = new Dictionary<string, Vector2>();
        private float baseRange;
        private float baseAngle;
        public int InstalledCount => installed.Count;

        private void Awake()
        {
            flashlight = GetComponent<Flashlight2D>();
            life = GetComponent<PlayerLife2D>();
            baseRange = flashlight.Range;
            baseAngle = flashlight.BeamAngle;
        }

        public bool HasModule(string moduleId) => !string.IsNullOrWhiteSpace(moduleId) && installed.ContainsKey(moduleId);

        public bool TryInstall(string moduleId, string displayName, float rangeBonus, float angleBonus, out string message)
        {
            message = null;
            if (flashlight == null || !isActiveAndEnabled || (life != null && life.IsDead)
                || string.IsNullOrWhiteSpace(moduleId) || HasModule(moduleId)
                || float.IsNaN(rangeBonus) || float.IsInfinity(rangeBonus)
                || float.IsNaN(angleBonus) || float.IsInfinity(angleBonus)
                || rangeBonus < 0 || angleBonus < 0 || (rangeBonus == 0 && angleBonus == 0)) return false;

            float previousRange = flashlight.Range;
            float previousAngle = flashlight.BeamAngle;
            // Все бонусы вычисляются заново от базы; повторный ID не накапливает прибавки.
            float totalRange = baseRange + rangeBonus;
            float totalAngle = baseAngle + angleBonus;
            foreach (Vector2 bonus in installed.Values) { totalRange += bonus.x; totalAngle += bonus.y; }
            if (Mathf.Approximately(previousRange, totalRange)
                && Mathf.Approximately(previousAngle, Mathf.Clamp(totalAngle, 20f, 140f))) return false;

            installed.Add(moduleId, new Vector2(rangeBonus, angleBonus));
            flashlight.SetOptics(totalRange, totalAngle);
            string change = rangeBonus > 0 ? $"дальность {previousRange:0.#} → {flashlight.Range:0.#}" : "";
            if (angleBonus > 0)
                change += (change.Length > 0 ? "; " : "") + $"угол {previousAngle:0.#}° → {flashlight.BeamAngle:0.#}°";
            message = displayName + " установлена · " + change;
            return true;
        }
    }
}
