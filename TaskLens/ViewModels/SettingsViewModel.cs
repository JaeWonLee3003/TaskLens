using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TaskLens.Core;
using TaskLens.Theme;

namespace TaskLens.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private bool _autoStart;
        private bool _enableCpuWarning;
        private bool _enableRamWarning;
        private bool _isDarkTheme = true; // 기본 테마 상태

        public bool AutoStart
        {
            get => _autoStart;
            set { _autoStart = value; OnPropertyChanged(); }
        }

        public bool EnableCpuWarning
        {
            get => _enableCpuWarning;
            set { _enableCpuWarning = value; OnPropertyChanged(); }
        }

        public bool EnableRamWarning
        {
            get => _enableRamWarning;
            set { _enableRamWarning = value; OnPropertyChanged(); }
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (_isDarkTheme != value)
                {
                    _isDarkTheme = value;
                    OnPropertyChanged();
                    ThemeManager.ApplyTheme(value ? "DarkTheme" : "LightTheme");
                }
            }
        }

        public ICommand SaveSettingsCommand { get; }

        public SettingsViewModel ()
        {
            // 임시 초기값
            AutoStart = false;
            EnableCpuWarning = true;
            EnableRamWarning = true;
            IsDarkTheme = true; // 기본 테마로 다크 사용

            SaveSettingsCommand = new RelayCommand(o => SaveSettings());
        }

        private CultureInfo _selectedLanguage; // 기본값: 한국어
        public CultureInfo SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value)
                {
                    _selectedLanguage = value;
                    OnPropertyChanged();

                    // 언어 코드 변경 시 리소스 업데이트
                    ApplyLanguage(value);
                }
            }
        }

        private void ApplyLanguage (CultureInfo langCode)
        {
            var dict = new ResourceDictionary();

            switch (langCode.Name)
            {
                case "en":
                    dict.Source = new Uri("Resources/StringResources.en.xaml", UriKind.Relative);
                    break;
                case "ko":
                    dict.Source = new Uri("Resources/StringResources.ko.xaml", UriKind.Relative);
                    break;
            }

            // 기존 리소스를 제거하고 새로운 리소스를 추가
            var existingDict = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source?.OriginalString.Contains("StringResources") == true);
            if (existingDict != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(existingDict);
            }

            Application.Current.Resources.MergedDictionaries.Add(dict);
        }




        private void SaveSettings ()
        {
            // TODO: 실제 저장 로직 연결
            System.Windows.MessageBox.Show(
                "설정이 성공적으로 저장되었습니다.",
                "설정 저장 완료",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged ([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
