using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TaskLens.Helpers;

namespace TaskLens.Views
{
    public partial class ResourceView : UserControl
    {
        public ResourceView ()
        {
            InitializeComponent();
        }

        private void KillProcess_Click (object sender, RoutedEventArgs e)
        {
            if (ProcessGrid.SelectedItem is Models.ProcessInfoModel selected)
            {
                try
                {
                    var processes = Process.GetProcessesByName(selected.Name);
                    foreach (var proc in processes)
                    {
                        proc.Kill();
                    }
                    MessageBox.Show($"프로세스 '{selected.Name}' 작업을 종료했습니다.", "성공",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch
                {
                    MessageBox.Show("프로세스를 종료하는 데 실패했습니다.", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ProcessGrid_PreviewMouseRightButtonDown (object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var row = UIHelpers.FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (row != null)
            {
                row.IsSelected = true;
            }
        }

        private string GetProcessPathSafe (string processName)
        {
            try
            {
                var searcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT ExecutablePath FROM Win32_Process WHERE Name = '{processName}.exe'");

                foreach (var obj in searcher.Get())
                {
                    var pathObj = obj["ExecutablePath"];
                    if (pathObj != null)
                        return pathObj.ToString();
                }
            }
            catch { }

            return string.Empty;
        }



        private void OpenFileLocation_Click (object sender, RoutedEventArgs e)
        {
            if (ProcessGrid.SelectedItem is Models.ProcessInfoModel selected)
            {
                string path = GetProcessPathSafe(selected.Name);
                if (!string.IsNullOrEmpty(path))
                {
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                }
                else
                {
                    MessageBox.Show("파일 경로를 가져올 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        private void ProcessGrid_PreviewKeyDown (object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F2)
            {
                if (DataContext is ViewModels.ResourceViewModel vm &&
                    ProcessGrid.SelectedItem is Models.ProcessInfoModel selected)
                {
                    vm.AnalyzeProcessCommand.Execute(selected);
                    e.Handled = true;
                }
            }
        }
    }
}
