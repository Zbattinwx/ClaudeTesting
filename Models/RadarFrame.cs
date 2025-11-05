using System;
using System.Windows.Media.Imaging;

namespace OhioNewsWeather.WeatherApp.Models
{
    public class RadarFrame
    {
        public DateTime Timestamp { get; set; }
        public string ImageUrl { get; set; }
        public BitmapImage Image { get; set; }
        public RadarProduct Product { get; set; }
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public int ZoomLevel { get; set; }

        public enum RadarProduct
        {
            Reflectivity,
            Velocity,
            CorrelationCoefficient,
            DifferentialReflectivity,
            SpecificDifferentialPhase
        }
    }
}
