using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfAppGraph.Converters
{
    public class VertexStateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // В реальности будет enum VertexState
            if (value is string state)
            {
                return state switch
                {
                    "Selected" => Brushes.Gold,
                    "Target" => Brushes.LightGreen,
                    "Visited" => Brushes.LightCoral,
                    "Current" => Brushes.Orange,
                    _ => Brushes.LightBlue
                };
            }
            return Brushes.LightBlue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}