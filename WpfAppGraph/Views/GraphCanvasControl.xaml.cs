using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfAppGraph.ViewModels;

namespace WpfAppGraph.Views
{
    /// <summary>
    /// Логика взаимодействия для GraphCanvasControl.xaml
    /// </summary>
    public partial class GraphCanvasControl : UserControl
    {
        public GraphCanvasControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Отлавливание кликов по холсту
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is GraphCanvasVM vm)
            {
                var position = e.GetPosition((IInputElement)sender);
                vm.OnCanvasClick(position);
            }
        }
    }
}
