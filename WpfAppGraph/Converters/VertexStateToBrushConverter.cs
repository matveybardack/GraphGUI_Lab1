using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WpfAppGraph.Models.Enums;

namespace WpfAppGraph.Converters
{
    public class VertexStateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as VertexState?) switch
            {
                VertexState.Selected => Brushes.Orange,
                VertexState.Target => Brushes.Red,
                VertexState.Visited => Brushes.LightBlue,
                VertexState.Active => Brushes.Gray,
                VertexState.Finished => Brushes.Black,
                _ => Brushes.WhiteSmoke // Default
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}