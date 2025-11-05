using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using OhioNewsWeather.WeatherApp.Models;

namespace OhioNewsWeather.WeatherApp.Services
{
    public class GoesSatelliteService : ISatelliteService
    {
        private readonly HttpClient _httpClient;
        private const string SatelliteBaseUrl = "https://cdn.star.nesdis.noaa.gov/GOES16/ABI/CONUS";

        public GoesSatelliteService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<SatelliteImage[]> GetVisibleSatelliteAsync(int frameCount = 10)
        {
            return await GetSatelliteImagesAsync("GEOCOLOR", SatelliteImage.SatelliteType.Visible, frameCount);
        }

        public async Task<SatelliteImage[]> GetInfraredSatelliteAsync(int frameCount = 10)
        {
            return await GetSatelliteImagesAsync("13", SatelliteImage.SatelliteType.Infrared, frameCount);
        }

        private async Task<SatelliteImage[]> GetSatelliteImagesAsync(string channel, SatelliteImage.SatelliteType type, int frameCount)
        {
            var images = new System.Collections.Generic.List<SatelliteImage>();

            try
            {
                // Get the latest image (GOES updates every 5 minutes)
                var baseUrl = $"{SatelliteBaseUrl}/{channel}";

                // For simplicity, we'll get the latest image URL pattern
                // In production, you'd parse the available images from the directory
                var now = DateTime.UtcNow;

                for (int i = frameCount - 1; i >= 0; i--)
                {
                    var timestamp = now.AddMinutes(-5 * i);
                    var imageUrl = $"{baseUrl}/{timestamp:yyyyMMddHHmm}.jpg";

                    try
                    {
                        var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
                        var bitmap = new BitmapImage();
                        using (var stream = new System.IO.MemoryStream(imageBytes))
                        {
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = stream;
                            bitmap.EndInit();
                            bitmap.Freeze();
                        }

                        images.Add(new SatelliteImage
                        {
                            Timestamp = timestamp,
                            ImageUrl = imageUrl,
                            Image = bitmap,
                            Type = type,
                            Sector = "CONUS"
                        });
                    }
                    catch
                    {
                        // Skip missing images
                        continue;
                    }
                }
            }
            catch (Exception)
            {
                // Return empty array on error
            }

            return images.ToArray();
        }
    }
}
