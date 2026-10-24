using System;

namespace EmergencyShift.Features.ScriptedEncounters.Config
{
    public sealed class ScriptedRouteSettings
    {
        public float Speed { get; }
        public float ReactionSeconds { get; }
        public float ArrivalDistance { get; }
        public float BlockedSeconds { get; }
        public float ProgressDistance { get; }

        public ScriptedRouteSettings(float speed, float reactionSeconds, float arrivalDistance, float blockedSeconds, float progressDistance)
        {
            if (!Positive(speed) || !float.IsFinite(reactionSeconds) || reactionSeconds < 0 ||
                !Positive(arrivalDistance) || !Positive(blockedSeconds) || !Positive(progressDistance) || progressDistance >= arrivalDistance)
            {
                throw new ArgumentException("Invalid scripted route speed, reaction, arrival or progress settings.");
            }

            Speed = speed;
            ReactionSeconds = reactionSeconds;
            ArrivalDistance = arrivalDistance;
            BlockedSeconds = blockedSeconds;
            ProgressDistance = progressDistance;
        }

        private static bool Positive(float value) => float.IsFinite(value) && value > 0;
    }
}
