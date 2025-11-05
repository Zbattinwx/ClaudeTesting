using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OhioNewsWeather.WeatherApp.Models;

namespace OhioNewsWeather.WeatherApp.Services
{
    public class NoaaWeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.weather.gov";

        public NoaaWeatherService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "(OhioWeather, contact@ohionewsweather.com)");
        }

        public async Task<WeatherConditions> GetCurrentConditionsAsync(Location location)
        {
            try
            {
                // Get grid point
                var pointUrl = $"{BaseUrl}/points/{location.Latitude},{location.Longitude}";
                var pointResponse = await _httpClient.GetStringAsync(pointUrl);
                var pointData = JsonDocument.Parse(pointResponse);

                var observationStationsUrl = pointData.RootElement
                    .GetProperty("properties")
                    .GetProperty("observationStations")
                    .GetString();

                // Get nearest station
                var stationsResponse = await _httpClient.GetStringAsync(observationStationsUrl);
                var stationsData = JsonDocument.Parse(stationsResponse);
                var stationUrl = stationsData.RootElement
                    .GetProperty("features")[0]
                    .GetProperty("id")
                    .GetString();

                // Get latest observation
                var observationUrl = $"{stationUrl}/observations/latest";
                var obsResponse = await _httpClient.GetStringAsync(observationUrl);
                var obsData = JsonDocument.Parse(obsResponse);
                var properties = obsData.RootElement.GetProperty("properties");

                return new WeatherConditions
                {
                    Location = location,
                    Timestamp = properties.GetProperty("timestamp").GetDateTime(),
                    Temperature = GetCelsiusValue(properties, "temperature"),
                    Dewpoint = GetCelsiusValue(properties, "dewpoint"),
                    Humidity = GetValue(properties, "relativeHumidity"),
                    WindSpeed = GetValue(properties, "windSpeed"),
                    WindDirection = (int)GetValue(properties, "windDirection"),
                    WindGust = GetValue(properties, "windGust"),
                    Pressure = GetValue(properties, "barometricPressure"),
                    Visibility = GetValue(properties, "visibility"),
                    Conditions = properties.GetProperty("textDescription").GetString()
                };
            }
            catch (Exception)
            {
                // Return default conditions on error
                return new WeatherConditions { Location = location };
            }
        }

        public async Task<Forecast> GetForecastAsync(Location location)
        {
            try
            {
                var pointUrl = $"{BaseUrl}/points/{location.Latitude},{location.Longitude}";
                var pointResponse = await _httpClient.GetStringAsync(pointUrl);
                var pointData = JsonDocument.Parse(pointResponse);

                var forecastUrl = pointData.RootElement
                    .GetProperty("properties")
                    .GetProperty("forecast")
                    .GetString();

                var forecastResponse = await _httpClient.GetStringAsync(forecastUrl);
                var forecastData = JsonDocument.Parse(forecastResponse);
                var periods = forecastData.RootElement.GetProperty("properties").GetProperty("periods");

                var forecastPeriods = new System.Collections.Generic.List<ForecastPeriod>();
                foreach (var period in periods.EnumerateArray())
                {
                    forecastPeriods.Add(new ForecastPeriod
                    {
                        StartTime = period.GetProperty("startTime").GetDateTime(),
                        EndTime = period.GetProperty("endTime").GetDateTime(),
                        Name = period.GetProperty("name").GetString(),
                        Temperature = period.GetProperty("temperature").GetDouble(),
                        TemperatureUnit = period.GetProperty("temperatureUnit").GetString(),
                        WindSpeed = period.GetProperty("windSpeed").GetString(),
                        WindDirection = period.GetProperty("windDirection").GetString(),
                        ShortForecast = period.GetProperty("shortForecast").GetString(),
                        DetailedForecast = period.GetProperty("detailedForecast").GetString(),
                        Icon = period.GetProperty("icon").GetString()
                    });
                }

                return new Forecast
                {
                    Location = location,
                    Periods = forecastPeriods.ToArray()
                };
            }
            catch (Exception)
            {
                return new Forecast { Location = location, Periods = Array.Empty<ForecastPeriod>() };
            }
        }

        public async Task<HourlyForecast[]> GetHourlyForecastAsync(Location location)
        {
            try
            {
                var pointUrl = $"{BaseUrl}/points/{location.Latitude},{location.Longitude}";
                var pointResponse = await _httpClient.GetStringAsync(pointUrl);
                var pointData = JsonDocument.Parse(pointResponse);

                var hourlyForecastUrl = pointData.RootElement
                    .GetProperty("properties")
                    .GetProperty("forecastHourly")
                    .GetString();

                var forecastResponse = await _httpClient.GetStringAsync(hourlyForecastUrl);
                var forecastData = JsonDocument.Parse(forecastResponse);
                var periods = forecastData.RootElement.GetProperty("properties").GetProperty("periods");

                var hourlyForecasts = new System.Collections.Generic.List<HourlyForecast>();
                foreach (var period in periods.EnumerateArray())
                {
                    hourlyForecasts.Add(new HourlyForecast
                    {
                        Time = period.GetProperty("startTime").GetDateTime(),
                        Temperature = period.GetProperty("temperature").GetDouble(),
                        Humidity = period.GetProperty("relativeHumidity").GetProperty("value").GetInt32(),
                        Dewpoint = period.GetProperty("dewpoint").GetProperty("value").GetDouble(),
                        WindSpeed = period.GetProperty("windSpeed").GetString(),
                        WindDirection = period.GetProperty("windDirection").GetString(),
                        ShortForecast = period.GetProperty("shortForecast").GetString(),
                        Icon = period.GetProperty("icon").GetString()
                    });
                }

                return hourlyForecasts.ToArray();
            }
            catch (Exception)
            {
                return Array.Empty<HourlyForecast>();
            }
        }

        private double GetCelsiusValue(JsonElement properties, string propertyName)
        {
            try
            {
                var value = properties.GetProperty(propertyName).GetProperty("value");
                if (value.ValueKind == JsonValueKind.Null)
                    return 0;
                return value.GetDouble();
            }
            catch
            {
                return 0;
            }
        }

        private double GetValue(JsonElement properties, string propertyName)
        {
            try
            {
                var value = properties.GetProperty(propertyName).GetProperty("value");
                if (value.ValueKind == JsonValueKind.Null)
                    return 0;
                return value.GetDouble();
            }
            catch
            {
                return 0;
            }
        }
    }
}
