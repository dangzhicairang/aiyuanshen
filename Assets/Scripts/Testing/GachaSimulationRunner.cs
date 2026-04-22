using System.Collections.Generic;
using GachaDemo.Domain;
using GachaDemo.Infrastructure;
using GachaDemo.Services;
using UnityEngine;

namespace GachaDemo.Testing
{
    public sealed class GachaSimulationRunner : MonoBehaviour
    {
        [SerializeField] private string poolId = "limited_character";
        [SerializeField] private int totalDraws = 100000;

        [ContextMenu("Run Gacha Simulation")]
        public void Run()
        {
            var storage = new PlayerPrefsStorage();
            storage.SaveCurrency("gacha.currency", int.MaxValue / 4);
            var service = new MockGachaService(new ConfigRepository("Configs/gacha_config"), storage);
            var stats = new Dictionary<int, int> { { 3, 0 }, { 4, 0 }, { 5, 0 } };
            var drawsLeft = totalDraws;

            while (drawsLeft > 0)
            {
                var batch = Mathf.Min(10, drawsLeft);
                var result = service.Draw(new GachaRequest { PoolId = poolId, DrawCount = batch, CurrencyType = "Primogem" });
                foreach (var reward in result.Rewards)
                {
                    stats[reward.Star]++;
                }

                drawsLeft -= batch;
            }

            Debug.Log($"Simulation {totalDraws}: 3*={stats[3]}, 4*={stats[4]}, 5*={stats[5]}");
        }
    }
}
