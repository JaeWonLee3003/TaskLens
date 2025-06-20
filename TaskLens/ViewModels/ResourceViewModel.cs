using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TaskLens.Core;
using TaskLens.Helpers;
using TaskLens.Models;

namespace TaskLens.ViewModels
{
    public class ResourceViewModel : INotifyPropertyChanged
    {
        public SeriesCollection CpuSeries { get; set; }
        public SeriesCollection RamSeries { get; set; }
        public ObservableCollection<ProcessInfoModel> TopProcessList { get; set; } = new ObservableCollection<ProcessInfoModel>();

        private ChartValues<float> _cpuValues;
        private ChartValues<float> _ramValues;

        private DispatcherTimer _timer;
        private readonly DispatcherTimer _processUpdateTimer = new DispatcherTimer();
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _ramCounter;

        private float _totalRamMB;

        private ProcessInfoModel _selectedProcess;
        public ProcessInfoModel SelectedProcess
        {
            get => _selectedProcess;
            set { _selectedProcess = value; OnPropertyChanged(); }
        }

        private string _aiResultText;
        public string AiResultText
        {
            get => _aiResultText;
            set { _aiResultText = value; OnPropertyChanged(); }
        }

        public ICommand AnalyzeProcessCommand { get; }

        private DateTime _lastCpuWarningTime = DateTime.MinValue;
        private DateTime _lastRamWarningTime = DateTime.MinValue;
        private readonly TimeSpan _warningCooldown = TimeSpan.FromMinutes(1);

        public ResourceViewModel ()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            _totalRamMB = GetTotalPhysicalMemoryInMB();

            _cpuValues = new ChartValues<float>();
            _ramValues = new ChartValues<float>();

            CpuSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "CPU",
                    Values = _cpuValues,
                    PointGeometry = null
                }
            };

            RamSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "RAM",
                    Values = _ramValues,
                    PointGeometry = null
                }
            };

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += UpdateData;
            _timer.Start();

            _processUpdateTimer.Interval = TimeSpan.FromSeconds(5);
            _processUpdateTimer.Tick += (s, e) => UpdateTopProcesses();
            _processUpdateTimer.Start();

            AnalyzeProcessCommand = new RelayCommand(async o =>
            {
                if (SelectedProcess == null)
                {
                    MessageBox.Show(
                        "먼저 프로세스를 선택해주세요.",
                        "프로세스 미선택",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    try
                    {
                        var result = await OllamaClient.GetProcessExplanationAsync(SelectedProcess.Name);

                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"Error : {e}");
                    }
                }
            });
        }

        private void UpdateData (object sender, EventArgs e)
        {
            float cpu = _cpuCounter.NextValue();
            float availableRam = _ramCounter.NextValue();
            float usedRamPercent = 100 - (availableRam / _totalRamMB * 100);

            _cpuValues.Add(cpu);
            _ramValues.Add(usedRamPercent);

            const int maxPoints = 30;
            if (_cpuValues.Count > maxPoints) _cpuValues.RemoveAt(0);
            if (_ramValues.Count > maxPoints) _ramValues.RemoveAt(0);

            OnPropertyChanged(nameof(CpuSeries));
            OnPropertyChanged(nameof(RamSeries));

            if (Application.Current.MainWindow is MainWindow window &&
                window.DataContext is MainViewModel vm)
            {
                if (usedRamPercent >= 80 && DateTime.Now - _lastRamWarningTime > _warningCooldown)
                {
                    vm.AddWarning($"[RAM 경고] 사용률 {usedRamPercent:F1}% 초과");
                    _lastRamWarningTime = DateTime.Now;
                }

                if (vm.SettingsView.DataContext is SettingsViewModel settingsVm &&
                    settingsVm.EnableCpuWarning && cpu >= 80 &&
                    DateTime.Now - _lastCpuWarningTime > _warningCooldown)
                {
                    vm.AddWarning($"[CPU 경고] 사용률 {cpu:F1}% 초과");
                    _lastCpuWarningTime = DateTime.Now;
                }
            }
        }

        private void UpdateTopProcesses ()
        {
            Task.Run(() =>
            {
                var processList = Process.GetProcesses()
                    .Select(p =>
                    {
                        float ramMb = 0;
                        string exePath = "";
                        //ImageSource icon = null;

                        try
                        {
                            ramMb = p.WorkingSet64 / 1024f / 1024f;
                            //exePath = IconHelper.GetProcessPathSafe(p.ProcessName);
                            //if (!string.IsNullOrEmpty(exePath))
                            //{
                            //    // 아이콘 로딩도 백그라운드에서 시도
                            //    icon = IconHelper.GetProcessIcon(exePath  );
                            //}
                        }
                        catch { }

                        return new ProcessInfoModel
                        {
                            Name = p.ProcessName,
                            Ram = ramMb,
                            //Icon = icon,
                            AiDescription = "분석 대기 중"
                        };
                    })
                    .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                    .OrderByDescending(p => p.Ram)
                    .Take(10)
                    .ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    TopProcessList.Clear();
                    foreach (var p in processList)
                        TopProcessList.Add(p);
                });
            });
        }



        private float GetTotalPhysicalMemoryInMB ()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (var obj in searcher.Get())
                {
                    var bytes = Convert.ToDouble(obj["TotalPhysicalMemory"]);
                    return (float)(bytes / 1024 / 1024);
                }
            }
            catch
            {
                return 8192;
            }

            return 8192;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged ([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
