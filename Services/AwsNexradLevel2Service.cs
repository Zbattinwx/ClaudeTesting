using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OhioNewsWeather.WeatherApp.Models;

namespace OhioNewsWeather.WeatherApp.Services
{
    /// <summary>
    /// Professional NEXRAD Level 2 service using NOAA NOMADS real-time data
    /// Downloads and parses raw Level 2 files, renders high-quality radar imagery
    /// </summary>
    public class AwsNexradLevel2Service : IRadarService
    {
        private readonly HttpClient _httpClient;
        private readonly IRadarSiteService _radarSiteService;
        private readonly Level2Parser _parser;

        private const string NOMADS_BASE_URL = "https://nomads.ncep.noaa.gov/pub/data/nccf/radar/nexrad_level2";

        public AwsNexradLevel2Service(IHttpClientFactory httpClientFactory, IRadarSiteService radarSiteService)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(2);
            _radarSiteService = radarSiteService;
            _parser = new Level2Parser();
        }

        public async Task<RadarFrame[]> GetRadarFramesAsync(RadarSite site, RadarFrame.RadarProduct product = RadarFrame.RadarProduct.Reflectivity, int frameCount = 10)
        {
            var frames = new List<RadarFrame>();

            System.Diagnostics.Debug.WriteLine($"=== Fetching {frameCount} Level 2 files for {site.SiteId} ===");

            try
            {
                var files = await ListLevel2FilesAsync(site.SiteId, frameCount);

                if (files == null || files.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"No Level 2 files found");
                    return frames.ToArray();
                }

                System.Diagnostics.Debug.WriteLine($"Found {files.Count} files, processing...");

                foreach (var fileInfo in files.Take(frameCount))
                {
                    try
                    {
                        var frame = await ProcessLevel2FileAsync(site, fileInfo, product);
                        if (frame != null)
                        {
                            frames.Add(frame);
                            System.Diagnostics.Debug.WriteLine($"✓ Frame {frames.Count}/{frameCount}: {fileInfo.Timestamp:HH:mm:ss}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"✗ Failed {fileInfo.FileName}: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"=== Successfully loaded {frames.Count} radar frames ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: {ex.Message}");
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

        private async Task<List<Level2FileInfo>> ListLevel2FilesAsync(string siteId, int maxFiles = 10)
        {
            try
            {
                var files = new List<Level2FileInfo>();
                var directoryUrl = $"{NOMADS_BASE_URL}/{siteId}/";

                System.Diagnostics.Debug.WriteLine($"Listing: {directoryUrl}");

                var html = await _httpClient.GetStringAsync(directoryUrl);

                // Parse HTML for .bz2 files
                var pattern = $@"{siteId}_(\d{{8}})_(\d{{6}})\.bz2";
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                var matches = regex.Matches(html);

                foreach (Match match in matches)
                {
                    if (match.Success && match.Groups.Count >= 3)
                    {
                        var fileName = match.Value;
                        var datePart = match.Groups[1].Value;
                        var timePart = match.Groups[2].Value;

                        var timestamp = ParseTimestamp(datePart, timePart);
                        if (timestamp.HasValue)
                        {
                            files.Add(new Level2FileInfo
                            {
                                FileName = fileName,
                                Url = $"{directoryUrl}{fileName}",
                                Timestamp = timestamp.Value
                            });
                        }
                    }
                }

                return files.OrderByDescending(f => f.Timestamp).Take(maxFiles).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error listing files: {ex.Message}");
                return new List<Level2FileInfo>();
            }
        }

        private async Task<RadarFrame> ProcessLevel2FileAsync(RadarSite site, Level2FileInfo fileInfo, RadarFrame.RadarProduct product)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"\n--- Processing {fileInfo.FileName} ---");

                // Download
                var fileData = await _httpClient.GetByteArrayAsync(fileInfo.Url);
                System.Diagnostics.Debug.WriteLine($"Downloaded: {fileData.Length / 1024}KB");

                // Parse
                var level2Data = _parser.ParseLevel2File(fileData);
                if (level2Data == null || level2Data.Sweeps.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Parse failed");
                    return null;
                }

                // Get base sweep
                var baseSweep = level2Data.Sweeps.OrderBy(s => s.ElevationAngle).FirstOrDefault();
                if (baseSweep == null || baseSweep.Radials.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No valid sweep");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"Rendering: {baseSweep.Radials.Count} radials at {baseSweep.ElevationAngle:F1}°");

                // Render
                var image = RenderRadarData(baseSweep, product, site);
                if (image == null)
                {
                    System.Diagnostics.Debug.WriteLine("Render failed");
                    return null;
                }

                return new RadarFrame
                {
                    Timestamp = fileInfo.Timestamp,
                    ImageUrl = fileInfo.Url,
                    Image = image,
                    Product = product,
                    CenterLatitude = site.Latitude,
                    CenterLongitude = site.Longitude,
                    ZoomLevel = 8
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Processing error: {ex.Message}");
                return null;
            }
        }

        private BitmapImage RenderRadarData(Level2Sweep sweep, RadarFrame.RadarProduct product, RadarSite site)
        {
            try
            {
                int width = 1800;
                int height = 1800;
                int centerX = width / 2;
                int centerY = height / 2;

                byte[] pixels = new byte[width * height * 4];

                // Initialize transparent
                for (int i = 0; i < pixels.Length; i += 4)
                {
                    pixels[i] = 0;     // B
                    pixels[i + 1] = 0; // G
                    pixels[i + 2] = 0; // R
                    pixels[i + 3] = 0; // A
                }

                double maxRangeKm = 230.0;
                double pixelsPerKm = (width / 2.0) * 0.9 / maxRangeKm;

                int validGates = 0;
                foreach (var radial in sweep.Radials)
                {
                    if (radial.ReflectivityGates == null || radial.ReflectivityGates.Count == 0)
                        continue;

                    // Convert azimuth (0° = North, clockwise)
                    double azimuthRad = (radial.Azimuth - 90.0) * Math.PI / 180.0;
                    double cosAz = Math.Cos(azimuthRad);
                    double sinAz = Math.Sin(azimuthRad);

                    double gateSpacingKm = 0.25;

                    for (int gateIndex = 0; gateIndex < radial.ReflectivityGates.Count; gateIndex++)
                    {
                        float value = radial.ReflectivityGates[gateIndex];

                        if (float.IsNaN(value) || value < -30) continue;

                        validGates++;

                        double rangeKm = gateIndex * gateSpacingKm;
                        if (rangeKm > maxRangeKm) break;

                        double rangePixels = rangeKm * pixelsPerKm;

                        int px = centerX + (int)(rangePixels * cosAz);
                        int py = centerY + (int)(rangePixels * sinAz);

                        int radius = Math.Max(2, (int)(pixelsPerKm * gateSpacingKm * 0.6));

                        for (int dy = -radius; dy <= radius; dy++)
                        {
                            for (int dx = -radius; dx <= radius; dx++)
                            {
                                if (dx * dx + dy * dy > radius * radius) continue;

                                int x = px + dx;
                                int y = py + dy;

                                if (x >= 0 && x < width && y >= 0 && y < height)
                                {
                                    var color = GetReflectivityColor(value);

                                    int pixelIndex = (y * width + x) * 4;
                                    pixels[pixelIndex] = color.B;
                                    pixels[pixelIndex + 1] = color.G;
                                    pixels[pixelIndex + 2] = color.R;
                                    pixels[pixelIndex + 3] = color.A;
                                }
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Rendered {validGates} gates");

                var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);

                var bitmapImage = new BitmapImage();
                using (var stream = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(stream);
                    stream.Position = 0;

                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                }

                return bitmapImage;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Render error: {ex.Message}");
                return null;
            }
        }

        private Color GetReflectivityColor(float dbz)
        {
            // NWS standard reflectivity color scale
            if (dbz < 5) return Color.FromArgb(0, 0, 0, 0);
            if (dbz < 10) return Color.FromArgb(200, 4, 233, 231);
            if (dbz < 15) return Color.FromArgb(220, 1, 159, 244);
            if (dbz < 20) return Color.FromArgb(240, 3, 0, 244);
            if (dbz < 25) return Color.FromArgb(255, 2, 253, 2);
            if (dbz < 30) return Color.FromArgb(255, 1, 197, 1);
            if (dbz < 35) return Color.FromArgb(255, 0, 142, 0);
            if (dbz < 40) return Color.FromArgb(255, 253, 248, 2);
            if (dbz < 45) return Color.FromArgb(255, 229, 188, 0);
            if (dbz < 50) return Color.FromArgb(255, 253, 139, 0);
            if (dbz < 55) return Color.FromArgb(255, 212, 0, 0);
            if (dbz < 60) return Color.FromArgb(255, 188, 0, 0);
            if (dbz < 65) return Color.FromArgb(255, 248, 0, 253);
            if (dbz < 70) return Color.FromArgb(255, 152, 84, 198);
            return Color.FromArgb(255, 253, 253, 253);
        }

        private DateTime? ParseTimestamp(string datePart, string timePart)
        {
            try
            {
                int year = int.Parse(datePart.Substring(0, 4));
                int month = int.Parse(datePart.Substring(4, 2));
                int day = int.Parse(datePart.Substring(6, 2));
                int hour = int.Parse(timePart.Substring(0, 2));
                int minute = int.Parse(timePart.Substring(2, 2));
                int second = int.Parse(timePart.Substring(4, 2));

                return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
            }
            catch
            {
                return null;
            }
        }

        private class Level2FileInfo
        {
            public string FileName { get; set; }
            public string Url { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
