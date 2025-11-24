using System;
using System.Globalization;
using System.Windows.Data;

namespace WslTamer.UI.Converters;

public class BooleanToAttachConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isAttached)
        {
            return isAttached ? "Detach" : "Attach";
        }
        return "Attach";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
