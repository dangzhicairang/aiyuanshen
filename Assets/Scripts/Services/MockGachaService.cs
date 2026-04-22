using System;
using System.Collections.Generic;
using GachaDemo.Domain;
using GachaDemo.Infrastructure;

namespace GachaDemo.Services
{
    public sealed class MockGachaService : IGachaService
    {
        private const int SingleDrawCost = 160;

        private readonly IConfigRepository _configRepository;
        private readonly ILocalStorage _storage;
        private readonly GachaEngine _engine;
        private readonly HashSet<string> _ownedRewards = new HashSet<string>();

        public MockGachaService(IConfigRepository configRepository, ILocalStorage storage)
        {
            _configRepository = configRepository;
            _storage = storage;
            _engine = new GachaEngine(Environment.TickCount);
        }

        public GachaResult Draw(GachaRequest request)
        {
            var pool = _configRepository.GetPool(request.PoolId);
            var rewards = _configRepository.GetRewards();
            var pity = _storage.LoadPity(request.PoolId);

            var currency = _storage.LoadCurrency("gacha.currency", 32000);
            var cost = SingleDrawCost * request.DrawCount;
            if (currency < cost)
            {
                throw new InvalidOperationException("Not enough currency.");
            }

            var resultRewards = _engine.Draw(pool, rewards, pity, request.DrawCount);
            foreach (var reward in resultRewards)
            {
                reward.IsDuplicate = !_ownedRewards.Add(reward.RewardId);
            }

            currency -= cost;
            _storage.SavePity(request.PoolId, pity);
            _storage.SaveCurrency("gacha.currency", currency);

            return new GachaResult
            {
                TransactionId = Guid.NewGuid().ToString("N"),
                PoolId = request.PoolId,
                Cost = cost,
                CurrencyAfter = currency,
                PityState = pity,
                Rewards = resultRewards
            };
        }
    }
}
