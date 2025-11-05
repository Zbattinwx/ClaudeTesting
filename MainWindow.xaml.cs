using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using OhioNewsWeather.WeatherApp.ViewModels;
using OhioNewsWeather.WeatherApp.Views;

namespace OhioNewsWeather.WeatherApp
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly System.IServiceProvider _serviceProvider;

        public MainWindow(MainViewModel viewModel, System.IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _serviceProvider = serviceProvider;
            DataContext = _viewModel;
        }

        private void RadarButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Loading Radar...");
            LoadRadarView();
        }

        private void LoadRadarView()
        {
            var radarViewModel = _serviceProvider.GetRequiredService<RadarViewModel>();
            var radarView = new RadarView
            {
                DataContext = radarViewModel
            };

            ContentArea.Children.Clear();
            ContentArea.Children.Add(radarView);
        }

        private void SatelliteButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Loading Satellite...");
            // TODO: Load Satellite view
        }

        private void AlertsButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Loading Alerts...");
            // TODO: Load Alerts view
        }

        private void ConditionsButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Loading Conditions...");
            // TODO: Load Conditions view
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Opening Export...");
            // TODO: Open Export dialog
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Opening Settings...");
            // TODO: Open Settings view
        }

        private void UpdateStatus(string message)
        {
            StatusText.Text = message;
            LastUpdateText.Text = $"Last Updated: {System.DateTime.Now:h:mm:ss tt}";
        }
    }
}
