using System;

namespace OhioNewsWeather.WeatherApp.Models
{
    public class WeatherConditions
    {
        public Location Location { get; set; }
        public DateTime Timestamp { get; set; }
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public double Humidity { get; set; }
        public double Dewpoint { get; set; }
        public double WindSpeed { get; set; }
        public int WindDirection { get; set; }
        public double WindGust { get; set; }
        public double Pressure { get; set; }
        public double Visibility { get; set; }
        public string Conditions { get; set; }
        public string Icon { get; set; }
    }
}
