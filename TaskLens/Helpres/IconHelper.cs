using System;
using System.Drawing;
using System.Management;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TaskLens.Helpers
{
    public static class IconHelper
    {
        public static ImageSource GetProcessIcon(string exePath)
        {
            try
            {
                Icon icon = Icon.ExtractAssociatedIcon(exePath);
                return Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(20, 20));
            }
            catch
            {
                return null;
            }


        }

        public static string GetProcessPathSafe(string processName)
        {
            try
            {
                var searcher = new ManagementObjectSearcher(
                    $"SELECT ExecutablePath FROM Win32_Process WHERE Name = '{processName}.exe'");
                foreach (var obj in searcher.Get())
                {
                    var pathObj = obj["ExecutablePath"];
                    if (pathObj != null)
                        return pathObj.ToString();
                }
            }
            catch { }
            return string.Empty;
        }
    }
}
