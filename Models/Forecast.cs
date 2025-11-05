using System;

namespace OhioNewsWeather.WeatherApp.Models
{
    public class Forecast
    {
        public Location Location { get; set; }
        public ForecastPeriod[] Periods { get; set; }
    }

    public class ForecastPeriod
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Name { get; set; }
        public double Temperature { get; set; }
        public string TemperatureUnit { get; set; }
        public string WindSpeed { get; set; }
        public string WindDirection { get; set; }
        public string ShortForecast { get; set; }
        public string DetailedForecast { get; set; }
        public string Icon { get; set; }
        public int PrecipitationChance { get; set; }
    }
}
