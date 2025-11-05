using System.Collections.Generic;
using OhioNewsWeather.WeatherApp.Models;

namespace OhioNewsWeather.WeatherApp.Services
{
    public interface IRadarSiteService
    {
        IEnumerable<RadarSite> GetOhioRadarSites();
        RadarSite GetRadarSiteById(string siteId);
        RadarSite GetDefaultRadarSite();
        RadarSite GetNearestRadarSite(Location location);
    }
}
