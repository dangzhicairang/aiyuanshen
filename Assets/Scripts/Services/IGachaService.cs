using GachaDemo.Domain;

namespace GachaDemo.Services
{
    public interface IGachaService
    {
        GachaResult Draw(GachaRequest request);
    }
}
