using System.Collections.Generic;
using System.Linq;
using OhioNewsWeather.WeatherApp.Models;

namespace OhioNewsWeather.WeatherApp.Services
{
    public class OhioLocationService : ILocationService
    {
        private readonly List<Location> _locations;

        public OhioLocationService()
        {
            _locations = InitializeOhioLocations();
        }

        public IEnumerable<Location> GetOhioLocations()
        {
            return _locations;
        }

        public Location GetLocationByName(string name)
        {
            return _locations.FirstOrDefault(l =>
                l.City.Equals(name, System.StringComparison.OrdinalIgnoreCase) ||
                l.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
        }

        public Location GetDefaultLocation()
        {
            return _locations.FirstOrDefault(l => l.IsDefault) ?? _locations.First();
        }

        private List<Location> InitializeOhioLocations()
        {
            return new List<Location>
            {
                // Major Cities
                new Location("Columbus", "Columbus", "Franklin", 39.9612, -82.9988, 905748) { IsDefault = true },
                new Location("Cleveland", "Cleveland", "Cuyahoga", 41.4993, -81.6944, 372624),
                new Location("Cincinnati", "Cincinnati", "Hamilton", 39.1031, -84.5120, 309317),
                new Location("Toledo", "Toledo", "Lucas", 41.6528, -83.5379, 272779),
                new Location("Akron", "Akron", "Summit", 41.0814, -81.5190, 190469),
                new Location("Dayton", "Dayton", "Montgomery", 39.7589, -84.1916, 137644),

                // Medium Cities (50k-100k)
                new Location("Parma", "Parma", "Cuyahoga", 41.4045, -81.7229, 78623),
                new Location("Canton", "Canton", "Stark", 40.7989, -81.3781, 70872),
                new Location("Youngstown", "Youngstown", "Mahoning", 41.0998, -80.6495, 60068),
                new Location("Lorain", "Lorain", "Lorain", 41.4528, -82.1824, 64097),
                new Location("Hamilton", "Hamilton", "Butler", 39.3995, -84.5613, 62477),
                new Location("Springfield", "Springfield", "Clark", 39.9242, -83.8088, 58662),

                // Small Cities (25k-50k)
                new Location("Kettering", "Kettering", "Montgomery", 39.6895, -84.1688, 55870),
                new Location("Elyria", "Elyria", "Lorain", 41.3683, -82.1076, 53844),
                new Location("Lakewood", "Lakewood", "Cuyahoga", 41.4820, -81.7982, 50942),
                new Location("Cuyahoga Falls", "Cuyahoga Falls", "Summit", 41.1339, -81.4846, 49267),
                new Location("Middletown", "Middletown", "Butler", 39.5150, -84.3983, 48630),
                new Location("Newark", "Newark", "Licking", 40.0581, -82.4013, 47573),
                new Location("Euclid", "Euclid", "Cuyahoga", 41.5931, -81.5268, 47676),
                new Location("Mansfield", "Mansfield", "Richland", 40.7584, -82.5154, 46830),
                new Location("Mentor", "Mentor", "Lake", 41.6662, -81.3398, 46979),
                new Location("Beavercreek", "Beavercreek", "Greene", 39.7092, -84.0633, 46549),
                new Location("Cleveland Heights", "Cleveland Heights", "Cuyahoga", 41.5201, -81.5563, 44310),
                new Location("Strongsville", "Strongsville", "Cuyahoga", 41.3145, -81.8357, 44750),
                new Location("Dublin", "Dublin", "Franklin", 40.0992, -83.1141, 47619),
                new Location("Fairfield", "Fairfield", "Butler", 39.3456, -84.5603, 42623),
                new Location("Findlay", "Findlay", "Hancock", 41.0442, -83.6499, 41512),
                new Location("Warren", "Warren", "Trumbull", 41.2376, -80.8184, 39201),
                new Location("Lancaster", "Lancaster", "Fairfield", 39.7137, -82.5993, 40552),
                new Location("Lima", "Lima", "Allen", 40.7426, -84.1052, 38771),
                new Location("Huber Heights", "Huber Heights", "Miami", 39.8439, -84.1246, 38142),
                new Location("Westerville", "Westerville", "Franklin", 40.1262, -82.9291, 39190),
                new Location("Marion", "Marion", "Marion", 40.5887, -83.1285, 36011),

                // Additional Towns (10k-25k)
                new Location("Grove City", "Grove City", "Franklin", 39.8814, -83.0930, 41252),
                new Location("Reynoldsburg", "Reynoldsburg", "Franklin", 39.9548, -82.8121, 41076),
                new Location("Delaware", "Delaware", "Delaware", 40.2987, -83.0680, 41302),
                new Location("Upper Arlington", "Upper Arlington", "Franklin", 39.9945, -83.0627, 35240),
                new Location("Gahanna", "Gahanna", "Franklin", 40.0192, -82.8796, 35726),
                new Location("Mason", "Mason", "Warren", 39.3600, -84.3097, 34792),
                new Location("Westlake", "Westlake", "Cuyahoga", 41.4553, -81.9179, 32990),
                new Location("North Olmsted", "North Olmsted", "Cuyahoga", 41.4156, -81.9235, 31283),
                new Location("North Royalton", "North Royalton", "Cuyahoga", 41.3137, -81.7246, 30444),
                new Location("Brunswick", "Brunswick", "Medina", 41.2381, -81.8418, 34629),
                new Location("Stow", "Stow", "Summit", 41.1595, -81.4404, 34770),
                new Location("Hilliard", "Hilliard", "Franklin", 40.0334, -83.1582, 36534),
                new Location("Sandusky", "Sandusky", "Erie", 41.4489, -82.7079, 24651),
                new Location("Bowling Green", "Bowling Green", "Wood", 41.3748, -83.6513, 31638),
                new Location("Chillicothe", "Chillicothe", "Ross", 39.3331, -82.9824, 22059),
                new Location("Ashtabula", "Ashtabula", "Ashtabula", 41.8651, -80.7898, 17882),
                new Location("Wooster", "Wooster", "Wayne", 40.8051, -81.9351, 27232),
                new Location("Xenia", "Xenia", "Greene", 39.6848, -83.9297, 25719),
                new Location("Athens", "Athens", "Athens", 39.3292, -82.1013, 25806),
                new Location("Marietta", "Marietta", "Washington", 39.4151, -81.4548, 13710),
                new Location("Portsmouth", "Portsmouth", "Scioto", 38.7317, -82.9977, 20226),
                new Location("Zanesville", "Zanesville", "Muskingum", 39.9403, -82.0132, 24765),
                new Location("Ashland", "Ashland", "Ashland", 40.8687, -82.3182, 19872),
                new Location("Sidney", "Sidney", "Shelby", 40.2842, -84.1555, 20421),
                new Location("Fremont", "Fremont", "Sandusky", 41.3503, -83.1216, 16023),
                new Location("Cambridge", "Cambridge", "Guernsey", 40.0312, -81.5885, 10269),
                new Location("Piqua", "Piqua", "Miami", 40.1448, -84.2424, 20354),
                new Location("Marysville", "Marysville", "Union", 40.2364, -83.3672, 25094),
                new Location("Defiance", "Defiance", "Defiance", 41.2845, -84.3558, 17066),
                new Location("Norwalk", "Norwalk", "Huron", 41.2425, -82.6157, 16238),
                new Location("Tiffin", "Tiffin", "Seneca", 41.1145, -83.1780, 17640),
                new Location("Bellefontaine", "Bellefontaine", "Logan", 40.3612, -83.7596, 14115),
                new Location("Washington Court House", "Washington Court House", "Fayette", 39.5370, -83.4391, 13819),
                new Location("Ironton", "Ironton", "Lawrence", 38.5370, -82.6835, 10571),
                new Location("Mount Vernon", "Mount Vernon", "Knox", 40.3934, -82.4857, 16956),
                new Location("Medina", "Medina", "Medina", 41.1384, -81.8638, 26777),
                new Location("Willoughby", "Willoughby", "Lake", 41.6397, -81.4065, 22904),
                new Location("Van Wert", "Van Wert", "Van Wert", 40.8695, -84.5841, 10690),
                new Location("Galion", "Galion", "Crawford", 40.7337, -82.7899, 10051),
                new Location("Urbana", "Urbana", "Champaign", 40.1084, -83.7524, 11613),
                new Location("Wilmington", "Wilmington", "Clinton", 39.4453, -83.8285, 12477),
                new Location("Greenville", "Greenville", "Darke", 40.1028, -84.6330, 12786),
                new Location("Bellevue", "Bellevue", "Sandusky", 41.2734, -82.8418, 7891),
                new Location("Bucyrus", "Bucyrus", "Crawford", 40.8084, -82.9755, 11780),
                new Location("Celina", "Celina", "Mercer", 40.5548, -84.5672, 10735)
            };
        }
    }
}
