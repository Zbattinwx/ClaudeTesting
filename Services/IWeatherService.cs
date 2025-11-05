using System.Threading.Tasks;
using OhioNewsWeather.WeatherApp.Models;

namespace OhioNewsWeather.WeatherApp.Services
{
    public interface IWeatherService
    {
        Task<WeatherConditions> GetCurrentConditionsAsync(Location location);
        Task<Forecast> GetForecastAsync(Location location);
        Task<HourlyForecast[]> GetHourlyForecastAsync(Location location);
    }
}
