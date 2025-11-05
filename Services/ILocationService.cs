using System.Collections.Generic;
using OhioNewsWeather.WeatherApp.Models;

namespace OhioNewsWeather.WeatherApp.Services
{
    public interface ILocationService
    {
        IEnumerable<Location> GetOhioLocations();
        Location GetLocationByName(string name);
        Location GetDefaultLocation();
    }
}
