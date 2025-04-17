using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TaskLens.Core;

namespace TaskLens.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private bool _autoStart;
        private bool _enableCpuWarning;
        private bool _enableRamWarning;

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

        public ICommand SaveSettingsCommand { get; }

        public SettingsViewModel()
        {
            // 임시 초기값
            AutoStart = false;
            EnableCpuWarning = true;
            EnableRamWarning = true;

            SaveSettingsCommand = new RelayCommand(o => SaveSettings());
        }

        private void SaveSettings()
        {
            // TODO: 실제 저장 로직 연결
            System.Windows.MessageBox.Show(
    "설정이 성공적으로 저장되었습니다.",
    "설정 저장 완료",
    System.Windows.MessageBoxButton.OK,
    System.Windows.MessageBoxImage.Information
);

        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
