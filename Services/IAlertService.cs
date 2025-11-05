using System.Threading.Tasks;
using OhioNewsWeather.WeatherApp.Models;

namespace OhioNewsWeather.WeatherApp.Services
{
    public interface IAlertService
    {
        Task<WeatherAlert[]> GetActiveAlertsAsync(string state = "OH");
        Task<WeatherAlert[]> GetAlertsByLocationAsync(Location location);
    }
}
