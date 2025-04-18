using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TaskLens.Core;
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
                    System.Windows.MessageBox.Show(
            "먼저 프로세스를 선택해주세요.",
            "프로세스 미선택",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Warning
        );
                    return;
                }

                var result = await OllamaClient.GetProcessExplanationAsync(SelectedProcess.Name);
                SelectedProcess.AiDescription = result;

            });

        }

        private void UpdateTopProcesses ()
        {
            var processList = Process.GetProcesses()
                .Select(p =>
                {
                    float ramMb = 0;
                    try { ramMb = p.WorkingSet64 / 1024f / 1024f; } catch { }

                    return new ProcessInfoModel
                    {
                        Name = p.ProcessName,
                        Cpu = 0, // 추후 CPU 측정 붙일 수 있음
                        Ram = ramMb,
                        AiDescription = "분석 대기 중"
                    };
                })
                .OrderByDescending(p => p.Ram)
                .Take(10)
                .ToList();

            TopProcessList.Clear();
            foreach (var p in processList)
                TopProcessList.Add(p);
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
