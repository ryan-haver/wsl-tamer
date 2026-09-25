using System.ComponentModel;
using System.Windows.Media;
using Wpf.Ui.Abstractions.Controls;
using WslTamer.App.ViewModels;

namespace WslTamer.App.Views.Pages;

public partial class DashboardPage : INavigableView<DashboardViewModel>
{
    private static readonly Brush RunningBrush = Frozen(Color.FromRgb(0x10, 0x9E, 0x4A));
    private static readonly Brush StoppedBrush = Frozen(Color.FromRgb(0x8A, 0x8A, 0x8A));

    public DashboardPage(DashboardViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        viewModel.PropertyChanged += OnViewModelChanged;
        UpdateDot();
    }

    public DashboardViewModel ViewModel { get; }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardViewModel.IsRunning))
        {
            UpdateDot();
        }
    }

    private void UpdateDot() => StatusDot.Fill = ViewModel.IsRunning ? RunningBrush : StoppedBrush;

    private static SolidColorBrush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
