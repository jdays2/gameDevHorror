using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace EmergencyShift
{
    /// <summary>Explicit sandbox shortcuts. No campaign objectives or checkpoint slot.</summary>
    public sealed class SandboxSession2D : MonoBehaviour
    {
        [SerializeField]
        private PlayerLife2D player;

        public void Configure(PlayerLife2D life)
        {
            player = life;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (GameplayInputBlock.IsBlocked || keyboard == null || player == null || player.IsDead)
            {
                return;
            }

            // PlayerLife2D already handles R after death.
            if (keyboard.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(gameObject.scene.buildIndex);
                return;
            }

            if (keyboard.tKey.wasPressedThisFrame)
            {
                var weapon = player.GetComponent<PlayerWeapon2D>();
                if (weapon != null && weapon.HasWeapon)
                {
                    weapon.TryAddAmmo(weapon.MaximumAmmo);
                }
            }
        }
    }
}
