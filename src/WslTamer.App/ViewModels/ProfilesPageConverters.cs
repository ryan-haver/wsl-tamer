using System.Globalization;
using System.Windows.Data;

namespace WslTamer.App.ViewModels;

public static class ProfilesPageConverters
{
    public static IMultiValueConverter IdEquals { get; } = new IdEqualsConverter();

    public static IValueConverter NotNull { get; } = new NotNullConverter();

    private sealed class IdEqualsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
            values.Length == 2 && values[0] is Guid a && values[1] is Guid b && a == b;

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    private sealed class NotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is not null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
