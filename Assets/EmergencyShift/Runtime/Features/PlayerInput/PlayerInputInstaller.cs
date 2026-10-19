using EmergencyShift.Features.PlayerInput.View;
using UnityEngine.InputSystem;
using Zenject;

namespace EmergencyShift.Features.PlayerInput
{
    public sealed class PlayerInputInstaller : Installer<InputActionAsset, PlayerInputInstaller>
    {
        private readonly InputActionAsset controls;

        public PlayerInputInstaller(InputActionAsset controls)
        {
            this.controls = controls;
        }

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<InputActionPlayerInput>().AsSingle().WithArguments(controls);
            Container.Bind<GameplayInputBlock>().AsSingle();
        }
    }
}
