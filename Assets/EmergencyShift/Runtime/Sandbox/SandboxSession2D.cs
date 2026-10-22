using UnityEngine;
using EmergencyShift.Features.PlayerInput.Logic;
using Zenject;
using UnityEngine.SceneManagement;

namespace EmergencyShift
{
    /// <summary>Explicit sandbox shortcuts. No campaign objectives or checkpoint slot.</summary>
    public sealed class SandboxSession2D : MonoBehaviour
    {
        [SerializeField]
        private PlayerLife2D player;
        private PlayerWeapon2D weapon;
        private IPlayerInput input;
        private GameplayInputBlock inputBlock;

        [Inject]
        private void Construct(IPlayerInput playerInput, GameplayInputBlock block)
        {
            input = playerInput;
            inputBlock = block;
        }

        public void Configure(PlayerLife2D life)
        {
            player = life;
            CacheWeapon();
        }

        private void Awake() => CacheWeapon();

        private void CacheWeapon() => weapon = player != null ? player.GetComponent<PlayerWeapon2D>() : null;

        private void Update()
        {
            if (inputBlock.IsBlocked || player == null || player.IsDead)
            {
                return;
            }

            // PlayerLife2D already handles R after death.
            if (input.RestartPressed)
            {
                SceneManager.LoadScene(gameObject.scene.buildIndex);
                return;
            }

            if (input.RefillAmmoPressed)
            {
                if (weapon != null && weapon.HasWeapon)
                {
                    weapon.TryAddAmmo(weapon.MaximumAmmo);
                }
            }
        }
    }
}
