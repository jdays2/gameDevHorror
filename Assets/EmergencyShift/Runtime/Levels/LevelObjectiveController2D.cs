using System;
using UnityEngine;

namespace EmergencyShift
{
    public sealed class LevelObjectiveController2D : MonoBehaviour
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

            foreach (var rule in definition.objectives)
            {
                if (!rule.Matches(cards, weapon))
                {
                    continue;
                }

                Phase = rule.phase;
                Text = rule.text;
                Changed?.Invoke();
                return;
            }
        }
    }
}
