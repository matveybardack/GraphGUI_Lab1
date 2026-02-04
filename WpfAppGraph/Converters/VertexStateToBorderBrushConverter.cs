using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WpfAppGraph.Models.Enums;

namespace WpfAppGraph.Converters
{
    public class VertexStateToBorderBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as VertexState?) switch
            {
                VertexState.Selected => Brushes.DarkOrange,
                VertexState.Target => Brushes.DarkRed,
                VertexState.Visited => Brushes.SteelBlue,
                VertexState.Active => Brushes.DimGray,
                VertexState.Finished => Brushes.Black,
                _ => Brushes.Black // Default
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
