using System;
using System.Collections.Generic;
using System.Linq;
using OhioNewsWeather.WeatherApp.Models;

namespace OhioNewsWeather.WeatherApp.Services
{
    public class RadarSiteService : IRadarSiteService
    {
        private readonly List<RadarSite> _radarSites;

        public RadarSiteService()
        {
            _radarSites = InitializeOhioRadarSites();
        }

        public IEnumerable<RadarSite> GetOhioRadarSites()
        {
            return _radarSites;
        }

        public RadarSite GetRadarSiteById(string siteId)
        {
            return _radarSites.FirstOrDefault(s =>
                s.SiteId.Equals(siteId, StringComparison.OrdinalIgnoreCase));
        }

        public RadarSite GetDefaultRadarSite()
        {
            return _radarSites.FirstOrDefault(s => s.IsPrimary) ?? _radarSites.First();
        }

        public RadarSite GetNearestRadarSite(Location location)
        {
            if (location == null)
                return GetDefaultRadarSite();

            var nearest = _radarSites
                .OrderBy(site => CalculateDistance(location.Latitude, location.Longitude, site.Latitude, site.Longitude))
                .First();

            return nearest;
        }

        private List<RadarSite> InitializeOhioRadarSites()
        {
            return new List<RadarSite>
            {
                // Primary Ohio Sites
                new RadarSite("KCLE", "Cleveland", "Cleveland, OH", 41.4131, -81.8597, "OH", 763, true),
                new RadarSite("KILN", "Wilmington", "Wilmington, OH", 39.4202, -83.8217, "OH", 1056, true),

                // Surrounding Sites (cover Ohio regions)
                new RadarSite("KIND", "Indianapolis", "Indianapolis, IN", 39.7075, -86.2803, "IN", 810),
                new RadarSite("KIWX", "Northern Indiana", "North Webster, IN", 41.3586, -85.7000, "IN", 960),
                new RadarSite("KDTX", "Detroit", "White Lake, MI", 42.6997, -83.4717, "MI", 1072),
                new RadarSite("KPBZ", "Pittsburgh", "Pittsburgh, PA", 40.5317, -80.2178, "PA", 1185),
                new RadarSite("KRLX", "Charleston", "Charleston, WV", 38.3111, -81.7231, "WV", 1081)
            };
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula for distance calculation
            const double R = 6371; // Earth's radius in km

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
