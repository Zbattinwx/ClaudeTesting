using System;
using System.Collections.Generic;

namespace OhioNewsWeather.WeatherApp.Models
{
    public class WeatherAlert
    {
        public string Id { get; set; }
        public string Event { get; set; }
        public string Headline { get; set; }
        public string Description { get; set; }
        public string Instruction { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Urgency { get; set; }
        public DateTime Onset { get; set; }
        public DateTime Expires { get; set; }
        public string[] AffectedZones { get; set; }
        public List<Coordinate[]> Polygons { get; set; }
        public string SenderName { get; set; }

        public enum AlertSeverity
        {
            Unknown,
            Minor,
            Moderate,
            Severe,
            Extreme
        }
    }

    public class Coordinate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public Coordinate(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
