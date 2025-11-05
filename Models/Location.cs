namespace OhioNewsWeather.WeatherApp.Models
{
    public class Location
    {
        public string Name { get; set; }
        public string City { get; set; }
        public string County { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string State { get; set; } = "OH";
        public int Population { get; set; }
        public bool IsDefault { get; set; }

        public Location()
        {
        }

        public Location(string name, string city, string county, double latitude, double longitude, int population = 0)
        {
            Name = name;
            City = city;
            County = county;
            Latitude = latitude;
            Longitude = longitude;
            Population = population;
        }

        public override string ToString() => $"{City}, {State}";
    }
}
