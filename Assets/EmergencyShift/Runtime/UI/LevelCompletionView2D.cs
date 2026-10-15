using UnityEngine;
using UnityEngine.UI;

namespace EmergencyShift
{
    public sealed class LevelCompletionView2D : MonoBehaviour
    {
        [SerializeField]
        private LevelRunController2D run;
        [SerializeField]
        private LevelObjectiveController2D objectives;
        [SerializeField]
        private Text objective;
        [SerializeField]
        private GameObject completionPanel;
        [SerializeField]
        private Text completionText;
        public void Configure(LevelRunController2D owner, LevelObjectiveController2D goals, Text task, GameObject panel, Text result)
        {
            run = owner;
            objectives = goals;
            objective = task;
            completionPanel = panel;
            completionText = result;
        }

        private void OnEnable()
        {
            if (objectives != null)
            {
                objectives.Changed += Refresh;
            }

            if (run != null)
            {
                run.Finished += ShowResult;
            }
        }

        private void Start()
        {
            completionPanel.SetActive(false);
            Refresh();
        }

        private void OnDisable()
        {
            if (objectives != null)
            {
                objectives.Changed -= Refresh;
            }

            if (run != null)
            {
                run.Finished -= ShowResult;
            }
        }

        private void Refresh()
        {
            if (!run.Completed)
            {
                objective.text = objectives.Text;
            }
        }

        private void ShowResult()
        {
            objective.text = "ВЫХОД НАЙДЕН";
            int seconds = Mathf.FloorToInt(run.ElapsedSeconds);
            completionText.text = "ВЫ ВЫБРАЛИСЬ\n\nДемонстрационный сектор пройден\nВремя  " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00") + "\nМодулей фонаря  " + run.Player.GetComponent<FlashlightUpgrades>().InstalledCount + "\n\n[R]  Пройти заново";
            completionPanel.SetActive(true);
        }
    }
}
