using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WpfAppGraph.Models.Enums;

namespace WpfAppGraph.Converters
{
    public class EdgeTypeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as EdgeType?) switch
            {
                EdgeType.TreeEdge => Brushes.Blue,
                EdgeType.BackEdge => Brushes.Red,
                EdgeType.ForwardEdge => Brushes.Green,
                EdgeType.CrossEdge => Brushes.Orange,
                _ => Brushes.Black
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
