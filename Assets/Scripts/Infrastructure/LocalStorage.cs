using GachaDemo.Domain;
using UnityEngine;

namespace GachaDemo.Infrastructure
{
    public interface ILocalStorage
    {
        PityState LoadPity(string poolId);
        void SavePity(string poolId, PityState state);
        int LoadCurrency(string key, int fallback);
        void SaveCurrency(string key, int amount);
    }

    public sealed class PlayerPrefsStorage : ILocalStorage
    {
        public PityState LoadPity(string poolId)
        {
            var prefix = $"gacha.pity.{poolId}";
            return new PityState
            {
                SinceLastFiveStar = PlayerPrefs.GetInt(prefix + ".five", 0),
                SinceLastFourStar = PlayerPrefs.GetInt(prefix + ".four", 0),
                NextFiveStarGuaranteedUp = PlayerPrefs.GetInt(prefix + ".guarantee", 0) == 1
            };
        }

        public void SavePity(string poolId, PityState state)
        {
            var prefix = $"gacha.pity.{poolId}";
            PlayerPrefs.SetInt(prefix + ".five", state.SinceLastFiveStar);
            PlayerPrefs.SetInt(prefix + ".four", state.SinceLastFourStar);
            PlayerPrefs.SetInt(prefix + ".guarantee", state.NextFiveStarGuaranteedUp ? 1 : 0);
            PlayerPrefs.Save();
        }

        public int LoadCurrency(string key, int fallback) => PlayerPrefs.GetInt(key, fallback);

        public void SaveCurrency(string key, int amount)
        {
            PlayerPrefs.SetInt(key, amount);
            PlayerPrefs.Save();
        }
    }
}
