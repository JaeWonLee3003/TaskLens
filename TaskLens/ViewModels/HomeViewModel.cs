using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using TaskLens.Views;

namespace TaskLens.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        private DispatcherTimer _timer;
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _ramCounter;

        private string _cpuUsage;
        public string CpuUsage
        {
            get => _cpuUsage;
            set { _cpuUsage = value; OnPropertyChanged(); }
        }

        private string _ramUsage;
        public string RamUsage
        {
            get => _ramUsage;
            set { _ramUsage = value; OnPropertyChanged(); }
        }

        private string _warningCount = "0건"; // 아직은 고정 값
        public string WarningCount
        {
            get => _warningCount;
            set { _warningCount = value; OnPropertyChanged(); }
        }

        public HomeViewModel()
        {
            // 성능 카운터 초기화
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Tick += UpdateUsage;
            _timer.Start();
        }

        private void UpdateUsage(object sender, EventArgs e)
        {
            float cpu = _cpuCounter.NextValue();
            float availableRam = _ramCounter.NextValue();
            float totalRam = GetTotalPhysicalMemoryInMB();

            CpuUsage = $"{cpu:F1} %";
            RamUsage = $"{(100 - (availableRam / totalRam * 100)):F1} %";
        }

        private float GetTotalPhysicalMemoryInMB()
        {
            // 실제 RAM 총량(MB) 가져오기
            var ci = new Microsoft.VisualBasic.Devices.ComputerInfo();
            return ci.TotalPhysicalMemory / (1024 * 1024);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
