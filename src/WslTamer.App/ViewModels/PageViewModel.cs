using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Abstractions.Controls;

namespace WslTamer.App.ViewModels;

public abstract partial class PageViewModel : ObservableObject, INavigationAware
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyText;

    public virtual Task OnNavigatedToAsync() => Task.CompletedTask;

    public virtual Task OnNavigatedFromAsync() => Task.CompletedTask;

    /// <summary>Shows a busy indicator with <paramref name="text"/> while <paramref name="work"/> runs.</summary>
    protected async Task BusyAsync(string text, Func<Task> work)
    {
        IsBusy = true;
        BusyText = text;
        try
        {
            await work();
        }
        finally
        {
            IsBusy = false;
            BusyText = null;
        }
    }
}
