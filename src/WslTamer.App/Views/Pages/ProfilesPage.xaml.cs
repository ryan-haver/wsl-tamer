using Wpf.Ui.Abstractions.Controls;
using WslTamer.App.ViewModels;

namespace WslTamer.App.Views.Pages;

public partial class ProfilesPage : INavigableView<ProfilesViewModel>
{
    public ProfilesPage(ProfilesViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public ProfilesViewModel ViewModel { get; }
}
