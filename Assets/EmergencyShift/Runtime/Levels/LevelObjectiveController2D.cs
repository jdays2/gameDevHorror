using System;
using EmergencyShift.Features.LevelObjectives.Config;
using EmergencyShift.Features.LevelObjectives.Logic;
using EmergencyShift.Features.LevelProgress;
using UnityEngine;
using Zenject;

namespace EmergencyShift
{
    public sealed class LevelObjectiveController2D : MonoBehaviour, IObjectiveFacts, IObjectiveProgressView
    {
        [SerializeField]
        private LevelDefinition definition;
        [SerializeField]
        private PlayerAccessCards2D cards;
        [SerializeField]
        private PlayerWeapon2D weapon;
        private PlayerAccessCards2D subscribedCards;
        private PlayerWeapon2D subscribedWeapon;
        private LevelProgressFactory factory;
        private ObjectiveProgressController controller;
        private ObjectiveRuleSet rules;

        public int Phase => controller?.Phase ?? 0;
        public string Text => controller?.Text ?? "";
        public event Action Changed;
        bool IObjectiveFacts.HasWeapon => weapon != null && weapon.HasWeapon;
        bool IObjectiveFacts.HasCard(string id) => cards != null && cards.Has(id);

        [Inject]
        public void Construct(LevelProgressFactory progressFactory)
        {
            if (factory != null)
            {
                throw new InvalidOperationException("Objectives are already composed.");
            }

            factory = progressFactory ?? throw new ArgumentNullException(nameof(progressFactory));
        }

        private void OnEnable()
        {
            ApplyRules(LevelProgressFactory.Snapshot(definition));
            Subscribe();
            if (factory != null)
            {
                Refresh();
            }
        }

        private void Start() => Refresh();
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

        public void Configure(LevelDefinition data, PlayerAccessCards2D inventory, PlayerWeapon2D equipment)
        {
            ObjectiveRuleSet replacement = LevelProgressFactory.Snapshot(data);
            Unsubscribe();
            definition = data;
            cards = inventory;
            weapon = equipment;
            ApplyRules(replacement);
            if (isActiveAndEnabled)
            {
                Subscribe();
                if (factory != null)
                {
                    Refresh();
                }
            }
        }

        public void Refresh()
        {
            if (definition == null || cards == null || weapon == null)
            {
                return;
            }

            if (factory == null)
            {
                throw new InvalidOperationException("Objectives require their level composition.");
            }

            controller ??= factory.CreateObjectives(rules ?? LevelProgressFactory.Snapshot(definition), this);
            controller.Refresh(this);
        }

        void IObjectiveProgressView.ObjectivesRefreshed() => Changed?.Invoke();

        private void ApplyRules(ObjectiveRuleSet replacement)
        {
            rules = replacement;
            controller?.Configure(replacement);
        }

        private void Subscribe()
        {
            Unsubscribe();
            subscribedCards = cards;
            subscribedWeapon = weapon;
            if (subscribedCards != null)
            {
                subscribedCards.Changed += Refresh;
            }

            if (subscribedWeapon != null)
            {
                subscribedWeapon.OwnershipChanged += Refresh;
            }
        }

        private void Unsubscribe()
        {
            if (subscribedCards != null)
            {
                subscribedCards.Changed -= Refresh;
            }

            if (subscribedWeapon != null)
            {
                subscribedWeapon.OwnershipChanged -= Refresh;
            }

            subscribedCards = null;
            subscribedWeapon = null;
        }
    }
}
