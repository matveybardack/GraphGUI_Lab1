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

        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Проверяем, что DataContext это нужная нам ViewModel
            if (DataContext is GraphCanvasVM vm)
            {
                // Получаем позицию мыши относительно Canvas (или Grid, который мы сейчас добавим)
                var position = e.GetPosition((IInputElement)sender);

                // Вызываем метод ViewModel
                vm.OnCanvasClick(position);
            }
        }
    }
}
