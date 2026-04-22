using System.Collections;
using GachaDemo.Application;
using GachaDemo.Domain;
using GachaDemo.Infrastructure;
using GachaDemo.Services;
using UnityEngine;

namespace GachaDemo.Presentation
{
    public sealed class GachaController : MonoBehaviour
    {
        private const string CurrencyKey = "gacha.currency";
        private const int DemoRefillAmount = 32000;

        [SerializeField] private string poolId = "limited_character";
        [SerializeField] private string configPath = "Configs/gacha_config";
        [SerializeField] private GachaPanelUI gachaPanel;
        [SerializeField] private ResultPanelUI resultPanel;
        [SerializeField] private RevealTimelineController revealTimeline;

        private GachaUseCase _useCase;
        private ILocalStorage _storage;
        private bool _skipRequested;

        private void Start()
        {
            var repository = new ConfigRepository(configPath);
            _storage = new PlayerPrefsStorage();
            _useCase = new GachaUseCase(new MockGachaService(repository, _storage));

            gachaPanel.OnDrawClicked += HandleDraw;
            gachaPanel.OnSkipClicked += () => _skipRequested = true;
            RefreshStatus(_storage.LoadCurrency(CurrencyKey, DemoRefillAmount), _storage.LoadPity(poolId));
            resultPanel.Hide();
        }

        private void HandleDraw(int drawCount)
        {
            StopAllCoroutines();
            StartCoroutine(PlayDraw(drawCount));
        }

        private IEnumerator PlayDraw(int drawCount)
        {
            gachaPanel.SetButtonsInteractable(false);
            _skipRequested = false;
            resultPanel.Hide();

            GachaResult result;
            try
            {
                result = _useCase.Draw(poolId, drawCount);
            }
            catch (System.Exception ex)
            {
                if (ex.Message.Contains("Not enough currency"))
                {
                    _storage.SaveCurrency(CurrencyKey, DemoRefillAmount);
                    Debug.Log("演示模式：原石不足，已自动补充。");
                    result = _useCase.Draw(poolId, drawCount);
                }
                else
                {
                    Debug.LogWarning($"Draw failed: {ex.Message}");
                    gachaPanel.SetButtonsInteractable(true);
                    yield break;
                }
            }

            foreach (var reward in result.Rewards)
            {
                if (_skipRequested)
                {
                    break;
                }

                yield return revealTimeline.PlayReveal(reward);
            }

            resultPanel.Show(result.Rewards);
            RefreshStatus(result.CurrencyAfter, result.PityState);
            gachaPanel.SetButtonsInteractable(true);
        }

        private void RefreshStatus(int currency, PityState pity)
        {
            gachaPanel.SetCurrency(currency);
            gachaPanel.SetPityState(pity.SinceLastFourStar, pity.SinceLastFiveStar, pity.NextFiveStarGuaranteedUp);
        }
    }
}
