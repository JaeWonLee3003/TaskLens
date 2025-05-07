using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TaskLens.Theme;

namespace TaskLens
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup (StartupEventArgs e)
        {
            base.OnStartup(e);

            // 🌙 초기 테마 설정 (다크 or 라이트)
            // 실제 저장값이 있다면 Settings에서 불러오게 연결 가능
            ThemeManager.ApplyTheme("DarkTheme"); // 또는 "LightTheme"
        }
    }
}
