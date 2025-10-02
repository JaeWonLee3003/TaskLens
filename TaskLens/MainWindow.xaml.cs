using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TaskLens.ViewModels;
using System.Drawing;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using System.Diagnostics;
using TaskLens.Core;

namespace TaskLens
{
    public partial class MainWindow : Window
    {
        private NotifyIcon _notifyIcon;

        public MainWindow ()
        {
            InitializeComponent();
            DataContext = new MainViewModel();

            this.Loaded += MainWindow_Loaded;
            this.SourceInitialized += MainWindow_SourceInitialized;
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Ensure maximize button icon is set correctly on startup
            UpdateMaximizeButtonIcon();
        }

        private void MainWindow_Loaded (object sender, RoutedEventArgs e)
        {
            VersionTextBlock.Text = $"Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";

            // 트레이 아이콘 생성
            _notifyIcon = new NotifyIcon();
            try
            {
                _notifyIcon.Icon = new Icon("app.ico");  // Resources 폴더 대신 프로젝트 루트 사용
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"트레이 아이콘 로드 실패: {ex.Message}");
                // 기본 아이콘 사용 또는 생성
                _notifyIcon.Icon = SystemIcons.Application;
            }
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "TaskLens 작업 관리자";

            // 우클릭 메뉴
            _notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            _notifyIcon.ContextMenuStrip.Items.Add("Open", null, (s, ee) => ShowWindow());
            _notifyIcon.ContextMenuStrip.Items.Add("Close", null, (s, ee) => CloseApp());

            // 더블클릭 시 열기
            _notifyIcon.DoubleClick += (s, ee) => ShowWindow();

            // Ollama 설치 체크 (비동기로 실행)
            Task.Run(() =>
            {
                try
                {
                    if (!OllamaHelper.IsOllamaInstalled())
                    {
                        Dispatcher.Invoke(() =>
                        {
                            var result = MessageBox.Show("Ollama가 설치되어 있지 않습니다.\n\n설치하시겠습니까?",
                                "Ollama 설치 필요",
                                MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                try
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = "https://ollama.com/download",
                                        UseShellExecute = true
                                    });
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"브라우저를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Ollama가 설치되어야 일부 기능을 사용할 수 있습니다.", "안내", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ollama 체크 중 오류: {ex.Message}");
                }
            });
        }

        private void ShowWindow ()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void CloseApp ()
        {
            _notifyIcon.Visible = false;
            _notifyIcon?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _notifyIcon?.Dispose();
        }

        protected override void OnStateChanged (EventArgs e)
        {
            base.OnStateChanged(e);

            // Update maximize button icon based on window state
            UpdateMaximizeButtonIcon();

            if (WindowState == WindowState.Minimized)
            {
                this.Hide(); // 최소화 시 창 숨김
            }
        }

        private void UpdateMaximizeButtonIcon()
        {
            if (MaximizeIconPath == null) return;

            if (this.WindowState == WindowState.Maximized)
            {
                // Restore icon
                MaximizeIconPath.Data = Geometry.Parse("M8 3v3a2 2 0 0 1-2 2H3a2 2 0 0 0-2 2v2a2 2 0 0 0 2 2h3a2 2 0 0 0 2-2v-3a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2v-3a2 2 0 0 0-2-2H3a2 2 0 0 0-2-2V8a2 2 0 0 0 2-2h3a2 2 0 0 1 2-2z");
            }
            else
            {
                // Maximize icon
                MaximizeIconPath.Data = Geometry.Parse("M4 4v12h12V4H4zm0-2h12c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2z");
            }
        }

        public void ShowBalloon (string title, string message, ToolTipIcon icon)
        {
            _notifyIcon?.ShowBalloonTip(3000, title, message, icon);
        }

        // 🎯 Custom Title Bar Event Handlers
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🎯 MinimizeButton clicked");
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🎯 MaximizeButton clicked");
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                // Restore normal size when returning from maximized
                this.Width = 1200;
                this.Height = 800;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("🎯 CloseButton clicked");
            // 트레이 아이콘 정리 후 애플리케이션 완전 종료
            _notifyIcon?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        // 🖱️ Window Drag Functionality (Removed - now handled by TitleBar)

        // 🖱️ Window Drag Functionality (Optimized with DragMove)
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 버튼 클릭 시 드래그 방지
            if (e.OriginalSource is System.Windows.Controls.Button ||
                e.OriginalSource is System.Windows.Shapes.Path ||
                e.OriginalSource is System.Windows.Controls.Image)
            {
                return;
            }

            if (e.ClickCount == 2)
            {
                // 더블클릭으로 최대화/복원
                MaximizeButton_Click(sender, e);
            }
            else if (e.ClickCount == 1)
            {
                // DragMove()를 사용한 효율적인 드래그
                try
                {
                    this.DragMove();
                }
                catch (System.InvalidOperationException)
                {
                    // DragMove가 실패할 경우 무시 (일반적으로 발생하지 않음)
                }
            }
        }


    }
}
