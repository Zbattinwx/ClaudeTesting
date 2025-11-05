using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OhioNewsWeather.WeatherApp.Services;
using OhioNewsWeather.WeatherApp.Models;
using System.Collections.ObjectModel;

namespace OhioNewsWeather.WeatherApp.ViewModels
{
    public partial class AlertsViewModel : ObservableObject
    {
        private readonly IAlertService _alertService;
        private readonly ILocationService _locationService;

        [ObservableProperty]
        private ObservableCollection<WeatherAlert> _activeAlerts;

        [ObservableProperty]
        private WeatherAlert _selectedAlert;

        [ObservableProperty]
        private Location _selectedLocation;

        [ObservableProperty]
        private string _statusMessage;

        [ObservableProperty]
        private int _alertCount;

        public AlertsViewModel(
            IAlertService alertService,
            ILocationService locationService)
        {
            _alertService = alertService;
            _locationService = locationService;

            ActiveAlerts = new ObservableCollection<WeatherAlert>();
            SelectedLocation = _locationService.GetDefaultLocation();
            StatusMessage = "Ready";
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task LoadAlertsAsync()
        {
            try
            {
                StatusMessage = "Loading weather alerts...";

                var alerts = await _alertService.GetActiveAlertsAsync("OH");
                ActiveAlerts.Clear();
                foreach (var alert in alerts)
                {
                    ActiveAlerts.Add(alert);
                }

                AlertCount = ActiveAlerts.Count;
                StatusMessage = $"Loaded {AlertCount} active alerts";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Error loading alerts: {ex.Message}";
            }
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task RefreshAlertsAsync()
        {
            await LoadAlertsAsync();
        }

        partial void OnSelectedAlertChanged(WeatherAlert value)
        {
            if (value != null)
            {
                StatusMessage = $"Viewing: {value.Event}";
            }
        }
    }
}
