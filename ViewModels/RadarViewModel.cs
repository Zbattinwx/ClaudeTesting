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
        private readonly IRadarSiteService _radarSiteService;
        private readonly IExportService _exportService;

        [ObservableProperty]
        private ObservableCollection<RadarFrame> _radarFrames;

        [ObservableProperty]
        private RadarFrame _currentFrame;

        [ObservableProperty]
        private ObservableCollection<RadarSite> _radarSites;

        [ObservableProperty]
        private RadarSite _selectedRadarSite;

        [ObservableProperty]
        private RadarFrame.RadarProduct _selectedProduct;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private int _currentFrameIndex;

        [ObservableProperty]
        private string _statusMessage;

        [ObservableProperty]
        private int _animationSpeed;

        private System.Windows.Threading.DispatcherTimer _animationTimer;

        public RadarViewModel(
            IRadarService radarService,
            IRadarSiteService radarSiteService,
            IExportService exportService)
        {
            _radarService = radarService;
            _radarSiteService = radarSiteService;
            _exportService = exportService;

            RadarFrames = new ObservableCollection<RadarFrame>();
            RadarSites = new ObservableCollection<RadarSite>(_radarSiteService.GetOhioRadarSites());
            SelectedRadarSite = _radarSiteService.GetDefaultRadarSite();
            SelectedProduct = RadarFrame.RadarProduct.Reflectivity;
            AnimationSpeed = 500;
            StatusMessage = "Ready";

            InitializeAnimationTimer();
        }

        private void InitializeAnimationTimer()
        {
            _animationTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = System.TimeSpan.FromMilliseconds(AnimationSpeed)
            };
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        partial void OnAnimationSpeedChanged(int value)
        {
            if (_animationTimer != null)
            {
                _animationTimer.Interval = System.TimeSpan.FromMilliseconds(value);
            }
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task LoadRadarAsync()
        {
            if (SelectedRadarSite == null)
                return;

            try
            {
                StatusMessage = $"Loading {SelectedProduct} from {SelectedRadarSite.SiteId}...";

                var frames = await _radarService.GetRadarFramesAsync(SelectedRadarSite, SelectedProduct, 10);
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

                StatusMessage = $"Loaded {RadarFrames.Count} frames from {SelectedRadarSite.SiteId}";
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Error loading radar: {ex.Message}";
            }
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task ChangeProductAsync(RadarFrame.RadarProduct product)
        {
            SelectedProduct = product;
            await LoadRadarAsync();
        }

        [RelayCommand]
        private async System.Threading.Tasks.Task ChangeSiteAsync()
        {
            await LoadRadarAsync();
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
