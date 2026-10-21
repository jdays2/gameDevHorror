using System;
using System.Collections.Generic;
using EmergencyShift.Core.Time;
using EmergencyShift.Features.Interaction.Config;

namespace EmergencyShift.Features.Interaction.Logic
{
    public sealed class PickupNoticeModel
    {
        private readonly PlayerInteractionSettings settings;
        private readonly IGameClock clock;
        private readonly Queue<string> pending;
        private string current;
        private double until;

        public PickupNoticeModel(PlayerInteractionSettings settings, IGameClock clock)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            pending = new Queue<string>(settings.MaximumNotices);
        }

        public string Current => ValidClock && clock.Time < until ? current : null;
        public int PendingCount => pending.Count;
        internal bool ValidClock => double.IsFinite(clock.Time) && clock.Time >= 0;

        public void Tick()
        {
            if (ValidClock && clock.Time >= until && pending.Count > 0)
            {
                Start(pending.Dequeue());
            }
        }

        public void Clear()
        {
            current = null;
            until = 0;
            pending.Clear();
        }

        internal void Show(string message)
        {
            if (clock.Time >= until && pending.Count == 0)
            {
                Start(message);
                return;
            }

            if (pending.Count >= settings.MaximumNotices)
            {
                pending.Dequeue();
            }

            pending.Enqueue(message);
        }

        private void Start(string message)
        {
            current = message;
            until = clock.Time + settings.NoticeSeconds;
        }
    }
}
