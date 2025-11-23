using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WslTamer.UI.Converters;

public class StateToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isRunning && isRunning)
        {
            return System.Windows.Media.Brushes.Green;
        }
        return System.Windows.Media.Brushes.Red;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
