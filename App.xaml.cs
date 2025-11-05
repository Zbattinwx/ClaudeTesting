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
            services.AddSingleton<IRadarService, NoaaRadarService>();
            services.AddSingleton<ISatelliteService, GoesSatelliteService>();
            services.AddSingleton<ILocationService, OhioLocationService>();
            services.AddSingleton<IAlertService, NoaaAlertService>();
            services.AddSingleton<IExportService, ExportService>();

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<RadarViewModel>();
            services.AddTransient<SatelliteViewModel>();
            services.AddTransient<AlertsViewModel>();

            // Main Window
            services.AddSingleton<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
