using System;
using EmergencyShift.Features.Enemies.Logic;
using Unity.Profiling;

namespace EmergencyShift.Features.Enemies.View
{
    /// <summary>Keeps the existing build-only recorder outside the solver.</summary>
    public sealed class ProfiledEnemyPathSchedule2D : IEnemyPathSchedule
    {
        private static readonly ProfilerMarker marker = new ProfilerMarker("EmergencyShift.Navigation.Build");
        private readonly IEnemyPathSchedule schedule;
        public ProfiledEnemyPathSchedule2D(IEnemyPathSchedule schedule) => this.schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        public bool TryBeginBuild()
        {
            if (!schedule.TryBeginBuild())
            {
                return false;
            }

            marker.Begin();
            return true;
        }

        public void CompleteBuild()
        {
            try
            {
                schedule.CompleteBuild();
            }
            finally
            {
                marker.End();
            }
        }

        public void Invalidate() => schedule.Invalidate();
    }
}
