using GachaDemo.Domain;
using GachaDemo.Services;

namespace GachaDemo.Application
{
    public sealed class GachaUseCase
    {
        private readonly IGachaService _service;

        public GachaUseCase(IGachaService service)
        {
            _service = service;
        }

        public GachaResult Draw(string poolId, int drawCount)
        {
            var request = new GachaRequest
            {
                PoolId = poolId,
                DrawCount = drawCount,
                CurrencyType = "Primogem"
            };

            return _service.Draw(request);
        }
    }
}
