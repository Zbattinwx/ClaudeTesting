using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OhioNewsWeather.WeatherApp.Services;
using OhioNewsWeather.WeatherApp.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace OhioNewsWeather.WeatherApp.ViewModels
{
    public partial class RadarViewModel : ObservableObject
    {
        private readonly IRadarService _radarService;
        private readonly ILocationService _locationService;
        private readonly IExportService _exportService;

        [ObservableProperty]
        private ObservableCollection<RadarFrame> _radarFrames;

        [ObservableProperty]
        private RadarFrame _currentFrame;

        [ObservableProperty]
        private Location _selectedLocation;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private int _currentFrameIndex;

        [ObservableProperty]
        private string _statusMessage;

        private System.Windows.Threading.DispatcherTimer _animationTimer;

        public RadarViewModel(
            IRadarService radarService,
            ILocationService locationService,
            IExportService exportService)
        {
            _radarService = radarService;
            _locationService = locationService;
            _exportService = exportService;

            RadarFrames = new ObservableCollection<RadarFrame>();
            SelectedLocation = _locationService.GetDefaultLocation();
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
        private async System.Threading.Tasks.Task LoadRadarAsync()
        {
            if (SelectedLocation == null)
                return;

            try
            {
                StatusMessage = "Loading radar data...";

                var frames = await _radarService.GetRadarFramesAsync(SelectedLocation, 10);
                RadarFrames.Clear();
                foreach (var frame in frames)
                {
                    RadarFrames.Add(frame);
                }

                if (RadarFrames.Any())
                {
                    CurrentFrameIndex = RadarFrames.Count - 1;
                    CurrentFrame = RadarFrames[CurrentFrameIndex];
                }

                StatusMessage = $"Loaded {RadarFrames.Count} radar frames";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Error loading radar: {ex.Message}";
            }
        }

        [RelayCommand]
        private void PlayPause()
        {
            IsPlaying = !IsPlaying;
            if (IsPlaying)
            {
                _animationTimer.Start();
                StatusMessage = "Playing radar animation";
            }
            else
            {
                _animationTimer.Stop();
                StatusMessage = "Paused";
            }
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task ExportCurrentFrameAsync()
        {
            if (CurrentFrame?.Image == null)
                return;

            try
            {
                var filename = $"radar_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
                await _exportService.ExportToFileAsync(CurrentFrame.Image, filename);
                StatusMessage = $"Exported to {filename}";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
        }

        private void AnimationTimer_Tick(object sender, System.EventArgs e)
        {
            if (RadarFrames.Count == 0)
                return;

            CurrentFrameIndex = (CurrentFrameIndex + 1) % RadarFrames.Count;
            CurrentFrame = RadarFrames[CurrentFrameIndex];
        }
    }
}
