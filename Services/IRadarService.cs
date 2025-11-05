using System.Threading.Tasks;
using OhioNewsWeather.WeatherApp.Models;

namespace OhioNewsWeather.WeatherApp.Services
{
    public interface IRadarService
    {
        Task<RadarFrame[]> GetRadarFramesAsync(RadarSite site, RadarFrame.RadarProduct product = RadarFrame.RadarProduct.Reflectivity, int frameCount = 10);
        Task<RadarFrame> GetLatestRadarFrameAsync(RadarSite site, RadarFrame.RadarProduct product = RadarFrame.RadarProduct.Reflectivity);
        Task<RadarFrame[]> GetRadarFramesByLocationAsync(Location location, int frameCount = 10);
    }
}
