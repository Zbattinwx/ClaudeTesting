using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using OhioNewsWeather.WeatherApp.Models;
using OhioNewsWeather.WeatherApp.ViewModels;

namespace OhioNewsWeather.WeatherApp.Views
{
    public partial class RadarView : UserControl
    {
        private RadarViewModel ViewModel => DataContext as RadarViewModel;
        private double _currentZoom = 1.0;
        private const double ZoomIncrement = 0.2;
        private const double MinZoom = 0.5;
        private const double MaxZoom = 3.0;

        public RadarView()
        {
            InitializeComponent();
        }

        private void RadarSiteComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel != null && ViewModel.ChangeSiteCommand.CanExecute(null))
            {
                ViewModel.ChangeSiteCommand.Execute(null);
            }
        }

        private void ProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton button && button.Tag is string productName)
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                if (Enum.TryParse<RadarFrame.RadarProduct>(productName, out var product))
                {
                    if (ViewModel != null && ViewModel.ChangeProductCommand.CanExecute(product))
                    {
                        ViewModel.ChangeProductCommand.Execute(product);
                    }
                }

                // Hide loading overlay after a delay
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(3)
                };
                timer.Tick += (s, args) =>
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    timer.Stop();
                };
                timer.Start();
            }
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null && ViewModel.PlayPauseCommand.CanExecute(null))
            {
                ViewModel.PlayPauseCommand.Execute(null);

                // Update icon
                if (ViewModel.IsPlaying)
                {
                    PlayPauseIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Pause;
                    PlayPauseButton.Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(255, 152, 0));
                }
                else
                {
                    PlayPauseIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.Play;
                    PlayPauseButton.Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(76, 175, 80));
                }
            }
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentZoom < MaxZoom)
            {
                _currentZoom += ZoomIncrement;
                ApplyZoom();
            }
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (_currentZoom > MinZoom)
            {
                _currentZoom -= ZoomIncrement;
                ApplyZoom();
            }
        }

        private void ResetZoom_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom = 1.0;
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            RadarScaleTransform.ScaleX = _currentZoom;
            RadarScaleTransform.ScaleY = _currentZoom;
            ZoomLevelText.Text = $"{(_currentZoom * 100):0}%";
        }
    }

    // Value converter for max frame index
    public class MaxFrameIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count && count > 0)
            {
                return count - 1;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
