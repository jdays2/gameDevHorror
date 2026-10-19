using System;
using EmergencyShift.Features.Health.Config;
using EmergencyShift.Features.Health.Logic;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Instance-owned health. Death is emitted once; no AI, sprite, sound or species dependency.</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHealth2D : MonoBehaviour
    {
        private HealthModel model;
        private HealthModelFactory models;

        public int Current => model?.Current ?? 0;
        public int Maximum => model?.Maximum ?? 0;
        public bool IsAlive => Current > 0;

        public event Action Damaged;
        public event Action Died;

        [Inject]
        public void Construct(HealthModelFactory modelFactory)
        {
            models = modelFactory ?? throw new ArgumentNullException(nameof(modelFactory));
        }

        public void Initialize(int maximum)
        {
            DisconnectModel();
            if (models == null)
            {
                throw new InvalidOperationException("Enemy health requires level composition before initialization.");
            }

            model = models.Create(new HealthSettings(Math.Max(1, maximum), 0));
            model.Damaged += OnDamaged;
            model.Died += OnDied;
        }

        public bool TakeDamage(int amount)
        {
            return model != null && new DamageHealthCommand(model, amount).Execute().Success;
        }

        public void Restore(int current)
        {
            model?.Restore(current);
        }

        public void Kill()
        {
            if (IsAlive)
            {
                TakeDamage(Current);
            }
        }

        private void OnDamaged() => Damaged?.Invoke();
        private void OnDied() => Died?.Invoke();

        private void OnDestroy() => DisconnectModel();

        private void DisconnectModel()
        {
            if (model == null)
            {
                return;
            }

            model.Damaged -= OnDamaged;
            model.Died -= OnDied;
        }
    }
}
