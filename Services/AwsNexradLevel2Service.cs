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
    /// Service for accessing and processing NEXRAD Level 2 data from NOAA NOMADS NCEP server
    /// Downloads raw Level 2 files, parses binary data, and renders professional-grade radar imagery
    /// </summary>
    public class AwsNexradLevel2Service : IRadarService
    {
        private readonly HttpClient _httpClient;
        private readonly IRadarSiteService _radarSiteService;
        private readonly Level2Parser _parser;

        // NOAA NOMADS NCEP real-time Level 2 radar data
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

            System.Diagnostics.Debug.WriteLine($"Fetching {frameCount} Level 2 files from NOMADS NCEP for {site.SiteId}");

            try
            {
                // Get list of available files for this site
                var files = await ListLevel2FilesAsync(site.SiteId, frameCount);

                if (files == null || files.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"No Level 2 files found for {site.SiteId}");
                    return frames.ToArray();
                }

                System.Diagnostics.Debug.WriteLine($"Found {files.Count} Level 2 files");

                // Process each file (most recent first)
                foreach (var fileInfo in files.Take(frameCount))
                {
                    try
                    {
                        var frame = await ProcessLevel2FileAsync(site, fileInfo, product);
                        if (frame != null)
                        {
                            frames.Add(frame);
                            System.Diagnostics.Debug.WriteLine($"Loaded frame {frames.Count}/{frameCount} from {fileInfo.Timestamp:HH:mm:ss}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing file {fileInfo.FileName}: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Successfully loaded {frames.Count} Level 2 frames");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching Level 2 frames: {ex.Message}");
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

        /// <summary>
        /// List Level 2 files from NOMADS NCEP server by parsing directory listing
        /// </summary>
        private async Task<List<Level2FileInfo>> ListLevel2FilesAsync(string siteId, int maxFiles = 10)
        {
            try
            {
                var files = new List<Level2FileInfo>();

                // NOMADS directory structure: /SITE/ (e.g., /KCLE/)
                var directoryUrl = $"{NOMADS_BASE_URL}/{siteId}/";

                System.Diagnostics.Debug.WriteLine($"Listing NOMADS files: {directoryUrl}");

                try
                {
                    var html = await _httpClient.GetStringAsync(directoryUrl);

                    // Parse HTML directory listing
                    // Look for links like: KCLE_20251105_092830.bz2
                    var pattern = $@"{siteId}_(\d{{8}})_(\d{{6}})\.bz2";
                    var regex = new Regex(pattern, RegexOptions.IgnoreCase);

                    var matches = regex.Matches(html);

                    foreach (Match match in matches)
                    {
                        if (match.Success && match.Groups.Count >= 3)
                        {
                            var fileName = match.Value;
                            var datePart = match.Groups[1].Value; // YYYYMMDD
                            var timePart = match.Groups[2].Value; // HHMMSS

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

                    System.Diagnostics.Debug.WriteLine($"Found {files.Count} files in directory listing");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error listing NOMADS files: {ex.Message}");
                }

                // Sort by timestamp (newest first) and return requested count
                return files.OrderByDescending(f => f.Timestamp).Take(maxFiles).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error listing Level 2 files: {ex.Message}");
                return new List<Level2FileInfo>();
            }
        }

        /// <summary>
        /// Process a single Level 2 file: download, parse, render
        /// </summary>
        private async Task<RadarFrame> ProcessLevel2FileAsync(RadarSite site, Level2FileInfo fileInfo, RadarFrame.RadarProduct product)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Downloading {fileInfo.FileName} from NOMADS");

                // Download file
                var fileData = await _httpClient.GetByteArrayAsync(fileInfo.Url);
                System.Diagnostics.Debug.WriteLine($"Downloaded {fileData.Length / 1024}KB ({fileData.Length} bytes)");

                // Parse Level 2 data (parser will handle bzip2 decompression)
                var level2Data = _parser.ParseLevel2File(fileData);
                if (level2Data == null || level2Data.Sweeps.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to parse Level 2 data");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"Parsed {level2Data.Sweeps.Count} sweeps");

                // Get lowest elevation sweep (base scan)
                var baseSweep = level2Data.Sweeps.OrderBy(s => s.ElevationAngle).FirstOrDefault();
                if (baseSweep == null || baseSweep.Radials.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No base sweep found");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"Using base sweep at {baseSweep.ElevationAngle:F1}° with {baseSweep.Radials.Count} radials");

                // Render radar image
                var image = RenderRadarData(baseSweep, product, site);
                if (image == null)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to render radar image");
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
                System.Diagnostics.Debug.WriteLine($"Error processing Level 2 file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Render Level 2 radar data to a high-quality image
        /// </summary>
        private BitmapImage RenderRadarData(Level2Sweep sweep, RadarFrame.RadarProduct product, RadarSite site)
        {
            try
            {
                int width = 1800;  // High resolution for professional display
                int height = 1800;
                int centerX = width / 2;
                int centerY = height / 2;

                // Create pixel array (BGRA format)
                byte[] pixels = new byte[width * height * 4];

                // Initialize with transparent background
                for (int i = 0; i < pixels.Length; i += 4)
                {
                    pixels[i] = 0;     // B
                    pixels[i + 1] = 0; // G
                    pixels[i + 2] = 0; // R
                    pixels[i + 3] = 0; // A (transparent)
                }

                // Render each radial
                double maxRangeKm = 230.0; // Maximum range in km
                double pixelsPerKm = (width / 2.0) * 0.9 / maxRangeKm; // Use 90% of canvas

                int radialCount = 0;
                foreach (var radial in sweep.Radials)
                {
                    if (radial.ReflectivityGates == null || radial.ReflectivityGates.Count == 0)
                        continue;

                    radialCount++;

                    // Convert azimuth to radians (meteorological azimuth: 0° = North, clockwise)
                    double azimuthRad = (radial.Azimuth - 90.0) * Math.PI / 180.0;

                    double cosAz = Math.Cos(azimuthRad);
                    double sinAz = Math.Sin(azimuthRad);

                    // Draw each gate along this radial
                    double gateSpacingKm = 0.25; // 250m resolution for base reflectivity

                    for (int gateIndex = 0; gateIndex < radial.ReflectivityGates.Count; gateIndex++)
                    {
                        float value = radial.ReflectivityGates[gateIndex];

                        // Skip missing/invalid data
                        if (float.IsNaN(value) || value < -30)
                            continue;

                        // Calculate range for this gate
                        double rangeKm = gateIndex * gateSpacingKm;
                        if (rangeKm > maxRangeKm) break;

                        double rangePixels = rangeKm * pixelsPerKm;

                        // Calculate pixel position
                        int px = centerX + (int)(rangePixels * cosAz);
                        int py = centerY + (int)(rangePixels * sinAz);

                        // Draw a small filled circle for this gate to smooth appearance
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
                                    // Get color for this value
                                    var color = GetReflectivityColor(value);

                                    // Set pixel (BGRA format)
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

                System.Diagnostics.Debug.WriteLine($"Rendered {radialCount} radials to {width}x{height} image");

                // Create BitmapSource
                var bitmap = BitmapSource.Create(
                    width, height,
                    96, 96,
                    PixelFormats.Bgra32,
                    null,
                    pixels,
                    width * 4);

                // Convert to BitmapImage
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
                System.Diagnostics.Debug.WriteLine($"Error rendering radar data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Professional-grade NEXRAD reflectivity color scale
        /// </summary>
        private Color GetReflectivityColor(float dbz)
        {
            // NWS standard reflectivity color scale
            if (dbz < 5) return Color.FromArgb(0, 0, 0, 0);           // Transparent (no echo)
            if (dbz < 10) return Color.FromArgb(200, 4, 233, 231);     // Pale cyan
            if (dbz < 15) return Color.FromArgb(220, 1, 159, 244);     // Light blue
            if (dbz < 20) return Color.FromArgb(240, 3, 0, 244);       // Blue
            if (dbz < 25) return Color.FromArgb(255, 2, 253, 2);       // Bright green
            if (dbz < 30) return Color.FromArgb(255, 1, 197, 1);       // Green
            if (dbz < 35) return Color.FromArgb(255, 0, 142, 0);       // Dark green
            if (dbz < 40) return Color.FromArgb(255, 253, 248, 2);     // Yellow
            if (dbz < 45) return Color.FromArgb(255, 229, 188, 0);     // Gold
            if (dbz < 50) return Color.FromArgb(255, 253, 139, 0);     // Orange
            if (dbz < 55) return Color.FromArgb(255, 212, 0, 0);       // Red
            if (dbz < 60) return Color.FromArgb(255, 188, 0, 0);       // Dark red
            if (dbz < 65) return Color.FromArgb(255, 248, 0, 253);     // Magenta
            if (dbz < 70) return Color.FromArgb(255, 152, 84, 198);    // Purple
            return Color.FromArgb(255, 253, 253, 253);                 // White (extreme)
        }

        private DateTime? ParseTimestamp(string datePart, string timePart)
        {
            try
            {
                // YYYYMMDD format
                int year = int.Parse(datePart.Substring(0, 4));
                int month = int.Parse(datePart.Substring(4, 2));
                int day = int.Parse(datePart.Substring(6, 2));

                // HHMMSS format
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
