using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
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
            System.Diagnostics.Debug.WriteLine("🚀 ResourceViewModel 생성자 시작");

            // 🎯 즉시 테스트 데이터 추가 (가장 먼저)
            TopProcessList.Clear();
            TopProcessList.Add(new ProcessInfoModel
            {
                Name = "Chrome.exe",
                Ram = 250.5f,
                Icon = IconHelper.DefaultProcessIcon,
                AiDescription = "웹 브라우저"
            });
            TopProcessList.Add(new ProcessInfoModel
            {
                Name = "TaskLens.exe",
                Ram = 45.2f,
                Icon = IconHelper.DefaultProcessIcon,
                AiDescription = "작업 관리자"
            });
            TopProcessList.Add(new ProcessInfoModel
            {
                Name = "Visual Studio.exe",
                Ram = 512.8f,
                Icon = IconHelper.DefaultProcessIcon,
                AiDescription = "개발 도구"
            });

            System.Diagnostics.Debug.WriteLine($"✅ 테스트 데이터 추가 완료: {TopProcessList.Count}개");

            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                System.Diagnostics.Debug.WriteLine("✅ Performance counters 초기화 성공");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Performance counter initialization failed: {ex.Message}");
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
                Interval = TimeSpan.FromSeconds(2)
            };
            _timer.Tick += UpdateData;

            _processUpdateTimer.Interval = TimeSpan.FromSeconds(5);
            _processUpdateTimer.Tick += (s, e) => UpdateTopProcesses();

            // 타이머 시작
            StartMonitoring();

            AnalyzeProcessCommand = new RelayCommand(async o =>
            {
                if (SelectedProcess == null)
                {
                    MessageBox.Show("먼저 프로세스를 선택해주세요.", "프로세스 미선택", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var result = await OllamaClient.GetProcessExplanationAsync(SelectedProcess.Name);
                    AiResultText = result;
                }
                catch (Exception ex)
                {
                    AiResultText = $"AI 분석 중 오류가 발생했습니다: {ex.Message}";
                    System.Diagnostics.Debug.WriteLine($"Ollama client error: {ex.Message}");
                }
            });

            System.Diagnostics.Debug.WriteLine("🎉 ResourceViewModel 생성자 완료");
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
            System.Diagnostics.Debug.WriteLine("� UpdateTopProcesses 호출됨");

            // 간단히 UI 스레드에서 직접 처리 (테스트용)
            try
            {
                var processes = Process.GetProcesses();
                System.Diagnostics.Debug.WriteLine($"� 총 {processes.Length}개 프로세스 발견");

                var processList = new List<ProcessInfoModel>();

                foreach (var p in processes.Take(10)) // 처음 10개만
                {
                    try
                    {
                        if (p.HasExited) continue;

                        var processInfo = new ProcessInfoModel
                        {
                            Name = p.ProcessName,
                            Ram = p.WorkingSet64 / 1024f / 1024f,
                            Icon = IconHelper.DefaultProcessIcon,
                            AiDescription = "실시간 프로세스"
                        };

                        processList.Add(processInfo);
                    }
                    catch
                    {
                        // 접근 실패한 프로세스는 무시
                    }
                    finally
                    {
                        p?.Dispose();
                    }
                }

                // 기존 테스트 데이터 유지하면서 실제 데이터 추가
                var realProcesses = processList
                    .Where(p => p.Ram > 5.0f) // 5MB 이상만
                    .OrderByDescending(p => p.Ram)
                    .Take(5)
                    .ToList();

                // 기존 항목에 실제 프로세스 추가 (중복 제거는 나중에)
                foreach (var rp in realProcesses)
                {
                    TopProcessList.Add(rp);
                }

                System.Diagnostics.Debug.WriteLine($"✅ 업데이트 완료 - 현재 총 {TopProcessList.Count}개 항목");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ UpdateTopProcesses 오류: {ex.Message}");
            }
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

        public void StartMonitoring ()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("⏰ StartMonitoring 시작");

                if (_timer != null && !_timer.IsEnabled)
                {
                    _timer.Start();
                    System.Diagnostics.Debug.WriteLine("✅ Main timer 시작됨");
                }

                if (_processUpdateTimer != null && !_processUpdateTimer.IsEnabled)
                {
                    _processUpdateTimer.Start();
                    System.Diagnostics.Debug.WriteLine("✅ Process timer 시작됨");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ StartMonitoring error: {ex.Message}");
            }
        }
        public void StopMonitoring ()
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
