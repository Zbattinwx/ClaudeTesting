using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OhioNewsWeather.WeatherApp.Models;

namespace OhioNewsWeather.WeatherApp.Services
{
    public class NoaaAlertService : IAlertService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.weather.gov/alerts";

        public NoaaAlertService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "(OhioWeather, contact@ohionewsweather.com)");
        }

        public async Task<WeatherAlert[]> GetActiveAlertsAsync(string state = "OH")
        {
            try
            {
                var url = $"{BaseUrl}/active?area={state}";
                var response = await _httpClient.GetStringAsync(url);
                var data = JsonDocument.Parse(response);
                var features = data.RootElement.GetProperty("features");

                var alerts = new List<WeatherAlert>();
                foreach (var feature in features.EnumerateArray())
                {
                    var properties = feature.GetProperty("properties");
                    var geometry = feature.GetProperty("geometry");

                    var alert = new WeatherAlert
                    {
                        Id = properties.GetProperty("id").GetString(),
                        Event = properties.GetProperty("event").GetString(),
                        Headline = GetStringOrNull(properties, "headline"),
                        Description = GetStringOrNull(properties, "description"),
                        Instruction = GetStringOrNull(properties, "instruction"),
                        Severity = ParseSeverity(GetStringOrNull(properties, "severity")),
                        Urgency = GetStringOrNull(properties, "urgency"),
                        Onset = properties.GetProperty("onset").GetDateTime(),
                        Expires = properties.GetProperty("expires").GetDateTime(),
                        SenderName = GetStringOrNull(properties, "senderName")
                    };

                    // Parse affected zones
                    if (properties.TryGetProperty("affectedZones", out var zones))
                    {
                        var zoneList = new List<string>();
                        foreach (var zone in zones.EnumerateArray())
                        {
                            zoneList.Add(zone.GetString());
                        }
                        alert.AffectedZones = zoneList.ToArray();
                    }

                    // Parse geometry (polygon coordinates)
                    if (geometry.ValueKind != JsonValueKind.Null)
                    {
                        alert.Polygons = ParseGeometry(geometry);
                    }

                    alerts.Add(alert);
                }

                return alerts.OrderByDescending(a => a.Severity).ToArray();
            }
            catch (Exception)
            {
                return Array.Empty<WeatherAlert>();
            }
        }

        public async Task<WeatherAlert[]> GetAlertsByLocationAsync(Location location)
        {
            try
            {
                var url = $"{BaseUrl}/active?point={location.Latitude},{location.Longitude}";
                var response = await _httpClient.GetStringAsync(url);
                var data = JsonDocument.Parse(response);
                var features = data.RootElement.GetProperty("features");

                var alerts = new List<WeatherAlert>();
                foreach (var feature in features.EnumerateArray())
                {
                    var properties = feature.GetProperty("properties");
                    alerts.Add(new WeatherAlert
                    {
                        Id = properties.GetProperty("id").GetString(),
                        Event = properties.GetProperty("event").GetString(),
                        Headline = GetStringOrNull(properties, "headline"),
                        Description = GetStringOrNull(properties, "description"),
                        Severity = ParseSeverity(GetStringOrNull(properties, "severity")),
                        Onset = properties.GetProperty("onset").GetDateTime(),
                        Expires = properties.GetProperty("expires").GetDateTime()
                    });
                }

                return alerts.OrderByDescending(a => a.Severity).ToArray();
            }
            catch (Exception)
            {
                return Array.Empty<WeatherAlert>();
            }
        }

        private string GetStringOrNull(JsonElement element, string propertyName)
        {
            try
            {
                if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null)
                {
                    return prop.GetString();
                }
            }
            catch { }
            return null;
        }

        private WeatherAlert.AlertSeverity ParseSeverity(string severity)
        {
            return severity?.ToLower() switch
            {
                "extreme" => WeatherAlert.AlertSeverity.Extreme,
                "severe" => WeatherAlert.AlertSeverity.Severe,
                "moderate" => WeatherAlert.AlertSeverity.Moderate,
                "minor" => WeatherAlert.AlertSeverity.Minor,
                _ => WeatherAlert.AlertSeverity.Unknown
            };
        }

        private List<Coordinate[]> ParseGeometry(JsonElement geometry)
        {
            var polygons = new List<Coordinate[]>();
            try
            {
                if (geometry.TryGetProperty("type", out var type) && type.GetString() == "Polygon")
                {
                    var coordinates = geometry.GetProperty("coordinates")[0];
                    var coords = new List<Coordinate>();
                    foreach (var coord in coordinates.EnumerateArray())
                    {
                        coords.Add(new Coordinate(coord[1].GetDouble(), coord[0].GetDouble()));
                    }
                    polygons.Add(coords.ToArray());
                }
            }
            catch { }
            return polygons;
        }
    }
}
