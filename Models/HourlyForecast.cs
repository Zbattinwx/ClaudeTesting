using System;

namespace OhioNewsWeather.WeatherApp.Models
{
    public class HourlyForecast
    {
        public DateTime Time { get; set; }
        public double Temperature { get; set; }
        public double Dewpoint { get; set; }
        public int Humidity { get; set; }
        public string WindSpeed { get; set; }
        public string WindDirection { get; set; }
        public int PrecipitationChance { get; set; }
        public string ShortForecast { get; set; }
        public string Icon { get; set; }
    }
}
