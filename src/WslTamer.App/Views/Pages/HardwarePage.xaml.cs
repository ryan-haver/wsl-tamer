using Wpf.Ui.Abstractions.Controls;
using WslTamer.App.ViewModels;

namespace WslTamer.App.Views.Pages;

public partial class HardwarePage : INavigableView<HardwareViewModel>
{
    public HardwarePage(HardwareViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public HardwareViewModel ViewModel { get; }
}
