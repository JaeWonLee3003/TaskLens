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
using TaskLens.Core;
using TaskLens.ViewModels;

namespace TaskLens.Views
{
    /// <summary>
    /// ResourceView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ResourceView : UserControl
    {
        public ResourceView ()
        {
            InitializeComponent();
            this.DataContext = new ResourceViewModel();

        }

        private async void ProcessGrid_PreviewKeyDown (object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2) // 원하는 키 지정
            {
                if (DataContext is ResourceViewModel vm && vm.SelectedProcess != null)
                {
                    var result = await OllamaClient.GetProcessExplanationAsync(vm.SelectedProcess.Name);
                    vm.AiResultText = result;
                }
            }
        }
    }
}
