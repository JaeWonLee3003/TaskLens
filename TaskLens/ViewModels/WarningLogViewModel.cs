using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskLens.ViewModels
{
    public class WarningLogViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<string> _warnings = new ObservableCollection<string>();
        public ObservableCollection<string> Warnings
        {
            get => _warnings;
            set { _warnings = value; OnPropertyChanged(); }
        }

        public void AddWarning (string message)
        {
            Warnings.Insert(0, message); // 가장 최근 항목이 위로
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged ([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
