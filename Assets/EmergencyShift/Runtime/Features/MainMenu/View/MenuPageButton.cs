using EmergencyShift.Features.MainMenu.Logic;
using UnityEngine;

namespace EmergencyShift.Features.MainMenu.View
{
    public sealed class MenuPageButton : MonoBehaviour
    {
        [SerializeField] private MenuPage page;
        [SerializeField] private UnityEngine.UI.Button button;
        private MainMenuView owner;

        public void Bind(MainMenuView view)
        {
            owner = view;
            button.onClick.AddListener(Open);
        }

        private void OnDestroy() => button.onClick.RemoveListener(Open);
        private void Open() => owner.Request(page);
    }
}
