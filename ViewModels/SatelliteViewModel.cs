using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OhioNewsWeather.WeatherApp.Services;
using OhioNewsWeather.WeatherApp.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace OhioNewsWeather.WeatherApp.ViewModels
{
    public partial class SatelliteViewModel : ObservableObject
    {
        private readonly ISatelliteService _satelliteService;
        private readonly IExportService _exportService;

        [ObservableProperty]
        private ObservableCollection<SatelliteImage> _satelliteImages;

        [ObservableProperty]
        private SatelliteImage _currentImage;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private int _currentImageIndex;

        [ObservableProperty]
        private SatelliteImage.SatelliteType _selectedType;

        [ObservableProperty]
        private string _statusMessage;

        private System.Windows.Threading.DispatcherTimer _animationTimer;

        public SatelliteViewModel(
            ISatelliteService satelliteService,
            IExportService exportService)
        {
            _satelliteService = satelliteService;
            _exportService = exportService;

            SatelliteImages = new ObservableCollection<SatelliteImage>();
            SelectedType = SatelliteImage.SatelliteType.Visible;
            StatusMessage = "Ready";

            InitializeAnimationTimer();
        }

        private void InitializeAnimationTimer()
        {
            _animationTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = System.TimeSpan.FromMilliseconds(500)
            };
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task LoadSatelliteAsync()
        {
            try
            {
                StatusMessage = "Loading satellite imagery...";

                SatelliteImage[] images = SelectedType == SatelliteImage.SatelliteType.Visible
                    ? await _satelliteService.GetVisibleSatelliteAsync(10)
                    : await _satelliteService.GetInfraredSatelliteAsync(10);

                SatelliteImages.Clear();
                foreach (var image in images)
                {
                    SatelliteImages.Add(image);
                }

                if (SatelliteImages.Any())
                {
                    CurrentImageIndex = SatelliteImages.Count - 1;
                    CurrentImage = SatelliteImages[CurrentImageIndex];
                }

                StatusMessage = $"Loaded {SatelliteImages.Count} satellite images";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Error loading satellite: {ex.Message}";
            }
        }

        [RelayCommand]
        private void PlayPause()
        {
            IsPlaying = !IsPlaying;
            if (IsPlaying)
            {
                _animationTimer.Start();
                StatusMessage = "Playing satellite animation";
            }
            else
            {
                _animationTimer.Stop();
                StatusMessage = "Paused";
            }
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task ExportCurrentImageAsync()
        {
            if (CurrentImage?.Image == null)
                return;

            try
            {
                var filename = $"satellite_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
                await _exportService.ExportToFileAsync(CurrentImage.Image, filename);
                StatusMessage = $"Exported to {filename}";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
        }

        private void AnimationTimer_Tick(object sender, System.EventArgs e)
        {
            if (SatelliteImages.Count == 0)
                return;

            CurrentImageIndex = (CurrentImageIndex + 1) % SatelliteImages.Count;
            CurrentImage = SatelliteImages[CurrentImageIndex];
        }
    }
}
