using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using OhioNewsWeather.WeatherApp.Models;

namespace OhioNewsWeather.WeatherApp.Services
{
    public class NoaaRadarService : IRadarService
    {
        private readonly HttpClient _httpClient;
        private const string RadarBaseUrl = "https://opengeo.ncep.noaa.gov/geoserver/conus/conus_bref_qcd/ows";

        public NoaaRadarService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<RadarFrame[]> GetRadarFramesAsync(Location location, int frameCount = 10)
        {
            var frames = new System.Collections.Generic.List<RadarFrame>();
            var now = DateTime.UtcNow;

            // Generate frames for the last N intervals (6 minutes each)
            for (int i = frameCount - 1; i >= 0; i--)
            {
                var timestamp = now.AddMinutes(-6 * i);
                var frame = await GetRadarFrameAsync(location, timestamp);
                if (frame != null)
                {
                    frames.Add(frame);
                }
            }

            return frames.ToArray();
        }

        public async Task<RadarFrame> GetLatestRadarFrameAsync(Location location)
        {
            return await GetRadarFrameAsync(location, DateTime.UtcNow);
        }

        private async Task<RadarFrame> GetRadarFrameAsync(Location location, DateTime timestamp)
        {
            try
            {
                // Calculate bounding box (approximately 200km around location)
                var latOffset = 1.8; // ~200km
                var lonOffset = 2.4; // ~200km (adjusted for latitude)

                var minLon = location.Longitude - lonOffset;
                var maxLon = location.Longitude + lonOffset;
                var minLat = location.Latitude - latOffset;
                var maxLat = location.Latitude + latOffset;

                // Build WMS request for radar imagery
                var wmsUrl = $"{RadarBaseUrl}" +
                    $"?SERVICE=WMS" +
                    $"&VERSION=1.3.0" +
                    $"&REQUEST=GetMap" +
                    $"&LAYERS=conus_bref_qcd" +
                    $"&STYLES=" +
                    $"&CRS=EPSG:4326" +
                    $"&BBOX={minLat},{minLon},{maxLat},{maxLon}" +
                    $"&WIDTH=800" +
                    $"&HEIGHT=800" +
                    $"&FORMAT=image/png" +
                    $"&TRANSPARENT=true";

                var imageBytes = await _httpClient.GetByteArrayAsync(wmsUrl);
                var bitmap = new BitmapImage();
                using (var stream = new System.IO.MemoryStream(imageBytes))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }

                return new RadarFrame
                {
                    Timestamp = timestamp,
                    ImageUrl = wmsUrl,
                    Image = bitmap,
                    Product = RadarFrame.RadarProduct.Reflectivity,
                    CenterLatitude = location.Latitude,
                    CenterLongitude = location.Longitude,
                    ZoomLevel = 8
                };
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
