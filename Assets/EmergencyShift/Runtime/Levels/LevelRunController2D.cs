using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace EmergencyShift
{
    /// <summary>Owns completion and run lifetime. Objectives and UI are separate components.</summary>
    public sealed class LevelRunController2D : MonoBehaviour
    {
        [SerializeField]
        private PlayerLife2D player;
        [SerializeField]
        private PlayerAccessCards2D cards;
        [SerializeField]
        private LevelDefinition definition;
        [SerializeField]
        private LevelObjectiveController2D objectives;
        public event Action Finished;
        private float startedAt;
        private float restoredElapsed;
        public bool Completed
        {
            get; private set;
        }
        public int Phase => objectives != null ? objectives.Phase : 0;
        public float ElapsedSeconds => restoredElapsed + Time.time - startedAt;
        public PlayerLife2D Player => player;
        public LevelDefinition Definition => definition;

        public void SetDefinition(LevelDefinition data, LevelObjectiveController2D goals)
        {
            definition = data;
            objectives = goals;
        }

        public void Configure(PlayerLife2D life, PlayerAccessCards2D inventory)
        {
            player = life;
            cards = inventory;
        }

        private void Start()
        {
            startedAt = Time.time;
        }

        private void Update()
        {
            if (Completed && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                Restart();
            }
        }

        public void RestoreElapsed(float seconds)
        {
            restoredElapsed = Mathf.Max(0, seconds);
            startedAt = Time.time;
        }

        public bool TryFinish(PlayerLife2D actor)
        {
            if (Completed || actor != player || player.IsDead || definition == null || !cards.Has(definition.exitCardId))
            {
                return false;
            }

            Completed = true;
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var participant in root.GetComponentsInChildren<LevelParticipant2D>(true))
                {
                    participant.StopActivity();
                }
            }

            Finished?.Invoke();
            return true;
        }

        public void Restart()
        {
            if (!Completed)
            {
                return;
            }

            var checkpoint = GetComponent<LevelCheckpointController2D>();
            if (checkpoint != null)
            {
                checkpoint.Clear();
            }

            SceneManager.LoadScene(gameObject.scene.buildIndex);
        }
    }
}
