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
        private readonly IRadarSiteService _radarSiteService;

        public NoaaRadarService(IHttpClientFactory httpClientFactory, IRadarSiteService radarSiteService)
        {
            _httpClient = httpClientFactory.CreateClient();
            _radarSiteService = radarSiteService;
        }

        public async Task<RadarFrame[]> GetRadarFramesAsync(RadarSite site, RadarFrame.RadarProduct product = RadarFrame.RadarProduct.Reflectivity, int frameCount = 10)
        {
            var frames = new System.Collections.Generic.List<RadarFrame>();
            var now = DateTime.UtcNow;

            // Level 2 radar updates approximately every 4-6 minutes
            for (int i = frameCount - 1; i >= 0; i--)
            {
                var timestamp = now.AddMinutes(-5 * i);
                var frame = await GetRadarFrameBySiteAsync(site, product, timestamp);
                if (frame != null)
                {
                    frames.Add(frame);
                }
            }

            return frames.ToArray();
        }

        public async Task<RadarFrame> GetLatestRadarFrameAsync(RadarSite site, RadarFrame.RadarProduct product = RadarFrame.RadarProduct.Reflectivity)
        {
            return await GetRadarFrameBySiteAsync(site, product, DateTime.UtcNow);
        }

        public async Task<RadarFrame[]> GetRadarFramesByLocationAsync(Location location, int frameCount = 10)
        {
            var nearestSite = _radarSiteService.GetNearestRadarSite(location);
            return await GetRadarFramesAsync(nearestSite, RadarFrame.RadarProduct.Reflectivity, frameCount);
        }

        private async Task<RadarFrame> GetRadarFrameBySiteAsync(RadarSite site, RadarFrame.RadarProduct product, DateTime timestamp)
        {
            try
            {
                // Calculate bounding box around radar site (approximately 230km radius for Level 2)
                var radiusInDegrees = 2.5; // ~250km coverage

                var minLon = site.Longitude - radiusInDegrees;
                var maxLon = site.Longitude + radiusInDegrees;
                var minLat = site.Latitude - radiusInDegrees;
                var maxLat = site.Latitude + radiusInDegrees;

                // Get product layer name
                var productCode = GetProductLayerName(product);

                // Build WMS request for Iowa State Mesonet RIDGE service
                // Use the composite layer name (nexrad-n0r-900913) with site in CQL_FILTER
                var baseUrl = $"https://mesonet.agron.iastate.edu/cgi-bin/wms/nexrad/{productCode}.cgi";

                var wmsUrl = $"{baseUrl}" +
                    $"?SERVICE=WMS" +
                    $"&VERSION=1.1.1" +
                    $"&REQUEST=GetMap" +
                    $"&LAYERS=nexrad-{productCode}-900913" +
                    $"&CQL_FILTER=nexrad_id='{site.SiteId}'" +
                    $"&STYLES=" +
                    $"&SRS=EPSG:4326" +
                    $"&BBOX={minLon},{minLat},{maxLon},{maxLat}" +
                    $"&WIDTH=1024" +
                    $"&HEIGHT=1024" +
                    $"&FORMAT=image/png" +
                    $"&TRANSPARENT=true" +
                    $"&bgcolor=0x000000";

                var imageBytes = await _httpClient.GetByteArrayAsync(wmsUrl);

                // Check if we got valid data
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"No image data received from {wmsUrl}");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"Received {imageBytes.Length} bytes from {wmsUrl}");

                // If response is suspiciously small, it's probably an error message
                if (imageBytes.Length < 1000)
                {
                    var errorMessage = System.Text.Encoding.UTF8.GetString(imageBytes);
                    System.Diagnostics.Debug.WriteLine($"WMS Error Response: {errorMessage}");
                    return null;
                }

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
                    Product = product,
                    CenterLatitude = site.Latitude,
                    CenterLongitude = site.Longitude,
                    ZoomLevel = 8
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading radar frame from {site.SiteId}: {ex.Message}");
                return null;
            }
        }

        private string GetProductLayerName(RadarFrame.RadarProduct product)
        {
            return product switch
            {
                RadarFrame.RadarProduct.Reflectivity => "n0r",
                RadarFrame.RadarProduct.Velocity => "n0v",
                RadarFrame.RadarProduct.CorrelationCoefficient => "n0c",
                RadarFrame.RadarProduct.DifferentialReflectivity => "n0x",
                RadarFrame.RadarProduct.SpecificDifferentialPhase => "n0k",
                _ => "n0r"
            };
        }
    }
}
