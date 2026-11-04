using System;
using EmergencyShift.Features.Scorpion.Logic;
using UnityEngine;

namespace EmergencyShift.Features.Scorpion.View
{
    [DefaultExecutionOrder(10)]
    public sealed class ScorpionPack2D : MonoBehaviour
    {
        [SerializeField] private Scorpion2D[] members;
        [SerializeField] private Door2D[] entranceDoors;
        private readonly ScorpionPackModel model = new ScorpionPackModel();
        public bool IsAlerted => model.IsAlerted;
        public int AlarmCount { get; private set; }
        public Scorpion2D[] Members => members;

        private void Awake()
        {
            foreach (Scorpion2D member in members)
            {
                if (member == null || !member.transform.IsChildOf(transform))
                {
                    throw new InvalidOperationException("Room pack must explicitly own all its scorpions.");
                }
                member.BindPack(this);
            }
        }

        private void OnEnable()
        {
            foreach (Door2D door in entranceDoors)
            {
                door.MotionStarted += OnDoor;
            }
            if (model.IsAlerted)
            {
                AlertMembers();
            }
        }

        private void OnDisable()
        {
            foreach (Door2D door in entranceDoors)
            {
                if (door != null)
                {
                    door.MotionStarted -= OnDoor;
                }
            }
        }

        public void Alert(Scorpion2D witness)
        {
            if (!isActiveAndEnabled || !new AlertScorpionPackCommand(model).Execute().Success)
            {
                return;
            }
            AlertMembers();
            Scorpion2D caller = witness;
            if (caller == null || !caller.IsAlive)
            {
                foreach (Scorpion2D member in members)
                {
                    if (member != null && member.IsAlive)
                    {
                        caller = member;
                        break;
                    }
                }
            }
            if (caller != null)
            {
                caller.EmitAlarmCue();
                AlarmCount++;
            }
        }

        private void AlertMembers()
        {
            foreach (Scorpion2D member in members)
            {
                if (member != null && member.IsAlive)
                {
                    member.Alert();
                }
            }
        }

        private void OnDoor(bool opening)
        {
            if (opening)
            {
                Alert(null);
            }
        }
    }
}
