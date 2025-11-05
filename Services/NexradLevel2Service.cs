using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using OhioNewsWeather.WeatherApp.Models;

namespace OhioNewsWeather.WeatherApp.Services
{
    /// <summary>
    /// Service for accessing individual NEXRAD site imagery derived from Level 2 data
    /// Uses Iowa Environmental Mesonet's radar archive which provides high-quality
    /// PNG images from Level 2 data at 5-minute intervals for each radar site
    /// </summary>
    public class NexradLevel2Service : IRadarService
    {
        private readonly HttpClient _httpClient;
        private readonly IRadarSiteService _radarSiteService;

        // Iowa Environmental Mesonet radar archive provides individual site images from Level 2 data
        // Format: https://mesonet.agron.iastate.edu/archive/data/YYYY/MM/DD/GIS/ridge/PRODUCT/SITE/SITE_PRODUCT_YYYY_MM_DD_HHMM.png
        private const string IEM_ARCHIVE_BASE = "https://mesonet.agron.iastate.edu/archive/data";

        public NexradLevel2Service(IHttpClientFactory httpClientFactory, IRadarSiteService radarSiteService)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _radarSiteService = radarSiteService;
        }

        public async Task<RadarFrame[]> GetRadarFramesAsync(RadarSite site, RadarFrame.RadarProduct product = RadarFrame.RadarProduct.Reflectivity, int frameCount = 10)
        {
            var frames = new List<RadarFrame>();
            var now = DateTime.UtcNow;

            System.Diagnostics.Debug.WriteLine($"Fetching {frameCount} Level 2 archive frames for {site.SiteId}");

            // Level 2 radar updates approximately every 4-6 minutes
            // Try to get frames going back in time at 5-minute intervals
            for (int i = 0; i < frameCount * 2; i++) // Check more time slots to ensure we get enough frames
            {
                var timestamp = now.AddMinutes(-5 * i);
                var frame = await GetRadarFrameBySiteAsync(site, product, timestamp);
                if (frame != null)
                {
                    frames.Add(frame);
                    System.Diagnostics.Debug.WriteLine($"Successfully loaded frame {frames.Count}/{frameCount} for {timestamp:HH:mm}");

                    if (frames.Count >= frameCount)
                        break;
                }
            }

            System.Diagnostics.Debug.WriteLine($"Loaded {frames.Count} Level 2 archive frames total");
            return frames.ToArray();
        }

        public async Task<RadarFrame> GetLatestRadarFrameAsync(RadarSite site, RadarFrame.RadarProduct product = RadarFrame.RadarProduct.Reflectivity)
        {
            // Try the last 30 minutes to find the most recent frame
            for (int i = 0; i < 6; i++)
            {
                var timestamp = DateTime.UtcNow.AddMinutes(-5 * i);
                var frame = await GetRadarFrameBySiteAsync(site, product, timestamp);
                if (frame != null)
                    return frame;
            }
            return null;
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
                // Round timestamp to nearest 5-minute interval
                var roundedTime = RoundToNearest5Minutes(timestamp);

                // Build IEM archive URL
                // Format: https://mesonet.agron.iastate.edu/archive/data/YYYY/MM/DD/GIS/ridge/PRODUCT/SITE/SITE_PRODUCT_YYYY_MM_DD_HHMM.png
                var productCode = GetProductCode(product);
                var year = roundedTime.Year;
                var month = roundedTime.Month.ToString("D2");
                var day = roundedTime.Day.ToString("D2");
                var hour = roundedTime.Hour.ToString("D2");
                var minute = roundedTime.Minute.ToString("D2");

                var fileName = $"{site.SiteId}_{productCode}_{year}_{month}_{day}_{hour}{minute}.png";
                var imageUrl = $"{IEM_ARCHIVE_BASE}/{year}/{month}/{day}/GIS/ridge/{productCode}/{site.SiteId}/{fileName}";

                System.Diagnostics.Debug.WriteLine($"Attempting to fetch: {imageUrl}");

                // Try to download the image
                var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);

                // Check if we got valid data
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"No image data received from {imageUrl}");
                    return null;
                }

                // Check if response is too small (likely an error page)
                if (imageBytes.Length < 1000)
                {
                    System.Diagnostics.Debug.WriteLine($"Response too small ({imageBytes.Length} bytes), likely not a valid image");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"Successfully downloaded {imageBytes.Length} bytes for {site.SiteId} at {roundedTime:HH:mm}");

                // Convert to BitmapImage
                var bitmap = new BitmapImage();
                using (var stream = new MemoryStream(imageBytes))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }

                return new RadarFrame
                {
                    Timestamp = roundedTime,
                    ImageUrl = imageUrl,
                    Image = bitmap,
                    Product = product,
                    CenterLatitude = site.Latitude,
                    CenterLongitude = site.Longitude,
                    ZoomLevel = 8
                };
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"HTTP error loading frame for {site.SiteId} at {timestamp:HH:mm}: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading Level 2 frame from {site.SiteId} at {timestamp:HH:mm}: {ex.Message}");
                return null;
            }
        }

        private DateTime RoundToNearest5Minutes(DateTime timestamp)
        {
            // Round down to nearest 5-minute interval
            var minutes = timestamp.Minute;
            var roundedMinutes = (minutes / 5) * 5;
            return new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, roundedMinutes, 0, DateTimeKind.Utc);
        }

        private string GetProductCode(RadarFrame.RadarProduct product)
        {
            // IEM archive uses different product codes than WMS
            return product switch
            {
                RadarFrame.RadarProduct.Reflectivity => "N0Q", // Base Reflectivity (high-res)
                RadarFrame.RadarProduct.Velocity => "N0U",     // Base Velocity
                RadarFrame.RadarProduct.CorrelationCoefficient => "N0C", // Correlation Coefficient
                RadarFrame.RadarProduct.DifferentialReflectivity => "N0X", // Differential Reflectivity
                RadarFrame.RadarProduct.SpecificDifferentialPhase => "N0K", // Specific Differential Phase
                _ => "N0Q"
            };
        }
    }
}
