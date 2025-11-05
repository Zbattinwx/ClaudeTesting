# Ohio Weather - Professional Weather Application

A full-featured Windows weather application built for Ohio News and Weather forecasting group.

## Features

### Current Implementation (Phase 1-3)

✅ **Core Architecture**
- WPF .NET 8 application with modern Material Design UI
- MVVM pattern with dependency injection
- Modular service architecture

✅ **Data Services**
- NOAA/NWS API integration for weather data and alerts
- High-resolution Level 2 NEXRAD radar data
- Individual radar site selection (8 sites covering Ohio)
- GOES satellite imagery (visible and infrared)
- Comprehensive Ohio location database (75+ cities and towns)

✅ **Weather Features**
- Current conditions and observations
- 7-day forecast
- Hourly forecasts
- Severe weather alerts and warnings

✅ **Radar View (NEW in Phase 3)**
- **Individual Radar Site Selection** - Choose from 8 NEXRAD sites (KCLE, KILN, KIWX, etc.)
- **Level 2 Radar Products** - Reflectivity, Velocity, Correlation Coefficient
- **Hi-Res Display** - 1024x1024 radar imagery with 250km coverage
- **Animation Controls** - Play/pause, adjustable speed (100-1000ms), frame timeline
- **Zoom & Pan** - 50%-300% zoom with floating zoom controls
- **Real-time Updates** - 4-6 minute update intervals for storm tracking
- **ONW Branding** - Logo overlay and timestamp display
- **Dark Theme UI** - Professional broadcast-ready interface

✅ **Satellite (In Development)**
- Visible and infrared satellite imagery
- Loop playback with configurable speed
- Animation controls

✅ **Export System**
- Export current view to PNG/JPG
- Copy to clipboard functionality
- Facebook-optimized output
- Custom branding support

### Main Window Navigation

The application features a clean, modern interface with sidebar navigation:

- **Radar** ✅ - Interactive Level 2 radar display with site selection
- **Satellite** - Satellite imagery viewer (in development)
- **Alerts** - Active watches and warnings (in development)
- **Conditions** - Current weather observations (in development)
- **Export** - Social media export tools (in development)
- **Settings** - Application configuration (in development)

## Radar Features (Phase 3)

### NEXRAD Radar Sites

The application includes 8 NEXRAD Level 2 radar sites covering Ohio:

| Site ID | Location | State | Primary |
|---------|----------|-------|---------|
| KCLE | Cleveland, OH | OH | ✅ Primary |
| KILN | Wilmington, OH | OH | ✅ Primary |
| KIWX | North Webster, IN | IN | Coverage for NW Ohio |
| KLOT | Romeoville, IL | IL | Coverage for NW Ohio |
| KFWA | Fort Wayne, IN | IN | Coverage for NE Ohio |
| KPBZ | Pittsburgh, PA | PA | Coverage for E Ohio |
| RLBE | Charleston, WV | WV | Coverage for SE Ohio |
| KDTX | White Lake, MI | MI | Coverage for N Ohio |

### Radar Products

- **Reflectivity (N0R)** - Base reflectivity, shows precipitation intensity
- **Velocity (N0V)** - Base velocity, shows wind speed and direction (toward/away)
- **Correlation Coefficient (N0C)** - Shows precipitation type and quality

### Radar Controls

- **Site Selector** - Dropdown to choose radar site
- **Product Selector** - Radio buttons for Reflectivity, Velocity, Correlation
- **Load Radar** - Fetches latest 10 frames from selected site
- **Play/Pause** - Animates through radar frames
- **Speed Control** - Adjustable animation speed (100ms - 1000ms)
- **Frame Slider** - Manual frame navigation
- **Zoom Controls** - +/- buttons and reset (50% - 300%)
- **Export** - Save current frame to file

### Branding Integration

The radar view includes Ohio News and Weather branding:
- ONW logo overlay (top left)
- Timestamp display (bottom right)
- Dark professional theme
- Ready for broadcast and social media use

## Technical Architecture

### Project Structure

```
OhioWeather/
├── Models/              # Data models (Location, Alert, Radar, etc.)
├── Services/            # Weather data services
│   ├── IWeatherService.cs
│   ├── NoaaWeatherService.cs
│   ├── IRadarService.cs
│   ├── NoaaRadarService.cs
│   ├── ISatelliteService.cs
│   ├── GoesSatelliteService.cs
│   ├── IAlertService.cs
│   ├── NoaaAlertService.cs
│   ├── ILocationService.cs
│   ├── OhioLocationService.cs
│   ├── IExportService.cs
│   └── ExportService.cs
├── ViewModels/          # MVVM view models
│   ├── MainViewModel.cs
│   ├── RadarViewModel.cs
│   ├── SatelliteViewModel.cs
│   └── AlertsViewModel.cs
├── Views/               # Additional views (to be added)
├── Assets/              # Images, icons, branding
└── App.xaml             # Application entry point
```

### Dependencies

- **.NET 8** - Modern .NET runtime
- **WPF** - Windows Presentation Foundation
- **MaterialDesignThemes** - Modern UI components
- **CommunityToolkit.Mvvm** - MVVM helpers
- **Microsoft.Extensions.DependencyInjection** - Dependency injection
- **System.Text.Json** - JSON parsing

## Building and Running

### Prerequisites

- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 or JetBrains Rider

### Build Instructions

```bash
# Restore packages
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

### Creating an Installer

```bash
# Publish self-contained executable
dotnet publish -c Release -r win-x64 --self-contained
```

## Ohio Location Database

The application includes a comprehensive database of Ohio locations:

- **Major Cities** - Columbus, Cleveland, Cincinnati, Toledo, Akron, Dayton
- **Medium Cities** - 25 cities with populations 50k-100k
- **Small Cities** - 40+ towns with populations 10k-50k

Locations include:
- Geographic coordinates
- County information
- Population data
- Default location settings

## Weather Data Sources

### NOAA/NWS API
- Current conditions and observations
- 7-day and hourly forecasts
- Severe weather alerts and warnings
- **API Docs**: https://www.weather.gov/documentation/services-web-api

### NOAA Radar
- High-resolution base reflectivity
- Real-time updates (6-minute intervals)
- WMS tile service for smooth rendering
- **Service**: NOAA GeoServer OpenGeo

### GOES Satellite
- GOES-16 satellite imagery
- Visible, infrared, and water vapor channels
- 5-minute update intervals
- CONUS sector coverage
- **Source**: NOAA NESDIS

## Customization

### Branding

Place your assets in the `Assets/` directory:

1. **app-icon.ico** - Application icon
2. **Branding/logo.png** - Your logo
3. **Branding/header-*.png** - Radar legends and headers
4. **Templates/facebook-template.png** - Export templates

### Default Location

Edit `OhioLocationService.cs` to set a different default location:

```csharp
new Location("YourCity", "YourCity", "YourCounty", lat, lon, population) { IsDefault = true }
```

### Colors and Theme

Modify `App.xaml` to customize the color scheme:

```xml
<SolidColorBrush x:Key="PrimaryBrush" Color="#2196F3"/>
<SolidColorBrush x:Key="SecondaryBrush" Color="#03A9F4"/>
```

## Export Features

The export system is optimized for social media sharing:

- **High-DPI Output** - Sharp images for Facebook/Twitter
- **Custom Templates** - Add your branding automatically
- **Quick Export** - One-click save or copy to clipboard
- **Multiple Formats** - PNG, JPG, BMP, TIFF

## Next Phase Development

### Phase 3-7: UI Implementation
- Complete radar view with overlays
- Full satellite viewer
- Alert detail panels
- Temperature maps
- Current conditions dashboard

### Phase 8: Enhanced Export
- Template system
- Text overlays
- Batch export
- Facebook-specific formatting

### Phase 9: Settings & Customization
- Location management
- Auto-refresh configuration
- Keyboard shortcuts
- Export template editor

### Phase 10-12: Polish & Deploy
- Performance optimization
- Comprehensive testing
- Installer creation
- Auto-update system

## Contributing

This application is developed specifically for Ohio News and Weather. For feature requests or bug reports, contact the development team.

## License

Proprietary - Ohio News and Weather

## Support

For technical support or questions:
- Email: contact@ohionewsweather.com
- Documentation: See `/tasks/todo.md` for development roadmap

---

**Built with ❤️ for Ohio News and Weather**
