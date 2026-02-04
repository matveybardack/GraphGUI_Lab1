using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfAppGraph.Converters
{
    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? Enum.Parse(targetType, parameter.ToString()) : Binding.DoNothing;
        }
    }
}