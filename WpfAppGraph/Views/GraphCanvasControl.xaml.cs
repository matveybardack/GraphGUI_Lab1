using System.Windows.Controls;
using System.Windows.Input;

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

        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        //private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    // Пробрасываем клик в команду DrawGraphViewModel
        //    if (DataContext is ViewModels.DrawGraphVM vm)
        //    {
        //        var pos = e.GetPosition(MainCanvas);
        //        if (vm.CanvasClickCommand.CanExecute(pos))
        //            vm.CanvasClickCommand.Execute(pos);
        //    }
        //}
    }
}
