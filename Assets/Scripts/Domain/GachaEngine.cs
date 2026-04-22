using System;
using System.Collections.Generic;
using System.Linq;

namespace GachaDemo.Domain
{
    public sealed class GachaEngine
    {
        private readonly Random _random;

        public GachaEngine(int seed)
        {
            _random = new Random(seed);
        }

        public List<GachaReward> Draw(GachaPoolConfig pool, IReadOnlyDictionary<string, RewardDefinition> rewardMap, PityState state, int count)
        {
            var rewards = new List<GachaReward>(count);
            for (var i = 0; i < count; i++)
            {
                var star = RollStar(pool, state);
                var reward = RollRewardByStar(pool, rewardMap, state, star);
                rewards.Add(reward);
                UpdatePityState(state, reward.Star, reward.IsUp);
            }

            return rewards;
        }

        private int RollStar(GachaPoolConfig pool, PityState state)
        {
            if (state.SinceLastFiveStar + 1 >= pool.HardPity)
            {
                return 5;
            }

            if (state.SinceLastFourStar + 1 >= pool.FourStarHardPity)
            {
                return 4;
            }

            var fiveStarRate = pool.BaseFiveStarRate;
            if (state.SinceLastFiveStar + 1 >= pool.SoftPityStart)
            {
                var bonusSteps = state.SinceLastFiveStar + 1 - pool.SoftPityStart;
                fiveStarRate += bonusSteps * 0.06f;
            }

            var roll = (float)_random.NextDouble();
            if (roll < Math.Clamp(fiveStarRate, 0f, 1f))
            {
                return 5;
            }

            if (roll < Math.Clamp(fiveStarRate + pool.BaseFourStarRate, 0f, 1f))
            {
                return 4;
            }

            return 3;
        }

        private GachaReward RollRewardByStar(GachaPoolConfig pool, IReadOnlyDictionary<string, RewardDefinition> rewardMap, PityState state, int star)
        {
            var sourceIds = pool.Groups.First(g => g.Star == star).RewardIds;
            var finalId = sourceIds[_random.Next(0, sourceIds.Length)];
            var isUp = false;

            if (star == 5 && pool.UpFiveStarIds != null && pool.UpFiveStarIds.Length > 0)
            {
                if (state.NextFiveStarGuaranteedUp || _random.NextDouble() < 0.5)
                {
                    finalId = pool.UpFiveStarIds[_random.Next(0, pool.UpFiveStarIds.Length)];
                    isUp = true;
                }
            }
            else if (star == 4 && pool.UpFourStarIds != null && pool.UpFourStarIds.Length > 0 && _random.NextDouble() < 0.5)
            {
                finalId = pool.UpFourStarIds[_random.Next(0, pool.UpFourStarIds.Length)];
                isUp = true;
            }

            var def = rewardMap[finalId];
            return new GachaReward
            {
                RewardId = def.Id,
                RewardName = def.Name,
                Star = def.Star,
                IsUp = isUp
            };
        }

        private static void UpdatePityState(PityState state, int star, bool isUp)
        {
            state.SinceLastFiveStar++;
            state.SinceLastFourStar++;

            if (star == 5)
            {
                state.SinceLastFiveStar = 0;
                state.SinceLastFourStar = 0;
                state.NextFiveStarGuaranteedUp = !isUp;
            }
            else if (star == 4)
            {
                state.SinceLastFourStar = 0;
            }
        }
    }
}
