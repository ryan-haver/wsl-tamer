using System.Windows;
using System.Windows.Controls.Primitives;
using Wpf.Ui.Abstractions.Controls;
using WslTamer.App.ViewModels;

namespace WslTamer.App.Views.Pages;

public partial class DistributionsPage : INavigableView<DistributionsViewModel>
{
    public DistributionsPage(DistributionsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public DistributionsViewModel ViewModel { get; }

    private void More_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
        }
    }
}
