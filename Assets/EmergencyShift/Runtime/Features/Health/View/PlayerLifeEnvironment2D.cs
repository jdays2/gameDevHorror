using System;
using EmergencyShift.Features.Health.Logic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmergencyShift.Features.Health.View
{
    /// <summary>Native actor platform adapter. Components are cached once from its explicitly owned root.</summary>
    internal sealed class PlayerLifeEnvironment2D : IPlayerLifeEnvironment
    {
        private readonly Transform owner;
        private readonly Rigidbody2D body;
        private readonly PlayerMotor2D motor;
        private readonly PlayerInteraction interaction;
        private readonly Flashlight2D flashlight;
        private readonly PlayerWeapon2D weapon;

        public PlayerLifeEnvironment2D(Transform owner)
        {
            this.owner = owner != null ? owner : throw new ArgumentNullException(nameof(owner));
            body = owner.GetComponent<Rigidbody2D>();
            motor = owner.GetComponent<PlayerMotor2D>();
            interaction = owner.GetComponent<PlayerInteraction>();
            flashlight = owner.GetComponent<Flashlight2D>();
            weapon = owner.GetComponent<PlayerWeapon2D>();
        }

        public void SetDefeated(bool defeated)
        {
            motor.enabled = !defeated;
            interaction.enabled = !defeated;
            if (defeated)
            {
                flashlight.SetPower(false);
            }

            flashlight.enabled = !defeated;
            if (weapon != null)
            {
                weapon.enabled = !defeated;
            }

            if (defeated)
            {
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
            }
            else
            {
                body.simulated = true;
                body.linearVelocity = Vector2.zero;
            }
        }

        public void RestartRoom()
        {
            var checkpoint = LevelCheckpointController2D.For(owner);
            if (checkpoint != null)
            {
                checkpoint.Reload();
            }
            else
            {
                SceneManager.LoadScene(owner.gameObject.scene.buildIndex);
            }
        }
    }
}
