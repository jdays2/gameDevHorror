using System;
using UnityEngine;

namespace EmergencyShift
{
    [DisallowMultipleComponent, RequireComponent(typeof(PersistentEntity2D))]
    public sealed class StalkerCheckpoint2D : MonoBehaviour, ICheckpointParticipant
    {
        [Serializable]
        public sealed class State
        {
            public int format;
            public bool initialized;
            public bool hasRemains;
            public Quaternion visualRotation;
            public ShadowStalker2D.Snapshot actor;
            public RemainsSnapshot remains;
        }

        public string SaveKey => "stalker.v1";

        public string CaptureCheckpoint()
        {
            // Authored inactive actors have not run Awake. Their state is the pristine spawn.
            var actor = GetComponent<ShadowStalker2D>();
            var state = new State
            {
                format = 1
            };
            if (actor.Health > 0 || actor.State == ShadowStalker2D.BehaviourState.Dead)
            {
                state.initialized = true;
                state.visualRotation = GetComponent<StalkerSpriteAnimator>().VisualRotation;
                state.actor = actor.CaptureState();
            }

            state.remains = GetComponent<GoreBurst2D>().CaptureRemains();
            state.hasRemains = state.remains != null;
            return JsonUtility.ToJson(state);
        }

        public void ValidateCheckpoint(string json)
        {
            var saved = JsonUtility.FromJson<State>(json);
            if (saved == null || saved.format != 1)
            {
                throw new InvalidOperationException("Invalid stalker checkpoint");
            }

            if (saved.initialized)
            {
                var actor = GetComponent<ShadowStalker2D>();
                int maximum = actor != null && actor.Definition != null ? actor.Definition.MaximumHealth : int.MaxValue;
                ActorCheckpointValidation.ValidateStalker(saved.actor, maximum);
                ActorCheckpointValidation.ValidateVisualRotation(saved.visualRotation);
            }

            if (saved.hasRemains)
            {
                if (!saved.initialized || saved.actor.health > 0)
                {
                    throw new InvalidOperationException("Living Stalker cannot own remains.");
                }

                var gore = GetComponent<GoreBurst2D>();
                if (gore == null)
                {
                    throw new InvalidOperationException("Stalker remains require their native adapter.");
                }

                gore.ValidateRemains(saved.remains);
            }
        }

        public void RestoreCheckpoint(string json)
        {
            ValidateCheckpoint(json);
            var saved = JsonUtility.FromJson<State>(json);
            if (!saved.initialized)
            {
                return;
            }

            GetComponent<ShadowStalker2D>().RestoreState(saved.actor);
            GetComponent<StalkerSpriteAnimator>().RestoreVisualRotation(saved.visualRotation);
            if (saved.actor.health == 0)
            {
                GetComponent<GoreBurst2D>().RestoreRemains(saved.hasRemains ? saved.remains : null);
            }
        }
    }
}
