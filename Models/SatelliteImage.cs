using System;
using System.Windows.Media.Imaging;

namespace OhioNewsWeather.WeatherApp.Models
{
    public class SatelliteImage
    {
        public DateTime Timestamp { get; set; }
        public string ImageUrl { get; set; }
        public BitmapImage Image { get; set; }
        public SatelliteType Type { get; set; }
        public string Sector { get; set; }

        public enum SatelliteType
        {
            Visible,
            Infrared,
            WaterVapor,
            Shortwave
        }
    }
}
