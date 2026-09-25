using Wpf.Ui.Abstractions.Controls;
using WslTamer.App.ViewModels;

namespace WslTamer.App.Views.Pages;

public partial class WslConfigPage : INavigableView<WslConfigViewModel>
{
    public WslConfigPage(WslConfigViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public WslConfigViewModel ViewModel { get; }
}
