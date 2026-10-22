using System;
using EmergencyShift.Core.Commands;
using UnityEngine;

namespace EmergencyShift.Features.Health.Logic
{
    /// <summary>Health authority, physical defeat and cosmetic feedback have three explicit owners.</summary>
    public sealed class PlayerLifeController
    {
        private readonly IPlayerLifeEnvironment environment;
        private readonly IPlayerLifeView view;
        internal HealthModel Model { get; }
        public int Health => Model.Current;
        public int MaximumHealth => Model.Maximum;
        public bool IsDead => !Model.IsAlive;

        public PlayerLifeController(HealthModel model, IPlayerLifeEnvironment environment, IPlayerLifeView view)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public CommandResult TakeDamage(int amount, Vector2 direction)
        {
            if (!float.IsFinite(direction.x) || !float.IsFinite(direction.y))
            {
                return CommandResult.Fail("Damage direction must be finite.");
            }

            int previous = Health;
            CommandResult result = new DamageHealthCommand(Model, amount).Execute();
            if (!result.Success)
            {
                return result;
            }

            if (IsDead)
            {
                environment.SetDefeated(true);
            }

            view.PresentDamage(previous - Health, direction, IsDead);
            view.PresentHealth(Health, false);
            return result;
        }

        public CommandResult Restore(int current)
        {
            CommandResult result = new RestorePlayerHealthCommand(Model, current).Execute();
            if (result.Success)
            {
                environment.SetDefeated(false);
                view.PresentHealth(Health, true);
            }

            return result;
        }

        public CommandResult SetCapacity(int maximum, int current)
        {
            CommandResult result = new SetHealthCapacityCommand(Model, maximum, current).Execute();
            if (result.Success)
            {
                view.PresentHealth(Health, false);
            }

            return result;
        }

        public CommandResult Restart() => new RestartPlayerRoomCommand(Model, environment).Execute();
    }
}
