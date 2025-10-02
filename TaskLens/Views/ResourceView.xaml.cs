using System;
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
            this.Loaded += ResourceView_Loaded;
        }
        
        private void ResourceView_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"🖥️ ResourceView Loaded - DataContext: {DataContext?.GetType().Name}");
            
            if (DataContext is ViewModels.ResourceViewModel vm)
            {
                System.Diagnostics.Debug.WriteLine($"📊 ResourceViewModel 연결됨 - TopProcessList.Count: {vm.TopProcessList?.Count}");
                
                // 🧪 수동으로 테스트 데이터 추가 (디버깅용)
                this.Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    try
                    {
                        vm.TopProcessList.Add(new Models.ProcessInfoModel 
                        { 
                            Name = "수동추가 테스트", 
                            Ram = 123.45f, 
                            Icon = Helpers.IconHelper.DefaultProcessIcon,
                            AiDescription = "ResourceView에서 추가"
                        });
                        System.Diagnostics.Debug.WriteLine("✅ ResourceView에서 테스트 프로세스 추가됨");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ 테스트 프로세스 추가 실패: {ex.Message}");
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("❌ ResourceViewModel이 DataContext에 없습니다!");
            }
        }

        private void KillProcess_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessGrid.SelectedItem is Models.ProcessInfoModel selected)
            {
                // 사용자 확인 대화상자 추가
                var result = MessageBox.Show(
                    $"프로세스 '{selected.Name}'을(를) 종료하시겠습니까?\n\n⚠️ 저장되지 않은 데이터가 손실될 수 있습니다.",
                    "프로세스 종료 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                try
                {
                    var processes = Process.GetProcessesByName(selected.Name);
                    if (processes.Length == 0)
                    {
                        MessageBox.Show("프로세스를 찾을 수 없습니다.", "정보",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    int killedCount = 0;
                    foreach (var proc in processes)
                    {
                        try
                        {
                            if (!proc.HasExited)
                            {
                                proc.Kill();
                                proc.WaitForExit(3000); // 3초 대기
                                killedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to kill process {proc.Id}: {ex.Message}");
                        }
                        finally
                        {
                            proc?.Dispose();
                        }
                    }

                    if (killedCount > 0)
                    {
                        MessageBox.Show($"프로세스 '{selected.Name}' {killedCount}개를 종료했습니다.", "성공",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("프로세스를 종료할 수 없습니다. 관리자 권한이 필요할 수 있습니다.", "오류",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"프로세스를 종료하는 데 실패했습니다.\n오류: {ex.Message}", "오류",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Diagnostics.Debug.WriteLine($"KillProcess error: {ex.Message}");
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





        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessGrid.SelectedItem is Models.ProcessInfoModel selected)
            {
                try
                {
                    // IconHelper의 기존 메서드를 사용하여 중복 코드 제거
                    string path = IconHelper.GetProcessPathSafe(selected.Name);
                    if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                    {
                        // Windows 탐색기에서 파일 선택하여 열기
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"/select,\"{path}\"",
                            UseShellExecute = true
                        };
                        Process.Start(startInfo);
                    }
                    else
                    {
                        MessageBox.Show(
                            $"프로세스 '{selected.Name}'의 파일 경로를 가져올 수 없습니다.\n\n" +
                            "시스템 프로세스이거나 접근 권한이 없을 수 있습니다.",
                            "파일 위치 열기 실패",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"파일 위치를 열 수 없습니다.\n오류: {ex.Message}",
                        "오류",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    System.Diagnostics.Debug.WriteLine($"OpenFileLocation error: {ex.Message}");
                }
            }
        }



        private void ProcessGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F2)
            {
                if (DataContext is ViewModels.ResourceViewModel vm &&
                    ProcessGrid.SelectedItem is Models.ProcessInfoModel selected)
                {
                    // 선택된 프로세스가 있을 때만 AI 분석 실행
                    vm.SelectedProcess = selected; // SelectedProcess 설정
                    vm.AnalyzeProcessCommand.Execute(selected);
                    e.Handled = true;
                }
            }
            else if (e.Key == System.Windows.Input.Key.Delete)
            {
                // Delete 키로 프로세스 종료 (추가 기능)
                if (ProcessGrid.SelectedItem != null)
                {
                    KillProcess_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                }
            }
            else if (e.Key == System.Windows.Input.Key.Enter)
            {
                // Enter 키로 파일 위치 열기 (추가 기능)
                if (ProcessGrid.SelectedItem != null)
                {
                    OpenFileLocation_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                }
            }
        }
    }
}
