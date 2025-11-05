using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using OhioNewsWeather.WeatherApp.Models;

namespace OhioNewsWeather.WeatherApp.Services
{
    /// <summary>
    /// Service for accessing high-quality individual NEXRAD site imagery from Iowa State Mesonet
    /// Uses RIDGE2 service which provides individual site images generated from Level 2 data
    /// </summary>
    public class AwsNexradLevel2Service : IRadarService
    {
        private readonly HttpClient _httpClient;
        private readonly IRadarSiteService _radarSiteService;

        // Iowa State Mesonet RIDGE2 service - individual site composite imagery
        private const string IEM_RIDGE_BASE = "https://mesonet.agron.iastate.edu/cache/ridge/single";

        public AwsNexradLevel2Service(IHttpClientFactory httpClientFactory, IRadarSiteService radarSiteService)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _radarSiteService = radarSiteService;
        }

        public async Task<RadarFrame[]> GetRadarFramesAsync(RadarSite site, RadarFrame.RadarProduct product = RadarFrame.RadarProduct.Reflectivity, int frameCount = 10)
        {
            var frames = new List<RadarFrame>();

            System.Diagnostics.Debug.WriteLine($"Fetching {frameCount} RIDGE2 frames for {site.SiteId}");

            try
            {
                // Get list of available images from RIDGE2 service
                var availableImages = await ListAvailableImagesAsync(site.SiteId, product);

                if (availableImages == null || availableImages.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"No images found for {site.SiteId}");
                    return frames.ToArray();
                }

                System.Diagnostics.Debug.WriteLine($"Found {availableImages.Count} available images");

                // Get the most recent frames
                foreach (var imageInfo in availableImages.Take(frameCount))
                {
                    try
                    {
                        var frame = await DownloadRadarFrameAsync(site, imageInfo, product);
                        if (frame != null)
                        {
                            frames.Add(frame);
                            System.Diagnostics.Debug.WriteLine($"Loaded frame {frames.Count}/{frameCount} from {imageInfo.Timestamp:HH:mm:ss}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading frame: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Successfully loaded {frames.Count} radar frames");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching radar frames: {ex.Message}");
            }

            return frames.ToArray();
        }

        public async Task<RadarFrame> GetLatestRadarFrameAsync(RadarSite site, RadarFrame.RadarProduct product = RadarFrame.RadarProduct.Reflectivity)
        {
            var frames = await GetRadarFramesAsync(site, product, 1);
            return frames.Length > 0 ? frames[0] : null;
        }

        public async Task<RadarFrame[]> GetRadarFramesByLocationAsync(Location location, int frameCount = 10)
        {
            var nearestSite = _radarSiteService.GetNearestRadarSite(location);
            return await GetRadarFramesAsync(nearestSite, RadarFrame.RadarProduct.Reflectivity, frameCount);
        }

        private async Task<List<ImageInfo>> ListAvailableImagesAsync(string siteId, RadarFrame.RadarProduct product)
        {
            try
            {
                var productCode = GetProductCode(product);
                var images = new List<ImageInfo>();

                // RIDGE2 directory listing
                var directoryUrl = $"{IEM_RIDGE_BASE}/{siteId}/{productCode}/";

                System.Diagnostics.Debug.WriteLine($"Listing images from: {directoryUrl}");

                var html = await _httpClient.GetStringAsync(directoryUrl);

                // Parse HTML directory listing for image files
                // Format: KCLE_N0Q_2024_11_05_1830.png
                var pattern = $@"{siteId}_{productCode}_(\d{{4}})_(\d{{2}})_(\d{{2}})_(\d{{4}})\.png";
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);

                var matches = regex.Matches(html);

                foreach (Match match in matches)
                {
                    if (match.Success && match.Groups.Count >= 5)
                    {
                        var fileName = match.Value;
                        var year = int.Parse(match.Groups[1].Value);
                        var month = int.Parse(match.Groups[2].Value);
                        var day = int.Parse(match.Groups[3].Value);
                        var time = match.Groups[4].Value; // HHMM

                        int hour = int.Parse(time.Substring(0, 2));
                        int minute = int.Parse(time.Substring(2, 2));

                        var timestamp = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);

                        images.Add(new ImageInfo
                        {
                            FileName = fileName,
                            Url = $"{directoryUrl}{fileName}",
                            Timestamp = timestamp
                        });
                    }
                }

                // Sort by timestamp, newest first
                return images.OrderByDescending(i => i.Timestamp).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error listing images: {ex.Message}");
                return new List<ImageInfo>();
            }
        }

        private async Task<RadarFrame> DownloadRadarFrameAsync(RadarSite site, ImageInfo imageInfo, RadarFrame.RadarProduct product)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Downloading: {imageInfo.FileName}");

                var imageBytes = await _httpClient.GetByteArrayAsync(imageInfo.Url);

                if (imageBytes == null || imageBytes.Length < 1000)
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid image data ({imageBytes?.Length ?? 0} bytes)");
                    return null;
                }

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
                    Timestamp = imageInfo.Timestamp,
                    ImageUrl = imageInfo.Url,
                    Image = bitmap,
                    Product = product,
                    CenterLatitude = site.Latitude,
                    CenterLongitude = site.Longitude,
                    ZoomLevel = 8
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error downloading frame: {ex.Message}");
                return null;
            }
        }

        private string GetProductCode(RadarFrame.RadarProduct product)
        {
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

        private class ImageInfo
        {
            public string FileName { get; set; }
            public string Url { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
