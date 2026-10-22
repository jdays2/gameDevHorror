using System;
using EmergencyShift.Features.LevelProgress;
using EmergencyShift.Features.LevelProgress.Logic;
using EmergencyShift.Features.PlayerInput.Logic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace EmergencyShift
{
    /// <summary>Unity adapter for run commands, local input and scene lifetime.</summary>
    public sealed class LevelRunController2D : MonoBehaviour, ILevelRunEnvironment
    {
        [SerializeField]
        private PlayerLife2D player;
        [SerializeField]
        private PlayerAccessCards2D cards;
        [SerializeField]
        private LevelDefinition definition;
        [SerializeField]
        private LevelObjectiveController2D objectives;
        private IPlayerInput input;
        private LevelRunController controller;

        public bool Completed => controller?.Completed ?? false;
        public int Phase => objectives != null ? objectives.Phase : 0;
        public float ElapsedSeconds => RequireController().ElapsedSeconds;
        public PlayerLife2D Player => player;
        public LevelDefinition Definition => definition;
        public event Action Finished;

        [Inject]
        public void Construct(IPlayerInput playerInput, LevelProgressFactory factory)
        {
            if (controller != null)
            {
                throw new InvalidOperationException("The level run is already composed.");
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            input = playerInput;
            controller = factory.CreateRun(this);
            controller.Finished += OnFinished;
        }

        private void Start() => RequireController().Start();

        private void Update()
        {
            if (Completed && input != null && input.RestartPressed)
            {
                Restart();
            }
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.Finished -= OnFinished;
            }
        }

        public void SetDefinition(LevelDefinition data, LevelObjectiveController2D goals)
        {
            if (data != null)
            {
                data.Validate();
            }

            definition = data;
            objectives = goals;
        }

        public void Configure(PlayerLife2D life, PlayerAccessCards2D inventory)
        {
            player = life;
            cards = inventory;
        }

        public void RestoreElapsed(float seconds)
        {
            var result = RequireController().RestoreElapsed(seconds);
            if (!result.Success)
            {
                throw new ArgumentException(result.Reason, nameof(seconds));
            }
        }

        public bool TryFinish(PlayerLife2D actor)
        {
            bool ownsActor = player != null && actor == player;
            bool alive = ownsActor && !player.IsDead;
            bool hasExitAccess = !Completed && alive && definition != null && cards != null && cards.Has(definition.ExitCardId);
            return RequireController().TryFinish(ownsActor, alive, hasExitAccess).Success;
        }

        public void Restart() => RequireController().Restart();

        void ILevelRunEnvironment.StopParticipants()
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                foreach (LevelParticipant2D participant in root.GetComponentsInChildren<LevelParticipant2D>(true))
                {
                    participant.StopActivity();
                }
            }
        }

        void ILevelRunEnvironment.RestartScene()
        {
            var checkpoint = GetComponent<LevelCheckpointController2D>();
            if (checkpoint != null)
            {
                checkpoint.Clear();
            }

            SceneManager.LoadScene(gameObject.scene.buildIndex);
        }

        private void OnFinished() => Finished?.Invoke();

        private LevelRunController RequireController() => controller ?? throw new InvalidOperationException("The run requires its level composition.");
    }
}
