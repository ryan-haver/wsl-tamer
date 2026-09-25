using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wpf.Ui;
using Wpf.Ui.Controls;
using WslTamer.Core.Processes;
using WslTamer.Core.Storage;
using WslTamer.Core.Wsl;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace WslTamer.App.Services;

public enum Severity
{
    Info,
    Success,
    Warning,
    Error,
}

public interface ITrayNotifier
{
    void ShowBalloon(string title, string message, Severity severity);
}

/// <summary>Dialogs, notifications and error handling for view models and the tray.</summary>
public sealed class UserInteraction(
    IServiceProvider services,
    ISnackbarService snackbar,
    IContentDialogService dialogs,
    AppState state,
    ILogger<UserInteraction> logger)
{
    public async Task<bool> ConfirmAsync(string title, string message, string confirmText, bool destructive = false)
    {
        var box = new MessageBox
        {
            Title = title,
            Content = new System.Windows.Controls.TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, MaxWidth = 460 },
            PrimaryButtonText = confirmText,
            PrimaryButtonAppearance = destructive ? ControlAppearance.Danger : ControlAppearance.Primary,
            CloseButtonText = "Cancel",
        };

        return await box.ShowDialogAsync() == MessageBoxResult.Primary;
    }

    public async Task AlertAsync(string title, string message)
    {
        var box = new MessageBox
        {
            Title = title,
            Content = new System.Windows.Controls.TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, MaxWidth = 460 },
            CloseButtonText = "OK",
        };
        await box.ShowDialogAsync();
    }

    /// <summary>Shows a form dialog inside the main window.</summary>
    public Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog) => dialogs.ShowAsync(dialog, CancellationToken.None);

    public void Notify(string title, string message, Severity severity = Severity.Info)
    {
        var main = Application.Current?.MainWindow;
        if (main is { IsVisible: true } && main.WindowState != WindowState.Minimized)
        {
            snackbar.Show(title, message, severity switch
            {
                Severity.Success => ControlAppearance.Success,
                Severity.Warning => ControlAppearance.Caution,
                Severity.Error => ControlAppearance.Danger,
                _ => ControlAppearance.Secondary,
            }, null, TimeSpan.FromSeconds(severity == Severity.Error ? 8 : 4));
        }
        else if (state.Config.Preferences.ShowNotifications || severity == Severity.Error)
        {
            services.GetService<ITrayNotifier>()?.ShowBalloon(title, message, severity);
        }
    }

    /// <summary>
    /// Runs an operation and turns any failure into a readable message instead of a crash.
    /// Returns true on success.
    /// </summary>
    public async Task<bool> RunAsync(string failureTitle, Func<Task> operation)
    {
        try
        {
            await operation();
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex) when (ex is WslException or ProcessTimeoutException or IOException or UnauthorizedAccessException
            or ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or System.Management.ManagementException
            or System.Text.Json.JsonException)
        {
            logger.LogError(ex, "{Title}", failureTitle);
            Notify(failureTitle, ex.Message, Severity.Error);
            return false;
        }
    }
}
