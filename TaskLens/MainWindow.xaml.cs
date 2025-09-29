using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
            

            
        }

        private void MainWindow_Loaded (object sender, RoutedEventArgs e)
        {
            VersionTextBlock.Text = $"Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";

            // 트레이 아이콘 생성
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = new Icon("Resources/app.ico");  // ← 파일 없으면 에러
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "TaskLens 작업 관리자";

            // 우클릭 메뉴
            _notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            _notifyIcon.ContextMenuStrip.Items.Add("Open", null, (s, ee) => ShowWindow());
            _notifyIcon.ContextMenuStrip.Items.Add("Close", null, (s, ee) => CloseApp());

            // 더블클릭 시 열기
            _notifyIcon.DoubleClick += (s, ee) => ShowWindow();
            Task.Run(() =>
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
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "https://ollama.com/download",
                                UseShellExecute = true
                            });
                        }
                        else
                        {
                            MessageBox.Show("Ollama가 설치되어야 일부 기능을 사용할 수 있습니다.", "안내", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    });
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
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnStateChanged (EventArgs e)
        {
            base.OnStateChanged(e);
            if (WindowState == WindowState.Minimized)
            {
                this.Hide(); // 최소화 시 창 숨김
            }
        }

        public void ShowBalloon (string title, string message, ToolTipIcon icon)
        {
            _notifyIcon?.ShowBalloonTip(3000, title, message, icon);
        }
    }
}
