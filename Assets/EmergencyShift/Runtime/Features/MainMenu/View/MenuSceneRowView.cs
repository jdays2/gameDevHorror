using System;
using EmergencyShift.Features.MainMenu.Logic;
using UnityEngine;

namespace EmergencyShift.Features.MainMenu.View
{
    public sealed class MenuSceneRowView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button button;
        [SerializeField] private UnityEngine.UI.Text label;
        private Action<string> launch;
        private string sceneId;

        public void Bind(MenuScene scene, bool available, Action<string> onLaunch)
        {
            sceneId = scene.Id;
            label.text = available ? scene.Title : scene.Title + " · нет в сборке";
            button.interactable = available;
            launch = onLaunch;
            button.onClick.AddListener(Launch);
        }

        private void OnDestroy() => button.onClick.RemoveListener(Launch);
        private void Launch() => launch?.Invoke(sceneId);
    }
}
