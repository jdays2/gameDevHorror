using System;
using UnityEngine;

namespace EmergencyShift
{
    [DisallowMultipleComponent, RequireComponent(typeof(PersistentEntity2D), typeof(Sleeper2D))]
    public sealed class SleeperCheckpoint2D : MonoBehaviour, ICheckpointParticipant
    {
        [Serializable]
        public sealed class State
        {
            public int format;
            public bool initialized;
            public bool hasActor;
            public bool hasMelee;
            public bool hasRemains;
            public Quaternion visualRotation;
            public Sleeper2D.Snapshot actor;
            public RemainsSnapshot remains;
        }

        public string SaveKey => "sleeper.v1";
        public string CaptureCheckpoint()
        {
            var actor = GetComponent<Sleeper2D>();
            var saved = new State { format = 1 };
            if (actor.Health > 0 || actor.State == Sleeper2D.BehaviourState.Dead)
            {
                saved.initialized = true;
                saved.hasActor = true;
                saved.hasMelee = true;
                saved.actor = actor.CaptureState();
                saved.visualRotation = GetComponent<CreatureSpriteAnimator2D>().VisualRotation;
            }
            saved.remains = GetComponent<GoreBurst2D>().CaptureRemains();
            saved.hasRemains = saved.remains != null;
            return JsonUtility.ToJson(saved);
        }

        public void ValidateCheckpoint(string json)
        {
            var saved = JsonUtility.FromJson<State>(json);
            if (saved == null || saved.format != 1 || (saved.initialized && (!saved.hasActor || !saved.hasMelee || saved.actor == null || saved.actor.melee == null)))
            {
                throw new InvalidOperationException("Missing Sleeper checkpoint payload");
            }
            if (saved.initialized)
            {
                var actor = GetComponent<Sleeper2D>();
                int maximum = actor != null && actor.Definition != null ? actor.Definition.MaximumHealth : int.MaxValue;
                ActorCheckpointValidation.ValidateSleeper(saved.actor, maximum);
                ActorCheckpointValidation.ValidateVisualRotation(saved.visualRotation);
            }
            if (saved.hasRemains)
            {
                if (!saved.initialized || saved.actor.health > 0)
                {
                    throw new InvalidOperationException("Living Sleeper cannot own remains");
                }
                var gore = GetComponent<GoreBurst2D>();
                if (gore == null)
                {
                    throw new InvalidOperationException("Sleeper remains require their native adapter.");
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
            GetComponent<Sleeper2D>().RestoreState(saved.actor);
            GetComponent<CreatureSpriteAnimator2D>().RestoreVisualRotation(saved.visualRotation);
            if (saved.actor.health == 0)
            {
                GetComponent<GoreBurst2D>().RestoreRemains(saved.hasRemains ? saved.remains : null);
            }
        }
    }
}
