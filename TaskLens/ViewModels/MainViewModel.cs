using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
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
                OnPropertyChanged(nameof(IsHomeSelected));
                OnPropertyChanged(nameof(IsAnalysisSelected));
                OnPropertyChanged(nameof(IsWarningLogSelected));
                OnPropertyChanged(nameof(IsSettingsSelected));
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
        public ICommand AnalyzeProcessCommand { get; }

        // Navigation state properties
        public bool IsHomeSelected => CurrentView == HomeView;
        public bool IsAnalysisSelected => CurrentView == ResourceView;
        public bool IsWarningLogSelected => CurrentView == WarningLogView;
        public bool IsSettingsSelected => CurrentView == SettingsView;


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


            // 🔥 테스트로 경고 로그 추가
#if DEBUG
            var warningVM = (WarningLogViewModel)WarningLogView.DataContext;
            warningVM.AddWarning("테스트 경고: CPU 사용량 90% 초과");
            warningVM.AddWarning("테스트 경고: RAM 사용량 95% 초과");
#endif
        }

        public void AddWarning (string message)
        {
            if (WarningLogView.DataContext is WarningLogViewModel vm)
            {
                vm.AddWarning($"{DateTime.Now:HH:mm:ss} - {message}");

                // 🛎️ 풍선 알림 추가
                if (Application.Current.MainWindow is MainWindow mw)
                {
                    mw.ShowBalloon("⚠️ 경고 발생", message, System.Windows.Forms.ToolTipIcon.Warning);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
