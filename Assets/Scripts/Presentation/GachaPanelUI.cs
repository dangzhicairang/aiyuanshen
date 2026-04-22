using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GachaDemo.Presentation
{
    public sealed class GachaPanelUI : MonoBehaviour
    {
        [SerializeField] private Button singleDrawButton;
        [SerializeField] private Button tenDrawButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private TextMeshProUGUI currencyText;
        [SerializeField] private TextMeshProUGUI stateText;

        public event Action<int> OnDrawClicked;
        public event Action OnSkipClicked;

        private void Start()
        {
            if (singleDrawButton == null || tenDrawButton == null || skipButton == null)
            {
                Debug.LogError("GachaPanelUI buttons are not assigned.");
                return;
            }

            singleDrawButton.onClick.AddListener(() =>
            {
                OnDrawClicked?.Invoke(1);
            });
            tenDrawButton.onClick.AddListener(() =>
            {
                OnDrawClicked?.Invoke(10);
            });
            skipButton.onClick.AddListener(() =>
            {
                OnSkipClicked?.Invoke();
            });

            skipButton.interactable = false;
        }

        public void SetCurrency(int amount) => currencyText.text = $"原石  {amount:N0}";

        public void SetPityState(int pity4, int pity5, bool guaranteed)
        {
            stateText.text = $"四星保底 {pity4}/10  ·  五星保底 {pity5}/90  ·  下次五星 {(guaranteed ? "必定 UP" : "50/50")}";
        }

        public void SetButtonsInteractable(bool interactable)
        {
            singleDrawButton.interactable = interactable;
            tenDrawButton.interactable = interactable;
            skipButton.interactable = !interactable;
        }
    }
}
