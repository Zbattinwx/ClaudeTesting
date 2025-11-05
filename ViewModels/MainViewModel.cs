using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OhioNewsWeather.WeatherApp.Services;
using OhioNewsWeather.WeatherApp.Models;
using System.Collections.ObjectModel;

namespace OhioNewsWeather.WeatherApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ILocationService _locationService;
        private readonly IWeatherService _weatherService;
        private readonly IAlertService _alertService;

        [ObservableProperty]
        private Location _selectedLocation;

        [ObservableProperty]
        private WeatherConditions _currentConditions;

        [ObservableProperty]
        private ObservableCollection<WeatherAlert> _activeAlerts;

        [ObservableProperty]
        private string _statusMessage;

        public MainViewModel(
            ILocationService locationService,
            IWeatherService weatherService,
            IAlertService alertService)
        {
            _locationService = locationService;
            _weatherService = weatherService;
            _alertService = alertService;

            ActiveAlerts = new ObservableCollection<WeatherAlert>();
            SelectedLocation = _locationService.GetDefaultLocation();
            StatusMessage = "Ready";
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task LoadWeatherDataAsync()
        {
            if (SelectedLocation == null)
                return;

            try
            {
                StatusMessage = "Loading weather data...";

                // Load current conditions
                CurrentConditions = await _weatherService.GetCurrentConditionsAsync(SelectedLocation);

                // Load active alerts
                var alerts = await _alertService.GetAlertsByLocationAsync(SelectedLocation);
                ActiveAlerts.Clear();
                foreach (var alert in alerts)
                {
                    ActiveAlerts.Add(alert);
                }

                StatusMessage = "Weather data loaded successfully";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Error loading weather data: {ex.Message}";
            }
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task RefreshAsync()
        {
            await LoadWeatherDataAsync();
        }
    }
}
