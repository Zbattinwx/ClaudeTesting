using System.Threading.Tasks;
using OhioNewsWeather.WeatherApp.Models;

namespace OhioNewsWeather.WeatherApp.Services
{
    public interface IRadarService
    {
        Task<RadarFrame[]> GetRadarFramesAsync(Location location, int frameCount = 10);
        Task<RadarFrame> GetLatestRadarFrameAsync(Location location);
    }
}
