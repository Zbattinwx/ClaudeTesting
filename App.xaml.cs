using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using OhioNewsWeather.WeatherApp.Services;
using OhioNewsWeather.WeatherApp.ViewModels;

namespace OhioNewsWeather.WeatherApp
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Services
            services.AddHttpClient();
            services.AddSingleton<IWeatherService, NoaaWeatherService>();
            services.AddSingleton<IRadarSiteService, RadarSiteService>();
            services.AddSingleton<IRadarService, AwsNexradLevel2Service>(); // Professional-grade Level 2 data from AWS S3
            services.AddSingleton<ISatelliteService, GoesSatelliteService>();
            services.AddSingleton<ILocationService, OhioLocationService>();
            services.AddSingleton<IAlertService, NoaaAlertService>();
            services.AddSingleton<IExportService, ExportService>();

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<RadarViewModel>();
            services.AddTransient<SatelliteViewModel>();
            services.AddTransient<AlertsViewModel>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
            var mainWindow = new MainWindow(mainViewModel, _serviceProvider);
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
