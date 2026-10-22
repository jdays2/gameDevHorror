using System;
using EmergencyShift.Core.Commands;
using EmergencyShift.Core.Time;

namespace EmergencyShift.Features.LevelProgress.Logic
{
    public sealed class LevelRunModel
    {
        private readonly IGameClock clock;
        private double startedAt;
        private float restoredElapsed;
        private bool started;
        private bool restarting;
        public bool Completed { get; private set; }
        public bool CanRestart => Completed && !restarting;
        public float ElapsedSeconds => restoredElapsed + (started ? (float)(ReadTime() - startedAt) : 0);

        public LevelRunModel(IGameClock clock)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        internal void Start()
        {
            if (started)
            {
                return;
            }

            startedAt = ReadTime();
            started = true;
        }

        internal void RestoreElapsed(float seconds)
        {
            double now = ReadTime();
            restoredElapsed = Math.Max(0, seconds);
            startedAt = now;
            started = true;
        }

        internal CommandResult CanFinish(bool ownsActor, bool alive, bool hasExitAccess)
        {
            if (Completed || !ownsActor || !alive || !hasExitAccess)
            {
                return CommandResult.Fail("Only the living run owner with exit access may finish once.");
            }

            return CommandResult.Ok();
        }

        internal void Finish() => Completed = true;
        internal void BeginRestart() => restarting = true;
        internal void EndRestart() => restarting = false;

        private double ReadTime()
        {
            double now = clock.Time;
            if (!double.IsFinite(now) || now < 0)
            {
                throw new InvalidOperationException("The run requires finite nonnegative scaled time.");
            }

            return now;
        }
    }
}
