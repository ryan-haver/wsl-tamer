using System.ComponentModel;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using WslTamer.App.Views.Pages;

namespace WslTamer.App.Views;

/// <summary>
/// The settings window. Closing it hides it; WSL Tamer keeps running in the tray
/// until "Exit" is chosen there.
/// </summary>
public partial class MainWindow
{
    private bool _navigated;

    /// <summary>Set when the app is exiting so closing really closes.</summary>
    public bool AllowClose { get; set; }

    public MainWindow(IServiceProvider services, ISnackbarService snackbar, IContentDialogService dialogs)
    {
        InitializeComponent();
        SystemThemeWatcher.Watch(this);
        Navigation.SetServiceProvider(services);
        snackbar.SetSnackbarPresenter(Snackbar);
        dialogs.SetDialogHost(DialogHost);
    }

    public void ShowAndActivate()
    {
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        if (!_navigated)
        {
            _navigated = Navigation.Navigate(typeof(DashboardPage));
        }

        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            Hide();
        }

        base.OnClosing(e);
    }
}
