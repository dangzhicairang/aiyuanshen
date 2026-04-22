using System;
using System.Collections.Generic;
using System.Linq;
using GachaDemo.Domain;
using UnityEngine;

namespace GachaDemo.Infrastructure
{
    public interface IConfigRepository
    {
        GachaPoolConfig GetPool(string poolId);
        IReadOnlyDictionary<string, RewardDefinition> GetRewards();
    }

    public sealed class ConfigRepository : IConfigRepository
    {
        private readonly Dictionary<string, GachaPoolConfig> _poolMap;
        private readonly Dictionary<string, RewardDefinition> _rewardMap;

        public ConfigRepository(string resourcePath)
        {
            var textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset == null)
            {
                throw new InvalidOperationException($"Config missing at Resources/{resourcePath}.json");
            }

            var config = JsonUtility.FromJson<GachaConfigRoot>(textAsset.text);
            _poolMap = config.Pools.ToDictionary(x => x.PoolId, x => x);
            _rewardMap = config.Rewards.ToDictionary(x => x.Id, x => x);
        }

        public GachaPoolConfig GetPool(string poolId) => _poolMap[poolId];

        public IReadOnlyDictionary<string, RewardDefinition> GetRewards() => _rewardMap;
    }
}
