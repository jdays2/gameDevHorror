using System;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Reads existing format1 flags without treating an omitted bool as a saved false.</summary>
    public static class CheckpointBooleanPayload
    {
        [Serializable]
        public sealed class DoorState
        {
            public int format;
            public bool open;
            public bool unlocked;
        }

        [Serializable]
        private sealed class ConsumedState
        {
            public int format;
            public bool consumed;
            public int remaining;
        }

        public static string CaptureDoor(bool open, bool unlocked)
        {
            return JsonUtility.ToJson(new DoorState { format = 1, open = open, unlocked = unlocked });
        }

        public static DoorState ReadDoor(string json)
        {
            DoorState saved = JsonUtility.FromJson<DoorState>(json);
            if (saved == null || saved.format != 1)
            {
                throw new InvalidOperationException("Invalid door checkpoint");
            }

            // FromJsonOverwrite leaves absent fields unchanged. Opposite defaults must converge
            // for every required flag, including an explicitly serialized false in a legacy save.
            var presence = new DoorState { open = true, unlocked = true };
            JsonUtility.FromJsonOverwrite(json, presence);
            if (saved.open != presence.open || saved.unlocked != presence.unlocked)
            {
                throw new InvalidOperationException("Missing door checkpoint flags");
            }

            return saved;
        }

        public static string CaptureConsumed(bool consumed)
        {
            return JsonUtility.ToJson(new ConsumedState { format = 1, consumed = consumed });
        }

        public static bool ReadConsumed(string json)
        {
            ConsumedState saved = JsonUtility.FromJson<ConsumedState>(json);
            if (saved == null || saved.format != 1)
            {
                throw new InvalidOperationException("Invalid saved pickup/encounter");
            }

            var presence = new ConsumedState { consumed = true };
            JsonUtility.FromJsonOverwrite(json, presence);
            if (saved.consumed != presence.consumed)
            {
                throw new InvalidOperationException("Missing pickup/encounter consumed flag");
            }

            return saved.consumed;
        }
    }
}
