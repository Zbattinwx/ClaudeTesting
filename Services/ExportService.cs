using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OhioNewsWeather.WeatherApp.Services
{
    public class ExportService : IExportService
    {
        public async Task<string> ExportToFileAsync(BitmapSource image, string filename)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var encoder = GetEncoderFromFilename(filename);
                    encoder.Frames.Add(BitmapFrame.Create(image));

                    using (var stream = new FileStream(filename, FileMode.Create))
                    {
                        encoder.Save(stream);
                    }

                    return filename;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to export image: {ex.Message}", ex);
                }
            });
        }

        public async Task CopyToClipboardAsync(BitmapSource image)
        {
            await Task.Run(() =>
            {
                try
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        Clipboard.SetImage(image);
                    });
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to copy to clipboard: {ex.Message}", ex);
                }
            });
        }

        public BitmapSource CaptureCurrentView()
        {
            try
            {
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow == null)
                    return null;

                var width = (int)mainWindow.ActualWidth;
                var height = (int)mainWindow.ActualHeight;

                var renderBitmap = new RenderTargetBitmap(
                    width, height, 96, 96, PixelFormats.Pbgra32);

                renderBitmap.Render(mainWindow);
                return renderBitmap;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private BitmapEncoder GetEncoderFromFilename(string filename)
        {
            var extension = Path.GetExtension(filename).ToLower();
            return extension switch
            {
                ".png" => new PngBitmapEncoder(),
                ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 95 },
                ".bmp" => new BmpBitmapEncoder(),
                ".tiff" => new TiffBitmapEncoder(),
                _ => new PngBitmapEncoder()
            };
        }
    }
}
