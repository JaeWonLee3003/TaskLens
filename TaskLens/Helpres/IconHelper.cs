using System;
using System.Drawing;
using System.IO;
using System.Management;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TaskLens.Helpers
{
    public static class IconHelper
    {
        public static ImageSource DefaultProcessIcon { get; } = new BitmapImage(new Uri("pack://application:,,,/Resources/DefaultProcess.png"));

        static IconHelper()
        {
            try
            {
                // 리소스 경로가 실제로 존재하는지 확인!
                DefaultProcessIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/DefaultProcess.png"));
            }
            catch (Exception ex)
            {
                // 예외 발생 시 null로 처리 (또는 다른 안전한 기본값)
                DefaultProcessIcon = null;
                System.Diagnostics.Debug.WriteLine($"[IconHelper] 기본 아이콘 로딩 실패: {ex.Message}");
            }
        }

        public static string GetProcessPathSafe (string processName)
        {
            try
            {
                var searcher = new ManagementObjectSearcher(
                    $"SELECT ExecutablePath FROM Win32_Process WHERE Name = '{processName}.exe'");
                foreach (var obj in searcher.Get())
                {
                    var path = obj["ExecutablePath"];
                    if (path != null)
                        return path.ToString();
                }
                searcher.Dispose();
            }
            catch { }
            return string.Empty;
        }

        public static ImageSource GetProcessIcon (string exePath)
        {
            try
            {
                if (!File.Exists(exePath))
                    return null;

                Icon icon = Icon.ExtractAssociatedIcon(exePath);
                if (icon == null)
                    return null;

                Bitmap bmp = icon.ToBitmap();
                var stream = new MemoryStream();
                bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Seek(0, SeekOrigin.Begin);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                // 명시적 자원 해제
                bmp.Dispose();
                icon.Dispose();

                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
