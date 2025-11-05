using System.Threading.Tasks;
using OhioNewsWeather.WeatherApp.Models;

namespace OhioNewsWeather.WeatherApp.Services
{
    public interface ISatelliteService
    {
        Task<SatelliteImage[]> GetVisibleSatelliteAsync(int frameCount = 10);
        Task<SatelliteImage[]> GetInfraredSatelliteAsync(int frameCount = 10);
    }
}
