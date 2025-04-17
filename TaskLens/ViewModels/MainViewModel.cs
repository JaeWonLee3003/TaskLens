using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TaskLens.Core;
using TaskLens.Views;

namespace TaskLens.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        // 각 View (화면) 인스턴스
        public HomeView HomeView { get; }
        public ResourceView ResourceView { get; }
        public WarningLogView WarningLogView { get; }
        public SettingsView SettingsView { get; }

        // 화면 전환 커맨드
        public ICommand ShowHomeCommand { get; }
        public ICommand ShowAnalysisCommand { get; }
        public ICommand ShowWarningLogCommand { get; }
        public ICommand ShowSettingsCommand { get; }

        public MainViewModel()
        {
            // 각각 View + ViewModel 연결
            HomeView = new HomeView { DataContext = new HomeViewModel() };
            ResourceView = new ResourceView { DataContext = new ResourceViewModel() };
            WarningLogView = new WarningLogView { DataContext = new WarningLogViewModel() };
            SettingsView = new SettingsView { DataContext = new SettingsViewModel() };

            // 첫 화면
            CurrentView = HomeView;

            // 명령어 초기화
            ShowHomeCommand = new RelayCommand(o => CurrentView = HomeView);
            ShowAnalysisCommand = new RelayCommand(o => CurrentView = ResourceView);
            ShowWarningLogCommand = new RelayCommand(o => CurrentView = WarningLogView);
            ShowSettingsCommand = new RelayCommand(o => CurrentView = SettingsView);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
