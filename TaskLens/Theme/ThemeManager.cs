using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace TaskLens.Theme
{
    public static class ThemeManager
    {
        public static event EventHandler<string> ThemeChanged;
        
        private static string _currentTheme = "DarkTheme";
        public static string CurrentTheme
        {
            get => _currentTheme;
            private set
            {
                _currentTheme = value;
                ThemeChanged?.Invoke(null, value);
            }
        }

        public static bool IsDarkTheme => CurrentTheme == "DarkTheme";

        public static void ApplyTheme(string themeName)
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;

            // 기존 테마 제거
            var existingTheme = dictionaries.FirstOrDefault(d => 
                d.Source?.OriginalString.Contains("Theme/") == true);
            if (existingTheme != null)
                dictionaries.Remove(existingTheme);

            // 새 테마 적용
            var newDict = new ResourceDictionary
            {
                Source = new Uri($"Theme/{themeName}.xaml", UriKind.Relative)
            };
            dictionaries.Add(newDict);

            CurrentTheme = themeName;
        }

        public static void ToggleTheme()
        {
            string newTheme = IsDarkTheme ? "LightTheme" : "DarkTheme";
            ApplyTheme(newTheme);
        }

        public static void Initialize()
        {
            // 시스템 테마 감지 (Windows 10/11)
            try
            {
                var registry = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                
                if (registry?.GetValue("AppsUseLightTheme") is int useLightTheme)
                {
                    string systemTheme = useLightTheme == 0 ? "DarkTheme" : "LightTheme";
                    ApplyTheme(systemTheme);
                }
                else
                {
                    ApplyTheme("DarkTheme"); // 기본값
                }
            }
            catch
            {
                ApplyTheme("DarkTheme"); // 오류 시 기본값
            }
        }
    }
}
