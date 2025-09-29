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
    public class ResourceViewModel : INotifyPropertyChanged, IDisposable
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
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Performance counter initialization failed: {ex.Message}");
                // 성능 카운터 초기화 실패 시에도 계속 진행
            }

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
                Interval = TimeSpan.FromSeconds(2) // 1초 → 2초로 변경
            };
            _timer.Tick += UpdateData;

            _processUpdateTimer.Interval = TimeSpan.FromSeconds(10); // 5초 → 10초로 변경
            _processUpdateTimer.Tick += (s, e) => UpdateTopProcesses();

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

                try
                {
                    var result = await OllamaClient.GetProcessExplanationAsync(SelectedProcess.Name);
                    AiResultText = result;
                    //SelectedProcess.AiDescription = result;
                }
                catch (Exception ex)
                {
                    AiResultText = $"AI 분석 중 오류가 발생했습니다: {ex.Message}";
                    System.Diagnostics.Debug.WriteLine($"Ollama client error: {ex.Message}");
                }
            });
        }

        private void UpdateData (object sender, EventArgs e)
        {
            try
            {
                float cpu = 0;
                float availableRam = 0;

                // 성능 카운터가 null이 아닌 경우에만 값 조회
                if (_cpuCounter != null)
                    cpu = _cpuCounter.NextValue();

                if (_ramCounter != null)
                    availableRam = _ramCounter.NextValue();

                float usedRamPercent = _totalRamMB > 0 ? 100 - (availableRam / _totalRamMB * 100) : 0;

                _cpuValues.Add(cpu);
                _ramValues.Add(usedRamPercent);

                const int maxPoints = 30;
                if (_cpuValues.Count > maxPoints) _cpuValues.RemoveAt(0);
                if (_ramValues.Count > maxPoints) _ramValues.RemoveAt(0);

                OnPropertyChanged(nameof(CpuSeries));
                OnPropertyChanged(nameof(RamSeries));

                // 경고 시스템
                CheckAndSendWarnings(cpu, usedRamPercent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateData error: {ex.Message}");
            }
        }

        private void CheckAndSendWarnings (float cpu, float usedRamPercent)
        {
            try
            {
                if (Application.Current?.MainWindow is MainWindow window &&
                    window.DataContext is MainViewModel vm)
                {
                    if (usedRamPercent >= 80 && DateTime.Now - _lastRamWarningTime > _warningCooldown)
                    {
                        vm.AddWarning($"[RAM 경고] 사용률 {usedRamPercent:F1}% 초과");
                        _lastRamWarningTime = DateTime.Now;
                    }

                    if (vm.SettingsView?.DataContext is SettingsViewModel settingsVm &&
                        settingsVm.EnableCpuWarning && cpu >= 80 &&
                        DateTime.Now - _lastCpuWarningTime > _warningCooldown)
                    {
                        vm.AddWarning($"[CPU 경고] 사용률 {cpu:F1}% 초과");
                        _lastCpuWarningTime = DateTime.Now;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Warning system error: {ex.Message}");
            }
        }

        private void UpdateTopProcesses ()
        {
            // 🚀 백그라운드에서 비동기로 처리
            Task.Run(() =>
            {
                try
                {
                    var processList = Process.GetProcesses()
                        .AsParallel() // 병렬 처리로 성능 향상
                        .Where(p => !p.HasExited) // 종료된 프로세스 제외
                        .Select(p =>
                        {
                            float ramMb = 0;
                            ImageSource icon = IconHelper.DefaultProcessIcon; // 기본값 설정

                            try
                            {
                                ramMb = p.WorkingSet64 / 1024f / 1024f;

                                // 🎯 아이콘 로딩은 RAM이 높은 상위 10개만
                                if (ramMb > 50) // 50MB 이상만 아이콘 로드
                                {
                                    string exePath = IconHelper.GetProcessPathSafe(p.ProcessName);
                                    if (!string.IsNullOrEmpty(exePath))
                                    {
                                        icon = IconHelper.GetProcessIcon(exePath) ?? IconHelper.DefaultProcessIcon;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // 프로세스 접근 오류 시 기본값 유지
                                System.Diagnostics.Debug.WriteLine($"Process access error for {p.ProcessName}: {ex.Message}");
                            }
                            finally
                            {
                                p?.Dispose(); // Process 객체 해제
                            }

                            return new ProcessInfoModel
                            {
                                Name = p.ProcessName,
                                Ram = ramMb,
                                Icon = icon,
                                AiDescription = "분석 대기 중"
                            };
                        })
                        .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                        .OrderByDescending(p => p.Ram)
                        .Take(10)
                        .ToList();

                    // UI 스레드에서 컬렉션 업데이트
                    if (Application.Current?.Dispatcher != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            TopProcessList.Clear();
                            foreach (var p in processList)
                                TopProcessList.Add(p);
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateTopProcesses error: {ex.Message}");
                }
            });
        }



        private float GetTotalPhysicalMemoryInMB ()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    using (var collection = searcher.Get())
                    {
                        foreach (var obj in collection.Cast<ManagementObject>())
                        {
                            using (obj)
                            {
                                var bytes = Convert.ToDouble(obj["TotalPhysicalMemory"]);
                                return (float)(bytes / 1024 / 1024);
                            }
                        }
                    }
                }
            }
            catch (ManagementException ex)
            {
                System.Diagnostics.Debug.WriteLine($"WMI query failed: {ex.Message}");
                return 8192; // 기본값 8GB
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Memory query error: {ex.Message}");
                return 8192; // 기본값 8GB
            }

            return 8192;
        }

        // 🎯 타이머 제어 메서드
        public void StartMonitoring()
        {
            try
            {
                if (_timer != null && !_timer.IsEnabled)
                {
                    _timer.Start();
                }

                if (_processUpdateTimer != null && !_processUpdateTimer.IsEnabled)
                {
                    _processUpdateTimer.Start();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StartMonitoring error: {ex.Message}");
            }
        }

        public void StopMonitoring()
        {
            try
            {
                _timer?.Stop();
                _processUpdateTimer?.Stop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StopMonitoring error: {ex.Message}");
            }
        }

        // 🗑️ IDisposable 구현
        private bool _disposed = false;

        public void Dispose ()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (!_disposed && disposing)
            {
                StopMonitoring();
                _timer?.Stop();
                _processUpdateTimer?.Stop();
                _cpuCounter?.Dispose();
                _ramCounter?.Dispose();
                _disposed = true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged ([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
