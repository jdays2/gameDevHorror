using System;
using EmergencyShift.Features.LevelObjectives.Logic;
using UnityEngine;

namespace EmergencyShift
{
    public sealed class LevelObjectiveController2D : MonoBehaviour, IObjectiveFacts
    {
        [SerializeField]
        private LevelDefinition definition;
        [SerializeField]
        private PlayerAccessCards2D cards;
        [SerializeField]
        private PlayerWeapon2D weapon;
        public int Phase
        {
            get; private set;
        }
        public string Text { get; private set; } = "";

        public event Action Changed;
        bool IObjectiveFacts.HasWeapon => weapon != null && weapon.HasWeapon;
        bool IObjectiveFacts.HasCard(string id) => cards != null && cards.Has(id);
        public void Configure(LevelDefinition data, PlayerAccessCards2D inventory, PlayerWeapon2D equipment)
        {
            definition = data;
            cards = inventory;
            weapon = equipment;
        }

        private void OnEnable()
        {
            if (cards != null)
            {
                cards.Changed += Refresh;
            }

            if (weapon != null)
            {
                weapon.OwnershipChanged += Refresh;
            }
        }

        private void Start()
        {
            Refresh();
        }

        private void OnDisable()
        {
            if (cards != null)
            {
                cards.Changed -= Refresh;
            }

            if (weapon != null)
            {
                weapon.OwnershipChanged -= Refresh;
            }
        }

        public void Refresh()
        {
            if (definition == null || cards == null || weapon == null)
            {
                return;
            }

            definition.Validate();
            for (int index = 0; index < definition.ObjectiveCount; index++)
            {
                LevelObjectiveRule rule = definition.ObjectiveAt(index);
                if (!ObjectiveRuleMatcher.Matches(rule.Condition, rule.CardId, this))
                {
                    continue;
                }

                Phase = rule.Phase;
                Text = rule.Text;
                Changed?.Invoke();
                return;
            }
        }
    }
}
