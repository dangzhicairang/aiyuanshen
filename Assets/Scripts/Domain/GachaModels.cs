using System;
using System.Collections.Generic;

namespace GachaDemo.Domain
{
    public enum RewardType { Character, Weapon, Material }

    [Serializable]
    public class RewardDefinition
    {
        public string Id;
        public string Name;
        public RewardType Type;
        public int Star;
        public bool IsUp;
    }

    [Serializable]
    public class PoolRewardGroup
    {
        public int Star;
        public string[] RewardIds;
    }

    [Serializable]
    public class GachaPoolConfig
    {
        public string PoolId;
        public string DisplayName;
        public int SoftPityStart;
        public int HardPity;
        public int FourStarHardPity;
        public float BaseFiveStarRate;
        public float BaseFourStarRate;
        public string[] UpFiveStarIds;
        public string[] UpFourStarIds;
        public PoolRewardGroup[] Groups;
    }

    [Serializable]
    public class GachaRequest
    {
        public string PoolId;
        public int DrawCount;
        public string CurrencyType;
    }

    [Serializable]
    public class GachaReward
    {
        public string RewardId;
        public string RewardName;
        public int Star;
        public bool IsUp;
        public bool IsDuplicate;
    }

    [Serializable]
    public class PityState
    {
        public int SinceLastFiveStar;
        public int SinceLastFourStar;
        public bool NextFiveStarGuaranteedUp;
    }

    [Serializable]
    public class GachaResult
    {
        public string TransactionId;
        public string PoolId;
        public int Cost;
        public int CurrencyAfter;
        public PityState PityState;
        public List<GachaReward> Rewards = new List<GachaReward>(10);
    }

    [Serializable]
    public class GachaConfigRoot
    {
        public GachaPoolConfig[] Pools;
        public RewardDefinition[] Rewards;
    }
}
