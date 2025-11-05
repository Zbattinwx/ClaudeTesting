namespace OhioNewsWeather.WeatherApp.Models
{
    public class RadarSite
    {
        public string SiteId { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string State { get; set; }
        public int Elevation { get; set; }
        public bool IsPrimary { get; set; }

        public RadarSite()
        {
        }

        public RadarSite(string siteId, string name, string location, double latitude, double longitude, string state, int elevation, bool isPrimary = false)
        {
            SiteId = siteId;
            Name = name;
            Location = location;
            Latitude = latitude;
            Longitude = longitude;
            State = state;
            Elevation = elevation;
            IsPrimary = isPrimary;
        }

        public override string ToString() => $"{SiteId} - {Location}";
    }
}
