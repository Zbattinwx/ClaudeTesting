using System.Windows;
using OhioNewsWeather.WeatherApp.ViewModels;

namespace OhioNewsWeather.WeatherApp
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private void RadarButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Loading Radar...");
            // TODO: Load Radar view
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
