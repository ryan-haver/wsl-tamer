using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Wpf.Ui.Abstractions.Controls;
using WslTamer.App.ViewModels;

namespace WslTamer.App.Views.Pages;

public partial class AutomationPage : INavigableView<AutomationViewModel>
{
    public AutomationPage(AutomationViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public AutomationViewModel ViewModel { get; }
}

public static class AutomationPageConverters
{
    public static IValueConverter ZeroToVisible { get; } = new ZeroToVisibleConverter();

    private sealed class ZeroToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is 0 ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
