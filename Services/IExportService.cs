using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace OhioNewsWeather.WeatherApp.Services
{
    public interface IExportService
    {
        Task<string> ExportToFileAsync(BitmapSource image, string filename);
        Task CopyToClipboardAsync(BitmapSource image);
        BitmapSource CaptureCurrentView();
    }
}
