using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace TaskLens.Views
{
    /// <summary>
    /// SettingsView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public SettingsView ()
        {
            InitializeComponent();
            this.Loaded += SettingsView_Loaded;
        }

        private bool Isfirst = false;

        private void SettingsView_Loaded (object sender, RoutedEventArgs e)
        {
            var cultures = new CultureInfo[] { new CultureInfo("en"), new CultureInfo("ko") };
            LanguageComboBox.ItemsSource = cultures;

            // 현재 UI 언어 가져오기
            var currentCulture = System.Globalization.CultureInfo.CurrentUICulture;

            // 현재 언어가 목록에 있으면 선택, 없으면 첫 번째 항목 선택
            var selected = cultures.FirstOrDefault(c => c.TwoLetterISOLanguageName == currentCulture.TwoLetterISOLanguageName);
            LanguageComboBox.SelectedItem = selected ?? cultures[0];
        }
    }
}
